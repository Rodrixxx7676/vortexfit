using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using VortexFit.Data;
using VortexFit.Middleware;
using VortexFit.Models;
using VortexFit.Services;
using WebPush;

var builder = WebApplication.CreateBuilder(args);

// ── Kestrel: ocultar cabecera "Server" ────────────────────
builder.WebHost.ConfigureKestrel(k => k.AddServerHeader = false);

// ── Oracle DbContext ───────────────────────────────────────
builder.Services.AddDbContext<VortexFitDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleDb")));

// ── Sesiones ───────────────────────────────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout        = TimeSpan.FromMinutes(15);
    options.Cookie.HttpOnly    = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name        = ".StyleGym.Session";
    options.Cookie.SameSite    = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
    // Secure: Always en producción; SameAsRequest acepta HTTP en dev y HTTPS en prod
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// ── MVC ───────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ── Antiforgery ───────────────────────────────────────────
builder.Services.AddAntiforgery(options =>
{
    options.SuppressXFrameOptionsHeader = true; // lo manejamos en SecurityHeadersMiddleware
    options.Cookie.SameSite    = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.HttpOnly    = true;
});

// ── Notificaciones push (servicio de fondo) ───────────────
builder.Services.AddHostedService<PushNotificationService>();

// ── reCAPTCHA v3 ──────────────────────────────────────────
builder.Services.AddHttpClient("recaptcha");
builder.Services.AddScoped<RecaptchaService>();

// ── Protección brute force ─────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<LoginAttemptTracker>();

// ── Rate Limiting ─────────────────────────────────────────
// Limita solicitudes por IP para evitar abuso de endpoints sensibles.
builder.Services.AddRateLimiter(opt =>
{
    // Registro: máx. 5 cuentas por hora por IP
    opt.AddPolicy("register", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = 5,
                Window               = TimeSpan.FromHours(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0
            }));

    // Soporte: máx. 3 mensajes cada 15 minutos por IP
    opt.AddPolicy("soporte", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = 3,
                Window               = TimeSpan.FromMinutes(15),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0
            }));

    // Respuesta 429 cuando se supera el límite
    opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opt.OnRejected = async (ctx, token) =>
    {
        ctx.HttpContext.Response.ContentType = "application/json";
        await ctx.HttpContext.Response.WriteAsync(
            "{\"error\":\"Demasiadas solicitudes. Intenta de nuevo más tarde.\"}",
            cancellationToken: token);
    };
});

var app = builder.Build();

// ══════════════════════════════════════════════════════════
// INICIO: migraciones, seeds, VAPID keys
// ══════════════════════════════════════════════════════════
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<VortexFitDbContext>();
    await db.Database.MigrateAsync();

    // ── Seed admin ─────────────────────────────────────
    if (!await db.Socios.AnyAsync(s => s.Rol == "Admin"))
    {
        db.Socios.Add(new Socio
        {
            NombreCompleto   = "Administrador",
            Email            = "admin@stylegym.com",
            PasswordHash     = BCrypt.Net.BCrypt.HashPassword("Admin2025!"),
            Plan             = "Elite",
            Rol              = "Admin",
            Estado           = "Activo",
            CodigoAcceso     = GenerarCodigo(),
            FechaRegistro    = DateTime.UtcNow,
            FechaVencimiento = DateTime.UtcNow.AddYears(99)
        });
        await db.SaveChangesAsync();
    }

    // ── Asignar CodigoAcceso a socios que no tienen ─────
    var sinCodigo = await db.Socios
        .Where(s => s.CodigoAcceso == null || s.CodigoAcceso == string.Empty)
        .ToListAsync();
    foreach (var s in sinCodigo)
        s.CodigoAcceso = GenerarCodigo();
    if (sinCodigo.Any())
        await db.SaveChangesAsync();

    // ── VAPID keys: generar si no están configuradas ───
    var vapid = app.Configuration.GetSection("Vapid");
    if (string.IsNullOrEmpty(vapid["PublicKey"]))
    {
        var keys    = VapidHelper.GenerateVapidKeys();
        var keysFile = Path.Combine(Directory.GetCurrentDirectory(), "vapid-keys.json");
        await File.WriteAllTextAsync(keysFile,
            $"{{\"PublicKey\":\"{keys.PublicKey}\",\"PrivateKey\":\"{keys.PrivateKey}\"}}");
        app.Logger.LogWarning("VAPID keys generados y guardados en vapid-keys.json");
        app.Logger.LogWarning("VAPID Public:  {Key}", keys.PublicKey);
    }
}

// ── Manejo de errores ──────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // Strict-Transport-Security (30 días por defecto)
}

// ── Pipeline de seguridad ──────────────────────────────────
app.UseMiddleware<SecurityHeadersMiddleware>(); // cabeceras de seguridad
app.UseStatusCodePagesWithReExecute("/Home/NotFound");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();   // rate limiting antes de sesión y actions
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static string GenerarCodigo() =>
    Guid.NewGuid().ToString("N")[..12].ToUpper();

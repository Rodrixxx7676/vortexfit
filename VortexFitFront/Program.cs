using Microsoft.EntityFrameworkCore;
using VortexFit.Data;
using VortexFit.Models;
using VortexFit.Services;
using WebPush;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// ── Oracle DbContext ───────────────────────────────────────
builder.Services.AddDbContext<VortexFitDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleDb")));

// ── Sesiones ───────────────────────────────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout        = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly    = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name        = ".StyleGym.Session";
    options.Cookie.SameSite    = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
});

// ── MVC ───────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ── Antiforgery ───────────────────────────────────────────
builder.Services.AddAntiforgery(options =>
{
    options.SuppressXFrameOptionsHeader = true;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
});

// ── Notificaciones push (servicio de fondo) ───────────────
builder.Services.AddHostedService<PushNotificationService>();

// ── reCAPTCHA v3 ──────────────────────────────────────────
builder.Services.AddHttpClient("recaptcha");
builder.Services.AddScoped<RecaptchaService>();

// ── Protección brute force ─────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<LoginAttemptTracker>();

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
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/NotFound");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static string GenerarCodigo() =>
    Guid.NewGuid().ToString("N")[..12].ToUpper();

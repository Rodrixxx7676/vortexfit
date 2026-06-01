using Microsoft.EntityFrameworkCore;
using VortexFit.Data;
using VortexFit.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Oracle DbContext ──────────────────────────────
builder.Services.AddDbContext<VortexFitDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleDb")));

// ── Sesiones ──────────────────────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout           = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly       = true;
    options.Cookie.IsEssential    = true;
    options.Cookie.Name           = ".StyleGym.Session";
    options.Cookie.SameSite       = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
});

// ── MVC ──────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ── Antiforgery: quitar X-Frame-Options + cookie Lax para VS Code preview ──
builder.Services.AddAntiforgery(options =>
{
    options.SuppressXFrameOptionsHeader = true;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
});

var app = builder.Build();

// ── Migraciones pendientes + Seed admin ───────────────
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<VortexFitDbContext>();
    await db.Database.MigrateAsync();

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
            FechaRegistro    = DateTime.UtcNow,
            FechaVencimiento = DateTime.UtcNow.AddYears(99)
        });
        await db.SaveChangesAsync();
    }
}

// ── Manejo de errores ─────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ── 404 personalizado ─────────────────────────────
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

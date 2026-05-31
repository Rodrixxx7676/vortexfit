using Microsoft.EntityFrameworkCore;
using VortexFit.Data;

var builder = WebApplication.CreateBuilder(args);

// ── Oracle DbContext ──────────────────────────────
builder.Services.AddDbContext<VortexFitDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleDb")));

// ── Sesiones ──────────────────────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout        = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly    = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name        = ".StyleGym.Session";
});

// ── MVC ──────────────────────────────────────────
builder.Services.AddControllersWithViews();

var app = builder.Build();

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

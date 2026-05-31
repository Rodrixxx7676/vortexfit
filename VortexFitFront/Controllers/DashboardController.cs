using Microsoft.AspNetCore.Mvc;
using VortexFit.Data;
using VortexFit.Models;

namespace VortexFit.Controllers;

public class DashboardController : Controller
{
    private readonly VortexFitDbContext _db;

    public DashboardController(VortexFitDbContext db)
    {
        _db = db;
    }

    // ── Filtro de sesión ─────────────────────────────
    private IActionResult? RequireLogin()
    {
        if (HttpContext.Session.GetInt32("SocioId") is null)
            return RedirectToAction("Login", "Account",
                new { returnUrl = Request.Path });
        return null;
    }

    private int SocioId => HttpContext.Session.GetInt32("SocioId")!.Value;

    // ════════════════════════════════════════════════
    // INDEX — Panel principal
    // ════════════════════════════════════════════════
    public async Task<IActionResult> Index()
    {
        var redirect = RequireLogin();
        if (redirect != null) return redirect;

        var socio = await _db.Socios.FindAsync(SocioId);
        if (socio == null) return RedirectToAction("Logout", "Account");

        int diasRestantes = socio.FechaVencimiento.HasValue
            ? Math.Max(0, (socio.FechaVencimiento.Value - DateTime.UtcNow).Days)
            : 0;

        var vm = new DashboardViewModel
        {
            Socio          = socio,
            DiasRestantes  = diasRestantes,
            PorcentajePlan = diasRestantes > 0 ? Math.Min(100, (diasRestantes * 100) / 30) : 0
        };

        return View(vm);
    }

    // ════════════════════════════════════════════════
    // PERFIL — GET
    // ════════════════════════════════════════════════
    [HttpGet]
    public async Task<IActionResult> Perfil()
    {
        var redirect = RequireLogin();
        if (redirect != null) return redirect;

        var socio = await _db.Socios.FindAsync(SocioId);
        if (socio == null) return RedirectToAction("Logout", "Account");

        var vm = new PerfilViewModel
        {
            NombreCompleto = socio.NombreCompleto,
            Telefono       = socio.Telefono,
            Email          = socio.Email,
            Plan           = socio.Plan,
            Estado         = socio.Estado,
            FechaRegistro  = socio.FechaRegistro
        };

        return View(vm);
    }

    // ════════════════════════════════════════════════
    // PERFIL — POST
    // ════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Perfil(PerfilViewModel vm)
    {
        var redirect = RequireLogin();
        if (redirect != null) return redirect;

        var socio = await _db.Socios.FindAsync(SocioId);
        if (socio == null) return RedirectToAction("Logout", "Account");

        // Rellenar campos de solo lectura para la vista en caso de error
        vm.Email        = socio.Email;
        vm.Plan         = socio.Plan;
        vm.Estado       = socio.Estado;
        vm.FechaRegistro = socio.FechaRegistro;

        // Validar contraseña actual si quiere cambiarla
        bool quiereCambiarPassword = !string.IsNullOrWhiteSpace(vm.NuevaPassword);
        if (quiereCambiarPassword)
        {
            if (string.IsNullOrWhiteSpace(vm.PasswordActual))
            {
                ModelState.AddModelError("PasswordActual",
                    "Debes ingresar tu contraseña actual para cambiarla.");
            }
            else if (!BCrypt.Net.BCrypt.Verify(vm.PasswordActual, socio.PasswordHash))
            {
                ModelState.AddModelError("PasswordActual",
                    "La contraseña actual no es correcta.");
            }
        }

        if (!ModelState.IsValid)
            return View(vm);

        // Aplicar cambios
        socio.NombreCompleto = vm.NombreCompleto.Trim();
        socio.Telefono       = vm.Telefono?.Trim();

        if (quiereCambiarPassword)
            socio.PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.NuevaPassword!);

        await _db.SaveChangesAsync();

        // Actualizar nombre en sesión
        HttpContext.Session.SetString("SocioNombre", socio.NombreCompleto);

        TempData["SuccessMessage"] = "✅ Perfil actualizado correctamente.";
        return RedirectToAction(nameof(Perfil));
    }
}

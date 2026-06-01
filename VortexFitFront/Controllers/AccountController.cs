using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VortexFit.Data;
using VortexFit.Models;

namespace VortexFit.Controllers;

public class AccountController : Controller
{
    private readonly VortexFitDbContext _db;

    public AccountController(VortexFitDbContext db)
    {
        _db = db;
    }

    // ──────────────────────────────────────────
    // LOGIN — GET
    // ──────────────────────────────────────────

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // ──────────────────────────────────────────
    // LOGIN — POST
    // ──────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return View(model);

        // Buscar socio por email
        var socio = await _db.Socios
            .FirstOrDefaultAsync(s => s.Email.ToLower() == model.Email.ToLower());

        if (socio == null || !BCrypt.Net.BCrypt.Verify(model.Password, socio.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
            return View(model);
        }

        if (socio.Estado != "Activo")
        {
            ModelState.AddModelError(string.Empty, "Tu cuenta está suspendida. Contacta al gimnasio.");
            return View(model);
        }

        // Guardar datos básicos en sesión
        HttpContext.Session.SetInt32("SocioId",      socio.IdSocio);
        HttpContext.Session.SetString("SocioNombre", socio.NombreCompleto);
        HttpContext.Session.SetString("SocioPlan",   socio.Plan);
        HttpContext.Session.SetString("SocioRol",    socio.Rol);

        TempData["SuccessMessage"] = socio.Rol == "Admin"
            ? $"¡Bienvenido, {socio.NombreCompleto.Split(' ')[0]}! Accediste como administrador."
            : $"¡Bienvenido de vuelta, {socio.NombreCompleto.Split(' ')[0]}!";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        // Redirigir según rol
        return socio.Rol == "Admin"
            ? RedirectToAction("Index", "Admin")
            : RedirectToAction("Index", "Home");
    }

    // ──────────────────────────────────────────
    // REGISTRO — GET
    // ──────────────────────────────────────────

    [HttpGet]
    public IActionResult Register(string? plan = null)
    {
        var model = new RegisterViewModel
        {
            Plan = plan ?? string.Empty
        };
        return View(model);
    }

    // ──────────────────────────────────────────
    // REGISTRO — POST
    // ──────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // Verificar que el email no esté registrado
        bool emailExiste = await _db.Socios
            .AnyAsync(s => s.Email.ToLower() == model.Email.ToLower());

        if (emailExiste)
        {
            ModelState.AddModelError("Email", "Este correo ya está registrado.");
            return View(model);
        }

        // Calcular fecha de vencimiento según el plan (1 mes)
        var vencimiento = DateTime.UtcNow.AddMonths(1);

        // Crear el socio con contraseña hasheada
        var socio = new Socio
        {
            NombreCompleto   = model.FullName.Trim(),
            Email            = model.Email.Trim().ToLower(),
            Telefono         = model.Phone?.Trim(),
            Plan             = model.Plan,
            PasswordHash     = BCrypt.Net.BCrypt.HashPassword(model.Password),
            Estado           = "Activo",
            Rol              = "Usuario",
            FechaRegistro    = DateTime.UtcNow,
            FechaVencimiento = vencimiento
        };

        _db.Socios.Add(socio);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"¡Registro exitoso! Bienvenido a Style Gym, {socio.NombreCompleto.Split(' ')[0]}. Ya puedes iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }

    // ──────────────────────────────────────────
    // LOGOUT
    // ──────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}

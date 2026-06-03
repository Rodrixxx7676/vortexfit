using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VortexFit.Data;
using VortexFit.Models;
using VortexFit.Services;

namespace VortexFit.Controllers;

public class AccountController : Controller
{
    private readonly VortexFitDbContext  _db;
    private readonly RecaptchaService    _recaptcha;
    private readonly LoginAttemptTracker _attempts;

    public AccountController(VortexFitDbContext db, RecaptchaService recaptcha, LoginAttemptTracker attempts)
    {
        _db       = db;
        _recaptcha = recaptcha;
        _attempts  = attempts;
    }

    // ──────────────────────────────────────────
    // LOGIN — GET
    // ──────────────────────────────────────────

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"]    = returnUrl;
        ViewBag.RecaptchaSiteKey = _recaptcha.SiteKey;
        return View();
    }

    // ──────────────────────────────────────────
    // LOGIN — POST
    // ──────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewBag.RecaptchaSiteKey = _recaptcha.SiteKey;

        // ── Verificar bloqueo por intentos fallidos ────────
        var (blocked, remaining, permanent) = _attempts.CheckBlock(model.Email);
        if (blocked)
        {
            var secs = (int)(remaining?.TotalSeconds ?? 0);
            ViewBag.LockSeconds = secs;
            ViewBag.Permanent   = permanent;
            ModelState.AddModelError(string.Empty,
                permanent
                    ? "Cuenta bloqueada por demasiados intentos fallidos. Contacta al soporte."
                    : $"Demasiados intentos fallidos. Espera {secs} segundo{(secs != 1 ? "s" : "")}.");
            return View(model);
        }

        if (!ModelState.IsValid)
            return View(model);

        // ── reCAPTCHA v3 ──────────────────────────────────
        var captcha = await _recaptcha.VerifyAsync(model.RecaptchaToken, "login");
        if (!captcha.Success)
        {
            ModelState.AddModelError(string.Empty, "Verificación de seguridad fallida. Intenta de nuevo.");
            return View(model);
        }

        // ── Buscar socio ──────────────────────────────────
        var socio = await _db.Socios
            .FirstOrDefaultAsync(s => s.Email.ToLower() == model.Email.ToLower());

        if (socio == null || !BCrypt.Net.BCrypt.Verify(model.Password, socio.PasswordHash))
        {
            var (nowBlocked, lockFor) = _attempts.RecordFailure(model.Email);
            var attempts = _attempts.GetAttempts(model.Email);

            if (nowBlocked && lockFor.HasValue)
            {
                var secs = (int)lockFor.Value.TotalSeconds;
                ViewBag.LockSeconds = secs;
                ModelState.AddModelError(string.Empty,
                    $"Correo o contraseña incorrectos. Cuenta bloqueada por {secs} segundo{(secs != 1 ? "s" : "")} ({attempts} intentos fallidos).");
            }
            else
            {
                var left = 5 - (attempts % 5);
                ModelState.AddModelError(string.Empty,
                    attempts < 5
                        ? $"Correo o contraseña incorrectos. ({left} intento{(left != 1 ? "s" : "")} antes de penalización)"
                        : "Correo o contraseña incorrectos.");
            }
            return View(model);
        }

        if (socio.Estado != "Activo")
        {
            ModelState.AddModelError(string.Empty, "Tu cuenta está suspendida. Contacta al gimnasio.");
            return View(model);
        }

        // ── Login exitoso: limpiar intentos fallidos ──────
        _attempts.RecordSuccess(model.Email);

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
        ViewBag.RecaptchaSiteKey = _recaptcha.SiteKey;
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

        // ── reCAPTCHA v3 ──────────────────────────────────
        var captcha = await _recaptcha.VerifyAsync(model.RecaptchaToken, "register");
        if (!captcha.Success)
        {
            ModelState.AddModelError(string.Empty, "Verificación de seguridad fallida. Intenta de nuevo.");
            return View(model);
        }

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
            FechaVencimiento = vencimiento,
            CodigoAcceso     = Guid.NewGuid().ToString("N")[..12].ToUpper()
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

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using VortexFit.Data;
using VortexFit.Models;
using VortexFit.Services;

namespace VortexFit.Controllers;

public class AccountController : Controller
{
    private readonly VortexFitDbContext   _db;
    private readonly RecaptchaService     _recaptcha;
    private readonly LoginAttemptTracker  _attempts;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        VortexFitDbContext db,
        RecaptchaService recaptcha,
        LoginAttemptTracker attempts,
        ILogger<AccountController> logger)
    {
        _db        = db;
        _recaptcha = recaptcha;
        _attempts  = attempts;
        _logger    = logger;
    }

    // ──────────────────────────────────────────
    // LOGIN — GET
    // ──────────────────────────────────────────

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // Redirigir si ya hay sesión activa
        if (HttpContext.Session.GetInt32("SocioId") != null)
        {
            return HttpContext.Session.GetString("SocioRol") == "Admin"
                ? RedirectToAction("Index", "Admin")
                : RedirectToAction("Index", "Dashboard");
        }

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

            _logger.LogWarning("[SECURITY] Login bloqueado para {Email} — {Type}",
                model.Email, permanent ? "permanente" : $"{secs}s");
            return View(model);
        }

        if (!ModelState.IsValid)
            return View(model);

        // ── reCAPTCHA v3 ──────────────────────────────────
        var captcha = await _recaptcha.VerifyAsync(model.RecaptchaToken ?? string.Empty, "login");
        if (!captcha.Success)
        {
            _logger.LogWarning("[SECURITY] reCAPTCHA fallido en login para {Email}", model.Email);
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

            _logger.LogWarning("[SECURITY] Intento fallido #{Count} para {Email} desde {IP}",
                attempts,
                model.Email,
                HttpContext.Connection.RemoteIpAddress);

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
            _logger.LogWarning("[SECURITY] Intento de login en cuenta suspendida: {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "Tu cuenta está suspendida. Contacta al gimnasio.");
            return View(model);
        }

        // ── Login exitoso ─────────────────────────────────
        _attempts.RecordSuccess(model.Email);
        _logger.LogInformation("[SECURITY] Login exitoso: {Email} | Rol: {Rol} | IP: {IP}",
            socio.Email, socio.Rol, HttpContext.Connection.RemoteIpAddress);

        // Prevención de session fixation: limpiar sesión anterior antes de fijar nuevos valores
        HttpContext.Session.Clear();

        HttpContext.Session.SetInt32("SocioId",      socio.IdSocio);
        HttpContext.Session.SetString("SocioNombre", socio.NombreCompleto);
        HttpContext.Session.SetString("SocioPlan",   socio.Plan);
        HttpContext.Session.SetString("SocioRol",    socio.Rol);

        TempData["SuccessMessage"] = socio.Rol == "Admin"
            ? $"¡Bienvenido, {socio.NombreCompleto.Split(' ')[0]}! Accediste como administrador."
            : $"¡Bienvenido de vuelta, {socio.NombreCompleto.Split(' ')[0]}!";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

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
        // Redirigir si ya hay sesión activa
        if (HttpContext.Session.GetInt32("SocioId") != null)
            return RedirectToAction("Index", "Dashboard");

        ViewBag.RecaptchaSiteKey = _recaptcha.SiteKey;
        return View(new RegisterViewModel { Plan = plan ?? string.Empty });
    }

    // ──────────────────────────────────────────
    // REGISTRO — POST (rate limited por IP)
    // ──────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("register")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        ViewBag.RecaptchaSiteKey = _recaptcha.SiteKey;

        if (!ModelState.IsValid)
            return View(model);

        // ── reCAPTCHA v3 ──────────────────────────────────
        var captcha = await _recaptcha.VerifyAsync(model.RecaptchaToken ?? string.Empty, "register");
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
            // Mensaje genérico para no confirmar si el email existe (enumeración de cuentas)
            ModelState.AddModelError("Email", "No se pudo completar el registro. Verifica los datos ingresados.");
            _logger.LogWarning("[SECURITY] Intento de registro con email ya existente: {Email}", model.Email);
            return View(model);
        }

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
            FechaVencimiento = DateTime.UtcNow.AddMonths(1),
            CodigoAcceso     = Guid.NewGuid().ToString("N")[..12].ToUpper()
        };

        _db.Socios.Add(socio);
        await _db.SaveChangesAsync();

        _logger.LogInformation("[SECURITY] Nuevo registro: {Email} | Plan: {Plan}", socio.Email, socio.Plan);
        TempData["SuccessMessage"] = $"¡Registro exitoso! Bienvenido a Style Gym, {socio.NombreCompleto.Split(' ')[0]}. Ya puedes iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }

    // ──────────────────────────────────────────
    // SOPORTE TÉCNICO (rate limited por IP)
    // ──────────────────────────────────────────

    [HttpPost]
    [EnableRateLimiting("soporte")]
    public IActionResult Soporte([FromBody] SoporteDto dto)
    {
        // Validar campos obligatorios y tamaños
        if (string.IsNullOrWhiteSpace(dto.Nombre)  ||
            string.IsNullOrWhiteSpace(dto.Email)   ||
            string.IsNullOrWhiteSpace(dto.Mensaje))
            return BadRequest(new { ok = false, error = "Campos incompletos." });

        if (dto.Nombre.Length  > 100) return BadRequest(new { ok = false, error = "Nombre demasiado largo." });
        if (dto.Email.Length   > 200) return BadRequest(new { ok = false, error = "Email demasiado largo." });
        if (dto.Mensaje.Length > 1000) return BadRequest(new { ok = false, error = "Mensaje demasiado largo (máx. 1000 caracteres)." });

        // Validación básica de formato de email
        if (!dto.Email.Contains('@') || !dto.Email.Contains('.'))
            return BadRequest(new { ok = false, error = "Correo no válido." });

        _logger.LogInformation("[SOPORTE] {Timestamp:u} | {Email} | Asunto: {Asunto} | {Preview}",
            DateTime.UtcNow,
            dto.Email,
            dto.Asunto ?? "(sin asunto)",
            dto.Mensaje[..Math.Min(80, dto.Mensaje.Length)]);

        return Ok(new { ok = true });
    }

    // ──────────────────────────────────────────
    // LOGOUT
    // ──────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        var email = HttpContext.Session.GetString("SocioNombre") ?? "(desconocido)";
        _logger.LogInformation("[SECURITY] Logout: {Usuario}", email);
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}

// ── DTOs ──────────────────────────────────────────────────────
public record SoporteDto(string Nombre, string Email, string? Asunto, string Mensaje);

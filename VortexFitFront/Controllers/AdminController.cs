using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VortexFit.Data;
using VortexFit.Models;

namespace VortexFit.Controllers;

public class AdminController : Controller
{
    private readonly VortexFitDbContext _db;

    public AdminController(VortexFitDbContext db)
    {
        _db = db;
    }

    // Verificación de rol administrador
    private IActionResult? RequireAdmin()
    {
        if (HttpContext.Session.GetString("SocioRol") != "Admin")
            return RedirectToAction("Login", "Account");
        return null;
    }

    // ── PANEL PRINCIPAL ────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (RequireAdmin() is { } r) return r;

        var usuarios = await _db.Socios
            .Where(s => s.Rol == "Usuario")
            .ToListAsync();

        var vm = new AdminDashboardViewModel
        {
            TotalUsuarios    = usuarios.Count,
            UsuariosActivos  = usuarios.Count(s => s.Estado == "Activo"),
            PlanBasico       = usuarios.Count(s => s.Plan == "Basico"),
            PlanPro          = usuarios.Count(s => s.Plan == "Pro"),
            PlanElite        = usuarios.Count(s => s.Plan == "Elite"),
            UltimosRegistros = usuarios
                .OrderByDescending(s => s.FechaRegistro)
                .Take(6)
                .ToList()
        };

        ViewBag.AdminNombre = HttpContext.Session.GetString("SocioNombre") ?? "Administrador";
        return View(vm);
    }

    // ── GESTIÓN DE USUARIOS ────────────────────────
    [HttpGet]
    public async Task<IActionResult> Usuarios(string? buscar = null,
                                               string? plan   = null,
                                               string? estado = null)
    {
        if (RequireAdmin() is { } r) return r;

        var query = _db.Socios
            .Where(s => s.Rol == "Usuario")
            .AsQueryable();

        if (!string.IsNullOrEmpty(buscar))
            query = query.Where(s =>
                s.NombreCompleto.ToLower().Contains(buscar.ToLower()) ||
                s.Email.ToLower().Contains(buscar.ToLower()));

        if (!string.IsNullOrEmpty(plan) && plan != "Todos")
            query = query.Where(s => s.Plan == plan);

        if (!string.IsNullOrEmpty(estado) && estado != "Todos")
            query = query.Where(s => s.Estado == estado);

        var lista = await query
            .OrderByDescending(s => s.FechaRegistro)
            .ToListAsync();

        ViewBag.Buscar = buscar ?? string.Empty;
        ViewBag.Plan   = plan   ?? "Todos";
        ViewBag.Estado = estado ?? "Todos";
        ViewBag.AdminNombre = HttpContext.Session.GetString("SocioNombre") ?? "Administrador";

        return View(lista);
    }

    // ── GESTIÓN DE NOTICIAS ────────────────────────
    [HttpGet]
    public async Task<IActionResult> Noticias()
    {
        if (RequireAdmin() is { } r) return r;

        var noticias = await _db.Noticias
            .OrderByDescending(n => n.FechaPublicacion)
            .ToListAsync();

        ViewBag.AdminNombre = HttpContext.Session.GetString("SocioNombre") ?? "Administrador";
        return View(noticias);
    }

    [HttpGet]
    public IActionResult CrearNoticia()
    {
        if (RequireAdmin() is { } r) return r;
        ViewBag.AdminNombre = HttpContext.Session.GetString("SocioNombre") ?? "Administrador";
        return View(new Noticia());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearNoticia(Noticia model)
    {
        if (RequireAdmin() is { } r) return r;

        if (!ModelState.IsValid)
        {
            ViewBag.AdminNombre = HttpContext.Session.GetString("SocioNombre") ?? "Administrador";
            return View(model);
        }

        model.FechaPublicacion = DateTime.UtcNow;
        model.Activo = true;
        _db.Noticias.Add(model);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Noticia publicada correctamente.";
        return RedirectToAction(nameof(Noticias));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarNoticia(int id)
    {
        if (RequireAdmin() is { } r) return r;

        var noticia = await _db.Noticias.FindAsync(id);
        if (noticia != null)
        {
            noticia.Activo = false;   // soft-delete
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Noticias));
    }
}

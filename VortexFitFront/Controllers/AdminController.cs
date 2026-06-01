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

    // ── Guard ─────────────────────────────────────────────
    private IActionResult? RequireAdmin()
    {
        if (HttpContext.Session.GetString("SocioRol") != "Admin")
            return RedirectToAction("Login", "Account");
        return null;
    }

    // ════════════════════════════════════════════════
    // PANEL PRINCIPAL
    // ════════════════════════════════════════════════
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

        return View(vm);
    }

    // ════════════════════════════════════════════════
    // USUARIOS — lista + filtros
    // ════════════════════════════════════════════════
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

        return View(lista);
    }

    // ════════════════════════════════════════════════
    // USUARIOS — editar
    // ════════════════════════════════════════════════
    [HttpGet]
    public async Task<IActionResult> EditarUsuario(int id)
    {
        if (RequireAdmin() is { } r) return r;

        var socio = await _db.Socios.FindAsync(id);
        if (socio == null || socio.Rol == "Admin") return NotFound();

        var vm = new EditarUsuarioViewModel
        {
            IdSocio          = socio.IdSocio,
            NombreCompleto   = socio.NombreCompleto,
            Email            = socio.Email,
            Plan             = socio.Plan,
            Estado           = socio.Estado,
            Telefono         = socio.Telefono,
            FechaVencimiento = socio.FechaVencimiento
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarUsuario(EditarUsuarioViewModel vm)
    {
        if (RequireAdmin() is { } r) return r;

        if (!ModelState.IsValid)
            return View(vm);

        var socio = await _db.Socios.FindAsync(vm.IdSocio);
        if (socio == null || socio.Rol == "Admin") return NotFound();

        socio.Plan             = vm.Plan;
        socio.Estado           = vm.Estado;
        socio.Telefono         = vm.Telefono?.Trim();
        socio.FechaVencimiento = vm.FechaVencimiento;
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Usuario '{socio.NombreCompleto}' actualizado correctamente.";
        return RedirectToAction(nameof(Usuarios));
    }

    // ════════════════════════════════════════════════
    // NOTICIAS — lista
    // ════════════════════════════════════════════════
    [HttpGet]
    public async Task<IActionResult> Noticias()
    {
        if (RequireAdmin() is { } r) return r;

        var noticias = await _db.Noticias
            .OrderByDescending(n => n.FechaPublicacion)
            .ToListAsync();

        return View(noticias);
    }

    // ════════════════════════════════════════════════
    // NOTICIAS — crear
    // ════════════════════════════════════════════════
    [HttpGet]
    public IActionResult CrearNoticia()
    {
        if (RequireAdmin() is { } r) return r;
        return View(new Noticia());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearNoticia(Noticia model)
    {
        if (RequireAdmin() is { } r) return r;

        if (!ModelState.IsValid)
            return View(model);

        model.FechaPublicacion = DateTime.UtcNow;
        model.Activo = true;
        _db.Noticias.Add(model);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Noticia publicada correctamente.";
        return RedirectToAction(nameof(Noticias));
    }

    // ════════════════════════════════════════════════
    // NOTICIAS — editar
    // ════════════════════════════════════════════════
    [HttpGet]
    public async Task<IActionResult> EditarNoticia(int id)
    {
        if (RequireAdmin() is { } r) return r;

        var noticia = await _db.Noticias.FindAsync(id);
        if (noticia == null) return NotFound();

        return View(noticia);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarNoticia(Noticia model)
    {
        if (RequireAdmin() is { } r) return r;

        if (!ModelState.IsValid)
            return View(model);

        var noticia = await _db.Noticias.FindAsync(model.IdNoticia);
        if (noticia == null) return NotFound();

        noticia.Titulo    = model.Titulo;
        noticia.Resumen   = model.Resumen;
        noticia.Contenido = model.Contenido;
        noticia.Categoria = model.Categoria;
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Noticia actualizada correctamente.";
        return RedirectToAction(nameof(Noticias));
    }

    // ════════════════════════════════════════════════
    // NOTICIAS — ocultar / restaurar
    // ════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarNoticia(int id)
    {
        if (RequireAdmin() is { } r) return r;

        var noticia = await _db.Noticias.FindAsync(id);
        if (noticia != null)
        {
            noticia.Activo = false;
            await _db.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = "Noticia ocultada.";
        return RedirectToAction(nameof(Noticias));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestaurarNoticia(int id)
    {
        if (RequireAdmin() is { } r) return r;

        var noticia = await _db.Noticias.FindAsync(id);
        if (noticia != null)
        {
            noticia.Activo = true;
            await _db.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = "Noticia restaurada y visible al público.";
        return RedirectToAction(nameof(Noticias));
    }
}

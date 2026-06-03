using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VortexFit.Data;
using VortexFit.Filters;
using VortexFit.Models;

namespace VortexFit.Controllers;

[RequireAdmin]   // ← protege TODAS las acciones de este controlador
public class AdminController : Controller
{
    private readonly VortexFitDbContext _db;

    public AdminController(VortexFitDbContext db) => _db = db;

    // ════════════════════════════════════════════════
    // PANEL PRINCIPAL
    // ════════════════════════════════════════════════
    [HttpGet]
    public async Task<IActionResult> Index()
    {
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
        var socio = await _db.Socios.FindAsync(id);
        if (socio == null || socio.Rol == "Admin") return NotFound();

        return View(new EditarUsuarioViewModel
        {
            IdSocio          = socio.IdSocio,
            NombreCompleto   = socio.NombreCompleto,
            Email            = socio.Email,
            Plan             = socio.Plan,
            Estado           = socio.Estado,
            Telefono         = socio.Telefono,
            FechaVencimiento = socio.FechaVencimiento
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarUsuario(EditarUsuarioViewModel vm)
    {
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
        var noticias = await _db.Noticias
            .OrderByDescending(n => n.FechaPublicacion)
            .ToListAsync();

        return View(noticias);
    }

    // ════════════════════════════════════════════════
    // NOTICIAS — crear
    // ════════════════════════════════════════════════
    [HttpGet]
    public IActionResult CrearNoticia() => View(new Noticia());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearNoticia(Noticia model)
    {
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
        var noticia = await _db.Noticias.FindAsync(id);
        if (noticia == null) return NotFound();
        return View(noticia);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarNoticia(Noticia model)
    {
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
        var noticia = await _db.Noticias.FindAsync(id);
        if (noticia != null)
        {
            noticia.Activo = true;
            await _db.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = "Noticia restaurada y visible al público.";
        return RedirectToAction(nameof(Noticias));
    }

    // ════════════════════════════════════════════════
    // RESERVAS — vista admin
    // ════════════════════════════════════════════════
    [HttpGet]
    public async Task<IActionResult> Reservas(string? dia = null, string? clase = null)
    {
        var query = _db.Reservas
            .Include(r => r.Socio)
            .Where(r => r.Estado == "Confirmada")
            .AsQueryable();

        if (!string.IsNullOrEmpty(dia) && dia != "Todos")
            query = query.Where(r => r.Dia == dia);

        if (!string.IsNullOrEmpty(clase) && clase != "Todas")
            query = query.Where(r => r.NombreClase == clase);

        var reservas = await query
            .OrderBy(r => r.Dia)
            .ThenBy(r => r.Hora)
            .ToListAsync();

        var porClase = await _db.Reservas
            .Where(r => r.Estado == "Confirmada")
            .GroupBy(r => r.NombreClase)
            .Select(g => new { Clase = g.Key, Total = g.Count() })
            .ToListAsync();

        ViewBag.Dia      = dia    ?? "Todos";
        ViewBag.Clase    = clase  ?? "Todas";
        ViewBag.PorClase = porClase;
        ViewBag.Dias     = new[] { "Todos", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado" };
        ViewBag.Clases   = new[] { "Todas", "Spinning", "Yoga", "CrossFit", "Zumba", "Box" };

        return View(reservas);
    }

    // ════════════════════════════════════════════════
    // ESCÁNER QR
    // ════════════════════════════════════════════════
    [HttpGet]
    public IActionResult EscanearQR() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarAsistencia(string codigo, string? nombreClase = null)
    {
        // Sanear entrada
        codigo = (codigo ?? string.Empty).Trim().ToUpper();
        if (string.IsNullOrEmpty(codigo) || codigo.Length > 20)
        {
            TempData["ErrorMessage"] = "Código no válido.";
            return RedirectToAction(nameof(EscanearQR));
        }

        var socio = await _db.Socios
            .FirstOrDefaultAsync(s => s.CodigoAcceso == codigo);

        if (socio == null)
        {
            TempData["ErrorMessage"] = $"Código '{codigo}' no encontrado.";
            return RedirectToAction(nameof(EscanearQR));
        }

        _db.Asistencias.Add(new Asistencia
        {
            IdSocio     = socio.IdSocio,
            Fecha       = DateTime.UtcNow,
            NombreClase = nombreClase,
            Tipo        = "QR"
        });

        var reserva = await _db.Reservas.FirstOrDefaultAsync(r =>
            r.IdSocio     == socio.IdSocio &&
            r.Estado      == "Confirmada" &&
            r.NombreClase == (nombreClase ?? r.NombreClase));

        if (reserva != null)
            reserva.Estado = "Asistio";

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Asistencia registrada para {socio.NombreCompleto}.";
        ViewBag.UltimoSocio = socio;
        return RedirectToAction(nameof(EscanearQR));
    }

    // ════════════════════════════════════════════════
    // ASISTENCIAS — historial admin
    // ════════════════════════════════════════════════
    [HttpGet]
    public async Task<IActionResult> Asistencias(string? buscar = null)
    {
        var query = _db.Asistencias
            .Include(a => a.Socio)
            .AsQueryable();

        if (!string.IsNullOrEmpty(buscar))
            query = query.Where(a =>
                a.Socio.NombreCompleto.ToLower().Contains(buscar.ToLower()) ||
                a.Socio.Email.ToLower().Contains(buscar.ToLower()));

        var lista = await query
            .OrderByDescending(a => a.Fecha)
            .Take(200)
            .ToListAsync();

        ViewBag.Buscar = buscar ?? string.Empty;
        return View(lista);
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using VortexFit.Data;
using VortexFit.Models;

namespace VortexFit.Controllers;

public class DashboardController : Controller
{
    private readonly VortexFitDbContext _db;

    public DashboardController(VortexFitDbContext db) => _db = db;

    private IActionResult? RequireLogin()
    {
        if (HttpContext.Session.GetInt32("SocioId") is null)
            return RedirectToAction("Login", "Account", new { returnUrl = Request.Path });
        return null;
    }

    private int SocioId => HttpContext.Session.GetInt32("SocioId")!.Value;

    // ════════════════════════════════════════════════
    // INDEX
    // ════════════════════════════════════════════════
    public async Task<IActionResult> Index()
    {
        if (RequireLogin() is { } r) return r;

        var socio = await _db.Socios.FindAsync(SocioId);
        if (socio == null) return RedirectToAction("Logout", "Account");

        int diasRestantes = socio.FechaVencimiento.HasValue
            ? Math.Max(0, (socio.FechaVencimiento.Value - DateTime.UtcNow).Days)
            : 0;

        // Reservas próximas (primeras 3)
        var proximasClases = GetHorarioSemanal()
            .Select(h => new ClaseProxima
            {
                Nombre     = h.Nombre,
                Dia        = h.Dia,
                Hora       = h.Hora + " am",
                Instructor = h.Instructor,
                Cupo       = h.CupoMax - h.CupoActual,
                BadgeClass = h.BadgeClass
            })
            .Take(3)
            .ToList();

        var totalAsistencias = await _db.Asistencias.CountAsync(a => a.IdSocio == SocioId);
        var esteMes = await _db.Asistencias.CountAsync(a =>
            a.IdSocio == SocioId &&
            a.Fecha.Month == DateTime.UtcNow.Month &&
            a.Fecha.Year  == DateTime.UtcNow.Year);

        var vm = new DashboardViewModel
        {
            Socio           = socio,
            DiasRestantes   = diasRestantes,
            PorcentajePlan  = diasRestantes > 0 ? Math.Min(100, diasRestantes * 100 / 30) : 0,
            ClasesAsistidas = totalAsistencias,
            ClasesEsteMes   = esteMes,
            RachaActual     = await CalcularRacha(SocioId),
            ProximasClases  = proximasClases
        };

        return View(vm);
    }

    // ════════════════════════════════════════════════
    // PERFIL
    // ════════════════════════════════════════════════
    [HttpGet]
    public async Task<IActionResult> Perfil()
    {
        if (RequireLogin() is { } r) return r;
        var socio = await _db.Socios.FindAsync(SocioId);
        if (socio == null) return RedirectToAction("Logout", "Account");

        return View(new PerfilViewModel
        {
            NombreCompleto = socio.NombreCompleto,
            Telefono       = socio.Telefono,
            Email          = socio.Email,
            Plan           = socio.Plan,
            Estado         = socio.Estado,
            FechaRegistro  = socio.FechaRegistro
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Perfil(PerfilViewModel vm)
    {
        if (RequireLogin() is { } r) return r;
        var socio = await _db.Socios.FindAsync(SocioId);
        if (socio == null) return RedirectToAction("Logout", "Account");

        vm.Email = socio.Email; vm.Plan = socio.Plan;
        vm.Estado = socio.Estado; vm.FechaRegistro = socio.FechaRegistro;

        bool cambiarPass = !string.IsNullOrWhiteSpace(vm.NuevaPassword);
        if (cambiarPass)
        {
            if (string.IsNullOrWhiteSpace(vm.PasswordActual))
                ModelState.AddModelError("PasswordActual", "Debes ingresar tu contraseña actual.");
            else if (!BCrypt.Net.BCrypt.Verify(vm.PasswordActual, socio.PasswordHash))
                ModelState.AddModelError("PasswordActual", "La contraseña actual no es correcta.");
        }

        if (!ModelState.IsValid) return View(vm);

        socio.NombreCompleto = vm.NombreCompleto.Trim();
        socio.Telefono       = vm.Telefono?.Trim();
        if (cambiarPass)
            socio.PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.NuevaPassword!);

        await _db.SaveChangesAsync();
        HttpContext.Session.SetString("SocioNombre", socio.NombreCompleto);
        TempData["SuccessMessage"] = "Perfil actualizado correctamente.";
        return RedirectToAction(nameof(Perfil));
    }

    // ════════════════════════════════════════════════
    // CLASES — con estado de reserva
    // ════════════════════════════════════════════════
    [HttpGet]
    public async Task<IActionResult> Clases(string? dia = null)
    {
        if (RequireLogin() is { } r) return r;
        var socio = await _db.Socios.FindAsync(SocioId);
        if (socio == null) return RedirectToAction("Logout", "Account");

        var horario = GetHorarioSemanal();
        var filtrado = (string.IsNullOrEmpty(dia) || dia == "Todos")
            ? horario
            : horario.Where(c => c.Dia == dia).ToList();

        // Cargar reservas activas del socio
        var reservas = await _db.Reservas
            .Where(r => r.IdSocio == SocioId && r.Estado == "Confirmada")
            .Select(r => new { r.NombreClase, r.Dia, r.Hora, r.IdReserva })
            .ToListAsync();

        var vm = new ClasesViewModel
        {
            Socio     = socio,
            Horario   = filtrado.OrderBy(c => c.DiaOrden).ThenBy(c => c.Hora).ToList(),
            DiaFiltro = dia ?? "Todos",
            MisReservas = reservas.Select(r => $"{r.NombreClase}|{r.Dia}|{r.Hora}").ToHashSet()
        };

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReservarClase(string nombreClase, string dia, string hora, string instructor)
    {
        if (RequireLogin() is { } r) return r;

        // Verificar si ya tiene reserva
        bool yaReservado = await _db.Reservas.AnyAsync(r =>
            r.IdSocio    == SocioId &&
            r.NombreClase == nombreClase &&
            r.Dia         == dia &&
            r.Hora        == hora &&
            r.Estado      == "Confirmada");

        if (!yaReservado)
        {
            _db.Reservas.Add(new Reserva
            {
                IdSocio      = SocioId,
                NombreClase  = nombreClase,
                Dia          = dia,
                Hora         = hora,
                Instructor   = instructor,
                FechaReserva = DateTime.UtcNow,
                Estado       = "Confirmada"
            });
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"¡Reserva confirmada! {nombreClase} — {dia} {hora}";
        }
        else
        {
            TempData["ErrorMessage"] = "Ya tienes una reserva para esta clase.";
        }

        return RedirectToAction(nameof(Clases), new { dia });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelarReserva(string nombreClase, string dia, string hora)
    {
        if (RequireLogin() is { } r) return r;

        var reserva = await _db.Reservas.FirstOrDefaultAsync(r =>
            r.IdSocio     == SocioId &&
            r.NombreClase == nombreClase &&
            r.Dia         == dia &&
            r.Hora        == hora &&
            r.Estado      == "Confirmada");

        if (reserva != null)
        {
            reserva.Estado = "Cancelada";
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Reserva cancelada.";
        }

        return RedirectToAction(nameof(Clases), new { dia });
    }

    // ════════════════════════════════════════════════
    // QR DE ACCESO
    // ════════════════════════════════════════════════
    [HttpGet]
    public async Task<IActionResult> QR()
    {
        if (RequireLogin() is { } r) return r;
        var socio = await _db.Socios.FindAsync(SocioId);
        if (socio == null) return RedirectToAction("Logout", "Account");

        // Generar imagen QR como base64
        var qrGen  = new QRCodeGenerator();
        var qrData = qrGen.CreateQrCode(socio.CodigoAcceso, QRCodeGenerator.ECCLevel.M);
        var qrCode = new PngByteQRCode(qrData);
        var bytes  = qrCode.GetGraphic(12);
        ViewBag.QrDataUrl = $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
        ViewBag.Codigo    = socio.CodigoAcceso;

        return View(socio);
    }

    // ════════════════════════════════════════════════
    // PROGRESO — datos reales
    // ════════════════════════════════════════════════
    [HttpGet]
    public async Task<IActionResult> Progreso()
    {
        if (RequireLogin() is { } r) return r;
        var socio = await _db.Socios.FindAsync(SocioId);
        if (socio == null) return RedirectToAction("Logout", "Account");

        var now = DateTime.UtcNow;

        // Asistencia por mes (últimos 6 meses)
        var asistencias = await _db.Asistencias
            .Where(a => a.IdSocio == SocioId && a.Fecha >= now.AddMonths(-6))
            .ToListAsync();

        var meses = Enumerable.Range(0, 6).Select(i => now.AddMonths(-5 + i)).ToList();
        var porMes = meses.Select(m =>
            asistencias.Count(a => a.Fecha.Month == m.Month && a.Fecha.Year == m.Year)
        ).ToList();

        var total    = asistencias.Count;
        var racha    = await CalcularRacha(SocioId);
        var mejorRacha = racha; // simplificado — se puede mejorar con más lógica

        var vm = new ProgresoViewModel
        {
            Socio           = socio,
            ClasesAsistidas = total,
            RachaActual     = racha,
            RachaMejor      = Math.Max(racha, total > 0 ? 7 : 0),
            NombresMeses    = meses.Select(m => m.ToString("MMM")).ToList(),
            AsistenciaMeses = porMes,
            Records = new()
            {
                new() { Ejercicio="Press de banca",   Valor="80 kg",    Fecha="15 May 2026", Icono="fa-dumbbell",       Color="#3DD9D9" },
                new() { Ejercicio="Sentadilla",        Valor="100 kg",   Fecha="22 May 2026", Icono="fa-person",         Color="#a78bfa" },
                new() { Ejercicio="Peso muerto",       Valor="120 kg",   Fecha="01 Jun 2026", Icono="fa-weight-hanging", Color="#f97316" },
                new() { Ejercicio="Plancha",           Valor="3 min 45s",Fecha="28 May 2026", Icono="fa-stopwatch",      Color="#22c55e" },
            }
        };

        return View(vm);
    }

    // ════════════════════════════════════════════════
    // MEMBRESÍA
    // ════════════════════════════════════════════════
    [HttpGet]
    public async Task<IActionResult> Membresia()
    {
        if (RequireLogin() is { } r) return r;
        var socio = await _db.Socios.FindAsync(SocioId);
        if (socio == null) return RedirectToAction("Logout", "Account");

        int dias = socio.FechaVencimiento.HasValue
            ? Math.Max(0, (socio.FechaVencimiento.Value - DateTime.UtcNow).Days)
            : 0;

        var vm = new MembresiaViewModel
        {
            Socio          = socio,
            DiasRestantes  = dias,
            PorcentajePlan = dias > 0 ? Math.Min(100, dias * 100 / 30) : 0,
            Planes = new()
            {
                new() { Nombre="Basico", Precio="S/ 49.90", Color="#8A9BB0", Icono="fa-regular fa-circle", EsActual=socio.Plan=="Basico",
                    Beneficios=new(){"Acceso a máquinas de pesas","Acceso a zona de cardio","Horario: 6am – 8pm","Casillero de cortesía"} },
                new() { Nombre="Pro",    Precio="S/ 89.90", Color="#3DD9D9", Icono="fa-bolt",               EsActual=socio.Plan=="Pro",
                    Beneficios=new(){"Todo lo del plan Básico","Acceso a todas las clases grupales","Evaluación física mensual","App móvil + reservas online","1 sesión con PT al mes"} },
                new() { Nombre="Elite",  Precio="S/ 149.90",Color="#f97316", Icono="fa-crown",              EsActual=socio.Plan=="Elite",
                    Beneficios=new(){"Todo lo del plan Pro","Clases grupales ilimitadas","4 sesiones con PT al mes","Plan nutricional personalizado","Acceso 24/7","Priority booking"} },
            }
        };

        return View(vm);
    }

    // ════════════════════════════════════════════════
    // HELPERS
    // ════════════════════════════════════════════════
    private async Task<int> CalcularRacha(int socioId)
    {
        var fechas = await _db.Asistencias
            .Where(a => a.IdSocio == socioId)
            .Select(a => a.Fecha.Date)
            .Distinct()
            .OrderByDescending(f => f)
            .ToListAsync();

        if (!fechas.Any()) return 0;

        int racha = 1;
        for (int i = 0; i < fechas.Count - 1; i++)
        {
            if ((fechas[i] - fechas[i + 1]).Days == 1)
                racha++;
            else
                break;
        }
        return racha;
    }

    public static List<HorarioClase> GetHorarioSemanal() => new()
    {
        new() { Nombre="Spinning",  Dia="Lunes",     DiaOrden=0, Hora="06:00", Duracion=45, Instructor="Carlos Herrera",  CupoMax=15, CupoActual=8,  Nivel="Intermedio", Descripcion="Alta intensidad cardio en bicicleta estacionaria.",            BadgeClass="badge-spinning" },
        new() { Nombre="Yoga",      Dia="Lunes",     DiaOrden=0, Hora="08:00", Duracion=60, Instructor="Sofía Ramírez",   CupoMax=20, CupoActual=12, Nivel="Básico",     Descripcion="Relajación, flexibilidad y equilibrio mental.",                BadgeClass="badge-yoga"     },
        new() { Nombre="CrossFit",  Dia="Martes",    DiaOrden=1, Hora="07:00", Duracion=50, Instructor="Marco Torres",    CupoMax=12, CupoActual=10, Nivel="Avanzado",   Descripcion="Entrenamiento funcional de alta intensidad.",                  BadgeClass="badge-crossfit" },
        new() { Nombre="Zumba",     Dia="Martes",    DiaOrden=1, Hora="19:00", Duracion=55, Instructor="Lucía Mendoza",   CupoMax=25, CupoActual=18, Nivel="Básico",     Descripcion="Baile fitness con ritmos latinoamericanos.",                   BadgeClass="badge-zumba"    },
        new() { Nombre="Spinning",  Dia="Miércoles", DiaOrden=2, Hora="06:30", Duracion=45, Instructor="Carlos Herrera",  CupoMax=15, CupoActual=11, Nivel="Intermedio", Descripcion="Sesión matutina de cardio intensivo.",                         BadgeClass="badge-spinning" },
        new() { Nombre="Yoga",      Dia="Miércoles", DiaOrden=2, Hora="08:30", Duracion=60, Instructor="Sofía Ramírez",   CupoMax=20, CupoActual=7,  Nivel="Básico",     Descripcion="Yoga para reducir el estrés laboral.",                         BadgeClass="badge-yoga"     },
        new() { Nombre="Box",       Dia="Jueves",    DiaOrden=3, Hora="07:00", Duracion=60, Instructor="Rafael Vega",     CupoMax=14, CupoActual=9,  Nivel="Intermedio", Descripcion="Técnica de boxeo y acondicionamiento físico.",                 BadgeClass="badge-box"      },
        new() { Nombre="CrossFit",  Dia="Jueves",    DiaOrden=3, Hora="18:00", Duracion=50, Instructor="Marco Torres",    CupoMax=12, CupoActual=5,  Nivel="Avanzado",   Descripcion="WOD con movimientos compuestos y levantamiento olímpico.",     BadgeClass="badge-crossfit" },
        new() { Nombre="Spinning",  Dia="Viernes",   DiaOrden=4, Hora="06:00", Duracion=45, Instructor="Carlos Herrera",  CupoMax=15, CupoActual=13, Nivel="Intermedio", Descripcion="Sesión HIIT en bicicleta para cerrar la semana.",              BadgeClass="badge-spinning" },
        new() { Nombre="Zumba",     Dia="Viernes",   DiaOrden=4, Hora="19:30", Duracion=55, Instructor="Lucía Mendoza",   CupoMax=25, CupoActual=20, Nivel="Básico",     Descripcion="La clase más popular del viernes, ¡no te la pierdas!",        BadgeClass="badge-zumba"    },
        new() { Nombre="CrossFit",  Dia="Sábado",    DiaOrden=5, Hora="08:00", Duracion=60, Instructor="Marco Torres",    CupoMax=12, CupoActual=4,  Nivel="Avanzado",   Descripcion="CrossFit de fin de semana: más tiempo, más intensidad.",       BadgeClass="badge-crossfit" },
        new() { Nombre="Yoga",      Dia="Sábado",    DiaOrden=5, Hora="10:00", Duracion=75, Instructor="Sofía Ramírez",   CupoMax=20, CupoActual=6,  Nivel="Básico",     Descripcion="Yoga restaurativo para el descanso activo del fin de semana.", BadgeClass="badge-yoga"     },
        new() { Nombre="Box",       Dia="Sábado",    DiaOrden=5, Hora="11:30", Duracion=60, Instructor="Rafael Vega",     CupoMax=14, CupoActual=8,  Nivel="Intermedio", Descripcion="Sparring suave, trabajo de saco y coordinación.",              BadgeClass="badge-box"      },
    };
}

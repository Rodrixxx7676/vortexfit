using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using VortexFit.Data;
using VortexFit.Filters;
using VortexFit.Models;

namespace VortexFit.Controllers;

[RequireLogin]   // ← protege TODAS las acciones de este controlador
public class DashboardController : Controller
{
    private readonly VortexFitDbContext _db;

    public DashboardController(VortexFitDbContext db) => _db = db;

    /// <summary>ID del socio autenticado (garantizado por [RequireLogin]).</summary>
    private int SocioId => HttpContext.Session.GetInt32("SocioId")!.Value;

    // ════════════════════════════════════════════════
    // INDEX
    // ════════════════════════════════════════════════
    public async Task<IActionResult> Index()
    {
        var socio = await _db.Socios.FindAsync(SocioId);
        if (socio == null) return RedirectToAction("Logout", "Account");

        int diasRestantes = socio.FechaVencimiento.HasValue
            ? Math.Max(0, (socio.FechaVencimiento.Value - DateTime.UtcNow).Days)
            : 0;

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
        var socio = await _db.Socios.FindAsync(SocioId);
        if (socio == null) return RedirectToAction("Logout", "Account");

        var horario  = GetHorarioSemanal();
        var filtrado = (string.IsNullOrEmpty(dia) || dia == "Todos")
            ? horario
            : horario.Where(c => c.Dia == dia).ToList();

        var reservas = await _db.Reservas
            .Where(r => r.IdSocio == SocioId && r.Estado == "Confirmada")
            .Select(r => new { r.NombreClase, r.Dia, r.Hora, r.IdReserva })
            .ToListAsync();

        var vm = new ClasesViewModel
        {
            Socio       = socio,
            Horario     = filtrado.OrderBy(c => c.DiaOrden).ThenBy(c => c.Hora).ToList(),
            DiaFiltro   = dia ?? "Todos",
            MisReservas = reservas.Select(r => $"{r.NombreClase}|{r.Dia}|{r.Hora}").ToHashSet()
        };

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReservarClase(string nombreClase, string dia, string hora, string instructor)
    {
        bool yaReservado = await _db.Reservas.AnyAsync(r =>
            r.IdSocio     == SocioId &&
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
        var socio = await _db.Socios.FindAsync(SocioId);
        if (socio == null) return RedirectToAction("Logout", "Account");

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
        var socio = await _db.Socios.FindAsync(SocioId);
        if (socio == null) return RedirectToAction("Logout", "Account");

        var now = DateTime.UtcNow;

        var asistencias = await _db.Asistencias
            .Where(a => a.IdSocio == SocioId && a.Fecha >= now.AddMonths(-6))
            .ToListAsync();

        var meses = Enumerable.Range(0, 6).Select(i => now.AddMonths(-5 + i)).ToList();
        var porMes = meses.Select(m =>
            asistencias.Count(a => a.Fecha.Month == m.Month && a.Fecha.Year == m.Year)
        ).ToList();

        var total = asistencias.Count;
        var racha = await CalcularRacha(SocioId);

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
    // RUTINA SEMANAL
    // ════════════════════════════════════════════════
    [HttpGet]
    public async Task<IActionResult> Rutina()
    {
        var socio = await _db.Socios.FindAsync(SocioId);
        if (socio == null) return RedirectToAction("Logout", "Account");
        return View(BuildRutina(socio));
    }

    // ════════════════════════════════════════════════
    // PLAN NUTRICIONAL
    // ════════════════════════════════════════════════
    [HttpGet]
    public async Task<IActionResult> Nutricion()
    {
        var socio = await _db.Socios.FindAsync(SocioId);
        if (socio == null) return RedirectToAction("Logout", "Account");
        return View(BuildNutricion(socio));
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

    // ─── Rutina según plan ────────────────────────────────────
    private static RutinaViewModel BuildRutina(Socio socio)
    {
        var plan = socio.Plan;

        // Entrenador asignado según plan
        var entrenador = plan switch
        {
            "Elite"  => "Marco Torres",
            "Pro"    => "Carlos Herrera",
            _        => "Sofía Ramírez"
        };

        var objetivo = plan switch
        {
            "Elite"  => "Hipertrofia + Rendimiento",
            "Pro"    => "Fuerza + Definición",
            _        => "Acondicionamiento General"
        };

        // ── Ejercicios comunes ────────────────────────────────
        static EjercicioRutina E(string nombre, string series, string musculo, string icono, string color, string? nota = null)
            => new() { Nombre=nombre, Series=series, Musculo=musculo, Icono=icono, Color=color, Nota=nota };

        // ══ BÁSICO: Full Body 3 días ══════════════════════════
        if (plan == "Basico") return new RutinaViewModel
        {
            Socio = socio, Entrenador = entrenador, Objetivo = objetivo,
            Semana = new()
            {
                new() { Dia="Lunes",     Abrev="LUN", Tipo="Full Body A", Icono="fa-dumbbell",  Color="#3DD9D9", Ejercicios = new()
                {
                    E("Press de banca",      "4×10", "Pecho",    "fa-dumbbell",       "#3DD9D9"),
                    E("Sentadilla",           "4×10", "Piernas",  "fa-person",         "#a78bfa"),
                    E("Remo con barra",       "3×12", "Espalda",  "fa-weight-hanging", "#f97316"),
                    E("Plancha abdominal",    "3×45s","Core",     "fa-stopwatch",      "#22c55e", "Mantén la posición sin arquear la espalda"),
                }},
                new() { Dia="Martes",    Abrev="MAR", Tipo="Descanso activo", Icono="fa-person-walking", Color="#64748b", Descanso=true },
                new() { Dia="Miércoles", Abrev="MIÉ", Tipo="Full Body B", Icono="fa-dumbbell",  Color="#a78bfa", Ejercicios = new()
                {
                    E("Press militar",        "4×10", "Hombros",  "fa-dumbbell",       "#a78bfa"),
                    E("Peso muerto",          "4×8",  "Espalda",  "fa-weight-hanging", "#f97316", "Mantén la espalda recta"),
                    E("Dominadas asistidas",  "3×8",  "Espalda",  "fa-arrow-up",       "#3DD9D9"),
                    E("Curl de bíceps",       "3×12", "Bíceps",   "fa-hand-fist",      "#22c55e"),
                }},
                new() { Dia="Jueves",    Abrev="JUE", Tipo="Descanso activo", Icono="fa-person-walking", Color="#64748b", Descanso=true },
                new() { Dia="Viernes",   Abrev="VIE", Tipo="Full Body + Cardio", Icono="fa-heart-pulse", Color="#ef4444", Ejercicios = new()
                {
                    E("Sentadilla búlgara",   "3×10", "Piernas",  "fa-person",         "#a78bfa"),
                    E("Fondos en paralelas",  "3×10", "Tríceps",  "fa-dumbbell",       "#3DD9D9"),
                    E("Cardio (bicicleta)",   "20min","Cardio",   "fa-heart-pulse",    "#ef4444", "Ritmo moderado, zona 2"),
                }},
                new() { Dia="Sábado",    Abrev="SÁB", Tipo="Descanso", Icono="fa-bed", Color="#64748b", Descanso=true },
                new() { Dia="Domingo",   Abrev="DOM", Tipo="Descanso", Icono="fa-bed", Color="#64748b", Descanso=true },
            }
        };

        // ══ PRO: Upper/Lower 5 días ═══════════════════════════
        if (plan == "Pro") return new RutinaViewModel
        {
            Socio = socio, Entrenador = entrenador, Objetivo = objetivo,
            Semana = new()
            {
                new() { Dia="Lunes",     Abrev="LUN", Tipo="Pecho + Tríceps", Icono="fa-dumbbell", Color="#3DD9D9", Ejercicios = new()
                {
                    E("Press de banca plano",  "5×8",  "Pecho",    "fa-dumbbell",       "#3DD9D9"),
                    E("Press inclinado",        "4×10", "Pecho sup","fa-dumbbell",       "#3DD9D9"),
                    E("Aperturas con mancuerna","3×12", "Pecho",    "fa-arrows-left-right","#a78bfa"),
                    E("Fondos en paralelas",    "4×12", "Tríceps",  "fa-dumbbell",       "#f97316"),
                    E("Press francés",          "3×12", "Tríceps",  "fa-dumbbell",       "#f97316"),
                }},
                new() { Dia="Martes",    Abrev="MAR", Tipo="Espalda + Bíceps", Icono="fa-weight-hanging", Color="#a78bfa", Ejercicios = new()
                {
                    E("Peso muerto",            "4×6",  "Espalda",  "fa-weight-hanging", "#a78bfa", "Peso progresivo, técnica perfecta"),
                    E("Dominadas",              "4×8",  "Espalda",  "fa-arrow-up",       "#3DD9D9"),
                    E("Remo con barra",         "4×10", "Espalda",  "fa-weight-hanging", "#a78bfa"),
                    E("Curl bíceps barra",      "4×12", "Bíceps",   "fa-hand-fist",      "#22c55e"),
                    E("Curl martillo",          "3×12", "Bíceps",   "fa-hand-fist",      "#22c55e"),
                }},
                new() { Dia="Miércoles", Abrev="MIÉ", Tipo="Piernas", Icono="fa-person", Color="#ef4444", Ejercicios = new()
                {
                    E("Sentadilla libre",       "5×8",  "Cuádriceps","fa-person",        "#ef4444", "Profundidad completa"),
                    E("Prensa de piernas",      "4×12", "Piernas",  "fa-person",         "#ef4444"),
                    E("Peso muerto rumano",     "4×10", "Isquios",  "fa-weight-hanging", "#a78bfa"),
                    E("Extensión de piernas",   "3×15", "Cuádriceps","fa-person",        "#ef4444"),
                    E("Curl de piernas",        "3×15", "Isquios",  "fa-person",         "#a78bfa"),
                    E("Gemelos de pie",         "4×20", "Gemelos",  "fa-person",         "#3DD9D9"),
                }},
                new() { Dia="Jueves",    Abrev="JUE", Tipo="Hombros + Core", Icono="fa-dumbbell", Color="#f97316", Ejercicios = new()
                {
                    E("Press militar con barra","5×8",  "Hombros",  "fa-dumbbell",       "#f97316"),
                    E("Elevaciones laterales",  "4×15", "Hombros",  "fa-dumbbell",       "#f97316"),
                    E("Elevaciones frontales",  "3×12", "Hombros",  "fa-dumbbell",       "#f97316"),
                    E("Encogimientos",          "4×15", "Trapecios","fa-weight-hanging", "#a78bfa"),
                    E("Plancha + variaciones",  "3×1min","Core",    "fa-stopwatch",      "#22c55e"),
                    E("Ab wheel",              "3×10", "Core",     "fa-stopwatch",      "#22c55e"),
                }},
                new() { Dia="Viernes",   Abrev="VIE", Tipo="Full Body + HIIT", Icono="fa-heart-pulse", Color="#22c55e", Ejercicios = new()
                {
                    E("Sentadilla frontal",     "4×8",  "Piernas",  "fa-person",         "#a78bfa"),
                    E("Press de banca inclinado","3×10","Pecho",    "fa-dumbbell",       "#3DD9D9"),
                    E("Remo en polea",          "3×12", "Espalda",  "fa-weight-hanging", "#a78bfa"),
                    E("HIIT (spinning)",        "20min","Cardio",   "fa-heart-pulse",    "#22c55e", "30s máximo / 30s recuperación × 20 rondas"),
                }},
                new() { Dia="Sábado",    Abrev="SÁB", Tipo="Descanso", Icono="fa-bed", Color="#64748b", Descanso=true },
                new() { Dia="Domingo",   Abrev="DOM", Tipo="Descanso", Icono="fa-bed", Color="#64748b", Descanso=true },
            }
        };

        // ══ ELITE: PPL 6 días ═════════════════════════════════
        return new RutinaViewModel
        {
            Socio = socio, Entrenador = entrenador, Objetivo = objetivo,
            Semana = new()
            {
                new() { Dia="Lunes",     Abrev="LUN", Tipo="Push — Pecho/Hombros/Tríceps", Icono="fa-dumbbell", Color="#3DD9D9", Ejercicios = new()
                {
                    E("Press de banca (5×5 fuerza)","5×5","Pecho",   "fa-dumbbell",       "#3DD9D9", "RPE 8 — 2 reps en reserva"),
                    E("Press inclinado mancuerna",   "4×8","Pecho",   "fa-dumbbell",       "#3DD9D9"),
                    E("Aperturas en cables",         "3×15","Pecho",  "fa-arrows-left-right","#a78bfa"),
                    E("Press militar (overhead)",    "4×8","Hombros", "fa-dumbbell",       "#f97316"),
                    E("Elevaciones lat. + frontal",  "4×12","Hombros","fa-dumbbell",       "#f97316"),
                    E("Press francés + fondos",      "4×10","Tríceps","fa-dumbbell",       "#22c55e"),
                }},
                new() { Dia="Martes",    Abrev="MAR", Tipo="Pull — Espalda/Bíceps", Icono="fa-weight-hanging", Color="#a78bfa", Ejercicios = new()
                {
                    E("Peso muerto (5×3 fuerza)",    "5×3","Espalda", "fa-weight-hanging", "#a78bfa", "RPE 9 — máximo esfuerzo"),
                    E("Dominadas lastradas",         "4×6","Espalda", "fa-arrow-up",       "#3DD9D9"),
                    E("Remo pendlay",                "4×8","Espalda", "fa-weight-hanging", "#a78bfa"),
                    E("Pull-over en polea",          "3×12","Espalda","fa-weight-hanging", "#a78bfa"),
                    E("Curl bíceps supinado",        "4×12","Bíceps", "fa-hand-fist",      "#22c55e"),
                    E("Curl concentrado",            "3×12","Bíceps", "fa-hand-fist",      "#22c55e"),
                }},
                new() { Dia="Miércoles", Abrev="MIÉ", Tipo="Legs — Cuádriceps/Isquios/Glúteos", Icono="fa-person", Color="#ef4444", Ejercicios = new()
                {
                    E("Sentadilla (5×5 fuerza)",     "5×5","Cuádriceps","fa-person",       "#ef4444", "Profundidad ATG"),
                    E("Prensa 45°",                  "4×12","Piernas", "fa-person",        "#ef4444"),
                    E("Peso muerto rumano",          "4×8","Isquios",  "fa-weight-hanging","#a78bfa"),
                    E("Hip thrust con barra",        "4×12","Glúteos", "fa-person",        "#f97316"),
                    E("Extensión + curl alternados", "3×15","Piernas", "fa-person",        "#ef4444"),
                    E("Gemelos + sóleo",             "5×20","Gemelos", "fa-person",        "#3DD9D9"),
                }},
                new() { Dia="Jueves",    Abrev="JUE", Tipo="Push — Variaciones", Icono="fa-dumbbell", Color="#f97316", Ejercicios = new()
                {
                    E("Press banca con mancuernas",  "4×10","Pecho",  "fa-dumbbell",       "#3DD9D9"),
                    E("Press declinado",             "3×12","Pecho",  "fa-dumbbell",       "#3DD9D9"),
                    E("Fondos en paralelas lastrados","4×8","Tríceps/Pecho","fa-dumbbell", "#f97316"),
                    E("Press Arnold",                "4×12","Hombros","fa-dumbbell",       "#f97316"),
                    E("Face pulls",                  "4×20","Rotadores","fa-dumbbell",     "#a78bfa", "Crucial para salud del hombro"),
                    E("Tríceps polea + kickback",    "4×12","Tríceps","fa-dumbbell",       "#22c55e"),
                }},
                new() { Dia="Viernes",   Abrev="VIE", Tipo="Pull — Variaciones", Icono="fa-weight-hanging", Color="#22c55e", Ejercicios = new()
                {
                    E("Remo Yates (barra)",          "4×8","Espalda",  "fa-weight-hanging","#a78bfa"),
                    E("Jalón al pecho + neutro",     "4×10","Espalda", "fa-arrow-up",      "#3DD9D9"),
                    E("Remo en polea sentado",       "4×12","Espalda", "fa-weight-hanging","#a78bfa"),
                    E("Curl martillo + zottman",     "4×12","Bíceps",  "fa-hand-fist",     "#22c55e"),
                    E("Plancha RKC 3×45s",           "3×45s","Core",   "fa-stopwatch",     "#22c55e"),
                    E("Ab wheel de rodillas",        "3×12","Core",    "fa-stopwatch",     "#22c55e"),
                }},
                new() { Dia="Sábado",    Abrev="SÁB", Tipo="Legs — Glúteos/Cardio", Icono="fa-heart-pulse", Color="#ef4444", Ejercicios = new()
                {
                    E("Sentadilla búlgara",          "4×10","Glúteos/Cuáds","fa-person",   "#a78bfa"),
                    E("Zancadas con mancuernas",     "4×12","Piernas", "fa-person",        "#ef4444"),
                    E("Good mornings",               "3×12","Isquios", "fa-weight-hanging","#f97316"),
                    E("Cardio LISS (30 min)",        "30min","Cardio", "fa-heart-pulse",   "#ef4444", "Frecuencia cardíaca zona 2 (60–70% FC máx)"),
                }},
                new() { Dia="Domingo",   Abrev="DOM", Tipo="Descanso total", Icono="fa-bed", Color="#64748b", Descanso=true },
            }
        };
    }

    // ─── Nutrición según plan ──────────────────────────────────
    private static NutricionViewModel BuildNutricion(Socio socio)
    {
        var plan = socio.Plan;
        static ItemComida I(string nombre, string porcion, string? p = null, string? c = null, string? g = null)
            => new() { Nombre=nombre, Porcion=porcion, Proteina=p, Carbs=c, Grasas=g };

        if (plan == "Basico") return new NutricionViewModel
        {
            Socio=socio, Objetivo="Acondicionamiento y bienestar general",
            CaloriasDiarias=2200, Proteinas="130g", Carbohidratos="250g", Grasas="70g",
            Comidas = new()
            {
                new() { Tipo="Desayuno", Hora="7:00 am", Icono="fa-sun", Color="#f97316", Calorias=480, Items=new()
                {
                    I("Avena con leche descremada", "1 taza (80g)", "12g","55g","5g"),
                    I("Plátano de seda",             "1 unidad",     "1g", "23g","0g"),
                    I("Huevos revueltos",            "2 unidades",   "12g","0g", "10g"),
                }},
                new() { Tipo="Almuerzo", Hora="1:00 pm", Icono="fa-bowl-food", Color="#3DD9D9", Calorias=680, Items=new()
                {
                    I("Arroz integral",              "1 taza cocida","4g", "45g","1g"),
                    I("Pechuga de pollo a la plancha","150g",         "37g","0g", "3g"),
                    I("Ensalada mixta con aceite",   "1 plato",       "2g", "8g", "7g"),
                    I("Lentejas guisadas",           "½ taza",        "9g", "20g","0g"),
                }},
                new() { Tipo="Merienda", Hora="4:00 pm", Icono="fa-apple-whole", Color="#22c55e", Calorias=280, Items=new()
                {
                    I("Yogur griego natural",        "200g",          "20g","8g", "0g"),
                    I("Nueces mixtas",               "20g",           "4g", "3g", "13g"),
                }},
                new() { Tipo="Cena", Hora="7:30 pm", Icono="fa-moon", Color="#a78bfa", Calorias=560, Items=new()
                {
                    I("Filete de merluza al horno",  "180g",          "35g","0g", "5g"),
                    I("Camote asado",                "1 mediano",     "2g", "27g","0g"),
                    I("Brócoli y zanahoria al vapor","2 tazas",       "5g", "12g","0g"),
                }},
            },
            Consejos = new() {
                "Bebe al menos 2.5 litros de agua al día.",
                "Come cada 3–4 horas para mantener el metabolismo activo.",
                "Prioriza proteínas y verduras en cada comida.",
                "Evita azúcares añadidos y frituras procesadas.",
            }
        };

        if (plan == "Pro") return new NutricionViewModel
        {
            Socio=socio, Objetivo="Fuerza y definición muscular",
            CaloriasDiarias=2800, Proteinas="180g", Carbohidratos="300g", Grasas="80g",
            Comidas = new()
            {
                new() { Tipo="Desayuno", Hora="6:30 am", Icono="fa-sun", Color="#f97316", Calorias=580, Items=new()
                {
                    I("Avena con proteína en polvo","1 taza + 1 scoop","35g","60g","6g"),
                    I("Huevos enteros + claras",    "2+3 claras",       "26g","0g", "10g"),
                    I("Frutos rojos",               "½ taza",           "1g", "10g","0g"),
                }},
                new() { Tipo="Pre-entreno", Hora="10:30 am", Icono="fa-bolt", Color="#f97316", Calorias=320, Items=new()
                {
                    I("Banana",                     "1 grande",         "1g", "30g","0g"),
                    I("Pan integral con mantequilla de maní","2 tostadas","10g","30g","8g"),
                }},
                new() { Tipo="Post-entreno", Hora="1:30 pm", Icono="fa-dumbbell", Color="#3DD9D9", Calorias=480, Items=new()
                {
                    I("Shake de proteína",          "1 scoop en agua",  "25g","5g", "2g"),
                    I("Arroz blanco",               "1½ taza cocida",   "5g", "65g","0g"),
                    I("Pechuga de pollo",           "200g",             "50g","0g", "4g"),
                }},
                new() { Tipo="Almuerzo / Cena", Hora="7:00 pm", Icono="fa-bowl-food", Color="#a78bfa", Calorias=720, Items=new()
                {
                    I("Salmón al horno",            "200g",             "40g","0g", "20g"),
                    I("Quinoa cocida",              "1 taza",           "8g", "39g","4g"),
                    I("Espárragos + espinacas",     "2 tazas",          "5g", "8g", "0g"),
                    I("Aguacate",                   "½ unidad",         "1g", "6g", "15g"),
                }},
                new() { Tipo="Snack nocturno", Hora="9:30 pm", Icono="fa-moon", Color="#64748b", Calorias=200, Items=new()
                {
                    I("Queso cottage",              "150g",             "18g","4g", "3g"),
                    I("Almendras",                  "15 unidades",      "4g", "4g", "8g"),
                }},
            },
            Consejos = new() {
                "Consume proteínas dentro de los 30 minutos post-entreno.",
                "Los carbohidratos son tus aliados: priorízalos antes y después de entrenar.",
                "El salmón y los huevos son tus mejores fuentes de omega-3 y proteína completa.",
                "Hidratación: 3–3.5 litros diarios + bebida isotónica en días de entreno intenso.",
                "Duerme 7–9 horas: el músculo crece mientras descansas.",
            }
        };

        // Elite
        return new NutricionViewModel
        {
            Socio=socio, Objetivo="Hipertrofia máxima y rendimiento atlético",
            CaloriasDiarias=3400, Proteinas="220g", Carbohidratos="380g", Grasas="95g",
            Comidas = new()
            {
                new() { Tipo="Desayuno", Hora="6:00 am", Icono="fa-sun", Color="#f97316", Calorias=680, Items=new()
                {
                    I("Avena + proteína + semillas","100g + 1 scoop","38g","72g","10g"),
                    I("Huevos revueltos con espinaca","3 enteros",    "21g","2g", "15g"),
                    I("Zumo de naranja natural",    "200ml",           "1g", "20g","0g"),
                }},
                new() { Tipo="Media mañana", Hora="9:30 am", Icono="fa-apple-whole", Color="#22c55e", Calorias=380, Items=new()
                {
                    I("Yogur griego 0% + granola", "250g + 30g",      "25g","35g","3g"),
                    I("Kiwi + fresas",             "1+5 unidades",     "1g", "18g","0g"),
                }},
                new() { Tipo="Pre-entreno", Hora="12:00 pm", Icono="fa-bolt", Color="#f97316", Calorias=420, Items=new()
                {
                    I("Arroz blanco",              "1½ taza cocida",   "5g", "65g","0g"),
                    I("Pechuga de pollo",          "150g",             "37g","0g", "3g"),
                    I("Plátano maduro",            "1 grande",         "1g", "30g","0g"),
                }},
                new() { Tipo="Post-entreno inmediato", Hora="3:30 pm", Icono="fa-dumbbell", Color="#3DD9D9", Calorias=420, Items=new()
                {
                    I("Shake: proteína + creatina","1.5 scoops",       "37g","6g", "2g"),
                    I("Dátiles (azúcar rápido)",   "4 unidades",       "0g", "28g","0g"),
                    I("Leche semidesnatada",        "200ml",            "7g", "10g","4g"),
                }},
                new() { Tipo="Cena principal", Hora="7:30 pm", Icono="fa-bowl-food", Color="#a78bfa", Calorias=860, Items=new()
                {
                    I("Filete de ternera magra",   "250g",             "52g","0g", "16g"),
                    I("Patata cocida",             "2 medianas",       "4g", "52g","0g"),
                    I("Brócoli + pimiento asado",  "2 tazas",          "5g", "12g","0g"),
                    I("Aceite de oliva virgen",    "2 cucharadas",     "0g", "0g", "28g"),
                }},
                new() { Tipo="Snack nocturno", Hora="10:00 pm", Icono="fa-moon", Color="#64748b", Calorias=260, Items=new()
                {
                    I("Caseína o queso cottage",   "200g",             "30g","6g", "4g"),
                    I("Mantequilla de almendras",  "1 cucharada",      "3g", "3g", "9g"),
                }},
            },
            Consejos = new() {
                "Cicla calorías: días de entreno +200 kcal, días de descanso -200 kcal.",
                "Creatina monohidrato: 5g/día post-entreno. Sin ciclos, uso continuo.",
                "Timing de carbohidratos: 60% antes y después del entreno.",
                "Monitorea tu peso semanal: objetivo +0.25–0.5 kg/semana en fase de volumen.",
                "Suplementación base: vitamina D3 (2000 UI), omega-3 (3g/día), magnesio (400mg noche).",
                "Marco Torres (tu entrenador) revisará tus macros cada 3 semanas.",
            }
        };
    }
}

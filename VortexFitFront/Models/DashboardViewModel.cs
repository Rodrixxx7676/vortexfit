namespace VortexFit.Models;

// ── Dashboard principal ────────────────────────────────────
public class DashboardViewModel
{
    public Socio   Socio          { get; set; } = null!;
    public int     DiasRestantes  { get; set; }
    public int     PorcentajePlan { get; set; }

    public int ClasesAsistidas { get; set; } = 47;
    public int ClasesEsteMes   { get; set; } = 12;
    public int RachaActual     { get; set; } = 5;

    public List<ClaseProxima> ProximasClases { get; set; } = new()
    {
        new() { Nombre = "Spinning",  Dia = "Lunes",     Hora = "06:00 am", Instructor = "Carlos Herrera",  Cupo = 8,  BadgeClass = "badge-spinning" },
        new() { Nombre = "Yoga",      Dia = "Miércoles", Hora = "08:00 am", Instructor = "Sofía Ramírez",   Cupo = 12, BadgeClass = "badge-yoga"     },
        new() { Nombre = "CrossFit",  Dia = "Viernes",   Hora = "10:00 am", Instructor = "Marco Torres",    Cupo = 5,  BadgeClass = "badge-crossfit" },
    };
}

public class ClaseProxima
{
    public string Nombre     { get; set; } = string.Empty;
    public string Dia        { get; set; } = string.Empty;
    public string Hora       { get; set; } = string.Empty;
    public string Instructor { get; set; } = string.Empty;
    public int    Cupo       { get; set; }
    public string BadgeClass { get; set; } = "badge-spinning";
}

// ── Clases ─────────────────────────────────────────────────
public class HorarioClase
{
    public string Nombre      { get; set; } = string.Empty;
    public string Dia         { get; set; } = string.Empty;
    public int    DiaOrden    { get; set; }
    public string Hora        { get; set; } = string.Empty;
    public int    Duracion    { get; set; }
    public string Instructor  { get; set; } = string.Empty;
    public int    CupoMax     { get; set; }
    public int    CupoActual  { get; set; }
    public string Nivel       { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string BadgeClass  { get; set; } = string.Empty;
}

public class ClasesViewModel
{
    public Socio              Socio       { get; set; } = null!;
    public List<HorarioClase> Horario     { get; set; } = new();
    public string             DiaFiltro   { get; set; } = "Todos";
    public HashSet<string>    MisReservas { get; set; } = new();
}

// ── Progreso ───────────────────────────────────────────────
public class RecordEjercicio
{
    public string Ejercicio { get; set; } = string.Empty;
    public string Valor     { get; set; } = string.Empty;
    public string Fecha     { get; set; } = string.Empty;
    public string Icono     { get; set; } = string.Empty;
    public string Color     { get; set; } = string.Empty;
}

public class ProgresoViewModel
{
    public Socio              Socio            { get; set; } = null!;
    public int                ClasesAsistidas  { get; set; }
    public int                RachaActual      { get; set; }
    public int                RachaMejor       { get; set; }
    public List<int>          AsistenciaMeses  { get; set; } = new();
    public List<string>       NombresMeses     { get; set; } = new();
    public List<RecordEjercicio> Records       { get; set; } = new();
}

// ── Membresía ──────────────────────────────────────────────
public class PlanInfo
{
    public string        Nombre     { get; set; } = string.Empty;
    public string        Precio     { get; set; } = string.Empty;
    public string        Color      { get; set; } = string.Empty;
    public string        Icono      { get; set; } = string.Empty;
    public bool          EsActual   { get; set; }
    public List<string>  Beneficios { get; set; } = new();
}

public class MembresiaViewModel
{
    public Socio         Socio          { get; set; } = null!;
    public int           DiasRestantes  { get; set; }
    public int           PorcentajePlan { get; set; }
    public List<PlanInfo> Planes        { get; set; } = new();
}

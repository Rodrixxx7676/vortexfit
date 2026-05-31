namespace VortexFit.Models;

public class DashboardViewModel
{
    public Socio   Socio          { get; set; } = null!;
    public int     DiasRestantes  { get; set; }
    public int     PorcentajePlan { get; set; }

    // Datos ficticios de actividad (se reemplazarán con Oracle en próximas fases)
    public int ClasesAsistidas   { get; set; } = 12;
    public int ClasesEsteMes     { get; set; } = 4;
    public int RachaActual       { get; set; } = 5;   // días consecutivos

    // Próximas clases (ficticias por ahora)
    public List<ClaseProxima> ProximasClases { get; set; } = new()
    {
        new() { Nombre = "Spinning",  Dia = "Lunes",     Hora = "06:00 am", Instructor = "Carlos Herrera",  Cupo = 8  },
        new() { Nombre = "Yoga",      Dia = "Miércoles", Hora = "08:00 am", Instructor = "Sofía Ramírez",   Cupo = 12 },
        new() { Nombre = "CrossFit",  Dia = "Viernes",   Hora = "10:00 am", Instructor = "Marco Torres",    Cupo = 5  },
    };
}

public class ClaseProxima
{
    public string Nombre     { get; set; } = string.Empty;
    public string Dia        { get; set; } = string.Empty;
    public string Hora       { get; set; } = string.Empty;
    public string Instructor { get; set; } = string.Empty;
    public int    Cupo       { get; set; }
}

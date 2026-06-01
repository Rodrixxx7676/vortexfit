namespace VortexFit.Models;

public class AdminDashboardViewModel
{
    public int TotalUsuarios   { get; set; }
    public int UsuariosActivos { get; set; }
    public int PlanBasico      { get; set; }
    public int PlanPro         { get; set; }
    public int PlanElite       { get; set; }
    public List<Socio> UltimosRegistros { get; set; } = new();
}

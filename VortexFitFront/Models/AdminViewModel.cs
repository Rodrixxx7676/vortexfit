using System.ComponentModel.DataAnnotations;

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

public class EditarUsuarioViewModel
{
    public int    IdSocio        { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Email          { get; set; } = string.Empty;

    [Required(ErrorMessage = "El plan es obligatorio.")]
    public string Plan           { get; set; } = string.Empty;

    [Required(ErrorMessage = "El estado es obligatorio.")]
    public string Estado         { get; set; } = string.Empty;

    [MaxLength(15)]
    public string? Telefono      { get; set; }

    public DateTime? FechaVencimiento { get; set; }
}

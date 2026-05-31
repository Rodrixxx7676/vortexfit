using System.ComponentModel.DataAnnotations;

namespace VortexFit.Models;

public class PerfilViewModel
{
    // Solo lectura
    public string Email         { get; set; } = string.Empty;
    public string Plan          { get; set; } = string.Empty;
    public string Estado        { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }

    // Editables
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Teléfono no válido.")]
    [StringLength(15, MinimumLength = 9, ErrorMessage = "Entre 9 y 15 dígitos.")]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    // Cambio de contraseña (opcional)
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mínimo 8 caracteres.")]
    [Display(Name = "Nueva contraseña")]
    public string? NuevaPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare("NuevaPassword", ErrorMessage = "Las contraseñas no coinciden.")]
    [Display(Name = "Confirmar nueva contraseña")]
    public string? ConfirmarPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Contraseña actual (para confirmar cambios)")]
    public string? PasswordActual { get; set; }
}

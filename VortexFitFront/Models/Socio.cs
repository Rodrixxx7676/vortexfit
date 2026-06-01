using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VortexFit.Models;

[Table("SOCIOS")]
public class Socio
{
    [Key]
    [Column("ID_SOCIO")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdSocio { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("NOMBRE_COMPLETO")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    [Column("EMAIL")]
    public string Email { get; set; } = string.Empty;

    [MaxLength(15)]
    [Column("TELEFONO")]
    public string? Telefono { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("PASSWORD_HASH")]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("PLAN")]
    public string Plan { get; set; } = string.Empty;  // Basico | Pro | Elite

    [Required]
    [MaxLength(20)]
    [Column("ESTADO")]
    public string Estado { get; set; } = "Activo";    // Activo | Inactivo | Suspendido

    [Column("FECHA_REGISTRO")]
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    [Column("FECHA_VENCIMIENTO")]
    public DateTime? FechaVencimiento { get; set; }

    [Required]
    [MaxLength(10)]
    [Column("ROL")]
    public string Rol { get; set; } = "Usuario";   // Admin | Usuario

    [MaxLength(20)]
    [Column("CODIGO_ACCESO")]
    public string CodigoAcceso { get; set; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VortexFit.Models;

[Table("RESERVAS")]
public class Reserva
{
    [Key]
    [Column("ID_RESERVA")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdReserva { get; set; }

    [Column("ID_SOCIO")]
    public int IdSocio { get; set; }

    [Required, MaxLength(50)]
    [Column("NOMBRE_CLASE")]
    public string NombreClase { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    [Column("DIA")]
    public string Dia { get; set; } = string.Empty;

    [Required, MaxLength(10)]
    [Column("HORA")]
    public string Hora { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("INSTRUCTOR")]
    public string Instructor { get; set; } = string.Empty;

    [Column("FECHA_RESERVA")]
    public DateTime FechaReserva { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(20)]
    [Column("ESTADO")]
    public string Estado { get; set; } = "Confirmada"; // Confirmada | Cancelada | Asistio

    public Socio Socio { get; set; } = null!;
}

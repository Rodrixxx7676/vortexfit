using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VortexFit.Models;

[Table("ASISTENCIAS")]
public class Asistencia
{
    [Key]
    [Column("ID_ASISTENCIA")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdAsistencia { get; set; }

    [Column("ID_SOCIO")]
    public int IdSocio { get; set; }

    [Column("FECHA")]
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    [Column("NOMBRE_CLASE")]
    public string? NombreClase { get; set; }

    [MaxLength(20)]
    [Column("TIPO")]
    public string Tipo { get; set; } = "QR"; // QR | Manual

    public Socio Socio { get; set; } = null!;
}

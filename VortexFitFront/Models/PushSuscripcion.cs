using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VortexFit.Models;

[Table("PUSH_SUSCRIPCIONES")]
public class PushSuscripcion
{
    [Key]
    [Column("ID_SUSCRIPCION")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdSuscripcion { get; set; }

    [Column("ID_SOCIO")]
    public int IdSocio { get; set; }

    [Required, MaxLength(500)]
    [Column("ENDPOINT")]
    public string Endpoint { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    [Column("P256DH")]
    public string P256dh { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Column("AUTH_KEY")]
    public string AuthKey { get; set; } = string.Empty;

    [Column("FECHA_REGISTRO")]
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public Socio Socio { get; set; } = null!;
}

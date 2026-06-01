using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VortexFit.Models;

[Table("NOTICIAS")]
public class Noticia
{
    [Key]
    [Column("ID_NOTICIA")]
    public int IdNoticia { get; set; }

    [Column("TITULO")]
    [Required, MaxLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [Column("RESUMEN")]
    [MaxLength(400)]
    public string Resumen { get; set; } = string.Empty;

    [Column("CONTENIDO")]
    [Required]
    public string Contenido { get; set; } = string.Empty;

    [Column("CATEGORIA")]
    [MaxLength(50)]
    public string Categoria { get; set; } = "Comunicado";

    [Column("FECHA_PUBLICACION")]
    public DateTime FechaPublicacion { get; set; } = DateTime.UtcNow;

    [Column("ACTIVO")]
    public bool Activo { get; set; } = true;
}

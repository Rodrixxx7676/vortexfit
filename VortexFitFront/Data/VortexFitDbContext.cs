using Microsoft.EntityFrameworkCore;
using VortexFit.Models;

namespace VortexFit.Data;

public class VortexFitDbContext : DbContext
{
    public VortexFitDbContext(DbContextOptions<VortexFitDbContext> options)
        : base(options) { }

    // ── Tablas ──────────────────────────────────
    public DbSet<Socio>    Socios    { get; set; }
    public DbSet<Noticia>  Noticias  { get; set; }

    // ── Configuración del modelo ─────────────────
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Noticia>(entity =>
        {
            entity.Property(n => n.FechaPublicacion)
                  .HasDefaultValueSql("SYSDATE");

            entity.Property(n => n.Activo)
                  .HasDefaultValue(true);
        });

        modelBuilder.Entity<Socio>(entity =>
        {
            // Email único
            entity.HasIndex(s => s.Email)
                  .IsUnique()
                  .HasDatabaseName("UQ_SOCIOS_EMAIL");

            // Valores por defecto Oracle
            entity.Property(s => s.Estado)
                  .HasDefaultValue("Activo");

            entity.Property(s => s.Rol)
                  .HasDefaultValue("Usuario");

            entity.Property(s => s.FechaRegistro)
                  .HasDefaultValueSql("SYSDATE");
        });
    }
}

using Microsoft.EntityFrameworkCore;
using VortexFit.Models;

namespace VortexFit.Data;

public class VortexFitDbContext : DbContext
{
    public VortexFitDbContext(DbContextOptions<VortexFitDbContext> options)
        : base(options) { }

    // ── Tablas ──────────────────────────────────
    public DbSet<Socio> Socios { get; set; }

    // ── Configuración del modelo ─────────────────
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Socio>(entity =>
        {
            // Email único
            entity.HasIndex(s => s.Email)
                  .IsUnique()
                  .HasDatabaseName("UQ_SOCIOS_EMAIL");

            // Valores por defecto Oracle
            entity.Property(s => s.Estado)
                  .HasDefaultValue("Activo");

            entity.Property(s => s.FechaRegistro)
                  .HasDefaultValueSql("SYSDATE");
        });
    }
}

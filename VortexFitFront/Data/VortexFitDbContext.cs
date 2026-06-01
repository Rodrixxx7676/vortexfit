using Microsoft.EntityFrameworkCore;
using VortexFit.Models;

namespace VortexFit.Data;

public class VortexFitDbContext : DbContext
{
    public VortexFitDbContext(DbContextOptions<VortexFitDbContext> options)
        : base(options) { }

    public DbSet<Socio>           Socios           { get; set; }
    public DbSet<Noticia>         Noticias         { get; set; }
    public DbSet<Reserva>         Reservas         { get; set; }
    public DbSet<Asistencia>      Asistencias      { get; set; }
    public DbSet<PushSuscripcion> PushSuscripciones{ get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Noticia>(entity =>
        {
            entity.Property(n => n.FechaPublicacion).HasDefaultValueSql("SYSDATE");
            entity.Property(n => n.Activo).HasDefaultValue(true);
        });

        modelBuilder.Entity<Socio>(entity =>
        {
            entity.HasIndex(s => s.Email).IsUnique().HasDatabaseName("UQ_SOCIOS_EMAIL");
            entity.Property(s => s.Estado).HasDefaultValue("Activo");
            entity.Property(s => s.Rol).HasDefaultValue("Usuario");
            entity.Property(s => s.FechaRegistro).HasDefaultValueSql("SYSDATE");
            entity.Property(s => s.CodigoAcceso).HasDefaultValue(string.Empty);
        });

        modelBuilder.Entity<Reserva>(entity =>
        {
            entity.HasOne(r => r.Socio)
                  .WithMany()
                  .HasForeignKey(r => r.IdSocio)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(r => r.FechaReserva).HasDefaultValueSql("SYSDATE");
            entity.Property(r => r.Estado).HasDefaultValue("Confirmada");
        });

        modelBuilder.Entity<Asistencia>(entity =>
        {
            entity.HasOne(a => a.Socio)
                  .WithMany()
                  .HasForeignKey(a => a.IdSocio)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(a => a.Fecha).HasDefaultValueSql("SYSDATE");
        });

        modelBuilder.Entity<PushSuscripcion>(entity =>
        {
            entity.HasOne(p => p.Socio)
                  .WithMany()
                  .HasForeignKey(p => p.IdSocio)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(p => p.Endpoint).IsUnique().HasDatabaseName("UQ_PUSH_ENDPOINT");
            entity.Property(p => p.FechaRegistro).HasDefaultValueSql("SYSDATE");
        });
    }
}

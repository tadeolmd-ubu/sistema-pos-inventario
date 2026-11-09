using Microsoft.EntityFrameworkCore;
using PosInventario.Models;

namespace PosInventario.Data;

public class AppDbContext : DbContext
{
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Elemento> Elementos => Set<Elemento>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Categoria>(e =>
        {
            e.ToTable("Categoria");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nombre).HasColumnName("nombre")
                .HasMaxLength(100).IsRequired();
            e.Property(x => x.Descripcion).HasColumnName("descripcion")
                .HasMaxLength(255);
            e.Property(x => x.Activo).HasColumnName("activo").IsRequired();

            e.HasIndex(x => x.Nombre);

            // Semilla opcional de catálogo:
            e.HasData(new Categoria { Id = 1, Nombre = "General", Activo = 1 });
        });

        modelBuilder.Entity<Elemento>(e =>
        {
            e.ToTable("Elemento");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nombre).HasColumnName("nombre")
                .HasMaxLength(100).IsRequired();
            e.Property(x => x.Descripcion).HasColumnName("descripcion")
                .HasMaxLength(500);
            e.Property(x => x.Activo).HasColumnName("activo").IsRequired();
            e.Property(x => x.FechaArchivado).HasColumnName("fechaArchivado");
            e.Property(x => x.FechaCreacion).HasColumnName("fechaCreacion").IsRequired();
            e.Property(x => x.FechaModificacion).HasColumnName("fechaModificacion").IsRequired();

            e.Property(x => x.IdCategoria).HasColumnName("idCategoria").IsRequired();
            e.HasOne(x => x.Categoria)
                .WithMany(c => c.Elementos)
                .HasForeignKey(x => x.IdCategoria);

            e.HasIndex(x => x.Nombre);
        });
    }
}

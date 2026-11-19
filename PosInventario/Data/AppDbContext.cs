using Microsoft.EntityFrameworkCore;
using PosInventario.Models;
using PosInventario.Models.Enums;

namespace PosInventario.Data;

public class AppDbContext : DbContext
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Abono> Abonos => Set<Abono>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<DetalleCompra> DetallesCompra => Set<DetalleCompra>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<Caja> Cajas => Set<Caja>();
    public DbSet<MovimientoCaja> MovimientosCaja => Set<MovimientoCaja>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();
    public DbSet<PagoVenta> PagosVenta => Set<PagoVenta>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("Usuario");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.NombreUsuario).HasColumnName("nombreUsuario")
                .UseCollation("NOCASE").HasMaxLength(50).IsRequired();
            e.Property(x => x.Nombre).HasColumnName("nombre")
                .HasMaxLength(100).IsRequired();
            e.Property(x => x.ContrasenaHash).HasColumnName("contrasenaHash")
                .HasMaxLength(255).IsRequired();
            e.Property(x => x.Rol).HasColumnName("rol")
                .HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.Activo).HasColumnName("activo").IsRequired();
            e.Property(x => x.FechaCreacion).HasColumnName("fechaCreacion").IsRequired();
            e.Property(x => x.FechaUltimoAcceso).HasColumnName("fechaUltimoAcceso");

            e.HasIndex(x => x.NombreUsuario).IsUnique();
            e.HasIndex(x => x.Activo);
        });

        modelBuilder.Entity<Producto>(e =>
        {
            e.ToTable("Producto", t =>
            {
                t.HasCheckConstraint("CK_Producto_precioVenta", "precioVenta >= 0");
                t.HasCheckConstraint("CK_Producto_costo", "costo >= 0");
                t.HasCheckConstraint("CK_Producto_stock", "stock >= 0");
                t.HasCheckConstraint("CK_Producto_stockMinimo", "stockMinimo >= 0");
            });
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Sku).HasColumnName("sku")
                .UseCollation("NOCASE").HasMaxLength(40).IsRequired();
            e.Property(x => x.Nombre).HasColumnName("nombre")
                .HasMaxLength(100).IsRequired();
            e.Property(x => x.Descripcion).HasColumnName("descripcion")
                .HasMaxLength(500);
            e.Property(x => x.UnidadMedida).HasColumnName("unidadMedida")
                .HasConversion<string>().HasMaxLength(15).IsRequired()
                .HasDefaultValue(UnidadMedida.Pieza);
            e.Property(x => x.PrecioVenta).HasColumnName("precioVenta")
                .HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.Costo).HasColumnName("costo")
                .HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.Stock).HasColumnName("stock")
                .HasConversion(CantidadesConvertidor.Instancia).IsRequired();
            e.Property(x => x.StockMinimo).HasColumnName("stockMinimo")
                .HasConversion(CantidadesConvertidor.Instancia).IsRequired();
            e.Property(x => x.FechaArchivado).HasColumnName("fechaArchivado");
            e.Property(x => x.FechaCreacion).HasColumnName("fechaCreacion").IsRequired();
            e.Property(x => x.FechaModificacion).HasColumnName("fechaModificacion").IsRequired();

            e.HasIndex(x => x.Sku).IsUnique();
            e.HasIndex(x => x.Nombre);
            e.HasIndex(x => x.FechaArchivado);
        });

        modelBuilder.Entity<Cliente>(e =>
        {
            e.ToTable("Cliente", t =>
                t.HasCheckConstraint("CK_Cliente_limiteCredito", "limiteCredito >= 0"));
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nombre).HasColumnName("nombre")
                .HasMaxLength(100).IsRequired();
            e.Property(x => x.Telefono).HasColumnName("telefono").HasMaxLength(30);
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(120);
            e.Property(x => x.Direccion).HasColumnName("direccion").HasMaxLength(200);
            e.Property(x => x.LimiteCredito).HasColumnName("limiteCredito")
                .HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.Activo).HasColumnName("activo").IsRequired();
            e.Property(x => x.FechaCreacion).HasColumnName("fechaCreacion").IsRequired();
            e.Property(x => x.FechaModificacion).HasColumnName("fechaModificacion").IsRequired();

            e.HasIndex(x => x.Nombre);
            e.HasIndex(x => x.Activo);
        });

        modelBuilder.Entity<Proveedor>(e =>
        {
            e.ToTable("Proveedor");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nombre).HasColumnName("nombre")
                .HasMaxLength(100).IsRequired();
            e.Property(x => x.Telefono).HasColumnName("telefono")
                .HasMaxLength(30).IsRequired();
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(120);
            e.Property(x => x.Direccion).HasColumnName("direccion").HasMaxLength(200);
            e.Property(x => x.Activo).HasColumnName("activo").IsRequired();
            e.Property(x => x.FechaCreacion).HasColumnName("fechaCreacion").IsRequired();

            e.HasIndex(x => x.Telefono).IsUnique();
            e.HasIndex(x => x.Nombre);
            e.HasIndex(x => x.Activo);
        });

        modelBuilder.Entity<Compra>(e =>
        {
            e.ToTable("Compra", t =>
                t.HasCheckConstraint("CK_Compra_total", "total >= 0"));
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.IdProveedor).HasColumnName("idProveedor").IsRequired();
            e.Property(x => x.IdUsuario).HasColumnName("idUsuario").IsRequired();
            e.Property(x => x.Fecha).HasColumnName("fecha").IsRequired();
            e.Property(x => x.NumeroComprobante).HasColumnName("numeroComprobante")
                .HasMaxLength(40);
            e.Property(x => x.Total).HasColumnName("total")
                .HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.Estado).HasColumnName("estado")
                .HasConversion<string>().HasMaxLength(15).IsRequired();
            e.Property(x => x.FechaAnulacion).HasColumnName("fechaAnulacion");
            e.Property(x => x.IdUsuarioAnulacion).HasColumnName("idUsuarioAnulacion");

            e.HasOne(x => x.Proveedor)
                .WithMany(p => p.Compras)
                .HasForeignKey(x => x.IdProveedor)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Usuario)
                .WithMany(u => u.Compras)
                .HasForeignKey(x => x.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(x => x.IdUsuarioAnulacion)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.IdProveedor);
            e.HasIndex(x => x.Fecha);
            e.HasIndex(x => x.Estado);
        });

        modelBuilder.Entity<DetalleCompra>(e =>
        {
            e.ToTable("DetalleCompra", t =>
            {
                t.HasCheckConstraint("CK_DetalleCompra_cantidad", "cantidad > 0");
                t.HasCheckConstraint("CK_DetalleCompra_costoUnitario", "costoUnitario >= 0");
                t.HasCheckConstraint("CK_DetalleCompra_subtotal", "subtotal >= 0");
            });
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.IdCompra).HasColumnName("idCompra").IsRequired();
            e.Property(x => x.IdProducto).HasColumnName("idProducto").IsRequired();
            e.Property(x => x.Cantidad).HasColumnName("cantidad")
                .HasConversion(CantidadesConvertidor.Instancia).IsRequired();
            e.Property(x => x.CostoUnitario).HasColumnName("costoUnitario")
                .HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.Subtotal).HasColumnName("subtotal")
                .HasColumnType("decimal(12,2)").IsRequired();

            e.HasOne(x => x.Compra)
                .WithMany(c => c.Detalles)
                .HasForeignKey(x => x.IdCompra)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Producto)
                .WithMany(p => p.DetallesCompra)
                .HasForeignKey(x => x.IdProducto)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.IdCompra);
            e.HasIndex(x => x.IdProducto);
        });

        modelBuilder.Entity<MovimientoInventario>(e =>
        {
            e.ToTable("MovimientoInventario", t =>
            {
                t.HasCheckConstraint("CK_MovimientoInventario_cantidad", "cantidad > 0");
                t.HasCheckConstraint("CK_MovimientoInventario_stockAnterior", "stockAnterior >= 0");
                t.HasCheckConstraint("CK_MovimientoInventario_stockResultante", "stockResultante >= 0");
                t.HasCheckConstraint(
                    "CK_MovimientoInventario_origen",
                    "idVenta IS NULL OR idCompra IS NULL");
            });
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.IdProducto).HasColumnName("idProducto").IsRequired();
            e.Property(x => x.IdUsuario).HasColumnName("idUsuario").IsRequired();
            e.Property(x => x.Tipo).HasColumnName("tipo")
                .HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.Cantidad).HasColumnName("cantidad")
                .HasConversion(CantidadesConvertidor.Instancia).IsRequired();
            e.Property(x => x.StockAnterior).HasColumnName("stockAnterior")
                .HasConversion(CantidadesConvertidor.Instancia).IsRequired();
            e.Property(x => x.StockResultante).HasColumnName("stockResultante")
                .HasConversion(CantidadesConvertidor.Instancia).IsRequired();
            e.Property(x => x.Referencia).HasColumnName("referencia").HasMaxLength(100);
            e.Property(x => x.IdVenta).HasColumnName("idVenta");
            e.Property(x => x.IdCompra).HasColumnName("idCompra");
            e.Property(x => x.Fecha).HasColumnName("fecha").IsRequired();

            e.HasOne(x => x.Producto)
                .WithMany(p => p.Movimientos)
                .HasForeignKey(x => x.IdProducto)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Usuario)
                .WithMany(u => u.MovimientosInventario)
                .HasForeignKey(x => x.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Venta)
                .WithMany(v => v.Movimientos)
                .HasForeignKey(x => x.IdVenta)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Compra)
                .WithMany(c => c.Movimientos)
                .HasForeignKey(x => x.IdCompra)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.IdProducto, x.Fecha });
            e.HasIndex(x => x.Fecha);
            e.HasIndex(x => x.IdVenta);
            e.HasIndex(x => x.IdCompra);
        });

        modelBuilder.Entity<Caja>(e =>
        {
            e.ToTable("Caja", t =>
            {
                t.HasCheckConstraint("CK_Caja_montoInicial", "montoInicial >= 0");
                t.HasCheckConstraint("CK_Caja_montoEsperado", "montoEsperado IS NULL OR montoEsperado >= 0");
                t.HasCheckConstraint("CK_Caja_montoFinal", "montoFinal IS NULL OR montoFinal >= 0");
            });
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.IdUsuarioApertura).HasColumnName("idUsuarioApertura").IsRequired();
            e.Property(x => x.FechaApertura).HasColumnName("fechaApertura").IsRequired();
            e.Property(x => x.MontoInicial).HasColumnName("montoInicial")
                .HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.IdUsuarioCierre).HasColumnName("idUsuarioCierre");
            e.Property(x => x.FechaCierre).HasColumnName("fechaCierre");
            e.Property(x => x.MontoEsperado).HasColumnName("montoEsperado")
                .HasColumnType("decimal(12,2)");
            e.Property(x => x.MontoFinal).HasColumnName("montoFinal")
                .HasColumnType("decimal(12,2)");
            e.Property(x => x.Diferencia).HasColumnName("diferencia")
                .HasColumnType("decimal(12,2)");
            e.Property(x => x.Estado).HasColumnName("estado")
                .HasConversion<string>().HasMaxLength(10).IsRequired();
            e.Property(x => x.Observaciones).HasColumnName("observaciones")
                .HasMaxLength(300);

            e.HasOne(x => x.UsuarioApertura)
                .WithMany(u => u.Cajas)
                .HasForeignKey(x => x.IdUsuarioApertura)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(x => x.IdUsuarioCierre)
                .OnDelete(DeleteBehavior.Restrict);

            // Garantiza una sola caja abierta a la vez.
            e.HasIndex(x => x.Estado).IsUnique()
                .HasFilter("estado = 'Abierta'");
            e.HasIndex(x => x.FechaApertura);
        });

        modelBuilder.Entity<MovimientoCaja>(e =>
        {
            e.ToTable("MovimientoCaja", t =>
                t.HasCheckConstraint("CK_MovimientoCaja_monto", "monto > 0"));
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.IdCaja).HasColumnName("idCaja").IsRequired();
            e.Property(x => x.IdUsuario).HasColumnName("idUsuario").IsRequired();
            e.Property(x => x.Tipo).HasColumnName("tipo")
                .HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.Monto).HasColumnName("monto")
                .HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.Descripcion).HasColumnName("descripcion")
                .HasMaxLength(200);
            e.Property(x => x.IdVenta).HasColumnName("idVenta");
            e.Property(x => x.Fecha).HasColumnName("fecha").IsRequired();

            e.HasOne(x => x.Caja)
                .WithMany(c => c.Movimientos)
                .HasForeignKey(x => x.IdCaja)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Usuario)
                .WithMany(u => u.MovimientosCaja)
                .HasForeignKey(x => x.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Venta)
                .WithMany(v => v.MovimientosCaja)
                .HasForeignKey(x => x.IdVenta)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.IdCaja, x.Fecha });
            e.HasIndex(x => x.IdVenta);
        });

        modelBuilder.Entity<Venta>(e =>
        {
            e.ToTable("Venta", t =>
            {
                t.HasCheckConstraint("CK_Venta_subtotal", "subtotal >= 0");
                t.HasCheckConstraint("CK_Venta_descuento", "descuento >= 0");
                t.HasCheckConstraint("CK_Venta_total", "total >= 0");
                t.HasCheckConstraint("CK_Venta_totalDevuelto", "totalDevuelto >= 0 AND totalDevuelto <= total");
                t.HasCheckConstraint("CK_Venta_montoRecibido", "montoRecibido >= total");
                t.HasCheckConstraint("CK_Venta_vuelto", "vuelto >= 0");
            });
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.IdUsuario).HasColumnName("idUsuario").IsRequired();
            e.Property(x => x.IdCaja).HasColumnName("idCaja").IsRequired();
            e.Property(x => x.IdCliente).HasColumnName("idCliente");
            e.Property(x => x.TipoVenta).HasColumnName("tipoVenta")
                .HasConversion<string>().HasMaxLength(15).IsRequired()
                .HasDefaultValue(TipoVenta.Contado);
            e.Property(x => x.Fecha).HasColumnName("fecha").IsRequired();
            e.Property(x => x.Subtotal).HasColumnName("subtotal")
                .HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.Descuento).HasColumnName("descuento")
                .HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.Total).HasColumnName("total")
                .HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.TotalDevuelto).HasColumnName("totalDevuelto")
                .HasColumnType("decimal(12,2)").IsRequired().HasDefaultValue(0m);
            e.Property(x => x.MetodoPago).HasColumnName("metodoPago")
                .HasConversion<string>().HasMaxLength(15).IsRequired();
            e.Property(x => x.MontoRecibido).HasColumnName("montoRecibido")
                .HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.Vuelto).HasColumnName("vuelto")
                .HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.Estado).HasColumnName("estado")
                .HasConversion<string>().HasMaxLength(15).IsRequired();
            e.Property(x => x.FechaAnulacion).HasColumnName("fechaAnulacion");
            e.Property(x => x.IdUsuarioAnulacion).HasColumnName("idUsuarioAnulacion");

            e.HasOne(x => x.Usuario)
                .WithMany(u => u.Ventas)
                .HasForeignKey(x => x.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Caja)
                .WithMany(c => c.Ventas)
                .HasForeignKey(x => x.IdCaja)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Cliente)
                .WithMany(c => c.Ventas)
                .HasForeignKey(x => x.IdCliente)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(x => x.IdUsuarioAnulacion)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.Fecha);
            e.HasIndex(x => x.IdCaja);
            e.HasIndex(x => x.Estado);
            e.HasIndex(x => x.IdUsuario);
            e.HasIndex(x => x.IdCliente);
            e.HasIndex(x => x.TipoVenta);
        });

        modelBuilder.Entity<Abono>(e =>
        {
            e.ToTable("Abono", t =>
            {
                t.HasCheckConstraint("CK_Abono_monto", "monto > 0");
                t.HasCheckConstraint(
                    "CK_Abono_metodo",
                    "metodo IN ('Efectivo', 'Tarjeta', 'Transferencia')");
            });
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.IdCliente).HasColumnName("idCliente").IsRequired();
            e.Property(x => x.IdVenta).HasColumnName("idVenta").IsRequired();
            e.Property(x => x.Monto).HasColumnName("monto")
                .HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.Metodo).HasColumnName("metodo")
                .HasConversion<string>().HasMaxLength(15).IsRequired();
            e.Property(x => x.Referencia).HasColumnName("referencia").HasMaxLength(100);
            e.Property(x => x.Fecha).HasColumnName("fecha").IsRequired();
            e.Property(x => x.IdUsuario).HasColumnName("idUsuario").IsRequired();

            e.HasOne(x => x.Cliente)
                .WithMany(c => c.Abonos)
                .HasForeignKey(x => x.IdCliente)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Venta)
                .WithMany(v => v.Abonos)
                .HasForeignKey(x => x.IdVenta)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Usuario)
                .WithMany(u => u.Abonos)
                .HasForeignKey(x => x.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.IdCliente, x.Fecha });
            e.HasIndex(x => x.IdVenta);
        });

        modelBuilder.Entity<PagoVenta>(e =>
        {
            e.ToTable("PagoVenta", t =>
            {
                t.HasCheckConstraint("CK_PagoVenta_monto", "monto > 0");
                t.HasCheckConstraint(
                    "CK_PagoVenta_montoDevuelto",
                    "montoDevuelto >= 0 AND montoDevuelto <= monto");
                t.HasCheckConstraint(
                    "CK_PagoVenta_metodo",
                    "metodo IN ('Efectivo', 'Tarjeta', 'Transferencia')");
            });
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.IdVenta).HasColumnName("idVenta").IsRequired();
            e.Property(x => x.Metodo).HasColumnName("metodo")
                .HasConversion<string>().HasMaxLength(15).IsRequired();
            e.Property(x => x.Monto).HasColumnName("monto")
                .HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.MontoDevuelto).HasColumnName("montoDevuelto")
                .HasColumnType("decimal(12,2)").IsRequired().HasDefaultValue(0m);

            e.HasOne(x => x.Venta)
                .WithMany(v => v.Pagos)
                .HasForeignKey(x => x.IdVenta)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.IdVenta);
        });

        modelBuilder.Entity<DetalleVenta>(e =>
        {
            e.ToTable("DetalleVenta", t =>
            {
                t.HasCheckConstraint("CK_DetalleVenta_cantidad", "cantidad > 0");
                t.HasCheckConstraint("CK_DetalleVenta_cantidadDevuelta", "cantidadDevuelta >= 0 AND cantidadDevuelta <= cantidad");
                t.HasCheckConstraint("CK_DetalleVenta_precioUnitario", "precioUnitario >= 0");
                t.HasCheckConstraint("CK_DetalleVenta_costoUnitario", "costoUnitario >= 0");
                t.HasCheckConstraint("CK_DetalleVenta_subtotal", "subtotal >= 0");
            });
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.IdVenta).HasColumnName("idVenta").IsRequired();
            e.Property(x => x.IdProducto).HasColumnName("idProducto").IsRequired();
            e.Property(x => x.Cantidad).HasColumnName("cantidad")
                .HasConversion(CantidadesConvertidor.Instancia).IsRequired();
            e.Property(x => x.CantidadDevuelta).HasColumnName("cantidadDevuelta")
                .HasConversion(CantidadesConvertidor.Instancia)
                .IsRequired().HasDefaultValue(0m);
            e.Property(x => x.PrecioUnitario).HasColumnName("precioUnitario")
                .HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.CostoUnitario).HasColumnName("costoUnitario")
                .HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.Subtotal).HasColumnName("subtotal")
                .HasColumnType("decimal(12,2)").IsRequired();

            e.HasOne(x => x.Venta)
                .WithMany(v => v.Detalles)
                .HasForeignKey(x => x.IdVenta)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Producto)
                .WithMany(p => p.DetallesVenta)
                .HasForeignKey(x => x.IdProducto)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.IdVenta);
            e.HasIndex(x => x.IdProducto);
        });
    }
}

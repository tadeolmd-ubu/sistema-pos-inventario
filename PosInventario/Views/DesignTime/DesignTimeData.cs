using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.ViewModels;

namespace PosInventario.Views.DesignTime;

public sealed class DesignTimeData
{
    public static readonly InicioDesignTime Inicio = new();
    public static readonly ProductosDesignTime Productos = new();
    public static readonly ProveedoresDesignTime Proveedores = new();
    public static readonly InventarioDesignTime Inventario = new();
    public static readonly ComprasDesignTime Compras = new();
    public static readonly UsuariosDesignTime Usuarios = new();
}

public sealed class InicioDesignTime : InicioViewModel
{
}

public sealed class ProductosDesignTime : ProductosViewModel
{
    public ProductosDesignTime()
    {
        var producto = new Producto
        {
            Id = 1,
            Sku = "ABC-001",
            Nombre = "Producto de ejemplo",
            Descripcion = "Dato de diseño, no viene de la base.",
            PrecioVenta = 1500.00m,
            Costo = 900.00m,
            Stock = 12,
            StockMinimo = 5,
            FechaCreacion = DateTime.Now,
            FechaModificacion = DateTime.Now
        };

        ProductosItems.Add(producto);
        SelectedProducto = producto;
    }
}

public sealed class ProveedoresDesignTime : ProveedoresViewModel
{
    public ProveedoresDesignTime()
    {
        var activo = new Proveedor
        {
            Id = 1,
            Nombre = "Distribuidora Central",
            Telefono = "11 5555-1000",
            Email = "ventas@central.com",
            Direccion = "Av. Siempre Viva 123",
            Activo = 1,
            FechaCreacion = DateTime.Now
        };

        var inactivo = new Proveedor
        {
            Id = 2,
            Nombre = "Proveedor Histórico",
            Telefono = "11 5555-2000",
            Activo = 0,
            FechaCreacion = DateTime.Now
        };

        ProveedoresItems.Add(activo);
        ProveedoresItems.Add(inactivo);
        SelectedProveedor = activo;
    }
}

public sealed class InventarioDesignTime : InventarioViewModel
{
    public InventarioDesignTime()
    {
        var producto = new Producto
        {
            Id = 1,
            Sku = "ABC-001",
            Nombre = "Producto de ejemplo",
            PrecioVenta = 1500.00m,
            Costo = 900.00m,
            Stock = 12,
            StockMinimo = 5,
            FechaCreacion = DateTime.Now,
            FechaModificacion = DateTime.Now
        };

        var usuario = new Usuario
        {
            Id = 1,
            NombreUsuario = "admin",
            Nombre = "Administrador General",
            Rol = RolUsuario.Administrador,
            Activo = 1,
            FechaCreacion = DateTime.Now
        };

        ProductosItems.Add(producto);
        SelectedProducto = producto;

        MovimientosItems.Add(new MovimientoInventario
        {
            Id = 1,
            IdProducto = producto.Id,
            IdUsuario = usuario.Id,
            Producto = producto,
            Usuario = usuario,
            Tipo = TipoMovimiento.Entrada,
            Cantidad = 12,
            StockAnterior = 0,
            StockResultante = 12,
            Referencia = "Carga inicial",
            Fecha = DateTime.Now
        });
    }
}

public sealed class ComprasDesignTime : ComprasViewModel
{
    public ComprasDesignTime()
    {
        var proveedor = new Proveedor
        {
            Id = 1,
            Nombre = "Distribuidora Central",
            Telefono = "11 5555-1000",
            Email = "ventas@central.com",
            Activo = 1,
            FechaCreacion = DateTime.Now
        };

        var producto = new Producto
        {
            Id = 1,
            Sku = "ABC-001",
            Nombre = "Producto de ejemplo",
            PrecioVenta = 1500.00m,
            Costo = 900.00m,
            Stock = 12,
            StockMinimo = 5,
            FechaCreacion = DateTime.Now,
            FechaModificacion = DateTime.Now
        };

        var usuario = new Usuario
        {
            Id = 1,
            NombreUsuario = "admin",
            Nombre = "Administrador General",
            Rol = RolUsuario.Administrador,
            Activo = 1,
            FechaCreacion = DateTime.Now
        };

        var compra = new Compra
        {
            Id = 1,
            IdProveedor = proveedor.Id,
            Proveedor = proveedor,
            IdUsuario = usuario.Id,
            Usuario = usuario,
            Fecha = DateTime.Now,
            NumeroComprobante = "FAC-001",
            Total = 1800.00m,
            Estado = EstadoCompra.Completada
        };

        compra.Detalles.Add(new DetalleCompra
        {
            Id = 1,
            IdCompra = compra.Id,
            IdProducto = producto.Id,
            Producto = producto,
            Cantidad = 2,
            CostoUnitario = 900.00m,
            Subtotal = 1800.00m
        });

        ProveedoresItems.Add(proveedor);
        ProductosItems.Add(producto);
        ComprasItems.Add(compra);

        SelectedProveedor = proveedor;
        SelectedProducto = producto;
        SelectedCompra = compra;

        LineasItems.Add(new LineaCompraItem(producto, 2, 900.00m));
    }
}

public sealed class UsuariosDesignTime : UsuariosViewModel
{
    public UsuariosDesignTime()
    {
        var admin = new Usuario
        {
            Id = 1,
            NombreUsuario = "admin",
            Nombre = "Administrador General",
            Rol = RolUsuario.Administrador,
            Activo = 1,
            FechaCreacion = DateTime.Now
        };

        var cajero = new Usuario
        {
            Id = 2,
            NombreUsuario = "laura",
            Nombre = "Laura Gómez",
            Rol = RolUsuario.Cajero,
            Activo = 1,
            FechaCreacion = DateTime.Now
        };

        UsuariosItems.Add(admin);
        UsuariosItems.Add(cajero);
        SelectedUsuario = cajero;
    }
}

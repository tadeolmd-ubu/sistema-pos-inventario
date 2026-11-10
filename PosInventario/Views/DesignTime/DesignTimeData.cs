using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.ViewModels;

namespace PosInventario.Views.DesignTime;

public sealed class DesignTimeData
{
    public static readonly InicioDesignTime Inicio = new();
    public static readonly ProductosDesignTime Productos = new();
    public static readonly ProveedoresDesignTime Proveedores = new();
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

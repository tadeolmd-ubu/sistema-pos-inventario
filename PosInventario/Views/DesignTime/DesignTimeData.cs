using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.ViewModels;

namespace PosInventario.Views.DesignTime;

public sealed class DesignTimeData
{
    public static readonly InicioDesignTime Inicio = new();
    public static readonly CajaDesignTime Caja = new();
    public static readonly ProductosDesignTime Productos = new();
    public static readonly ProveedoresDesignTime Proveedores = new();
    public static readonly InventarioDesignTime Inventario = new();
    public static readonly ComprasDesignTime Compras = new();
    public static readonly VentasDesignTime Ventas = new();
    public static readonly UsuariosDesignTime Usuarios = new();
    public static readonly ConfiguracionDesignTime Configuracion = new();
}

public sealed class ConfiguracionDesignTime : ConfiguracionViewModel
{
    public ConfiguracionDesignTime()
    {
        RutaBaseDatos = "/home/usuario/.local/share/SistemaInventario/SistemaInventario.db";
        CarpetaSugerida = "/home/usuario/.local/share/SistemaInventario/Respaldos";
        CarpetaDestinoRespaldo =
            "/home/usuario/.local/share/SistemaInventario/Respaldos";
        UltimoRespaldoTexto =
            "/home/usuario/.local/share/SistemaInventario/Respaldos/respaldo_20261110_101500_abcd.db";
        RespaldosAutomaticos = true;
        RetencionDiasSeleccionados = 30;
    }
}

public sealed class InicioDesignTime : InicioViewModel
{
    public InicioDesignTime()
    {
        EsAdministrador = true;
        VentasDelDia = 12;
        TotalDelDia = 15400.00m;
        TicketPromedio = 1283.33m;
        TotalEfectivo = 8200.00m;
        TotalTarjeta = 5200.00m;
        TotalTransferencia = 2000.00m;
        CajaAbierta = true;
        EstadoCajaTexto = "Abierta";
        UsuarioCajaTexto = "Administrador General";
        FechaAperturaCajaTexto = DateTime.Now.AddHours(-3).ToString("dd/MM/yyyy HH:mm");
        EfectivoEsperado = 9200.00m;
        ProductosActivos = 25;
        ProductosStockBajo = 2;

        var usuario = new Usuario
        {
            Id = 1,
            NombreUsuario = "admin",
            Nombre = "Administrador General",
            Rol = RolUsuario.Administrador,
            Activo = 1,
            FechaCreacion = DateTime.Now
        };

        UltimasVentas.Add(new Venta
        {
            Id = 12,
            IdUsuario = usuario.Id,
            Usuario = usuario,
            IdCaja = 1,
            Fecha = DateTime.Now.AddMinutes(-15),
            Subtotal = 450.00m,
            Total = 450.00m,
            MetodoPago = MetodoPago.Efectivo,
            MontoRecibido = 500.00m,
            Vuelto = 50.00m,
            Estado = EstadoVenta.Completada
        });

        UltimasVentas.Add(new Venta
        {
            Id = 11,
            IdUsuario = usuario.Id,
            Usuario = usuario,
            IdCaja = 1,
            Fecha = DateTime.Now.AddMinutes(-40),
            Subtotal = 250.00m,
            Total = 250.00m,
            MetodoPago = MetodoPago.Tarjeta,
            MontoRecibido = 250.00m,
            Estado = EstadoVenta.Completada
        });

        UltimasVentas.Add(new Venta
        {
            Id = 10,
            IdUsuario = usuario.Id,
            Usuario = usuario,
            IdCaja = 1,
            Fecha = DateTime.Now.AddMinutes(-70),
            Subtotal = 100.00m,
            Total = 100.00m,
            MetodoPago = MetodoPago.Efectivo,
            MontoRecibido = 100.00m,
            Estado = EstadoVenta.Anulada
        });

        StockBajo.Add(new Producto
        {
            Id = 1,
            Sku = "ABC-001",
            Nombre = "Producto con stock bajo",
            PrecioVenta = 150.00m,
            Costo = 90.00m,
            Stock = 2,
            StockMinimo = 5,
            FechaCreacion = DateTime.Now,
            FechaModificacion = DateTime.Now
        });

        StockBajo.Add(new Producto
        {
            Id = 2,
            Sku = "ABC-002",
            Nombre = "Otro producto bajo",
            PrecioVenta = 80.00m,
            Costo = 40.00m,
            Stock = 0,
            StockMinimo = 3,
            FechaCreacion = DateTime.Now,
            FechaModificacion = DateTime.Now
        });

        var proveedor = new Proveedor
        {
            Id = 1,
            Nombre = "Distribuidora Central",
            Telefono = "11 5555-1000",
            Activo = 1,
            FechaCreacion = DateTime.Now
        };

        UltimasCompras.Add(new Compra
        {
            Id = 1,
            IdProveedor = proveedor.Id,
            Proveedor = proveedor,
            IdUsuario = usuario.Id,
            Usuario = usuario,
            Fecha = DateTime.Now.AddDays(-1),
            NumeroComprobante = "FAC-001",
            Total = 1800.00m,
            Estado = EstadoCompra.Completada
        });
    }
}

public sealed class CajaDesignTime : CajaViewModel
{
    public CajaDesignTime()
    {
        var usuario = new Usuario
        {
            Id = 1,
            NombreUsuario = "admin",
            Nombre = "Administrador General",
            Rol = RolUsuario.Administrador,
            Activo = 1,
            FechaCreacion = DateTime.Now
        };

        var caja = new Caja
        {
            Id = 1,
            IdUsuarioApertura = usuario.Id,
            UsuarioApertura = usuario,
            FechaApertura = DateTime.Now.AddHours(-2),
            MontoInicial = 1000.00m,
            Estado = EstadoCaja.Abierta
        };

        CajaAbierta = caja;
        MontoEsperado = 1300.00m;

        MovimientosItems.Add(new MovimientoCaja
        {
            Id = 1,
            IdCaja = caja.Id,
            IdUsuario = usuario.Id,
            Usuario = usuario,
            Tipo = TipoMovimientoCaja.Ingreso,
            Monto = 500.00m,
            Descripcion = "Fondo adicional",
            Fecha = DateTime.Now.AddHours(-1)
        });

        MovimientosItems.Add(new MovimientoCaja
        {
            Id = 2,
            IdCaja = caja.Id,
            IdUsuario = usuario.Id,
            Usuario = usuario,
            Tipo = TipoMovimientoCaja.Retiro,
            Monto = 200.00m,
            Descripcion = "Retiro parcial",
            Fecha = DateTime.Now.AddMinutes(-30)
        });

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

        LineasVentaItems.Add(new LineaVentaItem(
            producto, 2, _ => { }, _ => { }, _ => { }));

        MetodoPagoSeleccionado = MetodoPago.Efectivo;
        MontoEfectivoTexto = "3500.00";

        HistorialItems.Add(new Caja
        {
            Id = 2,
            IdUsuarioApertura = usuario.Id,
            UsuarioApertura = usuario,
            FechaApertura = DateTime.Now.AddDays(-1),
            FechaCierre = DateTime.Now.AddDays(-1).AddHours(8),
            MontoInicial = 800.00m,
            MontoEsperado = 1200.00m,
            MontoFinal = 1180.00m,
            Diferencia = -20.00m,
            Estado = EstadoCaja.Cerrada
        });
    }
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

        Paginacion.EstablecerTotal(MovimientosItems.Count);
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
        Paginacion.EstablecerTotal(ComprasItems.Count);
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

public sealed class VentasDesignTime : VentasViewModel
{
    public VentasDesignTime()
    {
        var usuario = new Usuario
        {
            Id = 1,
            NombreUsuario = "admin",
            Nombre = "Administrador General",
            Rol = RolUsuario.Administrador,
            Activo = 1,
            FechaCreacion = DateTime.Now
        };

        VentasItems.Add(new Venta
        {
            Id = 32,
            IdUsuario = usuario.Id,
            Usuario = usuario,
            IdCaja = 1,
            Fecha = DateTime.Now.AddHours(-1),
            Subtotal = 900.00m,
            Descuento = 50.00m,
            Total = 850.00m,
            MetodoPago = MetodoPago.Efectivo,
            MontoRecibido = 1000.00m,
            Vuelto = 150.00m,
            Estado = EstadoVenta.Completada
        });

        VentasItems.Add(new Venta
        {
            Id = 31,
            IdUsuario = usuario.Id,
            Usuario = usuario,
            IdCaja = 1,
            Fecha = DateTime.Now.AddHours(-3),
            Subtotal = 250.00m,
            Total = 250.00m,
            MetodoPago = MetodoPago.Tarjeta,
            MontoRecibido = 250.00m,
            Estado = EstadoVenta.Anulada
        });

        SelectedVenta = VentasItems[0];
        Paginacion.EstablecerTotal(VentasItems.Count);
    }
}

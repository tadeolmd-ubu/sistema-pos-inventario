using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models;
using PosInventario.Models.Enums;

namespace PosInventario.Service;

public class VentaService
{
    private readonly AppDbContext _db;
    private readonly SesionActual _sesion;

    public VentaService(AppDbContext db, SesionActual sesion)
    {
        _db = db;
        _sesion = sesion;
    }

    public async Task<Resultado<Venta>> RegistrarAsync(
        IReadOnlyList<LineaVenta> lineas,
        MetodoPago metodoPago,
        decimal montoRecibido,
        decimal descuento = 0m)
    {
        if (!_sesion.Iniciada)
            return new Resultado<Venta>(false, "La sesión no está iniciada.", null);

        if (lineas is null || lineas.Count == 0)
            return new Resultado<Venta>(false, "Agregá al menos un producto.", null);

        var caja = await _db.Cajas
            .FirstOrDefaultAsync(c => c.Estado == EstadoCaja.Abierta);

        if (caja is null)
            return new Resultado<Venta>(
                false, "No hay una caja abierta para cobrar.", null);

        if (!_sesion.EsAdministrador && caja.IdUsuarioApertura != _sesion.IdUsuario)
            return new Resultado<Venta>(
                false, "La caja está abierta por otro usuario.", null);

        if (lineas.Select(l => l.IdProducto).Distinct().Count() != lineas.Count)
            return new Resultado<Venta>(
                false, "Hay productos repetidos en la venta.", null);

        foreach (var linea in lineas)
        {
            if (linea.Cantidad <= 0)
                return new Resultado<Venta>(
                    false, "La cantidad debe ser mayor que 0.", null);
        }

        var idsProductos = lineas.Select(l => l.IdProducto).ToList();

        var productos = await _db.Productos
            .Where(p => idsProductos.Contains(p.Id))
            .ToListAsync();

        if (productos.Count != idsProductos.Count)
            return new Resultado<Venta>(
                false, "Alguno de los productos no existe.", null);

        if (productos.Any(p => p.FechaArchivado is not null))
            return new Resultado<Venta>(
                false, "No se pueden vender productos archivados.", null);

        var subtotal = 0m;

        foreach (var linea in lineas)
        {
            var producto = productos.First(p => p.Id == linea.IdProducto);

            if (producto.Stock < linea.Cantidad)
                return new Resultado<Venta>(
                    false, $"No hay stock suficiente de {producto.Nombre}.", null);

            subtotal += producto.PrecioVenta * linea.Cantidad;
        }

        if (descuento < 0)
            return new Resultado<Venta>(
                false, "El descuento no puede ser negativo.", null);

        if (descuento > subtotal)
            return new Resultado<Venta>(
                false, "El descuento no puede superar el subtotal.", null);

        var total = subtotal - descuento;

        decimal vuelto;

        if (metodoPago == MetodoPago.Efectivo)
        {
            if (montoRecibido < total)
                return new Resultado<Venta>(
                    false, "El monto recibido es menor al total de la venta.", null);

            vuelto = montoRecibido - total;
        }
        else
        {
            montoRecibido = total;
            vuelto = 0m;
        }

        var momento = DateTime.Now;

        var venta = new Venta
        {
            IdUsuario = _sesion.IdUsuario,
            Usuario = _sesion.Usuario!,
            IdCaja = caja.Id,
            Fecha = momento,
            Subtotal = subtotal,
            Descuento = descuento,
            Total = total,
            MetodoPago = metodoPago,
            MontoRecibido = montoRecibido,
            Vuelto = vuelto,
            Estado = EstadoVenta.Completada
        };

        foreach (var linea in lineas)
        {
            var producto = productos.First(p => p.Id == linea.IdProducto);
            var precioUnitario = producto.PrecioVenta;
            var subtotalLinea = precioUnitario * linea.Cantidad;

            var stockAnterior = producto.Stock;
            producto.Stock -= linea.Cantidad;
            producto.FechaModificacion = momento;

            venta.Detalles.Add(new DetalleVenta
            {
                IdProducto = producto.Id,
                Producto = producto,
                Cantidad = linea.Cantidad,
                PrecioUnitario = precioUnitario,
                Subtotal = subtotalLinea
            });

            venta.Movimientos.Add(new MovimientoInventario
            {
                IdProducto = producto.Id,
                IdUsuario = _sesion.IdUsuario,
                Tipo = TipoMovimiento.Venta,
                Cantidad = linea.Cantidad,
                StockAnterior = stockAnterior,
                StockResultante = producto.Stock,
                Referencia = "Venta",
                Fecha = momento,
                Venta = venta
            });
        }

        // Solo el efectivo entra físicamente a la caja.
        if (metodoPago == MetodoPago.Efectivo && total > 0)
        {
            venta.MovimientosCaja.Add(new MovimientoCaja
            {
                IdCaja = caja.Id,
                IdUsuario = _sesion.IdUsuario,
                Tipo = TipoMovimientoCaja.Venta,
                Monto = total,
                Descripcion = "Venta",
                Fecha = momento,
                Venta = venta
            });
        }

        _db.Ventas.Add(venta);
        await _db.SaveChangesAsync();

        var mensaje = "Venta registrada.";

        return new Resultado<Venta>(true, mensaje, venta);
    }
}

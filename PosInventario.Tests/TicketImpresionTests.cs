using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;
using PosInventario.ViewModels;

namespace PosInventario.Tests;

public class TicketImpresionTests
{
    private static Venta CrearVenta(
        decimal descuento = 0m,
        MetodoPago metodoPago = MetodoPago.Efectivo,
        decimal totalDevuelto = 0m,
        EstadoVenta estado = EstadoVenta.Completada)
    {
        var usuario = new Usuario
        {
            Id = 1,
            NombreUsuario = "ana",
            Nombre = "Ana López"
        };

        var venta = new Venta
        {
            Id = 12,
            IdUsuario = usuario.Id,
            Usuario = usuario,
            Fecha = new DateTime(2026, 11, 11, 18, 30, 0),
            Subtotal = 51m,
            Descuento = descuento,
            Total = 51m - descuento,
            TotalDevuelto = totalDevuelto,
            MetodoPago = metodoPago,
            MontoRecibido = metodoPago == MetodoPago.Efectivo ? 60m : 0m,
            Vuelto = metodoPago == MetodoPago.Efectivo ? 9m + descuento : 0m,
            Estado = estado
        };

        var coca = new Producto { Id = 1, Nombre = "Coca Cola 600 ml" };
        venta.Detalles.Add(new DetalleVenta
        {
            Id = 1,
            IdVenta = venta.Id,
            IdProducto = coca.Id,
            Producto = coca,
            Cantidad = 2,
            PrecioUnitario = 18m,
            Subtotal = 36m
        });

        var sabritas = new Producto { Id = 2, Nombre = "Sabritas Original" };
        venta.Detalles.Add(new DetalleVenta
        {
            Id = 2,
            IdVenta = venta.Id,
            IdProducto = sabritas.Id,
            Producto = sabritas,
            Cantidad = 1,
            PrecioUnitario = 15m,
            Subtotal = 15m
        });

        return venta;
    }

    private sealed class ImpresoraFalsa : IImpresoraTicket
    {
        private readonly Resultado _resultado;

        public ImpresoraFalsa(Resultado resultado) => _resultado = resultado;

        public string? Contenido { get; private set; }
        public AnchoTicket? Ancho { get; private set; }

        public Task<Resultado> ImprimirAsync(string contenido, AnchoTicket ancho)
        {
            Contenido = contenido;
            Ancho = ancho;

            return Task.FromResult(_resultado);
        }
    }

    [Fact]
    public void GeneraTicketDe58MmDentroDelAncho()
    {
        var texto = GeneradorTicketTexto.Generar(
            CrearVenta(), AnchoTicket.Mm58);

        var lineas = texto.Split('\n');

        Assert.All(lineas, linea => Assert.True(
            linea.Length <= 32, $"Línea demasiado larga: '{linea}'"));

        Assert.Contains(lineas, l => l.Contains("SISTEMA DE INVENTARIO"));
        Assert.Contains(lineas, l => l.Contains("V-000012"));
        Assert.Contains(lineas, l => l.Contains("Coca Cola 600 ml"));
        Assert.Contains(lineas, l => l.Contains("TOTAL") && l.EndsWith("51.00"));
    }

    [Fact]
    public void GeneraTicketDe80MmDentroDelAncho()
    {
        var texto = GeneradorTicketTexto.Generar(
            CrearVenta(), AnchoTicket.Mm80);

        var lineas = texto.Split('\n');

        Assert.All(lineas, linea => Assert.True(
            linea.Length <= 48, $"Línea demasiado larga: '{linea}'"));

        Assert.Contains(lineas, l => l.Contains("TOTAL") && l.EndsWith("51.00"));
    }

    [Fact]
    public void ElPapelDe80MmAprovechaMasColumnas()
    {
        var venta = CrearVenta();
        var nombreLargo = new string('A', 40);
        venta.Detalles.First().Producto.Nombre = nombreLargo;

        var texto58 = GeneradorTicketTexto.Generar(venta, AnchoTicket.Mm58);
        var texto80 = GeneradorTicketTexto.Generar(venta, AnchoTicket.Mm80);

        Assert.DoesNotContain(nombreLargo, texto58);
        Assert.Contains(nombreLargo, texto80);
    }

    [Fact]
    public void IncluyeDescuentoEfectivoYCambio()
    {
        var texto = GeneradorTicketTexto.Generar(
            CrearVenta(descuento: 5m), AnchoTicket.Mm80);

        Assert.Contains("Descuento", texto);
        Assert.Contains("46.00", texto);
        Assert.Contains("Paga con", texto);
        Assert.Contains("Cambio", texto);
    }

    [Fact]
    public void NoIncluyeLineasDeEfectivoSiEsTarjeta()
    {
        var texto = GeneradorTicketTexto.Generar(
            CrearVenta(metodoPago: MetodoPago.Tarjeta), AnchoTicket.Mm58);

        Assert.DoesNotContain("Paga con", texto);
        Assert.DoesNotContain("Cambio", texto);
    }

    [Fact]
    public void MarcaVentaAnuladaYConDevoluciones()
    {
        var texto = GeneradorTicketTexto.Generar(
            CrearVenta(totalDevuelto: 20m, estado: EstadoVenta.Anulada),
            AnchoTicket.Mm58);

        Assert.Contains("ANULADA", texto);
        Assert.Contains("Devuelto", texto);
        Assert.Contains("Total neto", texto);
        Assert.Contains("31.00", texto);
    }

    [Fact]
    public void ParteNombresLargosSinExcederElAncho()
    {
        var venta = CrearVenta();
        venta.Detalles.First().Producto.Nombre =
            "Producto con un nombre extraordinariamente largo que no cabe";

        var texto = GeneradorTicketTexto.Generar(venta, AnchoTicket.Mm58);

        Assert.All(texto.Split('\n'), linea => Assert.True(linea.Length <= 32));
    }

    [Fact]
    public void ParteElNombreDelCajeroSinExcederElAncho()
    {
        var venta = CrearVenta();
        venta.Usuario.Nombre = "María de los Ángeles Fernández del Valle";

        var texto = GeneradorTicketTexto.Generar(venta, AnchoTicket.Mm58);

        Assert.All(texto.Split('\n'), linea => Assert.True(linea.Length <= 32));
    }

    [Fact]
    public void SePuedeCrearElViewModelConLaImpresoraDelSistema()
    {
        var vm = new TicketViewModel(CrearVenta());

        Assert.Equal(AnchoTicket.Mm80, vm.AnchoSeleccionado.Ancho);
        Assert.False(vm.HayMensajeImpresion);
    }

    [Fact]
    public async Task ImprimirUsaElAnchoSeleccionadoYElContenidoGenerado()
    {
        var impresora = new ImpresoraFalsa(
            new Resultado(true, "Ticket enviado a la impresora."));

        var vm = new TicketViewModel(CrearVenta(), impresora)
        {
            AnchoSeleccionado = new AnchoTicketOpcion(AnchoTicket.Mm58)
        };

        await vm.ImprimirCommand.ExecuteAsync(null);

        Assert.True(vm.ImpresionCorrecta);
        Assert.True(vm.EsMensajeExito);
        Assert.False(vm.Imprimiendo);
        Assert.Equal(AnchoTicket.Mm58, impresora.Ancho);
        Assert.Contains("V-000012", impresora.Contenido);
        Assert.Equal("Ticket enviado a la impresora.", vm.MensajeImpresion);
    }

    [Fact]
    public async Task ErrorDeImpresionSoloAvisaYNoLanza()
    {
        var impresora = new ImpresoraFalsa(
            new Resultado(false, "No se pudo imprimir el ticket."));

        var vm = new TicketViewModel(CrearVenta(), impresora);

        await vm.ImprimirCommand.ExecuteAsync(null);

        Assert.False(vm.ImpresionCorrecta);
        Assert.True(vm.EsMensajeError);
        Assert.False(vm.Imprimiendo);
        Assert.Equal("No se pudo imprimir el ticket.", vm.MensajeImpresion);
    }

    [Fact]
    public async Task ExcepcionDeLaImpresoraSeConvierteEnAviso()
    {
        var impresora = new ImpresoraSistema(
            (_, _) => throw new InvalidOperationException("sin impresora"));

        var vm = new TicketViewModel(CrearVenta(), impresora);

        await vm.ImprimirCommand.ExecuteAsync(null);

        Assert.False(vm.ImpresionCorrecta);
        Assert.True(vm.EsMensajeError);
        Assert.False(vm.Imprimiendo);
        Assert.Contains("No se pudo imprimir", vm.MensajeImpresion);
    }
}

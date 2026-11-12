using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;
using PosInventario.ViewModels;

namespace PosInventario.Tests;

public class CorteCajaImpresionTests
{
    private static Caja CrearCaja(
        decimal montoInicial = 500m,
        decimal? montoEsperado = 1300m,
        decimal? montoContado = 1290m,
        decimal? diferencia = -10m,
        string? observaciones = null)
    {
        var usuario = new Usuario
        {
            Id = 1,
            NombreUsuario = "ana",
            Nombre = "Ana López"
        };

        return new Caja
        {
            Id = 7,
            IdUsuarioApertura = usuario.Id,
            UsuarioApertura = usuario,
            FechaApertura = new DateTime(2026, 11, 12, 8, 0, 0),
            FechaCierre = new DateTime(2026, 11, 12, 20, 15, 0),
            MontoInicial = montoInicial,
            MontoEsperado = montoEsperado,
            MontoFinal = montoContado,
            Diferencia = diferencia,
            Estado = EstadoCaja.Cerrada,
            Observaciones = observaciones
        };
    }

    private static ResumenPagosCaja CrearResumen()
        => new(12, 800.25m, 300m, 134.25m);

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
    public void CorteDe58MmIncluyeVentasMetodosYMontos()
    {
        var texto = GeneradorCorteCajaTexto.Generar(
            CrearCaja(observaciones: "Faltó cambio"),
            CrearResumen(),
            AnchoTicket.Mm58);

        var lineas = texto.Split('\n');

        Assert.All(lineas, linea => Assert.True(
            linea.Length <= 32, $"Línea demasiado larga: '{linea}'"));

        Assert.Contains(lineas, l => l.Contains("Corte de caja #7"));
        Assert.Contains(lineas, l => l.Contains("VENTAS"));
        Assert.Contains(lineas, l => l.Contains("Ventas") && l.EndsWith("12"));
        Assert.Contains(lineas, l => l.Contains("Total vendido") && l.EndsWith("1234.50"));
        Assert.Contains(lineas, l => l.Contains("Efectivo") && l.EndsWith("800.25"));
        Assert.Contains(lineas, l => l.Contains("Tarjeta") && l.EndsWith("300.00"));
        Assert.Contains(lineas, l => l.Contains("Transferencia") && l.EndsWith("134.25"));
        Assert.Contains(lineas, l => l.Contains("Esperado") && l.EndsWith("1300.00"));
        Assert.Contains(lineas, l => l.Contains("Contado") && l.EndsWith("1290.00"));
        Assert.Contains(lineas, l => l.Contains("Diferencia") && l.EndsWith("-10.00"));
        Assert.Contains(lineas, l => l.Contains("FALTANTE: 10.00"));
        Assert.Contains(lineas, l => l.Contains("Observaciones"));
        Assert.Contains(lineas, l => l.Contains("Faltó cambio"));
        Assert.Contains(lineas, l => l.Contains("Firma"));
    }

    [Fact]
    public void CorteDe80MmRespetaElAnchoYEnvuelveObservacionesLargas()
    {
        var texto = GeneradorCorteCajaTexto.Generar(
            CrearCaja(observaciones: new string('A', 70)),
            CrearResumen(),
            AnchoTicket.Mm80);

        Assert.All(texto.Split('\n'), linea => Assert.True(
            linea.Length <= 48, $"Línea demasiado larga: '{linea}'"));

        Assert.Contains("Corte de caja #7", texto);
        Assert.Contains("TOTAL", texto.ToUpperInvariant());
    }

    [Fact]
    public void UsaLaDiferenciaGuardadaYClasificaSobrante()
    {
        var caja = CrearCaja(
            montoEsperado: 1000m,
            montoContado: 1005m,
            diferencia: 5m);

        var texto = GeneradorCorteCajaTexto.Generar(
            caja, CrearResumen(), AnchoTicket.Mm58);

        Assert.Contains("SOBRANTE: 5.00", texto);
        Assert.DoesNotContain("FALTANTE", texto);

        var vm = new CorteCajaViewModel(caja, CrearResumen());

        Assert.True(vm.HaySobrante);
        Assert.False(vm.HayFaltante);
        Assert.False(vm.SinDiferencia);
        Assert.Equal("+5.00", vm.DiferenciaTexto);
        Assert.Equal("Sobrante: 5.00", vm.SobranteTexto);
    }

    [Fact]
    public void CalculaLaDiferenciaSiNoEstaGuardada()
    {
        var caja = CrearCaja(
            montoEsperado: 1000m,
            montoContado: 990m,
            diferencia: null);

        var vm = new CorteCajaViewModel(caja, CrearResumen());

        Assert.Equal(-10m, vm.Diferencia);
        Assert.True(vm.HayFaltante);
        Assert.Equal("Faltante: 10.00", vm.FaltanteTexto);
    }

    [Fact]
    public void CajaSinDiferenciaSeMarcaEnElCorte()
    {
        var caja = CrearCaja(
            montoEsperado: 1000m,
            montoContado: 1000m,
            diferencia: 0m);

        var texto = GeneradorCorteCajaTexto.Generar(
            caja, CrearResumen(), AnchoTicket.Mm58);

        var vm = new CorteCajaViewModel(caja, CrearResumen());

        Assert.Contains("SIN DIFERENCIA", texto);
        Assert.True(vm.SinDiferencia);
        Assert.Equal("0.00", vm.DiferenciaTexto);
    }

    [Fact]
    public void IncluyeDatosDeAperturaYCierre()
    {
        var texto = GeneradorCorteCajaTexto.Generar(
            CrearCaja(observaciones: null),
            CrearResumen(),
            AnchoTicket.Mm58);

        Assert.Contains("Apertura: 12/11/2026 08:00", texto);
        Assert.Contains("Cierre: 12/11/2026 20:15", texto);
        Assert.Contains("Abrió: Ana López", texto);
    }

    [Fact]
    public void ViewModelExponeElResumenParaLaVistaPrevia()
    {
        var vm = new CorteCajaViewModel(CrearCaja(), CrearResumen());

        Assert.Equal("Corte de caja #7", vm.Numero);
        Assert.Equal("12", vm.CantidadVentasTexto);
        Assert.Equal("1234.50", vm.TotalVentasTexto);
        Assert.Equal("800.25", vm.TotalEfectivoTexto);
        Assert.Equal("300.00", vm.TotalTarjetaTexto);
        Assert.Equal("134.25", vm.TotalTransferenciaTexto);
        Assert.Equal("500.00", vm.MontoInicialTexto);
        Assert.Equal("1300.00", vm.MontoEsperadoTexto);
        Assert.Equal("1290.00", vm.MontoContadoTexto);
        Assert.Equal("-10.00", vm.DiferenciaTexto);
        Assert.Equal(AnchoTicket.Mm80, vm.AnchoSeleccionado.Ancho);
        Assert.False(vm.HayMensajeImpresion);
    }

    [Fact]
    public async Task ImprimirUsaElAnchoSeleccionadoYElContenidoGenerado()
    {
        var impresora = new ImpresoraFalsa(
            new Resultado(true, "Corte enviado a la impresora."));

        var vm = new CorteCajaViewModel(CrearCaja(), CrearResumen(), impresora)
        {
            AnchoSeleccionado = new AnchoTicketOpcion(AnchoTicket.Mm58)
        };

        await vm.ImprimirCommand.ExecuteAsync(null);

        Assert.True(vm.ImpresionCorrecta);
        Assert.True(vm.EsMensajeExito);
        Assert.False(vm.Imprimiendo);
        Assert.Equal(AnchoTicket.Mm58, impresora.Ancho);
        Assert.Contains("Corte de caja #7", impresora.Contenido);
        Assert.Contains("FALTANTE: 10.00", impresora.Contenido);
        Assert.Equal("Corte enviado a la impresora.", vm.MensajeImpresion);
    }

    [Fact]
    public async Task ErrorDeImpresionSoloAvisaYNoLanza()
    {
        var impresora = new ImpresoraFalsa(
            new Resultado(false, "No se pudo imprimir el corte."));

        var vm = new CorteCajaViewModel(CrearCaja(), CrearResumen(), impresora);

        await vm.ImprimirCommand.ExecuteAsync(null);

        Assert.False(vm.ImpresionCorrecta);
        Assert.True(vm.EsMensajeError);
        Assert.False(vm.Imprimiendo);
        Assert.Equal("No se pudo imprimir el corte.", vm.MensajeImpresion);
    }

    [Fact]
    public async Task ExcepcionDeLaImpresoraSeConvierteEnAviso()
    {
        var impresora = new ImpresoraSistema(
            (_, _) => throw new InvalidOperationException("sin impresora"));

        var vm = new CorteCajaViewModel(CrearCaja(), CrearResumen(), impresora);

        await vm.ImprimirCommand.ExecuteAsync(null);

        Assert.False(vm.ImpresionCorrecta);
        Assert.True(vm.EsMensajeError);
        Assert.False(vm.Imprimiendo);
        Assert.Contains("No se pudo imprimir", vm.MensajeImpresion);
    }
}

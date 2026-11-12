using PosInventario.ViewModels;

namespace PosInventario.Tests;

public class PaginacionViewModelTests
{
    [Fact]
    public void IniciaEnPrimeraPaginaConDiezRegistros()
    {
        var paginacion = new PaginacionViewModel();

        Assert.Equal(1, paginacion.PaginaActual);
        Assert.Equal(10, paginacion.TamanoPagina);
        Assert.Equal(0, paginacion.TotalRegistros);
        Assert.Equal(1, paginacion.TotalPaginas);
        Assert.False(paginacion.HayRegistros);
        Assert.False(paginacion.PuedeAnterior);
        Assert.False(paginacion.PuedeSiguiente);
        Assert.Equal("Sin registros", paginacion.Resumen);
    }

    [Fact]
    public void EstablecerTotalCalculaPaginasYResumen()
    {
        var paginacion = new PaginacionViewModel();

        paginacion.EstablecerTotal(25);

        Assert.Equal(3, paginacion.TotalPaginas);
        Assert.True(paginacion.HayRegistros);
        Assert.Equal(1, paginacion.Desde);
        Assert.Equal(10, paginacion.Hasta);
        Assert.Equal("1-10 de 25", paginacion.Resumen);
        Assert.Equal("Página 1 de 3", paginacion.PaginaTexto);
        Assert.True(paginacion.PuedeSiguiente);
    }

    [Fact]
    public void NavegarDisparaCambioYRespetaLosLimites()
    {
        var paginacion = new PaginacionViewModel();
        var cambios = 0;

        paginacion.CambioSolicitado += () => cambios++;
        paginacion.EstablecerTotal(25);

        paginacion.IrSiguienteCommand.Execute(null);

        Assert.Equal(2, paginacion.PaginaActual);
        Assert.Equal(1, cambios);
        Assert.Equal(11, paginacion.Desde);
        Assert.Equal(20, paginacion.Hasta);
        Assert.True(paginacion.PuedeAnterior);

        paginacion.IrUltimaCommand.Execute(null);

        Assert.Equal(3, paginacion.PaginaActual);
        Assert.Equal(2, cambios);
        Assert.False(paginacion.PuedeSiguiente);
        Assert.Equal(25, paginacion.Hasta);

        paginacion.IrSiguienteCommand.Execute(null);
        Assert.Equal(3, paginacion.PaginaActual);

        paginacion.IrPrimeraCommand.Execute(null);

        Assert.Equal(1, paginacion.PaginaActual);
        Assert.Equal(3, cambios);
    }

    [Fact]
    public void CambiarTamanoVuelveALaPrimeraPaginaYDisparaUnSoloCambio()
    {
        var paginacion = new PaginacionViewModel();
        var cambios = 0;

        paginacion.CambioSolicitado += () => cambios++;
        paginacion.EstablecerTotal(120);
        paginacion.IrUltimaCommand.Execute(null);

        Assert.Equal(12, paginacion.PaginaActual);
        Assert.Equal(1, cambios);

        paginacion.TamanoPagina = 50;

        Assert.Equal(1, paginacion.PaginaActual);
        Assert.Equal(3, paginacion.TotalPaginas);
        Assert.Equal(2, cambios);
        Assert.Equal("1-50 de 120", paginacion.Resumen);
    }

    [Fact]
    public void ReiniciarNoDisparaCambioYLimpiaElTotal()
    {
        var paginacion = new PaginacionViewModel();
        var cambios = 0;

        paginacion.CambioSolicitado += () => cambios++;
        paginacion.EstablecerTotal(30);
        paginacion.IrSiguienteCommand.Execute(null);

        paginacion.Reiniciar();

        Assert.Equal(1, paginacion.PaginaActual);
        Assert.Equal(0, paginacion.TotalRegistros);
        Assert.Equal(1, cambios);
    }

    [Fact]
    public void EstablecerUnTotalMenorAjustaLaPaginaActual()
    {
        var paginacion = new PaginacionViewModel();

        paginacion.EstablecerTotal(30);
        paginacion.IrUltimaCommand.Execute(null);

        Assert.Equal(3, paginacion.PaginaActual);

        paginacion.EstablecerTotal(12);

        Assert.Equal(2, paginacion.PaginaActual);
        Assert.Equal(2, paginacion.TotalPaginas);
    }
}

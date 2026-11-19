using PosInventario.Views;

namespace PosInventario.Tests;

public class AjusteVentanaTests
{
    [Theory]
    [InlineData(1280, 800, 1366, 728, true)]   // 1366x768 al 100 %
    [InlineData(1280, 800, 1920, 1040, false)] // 1920x1080 al 100 %
    [InlineData(1280, 800, 1536, 832, false)]  // 1920x1080 al 125 %
    [InlineData(1280, 800, 1092, 582, true)]   // 1366x768 al 125 %
    [InlineData(1280, 800, 2560, 1400, false)]
    [InlineData(1280, 800, 1000, 700, true)]
    public void DecideSiLaVentanaCabeEnLaPantalla(
        double anchoDeseado,
        double altoDeseado,
        double anchoDisponible,
        double altoDisponible,
        bool maximizar)
    {
        Assert.Equal(maximizar, AjusteVentana.DebeMaximizar(
            anchoDeseado, altoDeseado, anchoDisponible, altoDisponible));
    }

    [Fact]
    public void CabeExactamenteSinUsarElMargen()
    {
        Assert.False(AjusteVentana.DebeMaximizar(
            1280, 800, 1296, 816));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(double.NaN, double.NaN)]
    [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
    public void IgnoraMedidasInvalidas(double ancho, double alto)
    {
        Assert.False(AjusteVentana.DebeMaximizar(
            1280, 800, ancho, alto));
    }
}

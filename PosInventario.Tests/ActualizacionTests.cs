using PosInventario.Service;

namespace PosInventario.Tests;

public class ActualizacionTests
{
    [Theory]
    [InlineData("1.0.17", "1.0.5", true)]
    [InlineData("1.0.5", "1.0.17", false)]
    [InlineData("1.0.5", "1.0.5", false)]
    [InlineData("v1.0.18", "1.0.17", true)]
    [InlineData("2.0.0", "1.9.9", true)]
    [InlineData("1.10.0", "1.9.0", true)]
    [InlineData("1.0", "1.0.0", false)]
    [InlineData("1.0.0", "1.0", false)]
    [InlineData("1.0.18-beta", "1.0.17", true)]
    [InlineData("1.0.18+abc123", "1.0.17", true)]
    public void ComparaVersionesNumericas(
        string remota, string local, bool esperado)
        => Assert.Equal(esperado, ActualizacionService.EsVersionMasNueva(remota, local));

    [Theory]
    [InlineData("", "1.0.0")]
    [InlineData("1.0.0", "")]
    [InlineData("última", "1.0.0")]
    [InlineData("1.0.0", "última")]
    [InlineData("1..2", "1.0.0")]
    [InlineData(null, "1.0.0")]
    public void VersionesInvalidasNoOfrecenActualizacion(string? remota, string local)
        => Assert.False(ActualizacionService.EsVersionMasNueva(remota, local));

    [Theory]
    [InlineData("PosInventario-1.0.17-win-x64-installer.exe", true)]
    [InlineData("posinventario-1.0.17-win-x64-installer.EXE", true)]
    [InlineData("PosInventario-1.0.17-win-x64.zip", false)]
    [InlineData("PosInventario-1.0.17-win-x64-installer.exe.sha256", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void SoloAceptaElInstaladorDeWindows(string? nombre, bool esperado)
        => Assert.Equal(esperado, ActualizacionService.EsInstaladorDeWindows(nombre));

    [Theory]
    [InlineData("PosInventario-1.0.20-win-x64.zip", true)]
    [InlineData("posinventario-1.0.20-win-x64.ZIP", true)]
    [InlineData("PosInventario-1.0.20-win-x64-installer.exe", false)]
    [InlineData("PosInventario-1.0.20-win-x64.zip.sha256", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void SoloAceptaElPaquetePortable(string? nombre, bool esperado)
        => Assert.Equal(esperado, ActualizacionService.EsPaquetePortable(nombre));
}

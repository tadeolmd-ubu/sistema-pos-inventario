using PosInventario.Service;

namespace PosInventario.Tests;

public class InstanciaUnicaTests
{
    [Fact]
    public void NoPermiteDosBloqueosDelMismoArchivo()
    {
        var ruta = Path.Combine(
            Path.GetTempPath(), $"pos-instancia-{Guid.NewGuid():N}.lock");

        try
        {
            Assert.True(InstanciaUnica.TryAdquirir(ruta, out var primera));
            Assert.NotNull(primera);

            Assert.False(InstanciaUnica.TryAdquirir(ruta, out var segunda));
            Assert.Null(segunda);

            primera!.Dispose();

            Assert.True(InstanciaUnica.TryAdquirir(ruta, out var tercera));
            tercera!.Dispose();
        }
        finally
        {
            if (File.Exists(ruta))
                File.Delete(ruta);
        }
    }

    [Fact]
    public void RutaPredeterminadaQuedaEnLosDatosLocales()
    {
        var ruta = InstanciaUnica.RutaPredeterminada;

        Assert.EndsWith("instancia.lock", ruta);
        Assert.Contains("SistemaInventario", ruta);
    }
}

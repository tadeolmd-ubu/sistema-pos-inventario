using PosInventario.Service;

namespace PosInventario.Tests;

public class ConfiguracionAppServiceTests
{
    [Fact]
    public void GuardarYReleerMantieneLosValores()
    {
        var carpeta = CrearCarpetaDePrueba();
        var ruta = Path.Combine(carpeta, "configuracion.json");

        try
        {
            var servicio = new ConfiguracionAppService(ruta);

            Assert.True(servicio.Actual.RespaldosAutomaticos);
            Assert.Equal(
                ConfiguracionAppService.RetencionDiasPredeterminada,
                servicio.Actual.RetencionDias);

            var guardado = servicio.Guardar(false, 7);

            Assert.True(guardado.Correcta, guardado.Mensaje);

            var recargado = new ConfiguracionAppService(ruta);

            Assert.False(recargado.Actual.RespaldosAutomaticos);
            Assert.Equal(7, recargado.Actual.RetencionDias);
        }
        finally
        {
            Directory.Delete(carpeta, recursive: true);
        }
    }

    [Fact]
    public void RetencionFueraDeRangoSeRechaza()
    {
        var carpeta = CrearCarpetaDePrueba();
        var ruta = Path.Combine(carpeta, "configuracion.json");

        try
        {
            var servicio = new ConfiguracionAppService(ruta);

            var resultado = servicio.Guardar(true, 0);

            Assert.False(resultado.Correcta);
            Assert.Equal(
                ConfiguracionAppService.RetencionDiasPredeterminada,
                servicio.Actual.RetencionDias);
        }
        finally
        {
            Directory.Delete(carpeta, recursive: true);
        }
    }

    [Fact]
    public void ArchivoCorruptoUsaLosValoresPredeterminados()
    {
        var carpeta = CrearCarpetaDePrueba();
        var ruta = Path.Combine(carpeta, "configuracion.json");

        try
        {
            File.WriteAllText(ruta, "{ esto no es json");

            var servicio = new ConfiguracionAppService(ruta);

            Assert.True(servicio.Actual.RespaldosAutomaticos);
            Assert.Equal(
                ConfiguracionAppService.RetencionDiasPredeterminada,
                servicio.Actual.RetencionDias);
        }
        finally
        {
            Directory.Delete(carpeta, recursive: true);
        }
    }

    private static string CrearCarpetaDePrueba()
    {
        var carpeta = Path.Combine(
            Path.GetTempPath(),
            "PosInventario-tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(carpeta);
        return carpeta;
    }
}

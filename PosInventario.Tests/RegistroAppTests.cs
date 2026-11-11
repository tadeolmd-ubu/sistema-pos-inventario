using PosInventario.Service;

namespace PosInventario.Tests;

public class RegistroAppTests
{
    [Fact]
    public void LimpiarAntiguosBorraSoloLosVencidos()
    {
        var carpeta = Path.Combine(
            Path.GetTempPath(), $"pos-log-{Guid.NewGuid():N}");

        Directory.CreateDirectory(carpeta);

        try
        {
            var vencido = Path.Combine(carpeta, "app-20200101.log");
            var vigente = Path.Combine(carpeta, "app-20990101.log");

            File.WriteAllText(vencido, "viejo");
            File.WriteAllText(vigente, "nuevo");

            File.SetLastWriteTime(vencido, DateTime.Now.AddDays(-60));
            File.SetLastWriteTime(vigente, DateTime.Now);

            RegistroApp.LimpiarAntiguos(30, carpeta);

            Assert.False(File.Exists(vencido));
            Assert.True(File.Exists(vigente));
        }
        finally
        {
            Directory.Delete(carpeta, recursive: true);
        }
    }
}

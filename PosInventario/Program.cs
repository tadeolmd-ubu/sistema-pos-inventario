using System;
using System.Threading;
using Avalonia;

namespace PosInventario;

class Program
{
    private static readonly string NombreInstancia =
        $"SistemaInventario.{Environment.UserName}.InstanciaUnica";

    /// <summary>
    /// Indica que ya había otra instancia abierta y esta debe cerrarse.
    /// </summary>
    public static bool OtraInstancia { get; private set; }

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        using var instancia = new Mutex(
            initiallyOwned: true, NombreInstancia, out var creada);

        if (!creada)
        {
            OtraInstancia = true;
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            return;
        }

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            instancia.ReleaseMutex();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}

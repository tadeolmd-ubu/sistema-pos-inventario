using System;
using System.Threading.Tasks;
using Avalonia;
using PosInventario.Service;

namespace PosInventario;

class Program
{
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
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            RegistroApp.Escribir(
                "Excepción no controlada en un hilo de fondo.",
                e.ExceptionObject as Exception);

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            RegistroApp.Escribir("Tarea no observada.", e.Exception);
            e.SetObserved();
        };

        if (!InstanciaUnica.TryAdquirir(out var instancia) || instancia is null)
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
            instancia.Dispose();
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

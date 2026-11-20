using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PosInventario.Service;

namespace PosInventario.Views;

public partial class ActualizacionWindow : Window
{
    private readonly ActualizacionService? _servicio;
    private readonly ActualizacionDisponible? _actualizacion;
    private readonly RespaldoService? _respaldos;
    private readonly bool _puedeInstalar;
    private readonly string _carpetaRespaldos = string.Empty;

    public ActualizacionWindow() => InitializeComponent();

    public ActualizacionWindow(
        ActualizacionService servicio,
        ActualizacionDisponible actualizacion,
        RespaldoService respaldos,
        string carpetaRespaldos,
        bool puedeInstalar = true) : this()
    {
        _servicio = servicio;
        _actualizacion = actualizacion;
        _respaldos = respaldos;
        _carpetaRespaldos = carpetaRespaldos;
        _puedeInstalar = puedeInstalar;

        MensajeTexto.Text = puedeInstalar
            ? $"Versión instalada: v{servicio.VersionActual}\n" +
              $"Versión disponible: v{actualizacion.Version}\n\n" +
              "Se descargará e instalará la actualización y la aplicación se " +
              "volverá a abrir. Los datos, los respaldos y la configuración no " +
              "se modifican."
            : $"Versión actual: v{servicio.VersionActual}\n" +
              $"Versión disponible: v{actualizacion.Version}\n\n" +
              "Esta copia no está instalada, así que se abrirá la página de " +
              "descarga para que instales la versión nueva.";

        if (!puedeInstalar)
            BotonActualizar.Content = "Descargar";
    }

    private void Luego_Click(object? sender, RoutedEventArgs e) => Close(false);

    private async void Actualizar_Click(object? sender, RoutedEventArgs e)
    {
        if (_servicio is null || _actualizacion is null)
            return;

        if (!_puedeInstalar)
        {
            try
            {
                Process.Start(new ProcessStartInfo(_actualizacion.UrlInstalador)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                RegistroApp.Escribir("No se pudo abrir la página de descarga.", ex);
            }

            Close(false);
            return;
        }

        if (_respaldos is null)
            return;

        BotonActualizar.IsEnabled = false;
        BotonLuego.IsEnabled = false;
        ErrorTexto.IsVisible = false;
        PanelProgreso.IsVisible = true;
        EstadoTexto.Text = "Descargando...";

        var progreso = new Progress<int>(porcentaje =>
        {
            Progreso.Value = porcentaje;
            EstadoTexto.Text = $"Descargando... {porcentaje}%";
        });

        var resultado = await Actualizador.PrepararAsync(
            _servicio, _actualizacion, _respaldos, _carpetaRespaldos, progreso);

        if (!resultado.Correcta)
        {
            PanelProgreso.IsVisible = false;
            ErrorTexto.Text = resultado.Mensaje;
            ErrorTexto.IsVisible = true;
            BotonActualizar.IsEnabled = true;
            BotonLuego.IsEnabled = true;
            return;
        }

        EstadoTexto.Text = "Instalando y reiniciando la aplicación...";
        Close(true);
    }
}

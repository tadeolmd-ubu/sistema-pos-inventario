using CommunityToolkit.Mvvm.ComponentModel;

namespace PosInventario.ViewModels;

public enum TipoMensajeOperacion { Informacion, Exito, Error }

public sealed record MensajeDialogo(
    string Titulo, string Mensaje, TipoMensajeOperacion Tipo);

public abstract partial class OperacionViewModel : ObservableObject
{
    public event Action<MensajeDialogo>? MensajeSolicitado;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string mensajeOperacion = string.Empty;

    protected void SolicitarMensaje(MensajeDialogo mensaje)
        => MensajeSolicitado?.Invoke(mensaje);

    protected string Notificar(string mensaje, bool correcta = false)
    {
        SolicitarMensaje(new(
            correcta ? "Operación completada" : "Error",
            mensaje,
            correcta ? TipoMensajeOperacion.Exito : TipoMensajeOperacion.Error));

        return mensaje;
    }

    protected async Task EjecutarSeguroAsync(Func<Task> operacion)
    {
        if (IsBusy) return;

        IsBusy = true;
        try
        {
            await operacion();
        }
        catch (Exception ex)
        {
            MensajeOperacion = Notificar(
                $"No se pudo completar la operación: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

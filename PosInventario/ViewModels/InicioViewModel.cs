using CommunityToolkit.Mvvm.Input;

namespace PosInventario.ViewModels;

public partial class InicioViewModel : OperacionViewModel
{
    [RelayCommand]
    public Task CargarAsync() => Task.CompletedTask;
}

using PosInventario.ViewModels;

namespace PosInventario.Views.DesignTime;

public sealed class DesignTimeData
{
    public static readonly InicioDesignTime Inicio = new();
}

public sealed class InicioDesignTime : InicioViewModel
{
}

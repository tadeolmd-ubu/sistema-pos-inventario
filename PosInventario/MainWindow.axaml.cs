using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using PosInventario.ViewModels;
using PosInventario.Views;

namespace PosInventario;

public partial class MainWindow : Window
{
    private readonly Queue<MensajeDialogo> _mensajesPendientes = new();
    private bool _mostrandoMensaje;
    private MainViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
            ConectarViewModel(DataContext as MainViewModel);

        AddHandler(InputElement.GotFocusEvent, OnGotFocus, RoutingStrategies.Bubble);
        KeyDown += MainWindow_KeyDown;
    }

    private void ConectarViewModel(MainViewModel? viewModel)
    {
        if (_viewModel is not null)
            _viewModel.MensajeSolicitado -= MostrarMensaje;

        _viewModel = viewModel;

        if (_viewModel is not null)
            _viewModel.MensajeSolicitado += MostrarMensaje;
    }

    /// <summary>
    /// Actualiza el panel de comandos del menú lateral según la pestaña y el
    /// control que acaba de recibir el foco.
    /// </summary>
    private void OnGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (_viewModel is null || e.Source is not Control control)
            return;

        var vista = _viewModel.TituloVistaActual;

        var texto = control as TextBox ?? control.FindAncestorOfType<TextBox>();

        if (texto is not null)
        {
            var (titulo, items) = texto.Name switch
            {
                "BusquedaRapidaBox" => ("Caja · Búsqueda", new AtajoItem[]
                {
                    new("F2", "Buscar / escanear"),
                    new("Enter", "Agregar producto"),
                    new("Esc", "Limpiar campo"),
                    new("F4", "Cobrar"),
                    new("F1", "Ayuda")
                }),
                "DescuentoBox" => ("Caja · Descuento", new AtajoItem[]
                {
                    new("F6", "Enfocar descuento"),
                    new("Esc", "Limpiar campo"),
                    new("F4", "Cobrar"),
                    new("F1", "Ayuda")
                }),
                "PagoEfectivoBox" or "PagoEfectivoMixtoBox"
                    or "PagoTarjetaBox" or "PagoTransferenciaBox" =>
                    ("Caja · Pago", new AtajoItem[]
                    {
                        new("Enter", "Cobrar"),
                        new("Esc", "Limpiar campo"),
                        new("F7", "Método de pago"),
                        new("F4", "Cobrar"),
                        new("F1", "Ayuda")
                    }),
                "SkuBox" => ("Productos · SKU", new AtajoItem[]
                {
                    new("Enter", "Cargar producto"),
                    new("Esc", "Limpiar campo"),
                    new("Tab", "Siguiente"),
                    new("F1", "Ayuda")
                }),
                _ => ($"{vista} · Campo de texto", new AtajoItem[]
                {
                    new("Tab", "Siguiente"),
                    new("Esc", "Limpiar campo"),
                    new("F1", "Ayuda")
                })
            };

            _viewModel.EstablecerComandos(titulo, items);
            return;
        }

        var grid = control as DataGrid ?? control.FindAncestorOfType<DataGrid>();

        if (grid is not null)
        {
            if (vista == "Caja")
            {
                _viewModel.EstablecerComandos("Caja · Carrito", new AtajoItem[]
                {
                    new("↑/↓", "Navegar"),
                    new("Delete", "Quitar línea"),
                    new("+ / -", "Cambiar cantidad"),
                    new("F4", "Cobrar"),
                    new("F1", "Ayuda")
                });
            }
            else
            {
                _viewModel.EstablecerComandos($"{vista} · Lista", new AtajoItem[]
                {
                    new("↑/↓", "Navegar"),
                    new("Enter", "Abrir"),
                    new("Tab", "Siguiente"),
                    new("F1", "Ayuda")
                });
            }

            return;
        }

        var combo = control as ComboBox ?? control.FindAncestorOfType<ComboBox>();

        if (combo is not null)
        {
            if (vista == "Caja")
            {
                _viewModel.EstablecerComandos("Caja · Método de pago", new AtajoItem[]
                {
                    new("↑/↓", "Cambiar"),
                    new("Enter", "Abrir"),
                    new("Esc", "Cerrar"),
                    new("F7", "Método de pago"),
                    new("F4", "Cobrar"),
                    new("F1", "Ayuda")
                });
            }
            else
            {
                _viewModel.EstablecerComandos($"{vista} · Selector", new AtajoItem[]
                {
                    new("↑/↓", "Cambiar"),
                    new("Enter", "Abrir / confirmar"),
                    new("Esc", "Cerrar"),
                    new("Tab", "Siguiente"),
                    new("F1", "Ayuda")
                });
            }

            return;
        }

        var lista = control as ListBox ?? control.FindAncestorOfType<ListBox>();

        if (lista is not null)
        {
            if (vista == "Caja")
            {
                _viewModel.EstablecerComandos("Caja · Lista", new AtajoItem[]
                {
                    new("↑/↓", "Abrir detalle"),
                    new("Tab", "Siguiente"),
                    new("F1", "Ayuda")
                });
            }
            else
            {
                _viewModel.EstablecerComandos($"{vista} · Lista", new AtajoItem[]
                {
                    new("↑/↓", "Navegar"),
                    new("Enter", "Abrir"),
                    new("Tab", "Siguiente"),
                    new("F1", "Ayuda")
                });
            }

            return;
        }

        var check = control as CheckBox ?? control.FindAncestorOfType<CheckBox>();

        if (check is not null)
        {
            _viewModel.EstablecerComandos($"{vista} · Casilla", new AtajoItem[]
            {
                new("Espacio", "Marcar / desmarcar"),
                new("Tab", "Siguiente"),
                new("F1", "Ayuda")
            });
            return;
        }

        if (control is DatePicker or CalendarDatePicker)
        {
            _viewModel.EstablecerComandos($"{vista} · Fecha", new AtajoItem[]
            {
                new("Enter", "Abrir"),
                new("Esc", "Cerrar"),
                new("Tab", "Siguiente"),
                new("F1", "Ayuda")
            });
            return;
        }

        var boton = control as Button ?? control.FindAncestorOfType<Button>();

        if (boton is not null)
        {
            _viewModel.EstablecerComandos($"{vista} · Botón", new AtajoItem[]
            {
                new("Enter", "Activar (o Espacio)"),
                new("Tab", "Siguiente"),
                new("F1", "Ayuda")
            });
            return;
        }

        _viewModel.EstablecerComandos(
            vista, MainViewModel.ComandosPorDefecto(vista));
    }

    /// <summary>
    /// F1 global: muestra la ayuda del contexto actual. En Caja lo maneja la
    /// propia vista con el listado completo.
    /// </summary>
    private async void MainWindow_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled || e.Key != Key.F1 || _viewModel is null)
            return;

        e.Handled = true;

        var texto = string.Join(
            Environment.NewLine,
            _viewModel.ComandosItems.Select(i => $"{i.Tecla}  {i.Descripcion}"));

        if (string.IsNullOrWhiteSpace(texto))
            texto = "No hay atajos disponibles en este momento.";

        await new MensajeWindow(new MensajeDialogo(
            $"Atajos · {_viewModel.ComandosTitulo}",
            texto,
            TipoMensajeOperacion.Informacion)).ShowDialog(this);
    }

    private void MostrarMensaje(MensajeDialogo mensaje)
    {
        _mensajesPendientes.Enqueue(mensaje);

        if (!_mostrandoMensaje)
            _ = ProcesarMensajesAsync();
    }

    private async Task ProcesarMensajesAsync()
    {
        _mostrandoMensaje = true;

        try
        {
            while (_mensajesPendientes.TryDequeue(out var mensaje))
                await new MensajeWindow(mensaje).ShowDialog(this);
        }
        finally
        {
            _mostrandoMensaje = false;
        }
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models;
using PosInventario.Service;
using PosInventario.ViewModels;
using PosInventario.Views;

namespace PosInventario;

public partial class App : Application
{
    private static bool _errorGlobalMostrado;

    /// <summary>
    /// Lo consulta Program después de cerrar la aplicación para volver a
    /// iniciarla cuando se restauró un respaldo.
    /// </summary>
    public static bool ReiniciarSolicitado { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        RegistroApp.LimpiarAntiguos();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (Program.OtraInstancia)
            {
                desktop.MainWindow = CrearVentanaInstanciaAbierta();

                base.OnFrameworkInitializationCompleted();
                return;
            }

            Dispatcher.UIThread.UnhandledException += (_, e) =>
            {
                RegistroApp.Escribir(
                    "Excepción no controlada en la interfaz.", e.Exception);

                e.Handled = true;
                MostrarErrorGlobal(desktop, e.Exception);
            };

            string rutaBaseDatos;
            try
            {
                rutaBaseDatos = ResolverRutaBaseDatos();
            }
            catch (Exception ex)
            {
                RegistroApp.Escribir(
                    "No se pudo preparar la base de datos al iniciar.", ex);

                desktop.MainWindow = new Window
                {
                    Title = "Error al iniciar Sistema de inventario",
                    Width = 600,
                    Height = 200,
                    Content = new TextBlock
                    {
                        Text = $"No se pudo preparar la base de datos: {ex.Message}",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        Margin = new Thickness(24)
                    }
                };

                base.OnFrameworkInitializationCompleted();
                return;
            }

            var opciones = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(new SqliteConnectionStringBuilder
                {
                    DataSource = rutaBaseDatos,
                    // Espera a que otra conexión libere la base en lugar de
                    // fallar de inmediato con "database is locked".
                    DefaultTimeout = 30
                }.ToString())
                .Options;

            var dbContext = new AppDbContext(opciones);

            var sesion = new SesionActual();
            var respaldoService = new RespaldoService(dbContext, sesion);
            var configuracionAppService = new ConfiguracionAppService();
            var carpetaRespaldos = Path.Combine(
                Path.GetDirectoryName(rutaBaseDatos)!, "Respaldos");
            var respaldoAutomaticoService = new RespaldoAutomaticoService(
                respaldoService, configuracionAppService, carpetaRespaldos);
            var usuarioService = new UsuarioService(dbContext, sesion);
            var productoService = new ProductoService(dbContext, sesion);
            var proveedorService = new ProveedorService(dbContext, sesion);
            var clienteService = new ClienteService(dbContext, sesion);
            var inventarioService = new InventarioService(dbContext, sesion);
            var compraService = new CompraService(dbContext, sesion);
            var cajaService = new CajaService(dbContext, sesion);
            var ventaService = new VentaService(dbContext, sesion, clienteService);
            var reporteService = new ReporteService(dbContext, sesion);
            var versionService = new VersionService();
            var importacionService = new ImportacionDatosService(dbContext, sesion);

            var inicioViewModel = new InicioViewModel(
                ventaService, productoService, cajaService, compraService, sesion);
            var cajaViewModel = new CajaViewModel(
                cajaService, ventaService, productoService, sesion,
                respaldoAutomaticoService, clienteService);
            var productosViewModel = new ProductosViewModel(
                productoService, importacionService);
            var proveedoresViewModel = new ProveedoresViewModel(proveedorService);
            var clientesViewModel = new ClientesViewModel(clienteService);
            var inventarioViewModel = new InventarioViewModel(productoService, inventarioService);
            var comprasViewModel = new ComprasViewModel(
                compraService, proveedorService, productoService);
            var ventasViewModel = new VentasViewModel(ventaService);
            var reportesViewModel = new ReportesViewModel(reporteService);
            var usuariosViewModel = new UsuariosViewModel(usuarioService, sesion);
            var configuracionViewModel = new ConfiguracionViewModel(
                respaldoService, rutaBaseDatos, sesion, configuracionAppService);

            var mainViewModel = new MainViewModel(
                inicioViewModel, cajaViewModel, productosViewModel,
                proveedoresViewModel, inventarioViewModel, comprasViewModel,
                ventasViewModel, reportesViewModel, usuariosViewModel,
                configuracionViewModel, versionService, sesion,
                respaldoAutomaticoService, clientesViewModel);

            var mainWindow = new MainWindow { DataContext = mainViewModel };

            configuracionViewModel.ReinicioSolicitado += () =>
            {
                ReiniciarSolicitado = true;
                desktop.Shutdown();
            };

            mainWindow.IsEnabled = false;
            mainWindow.Opened += async (_, _) =>
            {
                try
                {
                    if (File.Exists(rutaBaseDatos) &&
                        (await dbContext.Database.GetPendingMigrationsAsync()).Any())
                    {
                        var respaldo = await respaldoService.CrearRespaldoAsync(
                            carpetaRespaldos);

                        if (!respaldo.Correcta)
                            throw new IOException("No se pudo crear el respaldo previo a la migración.");
                    }

                    await dbContext.Database.MigrateAsync();

                    if (!await IniciarSesionAsync(
                            mainWindow, desktop, sesion, usuarioService, mainViewModel))
                        return;
                }
                catch (Exception ex)
                {
                    RegistroApp.Escribir(
                        "No se pudo iniciar la base de datos o la sesión.", ex);

                    mainWindow.Content = new TextBlock
                    {
                        Text = $"No se pudo iniciar la base de datos: {ex.Message}",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        Margin = new Thickness(24)
                    };
                    mainWindow.IsEnabled = true;
                }
            };

            mainViewModel.CerrarSesionSolicitado += async () =>
            {
                mainWindow.IsEnabled = false;
                sesion.Cerrar();
                mainViewModel.AplicarSesion();
                await mainViewModel.InicializarAsync();

                await IniciarSesionAsync(
                    mainWindow, desktop, sesion, usuarioService, mainViewModel);
            };

            desktop.Exit += (_, _) => dbContext.Dispose();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Muestra el asistente de primer administrador si hace falta y luego el
    /// login. Devuelve false cuando la aplicación debe cerrarse.
    /// </summary>
    private static async Task<bool> IniciarSesionAsync(
        Window ventana,
        IClassicDesktopStyleApplicationLifetime desktop,
        SesionActual sesion,
        UsuarioService usuarioService,
        MainViewModel mainViewModel)
    {
        if (!await usuarioService.ExisteAlgunUsuarioAsync())
        {
            var setupViewModel = new SetupViewModel(usuarioService);

            var creado = await new SetupWindow { DataContext = setupViewModel }
                .ShowDialog<bool>(ventana);

            if (!creado)
            {
                desktop.Shutdown();
                return false;
            }
        }

        var loginViewModel = new LoginViewModel(usuarioService);

        var usuario = await new LoginWindow { DataContext = loginViewModel }
            .ShowDialog<Usuario?>(ventana);

        if (usuario is null)
        {
            desktop.Shutdown();
            return false;
        }

        sesion.Iniciar(usuario);
        mainViewModel.AplicarSesion();
        await mainViewModel.InicializarAsync();
        ventana.IsEnabled = true;

        await mainViewModel.EjecutarRespaldoDiarioAsync();

        _ = mainViewModel.ActualizarVersionAsync();

        return true;
    }

    /// <summary>
    /// Muestra un aviso cuando ocurre un error no controlado. Se muestra una
    /// sola vez para no llenar la pantalla de ventanas.
    /// </summary>
    private static void MostrarErrorGlobal(
        IClassicDesktopStyleApplicationLifetime desktop, Exception ex)
    {
        if (_errorGlobalMostrado)
            return;

        _errorGlobalMostrado = true;

        try
        {
            var ventana = new MensajeWindow(new MensajeDialogo(
                "Ocurrió un error inesperado",
                $"{ex.Message}\n\nEl detalle se guardó en:\n{RegistroApp.ArchivoActual}",
                TipoMensajeOperacion.Error));

            var principal = desktop.MainWindow;

            if (principal is { IsVisible: true })
                _ = ventana.ShowDialog(principal);
            else
                ventana.Show();
        }
        catch
        {
            // Si ni siquiera se puede avisar, el detalle ya quedó en el log.
        }
    }

    private static Window CrearVentanaInstanciaAbierta()
    {
        var ventana = new Window
        {
            Title = "Sistema de inventario",
            Width = 460,
            Height = 240,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        var cerrar = new Button
        {
            Content = "Cerrar",
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 100
        };

        cerrar.Click += (_, _) => ventana.Close();

        var titulo = new TextBlock
        {
            Text = "Sistema de inventario ya está abierto",
            FontSize = 18,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        };

        var detalle = new TextBlock
        {
            Text = "Cierra la ventana que ya tienes abierta y vuelve a iniciar la aplicación.",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Top
        };

        var acciones = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        acciones.Children.Add(cerrar);

        var contenido = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            RowSpacing = 16
        };

        Grid.SetRow(titulo, 0);
        Grid.SetRow(detalle, 1);
        Grid.SetRow(acciones, 2);

        contenido.Children.Add(titulo);
        contenido.Children.Add(detalle);
        contenido.Children.Add(acciones);

        ventana.Content = new Border
        {
            Padding = new Thickness(28),
            Child = contenido
        };

        return ventana;
    }

    private static string ResolverRutaBaseDatos()
    {
#if !DEBUG
        var carpeta = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SistemaInventario");

        Directory.CreateDirectory(carpeta);

        var ruta = Path.Combine(carpeta, "SistemaInventario.db");

        if (!File.Exists(ruta))
        {
            // Importa una instalación portable sin modificar su base original.
            var anterior = Path.Combine(AppContext.BaseDirectory, "SistemaInventario.db");

            if (File.Exists(anterior))
            {
                var temporal = ruta + ".tmp";

                try
                {
                    using (var origen = new SqliteConnection(new SqliteConnectionStringBuilder
                           {
                               DataSource = anterior,
                               Mode = SqliteOpenMode.ReadOnly,
                               Pooling = false
                           }.ToString()))
                    using (var destino = new SqliteConnection(new SqliteConnectionStringBuilder
                           {
                               DataSource = temporal,
                               Pooling = false
                           }.ToString()))
                    {
                        origen.Open();
                        destino.Open();
                        origen.BackupDatabase(destino);
                    }

                    File.Move(temporal, ruta);
                }
                finally
                {
                    File.Delete(temporal);
                }
            }
        }

        return ruta;
#else
        var directorio = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directorio is not null)
        {
            var rutaDirecta = Path.Combine(directorio.FullName, "SistemaInventario.db");
            if (File.Exists(rutaDirecta))
                return rutaDirecta;

            var rutaDentroDelProyecto = Path.Combine(
                directorio.FullName, "PosInventario", "SistemaInventario.db");

            if (File.Exists(rutaDentroDelProyecto))
                return rutaDentroDelProyecto;

            directorio = directorio.Parent;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "SistemaInventario.db");
#endif
    }
}

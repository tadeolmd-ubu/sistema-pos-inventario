using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
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
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            string rutaBaseDatos;
            try
            {
                rutaBaseDatos = ResolverRutaBaseDatos();
            }
            catch (Exception ex)
            {
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
                .UseSqlite(new SqliteConnectionStringBuilder { DataSource = rutaBaseDatos }.ToString())
                .Options;

            var dbContext = new AppDbContext(opciones);

            var sesion = new SesionActual();
            var respaldoService = new RespaldoService(dbContext);
            var usuarioService = new UsuarioService(dbContext, sesion);
            var productoService = new ProductoService(dbContext, sesion);
            var proveedorService = new ProveedorService(dbContext, sesion);
            var inventarioService = new InventarioService(dbContext, sesion);
            var compraService = new CompraService(dbContext, sesion);
            var cajaService = new CajaService(dbContext, sesion);
            var ventaService = new VentaService(dbContext, sesion);

            var inicioViewModel = new InicioViewModel(
                ventaService, productoService, cajaService, compraService, sesion);
            var cajaViewModel = new CajaViewModel(
                cajaService, ventaService, productoService, sesion);
            var productosViewModel = new ProductosViewModel(productoService);
            var proveedoresViewModel = new ProveedoresViewModel(proveedorService);
            var inventarioViewModel = new InventarioViewModel(productoService, inventarioService);
            var comprasViewModel = new ComprasViewModel(
                compraService, proveedorService, productoService);
            var usuariosViewModel = new UsuariosViewModel(usuarioService, sesion);
            var configuracionViewModel = new ConfiguracionViewModel(
                respaldoService, rutaBaseDatos, sesion);

            var mainViewModel = new MainViewModel(
                inicioViewModel, cajaViewModel, productosViewModel,
                proveedoresViewModel, inventarioViewModel, comprasViewModel,
                usuariosViewModel, configuracionViewModel, sesion);

            var mainWindow = new MainWindow { DataContext = mainViewModel };

            mainWindow.IsEnabled = false;
            mainWindow.Opened += async (_, _) =>
            {
                try
                {
                    if (File.Exists(rutaBaseDatos) &&
                        (await dbContext.Database.GetPendingMigrationsAsync()).Any())
                    {
                        var respaldo = await respaldoService.CrearRespaldoAsync(
                            Path.Combine(Path.GetDirectoryName(rutaBaseDatos)!, "Respaldos"));

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

        return true;
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

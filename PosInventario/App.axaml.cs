using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Service;
using PosInventario.ViewModels;
using PosInventario.Views;
using System.Diagnostics;

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

            // 1. Un servicio por entidad.
            var categoriaService = new CategoriaService(dbContext);
            var elementoService = new ElementoService(dbContext);
            var respaldoService = new RespaldoService(dbContext);

            // 2. Un ViewModel por pantalla.
            var inicioViewModel = new InicioViewModel(elementoService, categoriaService);
            var elementosViewModel = new ElementosViewModel(elementoService, categoriaService);

            // 3. Shell principal.
            var mainViewModel = new MainViewModel(inicioViewModel, elementosViewModel);

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
                    await mainViewModel.InicializarAsync();
                    mainWindow.IsEnabled = true;
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
                    return;
                }

                // Módulo opcional de actualización:
                // #if !DEBUG
                // try
                // {
                //     var actualizacion = await UpdateService.BuscarAsync();
                //     if (actualizacion is not null && mainWindow.IsVisible &&
                //         await new ActualizacionWindow(actualizacion).ShowDialog<bool>(mainWindow))
                //         desktop.Shutdown();
                // }
                // catch (Exception ex)
                // {
                //     Trace.TraceWarning("No se pudo consultar la actualización: {0}", ex.Message);
                // }
                // #endif
            };

            desktop.Exit += (_, _) => dbContext.Dispose();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
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

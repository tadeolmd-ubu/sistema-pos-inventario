# Arquitectura base

Documento operativo del esqueleto implementado. Resume las decisiones y las
reglas que deben respetarse al agregar módulos. El dominio real todavía no
existe: `Elemento` + `Categoria` son el módulo de referencia.

## Variables del proyecto

| Variable | Valor |
|---|---|
| Proyecto / namespace | `PosInventario` |
| Nombre visible | `Sistema de inventario` |
| Base SQLite | `SistemaInventario.db` |
| Carpeta de datos (Release) | `SistemaInventario` en `LocalApplicationData` |
| Repositorio de releases | `tadeolmd-ubu/sistemaPOS` |

> El nombre técnico original (`POS--Sistema-POS-Inventario`) no es válido como
> namespace/assembly de C#, por eso se usa `PosInventario`.

## Stack

- `net10.0`, `WinExe`, `Nullable` e `ImplicitUsings` habilitados.
- Avalonia 12.1.2 + FluentTheme + Inter.
- CommunityToolkit.Mvvm 8.4.2.
- EF Core SQLite 10.0.12 (runtime + design).
- xUnit 2.9.3 para pruebas.
- `dotnet-ef` 10.0.12 como herramienta local (`PosInventario/.config/dotnet-tools.json`).

## Estructura

```text
PosInventario.sln
PosInventario/
├── .config/dotnet-tools.json
├── Assets/
├── Controls/                  ResponsiveColumnsGrid, ResponsiveTwoPane
├── Data/                      AppDbContext, AppDbContextFactory
├── Migrations/                generadas con dotnet-ef
├── Models/                    Categoria, Elemento, Enums/
├── Service/                   Resultado, CategoriaService, ElementoService, RespaldoService
├── Styles/                    Styles.axaml, Converters.cs
├── ViewModels/                OperacionViewModel, MainViewModel, InicioViewModel, ElementosViewModel
├── Views/                     SidebarView, InicioView, ElementosView, MensajeWindow, DesignTime/
├── App.axaml(.cs)
├── MainWindow.axaml(.cs)
├── Program.cs
└── app.manifest
PosInventario.Tests/           pruebas de servicios y respaldo
```

## Dependencias entre capas

```text
Views → ViewModels → Service → Data + Models
```

1. `Models` no referencia nada.
2. `Data` referencia `Models` y EF Core.
3. `Service` referencia `Data`, `Models` y devuelve `Resultado`/`Resultado<T>`.
4. `ViewModels` referencia `Service` y `Models`; nunca crea un `AppDbContext`.
5. `Views` solo usa bindings y code-behind mínimo.
6. La composición manual vive únicamente en `App.axaml.cs`.
7. `Controls` y `Styles` no conocen el dominio.

## Arranque

`App.axaml.cs`:

1. Resuelve la ruta de la base (`Debug`: directorio actual y padres;
   `Release`: `LocalApplicationData` + importación portable).
2. Crea **un** `AppDbContext`.
3. Crea un servicio por entidad.
4. Crea un ViewModel por pantalla y el `MainViewModel`.
5. Asigna `DataContext` a `MainWindow`.
6. En `Opened`: respaldo si hay migraciones pendientes, `MigrateAsync()`,
   `InicializarAsync()`.
7. En `desktop.Exit`: `Dispose()` del contexto.

## Reglas de negocio

- Toda escritura devuelve `Resultado` o `Resultado<T>`.
- Validar → normalizar → verificar duplicados y relaciones → guardar.
- Lecturas con `AsNoTracking()`; `Include` solo para navegaciones usadas por la UI.
- Llamadas secuenciales sobre el `DbContext` compartido (sin paralelismo).
- Baja lógica con `FechaArchivado` (entidades) o `Activo` 1/0 (catálogos).
  Nunca `Remove()` de entidades de negocio.
- Fechas de calendario con `DateOnly`; instantes con `DateTime`.
- Enums persistidos como texto.

## Convenciones

- Carpetas en singular: `Models`, `Data`, `Service`, `ViewModels`, `Views`.
- Un archivo `.cs` por clase pública, con el mismo nombre.
- `x:DataType` obligatorio en cada XAML.
- `[ObservableProperty]` y `[RelayCommand]` para bindings y comandos.
- Navegación con un `DataTemplate` por ViewModel en `MainWindow.axaml`.
- Diálogos mediante `MensajeSolicitado` → `MensajeWindow` (nunca `MessageBox`).
- `DesignTimeData` permite abrir el diseñador sin base de datos.

## Cómo agregar un módulo

1. `Models/<Entidad>.cs` (y enum en `Models/Enums/` si aplica).
2. `DbSet` + mapeo en `Data/AppDbContext.cs`.
3. `Service/<Entidad>Service.cs` con el patrón de `ElementoService`.
4. `ViewModels/<Entidades>ViewModel.cs` heredando `OperacionViewModel`.
5. `Views/<Entidades>View.axaml` + code-behind.
6. `DataTemplate` en `MainWindow.axaml`, botón en `SidebarView.axaml`,
   comando en `MainViewModel` y creación en `App.axaml.cs`.
7. Instancia en `Views/DesignTime/DesignTimeData.cs`.
8. `cd PosInventario && dotnet tool run dotnet-ef migrations add Add<Entidades>`.
9. Test en `PosInventario.Tests/<Entidad>ServiceTests.cs`.
10. `dotnet build PosInventario.sln` y `dotnet test PosInventario.sln`.

## Cómo borrar el módulo de referencia

Borrar `Elemento`/`Categoria` de Models, Data, Service, ViewModels, Views,
DesignTimeData, navegación y tests; luego generar una migración que refleje el
cambio.

## Invariantes

1. Un solo `AppDbContext` por ejecución.
2. Composición manual; sin contenedor de DI.
3. Sin repositorios, Unit of Work, AutoMapper ni ReactiveUI.
4. Sin lógica de negocio en ViewModels, Views ni XAML.
5. Sin llamadas a servicios desde las Views.
6. `Activo` es `int` 1/0, nunca `bool`.
7. Las bases `.db` no se versionan.
8. Las migraciones se aplican al iniciar; si falla el respaldo previo, no se migra.
9. La app debe arrancar aunque falle una consulta de actualización.
10. Cada módulo nuevo se clona del patrón existente.

## Módulos opcionales (no incluidos)

PDF (QuestPDF), respaldo manual desde UI, actualización automática y
configuración. Se agregan solo cuando el proyecto los necesite.

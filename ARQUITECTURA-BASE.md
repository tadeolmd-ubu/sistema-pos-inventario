# Arquitectura base

Documento operativo del sistema implementado (v1 funcional en `dev`). Resume
las decisiones y las reglas que deben respetarse al agregar módulos. El
roadmap posterior a la v1 vive en `PENDIENTES.md`.

## Variables del proyecto

| Variable | Valor |
|---|---|
| Proyecto / namespace | `PosInventario` |
| Nombre visible | `Sistema de inventario` |
| Base SQLite | `SistemaInventario.db` |
| Carpeta de datos (Release) | `SistemaInventario` en `LocalApplicationData` |
| Repositorio de releases | `tadeolmd-ubu/sistema-pos-inventario` |

> El nombre técnico original (`POS--Sistema-POS-Inventario`) no es válido como
> namespace/assembly de C#, por eso se usa `PosInventario`.

## Stack

- `net10.0`, `WinExe`, `Nullable` e `ImplicitUsings` habilitados.
- Avalonia 12.1.2 + FluentTheme + Inter (+ `Avalonia.Controls.DataGrid`;
  `AvaloniaUI.DiagnosticsSupport` solo en Debug).
- CommunityToolkit.Mvvm 8.4.2.
- EF Core SQLite 10.0.12 (runtime + design).
- System.Drawing.Common 10.0.12 para la impresión en Windows.
- xUnit 2.9.3 para pruebas.
- `dotnet-ef` 10.0.12 como herramienta local (`PosInventario/.config/dotnet-tools.json`).
- Sin PDF ni red: la v1 es local (salvo el sondeo de versión contra la API de
  GitHub en `VersionService`). La impresión usa la cola del sistema (API de
  Windows o CUPS).

## Estructura

```text
PosInventario.sln
PosInventario/
├── .config/dotnet-tools.json
├── Assets/
├── Controls/                  ResponsiveColumnsGrid, ResponsiveTwoPane
├── Data/                      AppDbContext, AppDbContextFactory
├── Migrations/                generadas con dotnet-ef (InitialCreate)
├── Models/                    Usuario, Producto, Proveedor, Compra, Venta, Caja, enums
├── Service/                   Resultado, RespaldoService, RegistroApp,
│                              InstanciaUnica, SesionActual, VersionService,
│                              Usuario/Producto/Proveedor/Inventario/
│                              Compra/Caja/VentaService, Linea*, ResumenPagosCaja
├── Styles/                    Styles.axaml, Converters.cs
├── ViewModels/                MainViewModel + uno por módulo (Inicio, Caja,
│                              Productos, Proveedores, Inventario, Compras,
│                              Ventas, Usuarios, Configuración, Login, Setup,
│                              Ticket, CancelarVenta, CerrarCaja, CajaHistorial,
│                              MovimientoCaja, RestablecerContrasena,
│                              SeleccionarProducto) e ítems de líneas
├── Views/                     vistas por módulo + SidebarView, LoginWindow,
│                              SetupWindow, TicketWindow, CancelarVentaWindow,
│                              CerrarCajaWindow, CajaHistorialWindow,
│                              MovimientoCajaWindow, RegistrarMovimientoCajaWindow,
│                              RestablecerContrasenaWindow, SeleccionarProductoWindow,
│                              ConfirmacionWindow, MensajeWindow, DesignTime/
├── App.axaml(.cs)
├── MainWindow.axaml(.cs)
├── Program.cs
└── app.manifest
PosInventario.Tests/           pruebas de servicios, respaldo, instancia,
                               registro, ViewModels de caja/inicio y despliegue
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

`Program.cs`:

1. Registra los manejadores globales (`AppDomain`, `TaskScheduler`,
   `Dispatcher`) que escriben en `RegistroApp` sin lanzar.
2. `InstanciaUnica.TryAdquirir`: bloqueo de archivo; si ya hay otra instancia
   abierta, muestra el aviso "ya está abierto" y cierra.

`App.axaml.cs`:

1. Limpia logs con más de 30 días (`RegistroApp.LimpiarAntiguos`).
2. Resuelve la ruta de la base (`Debug`: directorio actual y padres;
   `Release`: `LocalApplicationData` + importación portable).
3. Crea **un** `AppDbContext` con `DefaultTimeout = 30` (espera el bloqueo de
   SQLite en vez de fallar de inmediato).
4. Crea un servicio por entidad + `SesionActual`, `RespaldoService` y
   `VersionService`.
5. Crea un ViewModel por pantalla y el `MainViewModel`; asigna `DataContext` a
   `MainWindow` (inhabilitada hasta iniciar sesión).
6. En `Opened`: respaldo previo si hay migraciones pendientes, `MigrateAsync()`,
   luego `IniciarSesionAsync`.
7. `IniciarSesionAsync`: abre el `SetupWindow` si no existe ningún usuario y
   luego el `LoginWindow`; al entrar, `sesion.Iniciar`, `AplicarSesion`,
   `InicializarAsync` y sondeo de versión remota en segundo plano.
8. En `desktop.Exit`: `Dispose()` del contexto.

## Sesión y roles

- `SesionActual` guarda el `Usuario` activo; `EsAdministrador` se consulta
  desde ViewModels y servicios.
- `MainViewModel` bloquea los comandos de módulos administrativos
  (Productos, Proveedores, Inventario, Compras, Ventas, Usuarios,
  Configuración); la caja está disponible para ambos roles.
- Los servicios también validan rol en operaciones sensibles (respaldos,
  cancelaciones) además de la UI.

## Servicios transversales

- `RegistroApp`: log en `LocalApplicationData/SistemaInventario/logs/app-*.log`
  con retención de 30 días. Nunca lanza.
- `InstanciaUnica`: bloqueo de archivo para una sola ejecución.
- `RespaldoService`: respaldo con `BackupDatabase` de SQLite; nombra
  `respaldo_<fecha>_<hash>.db` en la carpeta destino.
- `VersionService`: versión del ensamblado o de la última release de GitHub.

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

## Modelo de datos

```text
Usuario        Producto       Proveedor      Compra
DetalleCompra  MovimientoInventario            Caja
MovimientoCaja Venta          DetalleVenta
```

- `Producto` no tiene categoría.
- No hay entidad `Cliente`: la venta es a consumidor final.
- Enums: `RolUsuario`, `EstadoCompra`, `EstadoVenta`, `EstadoCaja`,
  `TipoMovimiento`, `TipoMovimientoCaja`, `MetodoPago`.

## Cómo agregar un módulo

1. `Models/<Entidad>.cs` (y enum en `Models/Enums/` si aplica).
2. `DbSet` + mapeo en `Data/AppDbContext.cs`.
3. `Service/<Entidad>Service.cs` con el patrón de `RespaldoService` y `Resultado`.
4. `ViewModels/<Entidades>ViewModel.cs` heredando `OperacionViewModel`.
5. `Views/<Entidades>View.axaml` + code-behind.
6. `DataTemplate` en `MainWindow.axaml`, botón en `SidebarView.axaml`,
   comando en `MainViewModel` y creación en `App.axaml.cs`.
7. Instancia en `Views/DesignTime/DesignTimeData.cs`.
8. `cd PosInventario && dotnet tool run dotnet-ef migrations add Add<Entidades>`.
9. Test en `PosInventario.Tests/<Entidad>ServiceTests.cs`.
10. `dotnet build PosInventario.sln` y `dotnet test PosInventario.sln`.

## Próximo paso

La base está estable y sus 131 pruebas pasan. El trabajo pendiente (respaldo
automático, restauración, reportes, instalador) está ordenado por fases y
prioridades en `PENDIENTES.md`.

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

## Módulos incluidos

- Sesión: `LoginViewModel`/`SetupViewModel` con `UsuarioService`
  (hash de contraseña, activación/desactivación, restablecimiento).
- Inicio: resumen del día con `InicioViewModel`.
- Caja: `CajaService` + `VentaService` (apertura/cierre con diferencia,
  cobros con descuento, pagos mixtos, anulación, movimientos de efectivo,
  ticket en pantalla, desglose por método de pago). El cobro mixto se captura
  con un selector que muestra los montos de efectivo, tarjeta y
  transferencia; la suma debe cubrir el total. Cada venta guarda sus pagos en
  `PagoVenta`; el cambio solo se calcula sobre el efectivo y solo esa parte
  entra a la caja. Las devoluciones (`VentaService.DevolverAsync`) pueden ser
  totales o parciales: regresan el stock, acumulan `CantidadDevuelta` por
  línea y `TotalDevuelto` por venta, y reembolsan el efectivo desde la caja
  abierta (la original si sigue abierta); en ventas mixtas el reembolso se
  reparte proporcionalmente entre los pagos originales y cada uno acumula su
  `MontoDevuelto`. Una caja cerrada nunca se recalcula. `AnularAsync` es el
  caso especial de devolver todo lo pendiente.
- Catálogos e inventario: `ProductoService`, `ProveedorService`,
  `InventarioService` (motivos de ajuste). En Productos el campo SKU es
  también el código de barras: al escanear y presionar Enter se carga el
  producto existente o se prepara el alta con ese SKU (`EscanearCodigoAsync`).
  En Caja el mismo código agrega el producto a la venta.
- Compras: `CompraService` con líneas y actualización de stock.
- Historial de ventas: `VentasViewModel` con filtros y reimpresión. El detalle
  de una caja cerrada (`CajaHistorialWindow`) abre el ticket de cada venta con
  el botón "Ver ticket" o doble clic, incluidas las canceladas.
- Impresión: `GeneradorTicketTexto` arma el ticket en 32 columnas (58 mm) o 48
  (80 mm); `ImpresoraSistema` lo envía a la impresora predeterminada (API de
  Windows con tamaño de papel o CUPS en Linux/macOS). Un fallo se devuelve como
  `Resultado` y se avisa en la ventana del ticket; la venta ya registrada no se
  pierde y la aplicación no se bloquea.
- Atajos y foco: `CajaView` captura F1–F10, Esc, Ctrl+Enter, Delete y +/-;
  al entrar a Caja enfoca la búsqueda y la restaura al cerrar diálogos. F2/F4
  se capturan en fase Tunnel para que ComboBox/DataGrid no los consuman; Esc
  en fase Bubble para respetar diálogos y desplegables. El detalle está en
  `README.md`.
- Devoluciones: `DevolverVentaViewModel`/`DevolverVentaWindow` con checklist
  por producto o por unidad cuando hay varias iguales, con "Seleccionar
  todos" y reembolso estimado antes de confirmar. Solo el vendedor original
  o un administrador, desde la caja abierta ("Devolver venta"), el detalle de
  una caja o el historial de ventas.
  Los totales de Inicio, cierre e historiales se muestran netos de
  devoluciones (`Venta.TotalNeto`).
- Configuración: respaldo manual desde la UI (solo administrador).

## Módulos opcionales (no incluidos)

PDF (QuestPDF), actualización automática y configuración persistente. Se
agregan según el roadmap de `PENDIENTES.md`.

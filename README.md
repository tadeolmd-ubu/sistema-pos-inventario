# Sistema de inventario

Aplicación de escritorio POS + inventario construida con Avalonia, MVVM
(CommunityToolkit), EF Core y SQLite. La v1 está implementada y funcional:
todos los módulos operan sobre la base local con sesión, roles, respaldos y
registro de errores.

## Estado actual (noviembre 2026, rama `dev`)

- **Sesión:** asistente de primer administrador (setup), login y cierre de
  sesión. Roles `Administrador` y `Cajero` con menús restringidos.
- **Inicio:** resumen del día (ventas, ingresos, stock bajo, compras recientes).
- **Caja (POS):** apertura y cierre con diferencia, cobro en efectivo, tarjeta
  o transferencia, descuento sobre el subtotal, búsqueda y alta por SKU
  (compatible con lector de código de barras), ticket en pantalla,
  devoluciones totales o parciales desde la caja en curso, el detalle de una
  caja o el historial de ventas (checklist por unidad y reembolso en la caja
  abierta) y movimientos de entrada/salida de efectivo.
- **Catálogos:** productos y proveedores (solo administrador). El campo
  **SKU es también el código de barras**: al escanear (o escribir) un código
  en Productos y presionar Enter se cargan los datos del producto si existe,
  o se prepara el alta con ese SKU si es nuevo.
- **Inventario:** entradas/salidas con motivo y ajuste de stock.
- **Compras:** registro con proveedor, líneas y actualización de costo/stock.
- **Ventas:** historial global con filtros por fecha y búsqueda, desglose por
  método de pago, reimpresión de ticket y botón de devolución (checklist por
  producto o unidad, con reembolso estimado). Cada venta muestra su total neto
  y, si aplica, cuánto se le ha devuelto.
- **Usuarios:** alta, edición, activación/desactivación y restablecimiento de
  contraseña.
- **Configuración:** respaldos manuales con `BackupDatabase` de SQLite.
- **Operación:** instancia única con bloqueo de archivo, manejo global de
  errores, logs en `LocalApplicationData/SistemaInventario/logs` y textos en
  español de México.
- **Tests:** 100 pruebas xUnit sobre servicios y ViewModels (SQLite en memoria y
  en disco); build de Windows en CI.

Lo que falta para llevarlo a un negocio real está ordenado por fases en
`PENDIENTES.md`.

## Requisitos

- .NET 10 SDK
- Windows x64 para publicar (desarrollo posible en Linux/macOS)

## Desarrollo

```bash
dotnet restore
dotnet build PosInventario.sln
dotnet test PosInventario.sln
dotnet run --project PosInventario/PosInventario.csproj
```

## Base de datos

SQLite local (`SistemaInventario.db`). Las migraciones se aplican al iniciar;
antes de migrar se crea un respaldo previo y, si falla, no se migra.

- En `Debug` la base se busca en el directorio actual y sus padres.
- En `Release` se usa `%LOCALAPPDATA%/SistemaInventario/SistemaInventario.db`
  (o `~/.local/share/SistemaInventario` en Linux) y se importa una base
  portable si existe junto al ejecutable.

Crear/actualizar la base en desarrollo:

```bash
cd PosInventario
dotnet tool restore
dotnet tool run dotnet-ef migrations add <Nombre>
dotnet tool run dotnet-ef database update
```

## Publicación

El workflow `.github/workflows/deploy.yml` ejecuta las pruebas, publica
`win-x64` self-contained y crea una release en cada push a `main`
(repositorio `tadeolmd-ubu/sistema-pos-inventario`).

## Estructura

```text
PosInventario/          aplicación (Models, Data, Migrations, Service, ViewModels, Views, Controls, Styles)
PosInventario.Tests/    pruebas xUnit sobre SQLite en memoria y en disco
```

Reglas de capas y convenciones: ver `ARQUITECTURA-BASE.md` y `COLABORACION.md`.

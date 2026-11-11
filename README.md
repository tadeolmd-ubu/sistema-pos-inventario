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
  o transferencia, **pagos mixtos** (se elige "Mixto" y se capturan los tres
  montos; la suma debe cubrir el total y el excedente en efectivo es cambio),
  descuento sobre el subtotal, búsqueda y alta por SKU
  (compatible con lector de código de barras), ticket en pantalla,
  devoluciones totales o parciales desde la caja en curso, el detalle de una
  caja o el historial de ventas (checklist por unidad y reembolso en la caja
  abierta; en ventas mixtas el reembolso se reparte entre los métodos
  originales) y movimientos de entrada/salida de efectivo. Al cerrar, si el
  monto contado difiere del esperado, muestra el faltante o sobrante y pide
  confirmación antes de cerrar.
- **Catálogos:** productos y proveedores (solo administrador). El campo
  **SKU es también el código de barras**: al escanear (o escribir) un código
  en Productos y presionar Enter se cargan los datos del producto si existe,
  o se prepara el alta con ese SKU si es nuevo.
- **Inventario:** entradas/salidas con motivo y ajuste de stock.
- **Compras:** registro con proveedor, líneas y actualización de costo/stock.
- **Ventas:** historial global con filtros por fecha y búsqueda, desglose por
  método de pago, reimpresión de ticket desde el historial o el detalle de una
  caja cerrada y botón de devolución (checklist por producto o unidad, con
  reembolso estimado). Cada venta muestra su total neto y, si aplica, cuánto se
  le ha devuelto. El ticket se puede imprimir en papel de 58 mm u 80 mm; si no
  hay impresora, la venta ya quedó registrada y solo se muestra un aviso.
- **Usuarios:** alta, edición, activación/desactivación y restablecimiento de
  contraseña.
- **Configuración:** respaldos manuales con `BackupDatabase` de SQLite.
- **Operación:** instancia única con bloqueo de archivo, manejo global de
  errores, logs en `LocalApplicationData/SistemaInventario/logs` y textos en
  español de México.
- **Tests:** 135 pruebas xUnit sobre servicios y ViewModels (SQLite en memoria y
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

## Atajos de teclado

En Caja el foco entra automáticamente en la búsqueda. Al cerrar un diálogo o
el ticket, el foco regresa a la búsqueda para seguir escaneando.

El menú lateral tiene un panel **Comandos** que cambia según la pestaña y el
control enfocado: búsqueda, carrito, descuento, pago, selector, lista, botón,
casilla o fecha. **F1** muestra la ayuda del contexto actual; en Caja abre el
listado completo.

| Atajo | Acción | Caso de uso |
|---|---|---|
| **F1** | Ayuda de atajos | Consultar el listado completo |
| **F2** | Enfocar búsqueda/SKU | Escanear o buscar sin usar el mouse |
| **F3** | Seleccionar productos | Agregar productos sin código de barras |
| **F4** o **Ctrl+Enter** | Cobrar | Cerrar la venta y abrir el ticket |
| **F5** | Actualizar caja | Refrescar movimientos e historial |
| **F6** | Enfocar descuento | Aplicar un descuento sobre el subtotal |
| **F7** | Enfocar monto de pago | Capturar el efectivo o el monto con el que se paga |
| **F8** | Devolver venta | Abrir el selector de ventas para devolución |
| **F9** | Registrar movimiento | Ingreso o retiro de efectivo |
| **F10** | Cerrar caja | Abrir la ventana de cierre |
| **Esc** | Limpiar venta / cerrar diálogo | Cancelar la captura actual; pide confirmación si hay productos |
| **Enter en búsqueda** | Agregar producto por SKU | Escanear o teclear un código |
| **Enter en pago** | Cobrar | Confirmar la venta desde el campo de pago |
| **Delete** | Quitar la línea seleccionada | Corregir el carrito con el teclado |
| **+ / -** | Cambiar cantidad de la línea seleccionada | Ajustar unidades sin botones |
| **↑/↓ + Enter** (selector de productos) | Navegar y agregar | Elegir un producto sin mouse |
| **Ctrl+P** (ticket) | Imprimir | Imprimir o reimprimir el ticket |
| **Tab / Shift+Tab** | Navegar entre controles | Operar toda la caja sin mouse |

Si el foco está en un campo de texto con contenido, **Esc** limpia primero ese
campo; si está vacío, limpia la venta (con confirmación cuando hay productos).
En el selector de productos, el primer resultado queda seleccionado al filtrar
y **Enter** lo agrega.

## Impresión de tickets

El ticket tiene un selector de 58 u 80 mm y un botón **Imprimir**. En Windows
se usa la impresora predeterminada y la app fija el tamaño del papel; en
Linux/macOS se envía a CUPS con `lp` o `lpr`. Si no hay impresora o falla, la
venta queda registrada y el ticket solo muestra un aviso, sin bloquear la
aplicación. La impresión ESC/POS directa y la apertura del cajón quedan para
una etapa posterior.

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

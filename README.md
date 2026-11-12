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
  confirmación antes de cerrar; después abre el **corte de caja imprimible**
  con las ventas, el desglose por método y la comparación de efectivo.
- **Catálogos:** productos y proveedores (solo administrador). El campo
  **SKU es también el código de barras**: al escanear (o escribir) un código
  en Productos y presionar Enter se cargan los datos del producto si existe,
  o se prepara el alta con ese SKU si es nuevo.
- **Inventario:** entradas/salidas con motivo, ajuste de stock e historial de
  movimientos por rango de fechas, paginado y exportable a CSV.
- **Compras:** registro con proveedor, líneas y actualización de costo/stock;
  historial por rango de fechas paginado y exportable a CSV.
- **Ventas:** historial global con filtros por fecha y búsqueda, desglose por
  método de pago, paginación, reimpresión de ticket desde el historial o el
  detalle de una caja cerrada, botón de devolución (checklist por producto o
  unidad, con reembolso estimado) y exportación a CSV de las ventas filtradas.
  Cada venta muestra su total neto y, si aplica, cuánto se le ha devuelto. El
  ticket se puede imprimir en papel de 58 mm u 80 mm; si no hay impresora, la
  venta ya quedó registrada y solo se muestra un aviso.
- **Usuarios:** alta, edición, activación/desactivación y restablecimiento de
  contraseña.
- **Configuración:** respaldos manuales con `BackupDatabase` de SQLite y
  restauración desde un archivo validado (solo administrador; crea un respaldo
  de seguridad y reinicia la aplicación al terminar). También permite activar
  respaldos automáticos, elegir cuántos días conservarlos y guardar esos
  ajustes. Cada respaldo se verifica antes de darlo por bueno.
- **Operación:** instancia única con bloqueo de archivo, manejo global de
  errores, logs en `LocalApplicationData/SistemaInventario/logs` y textos en
  español de México.
- **Tests:** 178 pruebas xUnit sobre servicios y ViewModels (SQLite en memoria y
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

La restauración desde Configuración valida el archivo (`integrity_check` y
tablas del sistema), crea un respaldo de seguridad de la base actual y la
reemplaza con confirmación. Al terminar, la aplicación se reinicia.

Los respaldos automáticos se crean al iniciar sesión (máximo uno por día) y al
cerrar la caja. Se guardan en la carpeta `Respaldos` junto a la base, con el
prefijo `respaldo_auto_`; la retención en días solo elimina esos archivos y
nunca los respaldos manuales. Los ajustes se guardan en
`LocalApplicationData/SistemaInventario/configuracion.json`.

En Configuración, elegir una carpeta solo cambia el destino del próximo
respaldo manual; el archivo se crea al presionar "Crear respaldo".

Antes de guardar un respaldo se comprueba que el archivo generado abra, pase
`PRAGMA integrity_check` y tenga las tablas mínimas del sistema. Si la
verificación falla, el respaldo se descarta.

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
| **Ctrl+P** (ticket o corte) | Imprimir | Imprimir o reimprimir el documento |
| **Tab / Shift+Tab** | Navegar entre controles | Operar toda la caja sin mouse |

Si el foco está en un campo de texto con contenido, **Esc** limpia primero ese
campo; si está vacío, limpia la venta (con confirmación cuando hay productos).
En el selector de productos, el primer resultado queda seleccionado al filtrar
y **Enter** lo agrega.

## Impresión de tickets y corte de caja

El ticket y el corte de caja tienen un selector de 58 u 80 mm y un botón
**Imprimir**. En Windows se usa la impresora predeterminada y la app fija el
tamaño del papel; en Linux/macOS se envía a CUPS con `lp` o `lpr`. Si no hay
impresora o falla, la operación ya quedó registrada y solo se muestra un aviso,
sin bloquear la aplicación.

Al cerrar una caja se abre el corte con las ventas, el desglose por método de
pago, el efectivo esperado y contado y la diferencia (faltante, sobrante o sin
diferencia). El corte se puede reimprimir desde el detalle de una caja cerrada
en el historial. La impresión ESC/POS directa y la apertura del cajón quedan
para una etapa posterior.

## Paginación

Ventas, Inventario, Compras y el selector de productos de Caja consultan la
base por páginas: no cargan todo el historial en memoria. El control
reutilizable `Paginador` muestra el rango visible, permite elegir **10, 25 o
50** registros por página (inicia en 10) y navega con
Primera/Anterior/Siguiente/Última. La consulta usa `Skip`/`Take` y un `Count`
para conocer el total; los filtros de fecha, búsqueda, producto y estado se
aplican en la misma consulta. El estado compartido vive en
`PaginacionViewModel`, de modo que otros listados pueden reutilizarlo.

El selector de productos de Caja usa la misma paginación, busca por SKU o
nombre directamente en la base y espera 200 ms tras la última tecla para no
lanzar una consulta por cada pulsación.

## Exportación a CSV

Ventas, Inventario y Compras tienen un botón **Exportar CSV** que guarda
exactamente las filas de la **página visible** en ese momento:

- **Ventas:** respeta el rango de fechas, la búsqueda y la casilla "Ver
  anuladas". Incluye ticket, fecha, cajero, método, estado, subtotal,
  descuento, total, devuelto y neto.
- **Inventario:** respeta el rango de fechas y, si hay un producto
  seleccionado, sus movimientos. Incluye producto, SKU, tipo, cantidad, stock
  anterior/resultante, referencia, fecha y usuario.
- **Compras:** respeta el rango de fechas. Incluye compra, fecha, proveedor,
  comprobante, estado, artículos, total y usuario.

El resumen del módulo cuenta todos los registros del filtro, mientras que el
CSV contiene solo la página actual. Desde la segunda página el nombre del
archivo agrega `_paginaN` para no sobrescribir exportaciones anteriores.

Los archivos se guardan en UTF-8 con BOM y usan coma como separador y punto
decimal, de modo que Excel los abre con acentos y columnas correctas. Los
campos con comas, comillas o saltos de línea se escapan, y los textos que
empiezan con `=`, `+`, `-` o `@` se neutralizan para evitar fórmulas. Si no hay
filas en el rango, el archivo solo contiene el encabezado.

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

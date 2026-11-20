# Guía de pruebas en Windows 11

Esta guía es para probar el sistema completo en una instalación real de
**Windows 11 de 64 bits** antes de entregarlo a un cliente. No sustituye la
matriz visual de [`PRUEBAS-WINDOWS.md`](PRUEBAS-WINDOWS.md), la complementa.

> Objetivo: confirmar que instala, abre, vende, respalda, migra y se comporta
> bien en Windows 11, incluyendo los módulos nuevos de clientes, crédito,
> productos por peso y exportación a Excel.

## 0. Antes de empezar

- [ ] Haz un respaldo de cualquier base real que tengas en la PC.
- [ ] No uses la base de un cliente para pruebas destructivas.
- [ ] Ten a mano una USB para probar el respaldo.
- [ ] Confirma la versión de Windows:
      `winver`
- [ ] Confirma que es de 64 bits.
- [ ] Ten al menos 1 GB libre; con 200 GB estás sobrado.
- [ ] Ten el manual y este documento abiertos.
- [ ] Anota la versión que vas a probar:
      `v________________`

Rutas importantes en Windows:

```text
Instalación normal:     %LOCALAPPDATA%\Programs\Sistema de inventario
Datos, respaldos, logs: %LOCALAPPDATA%\SistemaInventario
Base:                   %LOCALAPPDATA%\SistemaInventario\SistemaInventario.db
Respaldos:              %LOCALAPPDATA%\SistemaInventario\Respaldos
Logs:                   %LOCALAPPDATA%\SistemaInventario\logs
```

---

## 1. Preparar el código y compilar

Abre **PowerShell** en la carpeta del repositorio.

```powershell
git switch dev
git pull origin dev

dotnet --info
dotnet restore
dotnet build PosInventario.sln
dotnet test PosInventario.sln -c Release
```

Resultado esperado:

- `dotnet --info` muestra **.NET SDK 10**.
- Build: **0 errores, 0 advertencias**.
- Tests: **240 pruebas correctas**.

Si no tienes el SDK:

```powershell
winget install Microsoft.DotNet.SDK.10
```

Cierra y vuelve a abrir PowerShell después de instalarlo.

---

## 2. Probar la app desde código, con base limpia

La app en **Debug** busca `SistemaInventario.db` en la carpeta actual y sus
padres. Para no tocar una base real, ejecuta el `.dll` desde una carpeta de
prueba.

```powershell
dotnet build PosInventario.sln

New-Item -ItemType Directory -Force C:\POS-Pruebas\limpia
Set-Location C:\POS-Pruebas\limpia

dotnet C:\ruta\al\repo\PosInventario\bin\Debug\net10.0\PosInventario.dll
```

Sustituye `C:\ruta\al\repo` por la ruta real.

Resultado esperado:

- Se abre la ventana **Configuración inicial**.
- Se crea un archivo `SistemaInventario.db` en `C:\POS-Pruebas\limpia`.
- No se usa la base de otro cliente.

### Primer administrador

Crea:

```text
Usuario:    admin
Nombre:     Administrador de prueba
Contraseña: PruebaSegura123
```

- [ ] El administrador se crea correctamente.
- [ ] El login funciona.
- [ ] La app muestra la versión local.
- [ ] Se ve el menú completo para administrador.

---

## 3. Probar el instalador de Windows

La app en **Release** usa `%LOCALAPPDATA%\SistemaInventario`. Si ya tienes
datos reales ahí, respáldalos o usa otra cuenta de Windows para esta prueba.

### Opción A: compilar el instalador localmente

Necesitas Inno Setup 6.

```powershell
dotnet publish PosInventario/PosInventario.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishTrimmed=false -o publish

& "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" installer\PosInventario.iss
```

El instalador queda en `dist\`.

### Opción B: usar el instalador de la release

Descarga el instalador desde la release del repositorio o usa el que generó
GitHub Actions.

### Instalar

- [ ] El instalador abre.
- [ ] Si aparece SmartScreen, se puede continuar con **Más información →
      Ejecutar de todas formas**.
- [ ] Instala **por usuario**, sin pedir administrador.
- [ ] Crea acceso directo en el menú Inicio.
- [ ] Crea acceso directo en el escritorio si se marcó la casilla.
- [ ] Abre desde el menú Inicio.
- [ ] La app funciona sin instalar .NET aparte.

### Desinstalar

- [ ] El desinstalador aparece en **Configuración → Aplicaciones**.
- [ ] Desinstala sin borrar `%LOCALAPPDATA%\SistemaInventario`.
- [ ] La base, respaldos y logs siguen existiendo.
- [ ] Se puede reinstalar y conservar los datos.

---

## 4. Pruebas automáticas y smoke test

- [ ] `dotnet test PosInventario.sln -c Release` pasa completo.
- [ ] Abrir la app.
- [ ] Iniciar sesión.
- [ ] Abrir caja con $500.
- [ ] Vender un producto a público.
- [ ] Cerrar caja.
- [ ] Volver a abrir la app y confirmar que los datos siguen ahí.
- [ ] Abrir una segunda instancia de la app: debe avisar que ya está abierta.
- [ ] Revisar que se creó el log del día en la carpeta de logs.

---

## 5. Datos de prueba recomendados

Crea estos datos para probar todos los módulos.

### Productos

| SKU | Nombre | Unidad | Precio | Costo | Stock mínimo |
|---|---|---|---:|---:|---:|
| `PZA-001` | Refresco 600 ml | Pieza | 18 | 12 | 5 |
| `PZA-002` | Sabritas | Pieza | 15 | 9 | 5 |
| `KG-001` | Carne molida | Kilogramo | 120 | 80 | 1.5 |
| `KG-002` | Manzana | Kilogramo | 35 | 20 | 2 |

### Proveedor

```text
Nombre: Distribuidora Guasave
Teléfono: 555-1000
```

### Cliente

```text
Nombre: María López
Teléfono: 555-123-4567
Límite de crédito: 500
```

### Usuario cajero

```text
Usuario: cajero1
Nombre: Cajero de prueba
Contraseña: CajeroSeguro123
Rol: Cajero
```

---

## 6. Matriz funcional

Marca cada punto. Si algo no aplica, escribe **N/A**.

### 6.1 Productos

- [ ] Crear producto por pieza.
- [ ] Crear producto por kilogramo.
- [ ] El precio se muestra como `120.00/kg` en productos por peso.
- [ ] El stock se muestra en `pza` o `kg`.
- [ ] Editar nombre, precio, costo y stock mínimo.
- [ ] Un producto por pieza rechaza stock mínimo decimal.
- [ ] Un producto por peso acepta `1.5` de stock mínimo.
- [ ] Archivar un producto con stock 0 funciona.
- [ ] Archivar un producto con stock mayor a 0 se bloquea.
- [ ] Restaurar un producto archivado.
- [ ] Cambiar unidad de pieza a kilo sin historial funciona.
- [ ] Cambiar unidad con stock o historial se bloquea.

### 6.2 Inventario

- [ ] Entrada de 10 piezas a `PZA-001`.
- [ ] Entrada de `2.500 kg` a `KG-001`.
- [ ] Salida de 1 pieza.
- [ ] Salida de `0.250 kg`.
- [ ] Ajuste de stock contado.
- [ ] Producto por pieza rechaza cantidad `0.5`.
- [ ] Producto por peso acepta `0.250`.
- [ ] Cada movimiento aparece en el historial.
- [ ] El historial muestra unidad, stock anterior y resultante.

### 6.3 Compras

- [ ] Crear compra con proveedor.
- [ ] Agregar `PZA-001` con cantidad 10 y costo 12.
- [ ] Agregar `KG-001` con cantidad `1.250` y costo 80.
- [ ] El subtotal de la línea por peso se calcula bien.
- [ ] Al registrar, el stock aumenta.
- [ ] El costo del producto se actualiza.
- [ ] Anular la compra revierte el stock.
- [ ] Exportar CSV de compras.

### 6.4 Caja y ventas

- [ ] Abrir caja con $500.
- [ ] Vender `PZA-001` a **Público**.
- [ ] Vender `PZA-002` seleccionando a **María López**.
- [ ] Vender `KG-001` con `0.750 kg`.
- [ ] Editar la cantidad de una línea por peso.
- [ ] Aplicar descuento.
- [ ] Cobrar en efectivo y verificar cambio.
- [ ] Cobrar con tarjeta.
- [ ] Cobrar con transferencia.
- [ ] Cobrar con pago mixto.
- [ ] Verificar que el ticket muestra cliente solo si se seleccionó.
- [ ] Verificar que el ticket por peso muestra `kg` y precio por kg.
- [ ] Imprimir ticket en 58 mm.
- [ ] Imprimir ticket en 80 mm.
- [ ] Probar sin impresora: la venta se registra y solo avisa.
- [ ] Reimprimir ticket desde el historial.

### 6.5 Devoluciones

- [ ] Devolver una unidad de una venta por pieza.
- [ ] Devolver `0.250 kg` de una venta por peso.
- [ ] Verificar que el stock regresa.
- [ ] Verificar que el reembolso se calcula.
- [ ] Verificar que no se puede devolver más de lo vendido.
- [ ] Verificar que no se puede devolver dos veces lo mismo.
- [ ] Devolver una venta pagada con tarjeta sin caja abierta.
- [ ] Devolver una venta en efectivo con caja abierta.
- [ ] Verificar el movimiento de caja del reembolso.

### 6.6 Clientes y crédito

- [ ] Crear cliente con límite de crédito.
- [ ] Venta a crédito dentro del límite.
- [ ] La venta a crédito no entra al efectivo de la caja.
- [ ] El saldo del cliente aumenta.
- [ ] Venta a crédito sin cliente se rechaza.
- [ ] Venta a crédito que supera el límite se rechaza.
- [ ] Abono en efectivo con caja abierta.
- [ ] Abono con tarjeta o transferencia.
- [ ] El abono reduce el saldo.
- [ ] Un abono mayor al pendiente se rechaza.
- [ ] Devolución de venta a crédito reduce el saldo.
- [ ] Cliente con saldo no se puede desactivar.
- [ ] Cliente sin saldo sí se puede desactivar.

### 6.7 Historial de ventas

- [ ] Filtrar por fecha.
- [ ] Buscar por ticket.
- [ ] Buscar por cajero.
- [ ] Filtrar por método, incluido **Crédito**.
- [ ] Ver anuladas.
- [ ] Ver columna Cliente y valor **Público** cuando no hay cliente.
- [ ] Exportar CSV de la página visible.
- [ ] Abrir el CSV en Excel y revisar acentos y columnas.

### 6.8 Reportes y Excel

- [ ] Generar reporte de hoy.
- [ ] Agrupar por día, semana y mes.
- [ ] Ver ventas netas, devoluciones, utilidad y margen.
- [ ] Ver desglose por método, incluido **Crédito**.
- [ ] Ver productos más vendidos con cantidad y unidad.
- [ ] Exportar a Excel.
- [ ] Abrir el `.xlsx` en Excel.
- [ ] Revisar hojas Resumen, Por período, Métodos de pago y Productos.
- [ ] Confirmar que los totales coinciden con la pantalla.

### 6.9 Importación CSV

Descarga la plantilla desde **Productos → Descargar plantilla**.

Prueba un archivo como:

```csv
sku,nombre,unidad,descripcion,precio_venta,costo,stock_minimo,stock_inicial,proveedor,proveedor_telefono,proveedor_email,proveedor_direccion
IMP-001,Producto importado,pieza,,25,15,2,10,Proveedor Importado,555-2000,,
IMP-002,Producto a granel,kg,,35,20,1.5,2.5,,,,
```

- [ ] La vista previa muestra filas y errores.
- [ ] Importa productos nuevos.
- [ ] Importa producto por kg con stock decimal.
- [ ] Actualiza un SKU existente sin tocar su stock.
- [ ] Rechaza una fila con precio inválido.
- [ ] Rechaza una fila por pieza con stock decimal.
- [ ] Rechaza SKU repetido.
- [ ] No importa nada si hay errores.

### 6.10 Usuarios y permisos

- [ ] Crear cajero.
- [ ] Iniciar sesión como cajero.
- [ ] El cajero puede vender y abrir/cerrar caja.
- [ ] El cajero no entra a Productos, Proveedores, Clientes, Inventario,
      Compras, Ventas, Reportes, Usuarios ni Configuración.
- [ ] Restablecer contraseña.
- [ ] Desactivar y activar usuario.
- [ ] No se puede desactivar al último administrador.
- [ ] No se puede duplicar nombre de usuario.

### 6.11 Configuración y respaldos

- [ ] Crear respaldo manual.
- [ ] El respaldo se valida y muestra la ruta.
- [ ] Copiar el respaldo a USB.
- [ ] Activar/desactivar respaldos automáticos.
- [ ] Cambiar retención de días.
- [ ] Guardar configuración.
- [ ] Verificar que el respaldo automático se crea al iniciar sesión.
- [ ] Verificar que se crea al cerrar caja.
- [ ] Restaurar un respaldo en una copia de prueba.
- [ ] La restauración crea respaldo de seguridad y reinicia la app.
- [ ] Rechazar un archivo corrupto.
- [ ] Abrir la carpeta de logs y enviar el último archivo (si el botón está
      disponible; si no, abrir la ruta manualmente).

### 6.12 Migración de una base existente

- [ ] Copia una base real a `C:\POS-Pruebas\migracion\SistemaInventario.db`.
- [ ] Ejecuta la app desde esa carpeta.
- [ ] La app aplica las migraciones.
- [ ] Los productos y el stock se ven correctos.
- [ ] Las cantidades enteras siguen mostrándose como enteros.
- [ ] Una venta antigua abre su ticket.
- [ ] Un reporte antiguo conserva totales.
- [ ] Se crea un respaldo previo a migrar.

### 6.13 Actualización automática

- [ ] Con internet, al abrir una versión anterior aparece el aviso
      **Actualización disponible** con la versión instalada y la disponible.
- [ ] **Más tarde** cierra el aviso sin cambiar nada.
- [ ] **Actualizar ahora** descarga, crea un respaldo, reemplaza los archivos
      y reabre la app.
- [ ] La versión mostrada en el menú corresponde a la release nueva.
- [ ] La base, los respaldos y la configuración siguen disponibles.
- [ ] Se agregó un respaldo nuevo en `Respaldos` con la fecha del cambio.
- [ ] Sin internet el aviso no aparece y la app abre normal.
- [ ] Una copia portable abre la página de descarga en lugar de instalar.

---

## 7. Pruebas de interfaz en Windows 11

Prueba al menos estas resoluciones y escalados:

- [ ] 1366 × 768 al 100 %.
- [ ] 1366 × 768 al 125 %.
- [ ] 1920 × 1080 al 100 %.
- [ ] 1920 × 1080 al 125 %.
- [ ] 1920 × 1080 al 150 % si el equipo lo permite.

Revisa:

- [ ] La ventana principal no se sale de la pantalla.
- [ ] El menú lateral se puede recorrer.
- [ ] Las tablas tienen desplazamiento.
- [ ] Los diálogos de peso, abono, devolución y cierre caben.
- [ ] Los botones son alcanzables.
- [ ] Los textos no se enciman.
- [ ] El ticket y el corte se ven completos.
- [ ] El foco de teclado funciona en Caja.

---

## 8. Hardware

### Lector de código de barras

- [ ] El lector escribe el código como teclado.
- [ ] Enter agrega el producto.
- [ ] Un código inexistente muestra aviso.
- [ ] Un producto archivado no se vende.

### Impresora térmica

- [ ] Instalar controlador de 58 mm u 80 mm.
- [ ] Imprimir ticket de venta.
- [ ] Imprimir corte de caja.
- [ ] Reimprimir ticket desde historial.
- [ ] Probar sin impresora y confirmar que la venta no se pierde.

### Cajón de dinero

- [ ] Confirmar si el cliente lo usa.
- [ ] Si no hay soporte directo, documentar que se abre desde la impresora o
      manualmente. No marcarlo como incluido si no se probó.

---

## 9. Casos límite

- [ ] SKU vacío.
- [ ] Nombre menor a 3 caracteres.
- [ ] Precio negativo.
- [ ] Costo negativo.
- [ ] Stock mínimo negativo.
- [ ] Cantidad 0 o negativa.
- [ ] Venta sin productos.
- [ ] Venta sin caja abierta.
- [ ] Descuento mayor al subtotal.
- [ ] Pago insuficiente.
- [ ] Pago electrónico mayor al total.
- [ ] Mezcla de pagos que no cubre el total.
- [ ] Producto sin stock.
- [ ] Devolución mayor a lo vendido.
- [ ] Abono mayor al saldo.
- [ ] Restauración de archivo corrupto.
- [ ] Importación con errores.
- [ ] Segunda instancia de la app.

---

## 10. Evidencias

Guarda en una carpeta:

```text
C:\POS-Pruebas\evidencias\
├── capturas\
├── logs\
├── respaldos\
└── resultados.md
```

Registra:

- versión probada,
- fecha y hora,
- versión de Windows,
- resolución y escalado,
- si usaste base limpia o migrada,
- resultado de cada bloque,
- errores con captura y log,
- nombre del respaldo probado.

Criterio de aprobación:

- [ ] Build y tests pasan.
- [ ] La app abre e inicia sesión.
- [ ] Se puede vender, devolver, cobrar crédito y abonar.
- [ ] Se puede cerrar caja y ver el corte.
- [ ] Se puede exportar Excel y CSV.
- [ ] Se puede respaldar y restaurar en una copia.
- [ ] El instalador instala y desinstala sin borrar datos.
- [ ] No hay errores bloqueantes en los casos límite.
- [ ] La interfaz se ve bien en al menos 1366×768 y 1920×1080.

---

## 11. Limpieza después de probar

- [ ] Restaurar la base real si moviste la original.
- [ ] Borrar `C:\POS-Pruebas\limpia` y `C:\POS-Pruebas\migracion`.
- [ ] Desinstalar la versión de prueba.
- [ ] Restaurar `%LOCALAPPDATA%\SistemaInventario` si lo moviste.
- [ ] No borrar respaldos reales.
- [ ] No subir bases de prueba al repositorio.

---

## 12. Problemas comunes

### SmartScreen

Es normal si el instalador no tiene firma de código. Usa **Más información →
Ejecutar de todas formas** y documenta que es una versión de prueba.

### No abre la segunda instancia

Es el comportamiento correcto: hay una sola instancia por usuario. Cierra la
primera o revisa `%LOCALAPPDATA%\SistemaInventario\instancia.lock`.

### La base está bloqueada

Cierra la app. Si persiste, revisa el administrador de tareas y el archivo
`SistemaInventario.db` en la carpeta de datos.

### No imprime

Revisa controlador, papel, cola de impresión y que la impresora esté
predeterminada. La venta ya quedó registrada aunque falle la impresión.

### No abre Excel o CSV

Confirma que Excel esté instalado. El CSV usa UTF-8 con BOM; si Excel muestra
mal los acentos, ábrelo desde **Datos → Desde texto/CSV** y elige UTF-8.

### Error inesperado

Revisa el log más reciente:

```text
%LOCALAPPDATA%\SistemaInventario\logs\app-YYYYMMDD.log
```

Adjunta el log, la captura y los pasos para reproducirlo.

---

## 13. Checklist final

- [ ] Probé con base limpia.
- [ ] Probé con una base existente.
- [ ] Probé el instalador.
- [ ] Probé ventas de contado y crédito.
- [ ] Probé productos por peso.
- [ ] Probé devoluciones y abonos.
- [ ] Probé reportes y Excel.
- [ ] Probé CSV de ventas, inventario y compras.
- [ ] Probé respaldo y restauración.
- [ ] Probé usuarios y permisos.
- [ ] Probé la interfaz en Windows 11 a distintas resoluciones.
- [ ] Guardé evidencias y logs.
- [ ] Dejé la PC limpia y sin bases de prueba.

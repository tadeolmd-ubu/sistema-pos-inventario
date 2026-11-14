# Manual de usuario — Sistema de inventario

Guía para instalar y operar el punto de venta sin acompañamiento técnico.
Está escrita para una persona que nunca ha usado el sistema: sigue las
secciones en orden la primera vez y usa el índice como referencia después.

- **Aplica a:** versión 1.0.x (Windows 10 u 11 de 64 bits).
- **Tiempo de lectura:** 30–40 minutos.
- **Atajo:** si ya está instalado y solo quieres vender, ve directo a
  [Abrir la caja](#5-abrir-la-caja) y [Vender](#6-vender).

---

## Índice

1. [Lo mínimo para empezar](#1-lo-mínimo-para-empezar)
2. [Instalación](#2-instalación)
3. [Iniciar sesión](#3-iniciar-sesión)
4. [La pantalla principal](#4-la-pantalla-principal)
5. [Abrir la caja](#5-abrir-la-caja)
6. [Vender](#6-vender)
7. [Devoluciones](#7-devoluciones)
8. [Movimientos de efectivo](#8-movimientos-de-efectivo)
9. [Cerrar la caja](#9-cerrar-la-caja)
10. [Catálogos: productos y proveedores](#10-catálogos-productos-y-proveedores)
11. [Inventario](#11-inventario)
12. [Compras](#12-compras)
13. [Historial de ventas](#13-historial-de-ventas)
14. [Reportes](#14-reportes)
15. [Usuarios y roles](#15-usuarios-y-roles)
16. [Respaldos](#16-respaldos)
17. [Recuperación](#17-recuperación)
18. [Atajos de teclado](#18-atajos-de-teclado)
19. [Problemas comunes](#19-problemas-comunes)
20. [Rutina diaria](#20-rutina-diaria)
21. [Glosario](#21-glosario)

---

## 1. Lo mínimo para empezar

1. Instala la aplicación con el archivo `PosInventario-<versión>-win-x64-installer.exe`.
2. Ábrela y crea el **primer administrador** (solo la primera vez).
3. Entra a **Productos** y da de alta tus productos con precio y costo; luego
   carga las existencias en **Inventario** (entrada) o con una **Compra**.
4. Entra a **Caja**, escribe el efectivo con el que inicias y presiona
   **Abrir caja**.
5. Escanea o escribe el SKU, cobra y entrega el ticket.
6. Al terminar el día, cuenta el efectivo y presiona **Cerrar caja**.
7. Revisa que se haya creado el respaldo automático. Copia los respaldos a
   una USB o a la nube al menos una vez por semana.

> **Regla de oro:** sin caja abierta no se puede vender. Si la aplicación se
> cierra o se apaga la computadora, la caja sigue abierta y la venta se puede
> retomar al volver a entrar.

---

## 2. Instalación

### 2.1 Requisitos

- Windows 10 u 11, 64 bits.
- Al menos 500 MB libres en disco.
- Una impresora térmica de 58 u 80 mm es opcional: sin impresora el sistema
  funciona y el ticket se muestra en pantalla.
- No hace falta tener .NET instalado: el instalador incluye todo lo necesario.

### 2.2 Instalar

1. Descarga el archivo
   `PosInventario-<versión>-win-x64-installer.exe` desde la página de
   releases del repositorio:
   <https://github.com/tadeolmd-ubu/sistema-pos-inventario/releases/latest>.
2. Haz doble clic en el instalador.
3. Si Windows muestra el aviso **"Windows protegió tu PC"**, presiona
   **Más información** y luego **Ejecutar de todas formas**. Este aviso
   aparece porque la firma de código todavía está en trámite; la aplicación
   se instala igual.
4. Sigue el asistente. La instalación es **por usuario**: no pide permisos de
   administrador ni contraseña de Windows.
5. En la pantalla de tareas puedes marcar **Crear un acceso directo en el
   escritorio**. El acceso directo del menú Inicio se crea siempre.
6. Presiona **Instalar** y al terminar **Finalizar**.

El programa queda en:

```text
%LOCALAPPDATA%\Programs\Sistema de inventario
```

Los datos del negocio **no** se guardan ahí, sino en:

```text
%LOCALAPPDATA%\SistemaInventario
```

### 2.3 Primer arranque

1. Abre **Sistema de inventario** desde el menú Inicio o el escritorio.
2. Aparece la ventana **Configuración inicial**. Solo se muestra cuando
   todavía no existe ningún usuario en el sistema.
3. Captura:
   - **Nombre de usuario:** con el que entrarás (mínimo 3 caracteres).
   - **Nombre:** tu nombre completo (mínimo 3 caracteres).
   - **Contraseña:** mínimo 8 caracteres.
   - **Confirmar contraseña:** la misma contraseña.
4. Presiona **Crear administrador**.
5. En la ventana **Iniciar sesión**, captura el usuario y la contraseña que
   acabas de crear y presiona **Ingresar**.

> **Importante:** guarda la contraseña del administrador en un lugar seguro.
> Si eres el único administrador y la olvidas, no podrás recuperarla desde la
> aplicación; necesitarás asistencia técnica.

### 2.4 Actualizar

1. Descarga el instalador de la versión nueva.
2. Ejecútalo sobre la instalación actual. No es necesario desinstalar antes.
3. Tus datos, usuarios y respaldos se conservan.
4. Al abrir la versión nueva, el sistema actualiza la base de datos
   automáticamente. Antes de hacerlo crea un respaldo de seguridad.

### 2.5 Desinstalar

1. Abre **Configuración de Windows → Aplicaciones → Aplicaciones instaladas**.
2. Busca **Sistema de inventario** y elige **Desinstalar**.
3. La carpeta `%LOCALAPPDATA%\SistemaInventario` **no se borra**: ahí quedan
   la base, los respaldos y los logs. Esto permite reinstalar sin perder
   información.
4. Si de verdad quieres borrar todo (por ejemplo, para entregar la
   computadora), elimina manualmente esa carpeta después de desinstalar.
   Hazlo solo si ya copiaste los respaldos que necesitas.

---

## 3. Iniciar sesión

1. Abre la aplicación.
2. Captura **Usuario** y **Contraseña**.
3. Presiona **Ingresar** o la tecla **Enter**.
4. Si los datos son incorrectos verás **"Usuario o contraseña incorrectos."**.
   Revisa mayúsculas, minúsculas y Bloq Mayús.
5. Para salir sin entrar, presiona **Cancelar**.
6. Para cambiar de usuario estando dentro, usa **Cerrar sesión** al final del
   menú lateral.

Roles disponibles:

| Rol | Qué puede hacer |
|---|---|
| **Administrador** | Todo: vender, catálogos, inventario, compras, historial, reportes, usuarios y respaldos. |
| **Cajero** | Solo **Inicio** y **Caja**: abrir/cerrar caja, vender, devolver sus propias ventas y registrar movimientos de efectivo. |

> El cajero no ve los menús de administración. Si necesita consultar el
> historial o los reportes, pídeselo a un administrador.

---

## 4. La pantalla principal

La ventana tiene tres zonas:

- **Menú lateral izquierdo:** Inicio, Caja y, si eres administrador,
  Productos, Proveedores, Inventario, Compras, Ventas, Reportes, Usuarios y
  Configuración.
- **Panel Comandos:** debajo del menú, muestra los atajos de teclado que
  aplican a la pantalla actual. **F1** abre la ayuda completa.
- **Contenido:** la pantalla del módulo seleccionado. Arriba se muestra el
  nombre del sistema y el módulo actual.

En la parte inferior del menú aparecen tu nombre, tu rol, el botón
**Cerrar sesión** y la versión instalada.

### Inicio

Es el resumen del día. Muestra:

- **Ventas de hoy** y el total vendido.
- **Ticket promedio** por venta.
- **Caja:** si está abierta o cerrada, quién la abrió y el efectivo esperado.
- **Stock bajo:** productos con existencias en o por debajo del mínimo
  (solo administrador).
- **Ventas por método:** efectivo, tarjeta y transferencia.
- **Últimas ventas**, **Stock bajo** y **Últimas compras**.
- Botón **Actualizar** para refrescar los números.

---

## 5. Abrir la caja

La caja es el turno de trabajo. Se abre una vez al inicio y se cierra al
final. Solo puede existir **una caja abierta** en todo el sistema.

1. Entra a **Caja** en el menú lateral.
2. Si no hay caja abierta verás el recuadro **Abrir caja**.
3. Cuenta el efectivo que hay físicamente en el cajón (fondo inicial) y
   escríbelo en **Monto inicial**. Puede ser `0` si no dejas fondo.
4. Presiona **Abrir caja**.
5. El recuadro cambia a **Caja abierta** y muestra quién la abrió, la fecha,
   el monto inicial y el **Efectivo esperado**.

> El monto inicial debe ser un número mayor o igual a cero. Puedes escribir
> `1250.50` o `1250,50`.
>
> Si ves **"Ya hay una caja abierta en el sistema."**, otro usuario ya la
> abrió. Puedes seguir trabajando en ella; no la cierres sin avisar.

---

## 6. Vender

### 6.1 Agregar productos

Con la caja abierta, en la tarjeta **Venta**:

1. El cursor ya está en la búsqueda. **Escanea el código de barras** o escribe
   el **SKU** y presiona **Enter**.
2. También puedes presionar **Agregar** después de escribir el código.
3. Para buscar sin código, presiona **Seleccionar productos** (o **F3**):
   escribe parte del nombre o SKU, elige el producto y presiona **Enter**.
   Las flechas **←/→** cambian de página.
4. Cada producto aparece en la tabla con **Producto, SKU, Precio, Cantidad,
   Subtotal** y botones de acción.

Si el código no existe, el sistema avisa: **"No se encontró un producto con el
código…"**. Revisa el código o da de alta el producto en **Productos**.

### 6.2 Cambiar cantidades y aplicar descuento

- **+** y **−** aumentan o disminuyen una unidad.
- **✕** elimina la línea (pide confirmación).
- Con el teclado: selecciona una línea y usa **+ / −** para la cantidad o
  **Delete** para quitarla.
- **Descuento** (o **F6**): escribe el monto en pesos que quieres descontar
  del subtotal. No puede ser mayor que el subtotal.
- El sistema muestra **Subtotal**, **Descuento** y **Total**.

### 6.3 Cobrar

1. Elige **Método de pago**: **Efectivo**, **Tarjeta**, **Transferencia** o
   **Mixto**.
2. Según el método:
   - **Efectivo:** escribe en **Paga con** el billete o monto recibido. Puedes
     usar los botones rápidos **20, 50, 100, 200, 500**. El sistema calcula
     **Cambio** o **Faltante**.
   - **Tarjeta** o **Transferencia:** se cobra el total exacto; solo confirma
     que el pago se realizó en la terminal o en la cuenta.
   - **Mixto:** captura cuánto se pagó con **Efectivo**, **Tarjeta** y
     **Transferencia**. La suma debe cubrir el total; tarjeta y transferencia
     no pueden superarlo, y el excedente en efectivo se devuelve como cambio.
3. Presiona **Realizar venta** (o **F4**, **Ctrl+Enter** o **Enter** en el
   campo de pago).
4. Si falta dinero verás **Faltante** en rojo y la venta no se registra hasta
   cubrir el total.
5. Si hay cambio, entrega el **Cambio** que indica la pantalla.

### 6.4 Ticket

Al cobrar se abre la ventana del ticket con:

- Número de ticket (`V-000123`), fecha, cajero y detalle de productos.
- Subtotal, descuento, total, recibido y cambio.
- Selector de ancho de papel **58 mm** u **80 mm**.
- Botón **Imprimir** (**Ctrl+P**) y botón **Cerrar** (**Esc**).

La venta **ya quedó registrada** aunque la impresora falle o no exista. En ese
caso solo aparece un aviso y puedes continuar.

Si la venta se devolvió por completo, el ticket muestra la marca **ANULADA**.

### 6.5 Qué pasa con el inventario

- Cada venta descuenta automáticamente las unidades del stock.
- Si un producto no tiene stock, el sistema no permite agregarlo:
  **"El producto no tiene stock."** o **"No hay más stock disponible de ese
  producto."**.
- El movimiento queda registrado en **Inventario** y en el historial de
  **Ventas**.

### 6.6 Limpiar una venta

- Presiona **Limpiar** o **Esc**.
- Si hay productos, el sistema pide confirmación antes de vaciar el carrito.
- Si el cursor está en un campo con texto, **Esc** borra primero ese campo.

---

## 7. Devoluciones

Puedes devolver una venta completa o solo algunos productos o unidades.

### 7.1 Desde la caja

1. Con la caja abierta, presiona **Devolver venta** (**F8**).
2. Elige la venta de la lista. Solo aparecen ventas de la caja actual.
3. Presiona **Devolver**.
4. Marca los productos o unidades que regresan:
   - **Seleccionar todos** marca todo lo pendiente.
   - Si un producto tiene varias unidades, ábrelo y marca cada unidad.
5. Revisa **Unidades a devolver** y **Reembolso estimado**.
6. Presiona **Devolver**.

### 7.2 Desde el historial de ventas (administrador)

1. Entra a **Ventas**.
2. Filtra y selecciona la venta.
3. Presiona **Devolver**.
4. Sigue los mismos pasos del checklist.

### 7.3 Reglas del reembolso

- El **stock regresa** automáticamente al inventario.
- Si la venta se pagó total o parcialmente en **efectivo**, esa parte se
  descuenta de la caja. Se usa la caja original si sigue abierta o, si ya se
  cerró, la caja abierta actual.
- Si no hay una caja abierta y el reembolso incluye efectivo, el sistema
  pedirá abrir la caja primero.
- Si la venta fue **100 % con tarjeta o transferencia**, el sistema no mueve
  efectivo: el reembolso se hace fuera de la aplicación (terminal o banco) y
  aquí solo se regresa el stock.
- En ventas **mixtas**, el reembolso se reparte entre los métodos originales.
- Una devolución **parcial** deja la venta como completada y muestra cuánto se
  ha devuelto. Cuando se devuelven **todas** las unidades, la venta queda
  **Anulada**.
- Solo puede devolver **quien realizó la venta** o un **administrador**. No se
  pueden devolver más unidades de las vendidas.

---

## 8. Movimientos de efectivo

Sirven para registrar dinero que entra o sale del cajón sin ser una venta:
fondo adicional, pago a proveedor en efectivo, retiro para el banco, etc.

1. Con la caja abierta, presiona **Registrar movimiento de efectivo** (**F9**).
2. Elige **Tipo**:
   - **Ingreso:** entra efectivo al cajón.
   - **Retiro:** sale efectivo del cajón.
3. Escribe el **Monto**.
4. Escribe una **Descripción**. Es **obligatoria para los retiros** y
   recomendable siempre (por ejemplo, "Retiro para depósito").
5. Presiona **Registrar**.

Reglas:

- El monto debe ser mayor que cero.
- Un retiro no puede ser mayor que el **Efectivo esperado** en ese momento.
- Cada movimiento queda en la lista **Movimientos** de la caja con fecha,
  descripción y usuario. Haz clic en uno para ver el detalle; si viene de una
  venta, muestra también sus productos.

---

## 9. Cerrar la caja

1. Cuenta todo el efectivo físico del cajón.
2. En **Caja**, presiona **Cerrar caja** (**F10**).
3. Revisa el resumen: quién abrió, fecha, monto inicial y **Efectivo
   esperado**.
4. Escribe el **Monto contado**.
5. El sistema muestra la diferencia:
   - **Sin diferencia** (verde): coincide.
   - **Faltante** (rojo): hay menos dinero del esperado.
   - **Sobrante** (rojo): hay más dinero del esperado.
6. Si hay diferencia, escribe una **Observación** explicando qué pasó.
7. Presiona **Cerrar caja**.
8. Si el monto no coincide, aparece una confirmación:
   **"¿Cerrar la caja de todos modos?"**. Revisa el conteo antes de presionar
   **Confirmar**.
9. Al cerrar se abre el **Corte de caja** con:
   - Ventas de la caja, cantidad y total vendido.
   - Desglose por efectivo, tarjeta y transferencia.
   - Monto inicial, esperado, contado y diferencia.
10. Presiona **Imprimir corte** (**Ctrl+P**) si quieres el comprobante y luego
    **Cerrar**.

Después del cierre:

- La caja desaparece de la zona de venta y aparece en **Historial de cajas
  cerradas**.
- Haz clic en una caja cerrada para ver su detalle, sus ventas y volver a
  **Imprimir corte**.
- Se crea un **respaldo automático** de la base.

> El efectivo esperado se calcula así: monto inicial + ventas en efectivo −
> devoluciones en efectivo + ingresos − retiros.

---

## 10. Catálogos: productos y proveedores

> Estas pantallas son solo para **Administrador**.

### 10.1 Productos

**SKU y código de barras son el mismo dato.** Esto permite escanear el
producto en Caja y en el propio catálogo.

Para dar de alta un producto:

1. Entra a **Productos**.
2. Escanea el código de barras o escribe el **SKU** en **Código de barras /
   SKU** y presiona **Enter**.
   - Si el código ya existe, se cargan sus datos para editarlos.
   - Si es nuevo, el sistema prepara el alta con ese código.
3. Completa:
   - **Nombre** (mínimo 3 caracteres).
   - **Descripción** (opcional).
   - **Precio de venta**.
   - **Costo** (lo que te cuesta; se usa para la utilidad de Reportes).
   - **Stock mínimo** (para las alertas de stock bajo; `0` desactiva la
     alerta de ese producto).
4. Presiona **Guardar**.

El stock **no** se captura al crear el producto: empieza en `0`. Para cargar
existencias usa **Inventario → Entrada** o registra una **Compra**.

Otros botones:

- **Limpiar:** vacía el formulario para capturar otro producto.
- **Archivar:** oculta el producto sin borrar su historial. Solo se permite si
  el stock es `0`; si tiene existencias, primero ajusta el inventario.
- **Restaurar:** reactiva un producto archivado.
- **Ver archivados:** muestra los productos ocultos.
- **Buscar por SKU o nombre**.

### 10.2 Proveedores

1. Entra a **Proveedores**.
2. Captura **Nombre** (mínimo 3 caracteres), **Teléfono** (mínimo 3),
   **Correo electrónico** y **Dirección** (ambos opcionales).
3. Presiona **Guardar**.
4. Usa **Desactivar** / **Activar** para ocultarlo o recuperarlo, y
   **Ver inactivos** para ver los desactivados.

### 10.3 Importar productos y proveedores desde Excel

Para un negocio nuevo no hace falta capturar cientos de productos a mano.
Puedes preparar una hoja de Excel y cargarla de una vez.

**Formato admitido:** archivo CSV (también `.txt` o `.tsv`) separado por
coma, punto y coma o tabulador. Los archivos `.xlsx` **no** se leen
directamente: en Excel usa **Archivo → Guardar como → CSV UTF-8**.

Columnas de la plantilla:

| Columna | Obligatoria | Ejemplo | Notas |
|---|---|---|---|
| `sku` | Sí | `ABC-123` | También es el código de barras. Sin espacios, máximo 40 caracteres. |
| `nombre` | Sí | `Refresco 600 ml` | Mínimo 3 caracteres. |
| `descripcion` | No | `Botella de vidrio` | Máximo 500 caracteres. |
| `precio_venta` | Sí | `18.50` | Puede ser `18.50`, `18,50` o `1,234.56`. |
| `costo` | No | `12.00` | Si se omite queda en `0`. |
| `stock_minimo` | No | `5` | Si se omite queda en `0`. |
| `stock_inicial` | No | `24` | Solo se aplica a productos **nuevos**. |
| `proveedor` | No | `Distribuidora del Norte` | Si capturas datos del proveedor, el nombre y el teléfono son obligatorios. |
| `proveedor_telefono` | No | `555-123-4567` | Identifica al proveedor: si ya existe, se reutiliza. |
| `proveedor_email` | No | `ventas@proveedor.com` | Opcional. |
| `proveedor_direccion` | No | `Av. Principal 123` | Opcional. |

Pasos:

1. Entra a **Productos** y presiona **Descargar plantilla**.
2. Abre el archivo `plantilla-importacion.csv` en Excel.
3. Captura un producto por fila. No cambies los encabezados.
4. Guarda el archivo como **CSV UTF-8**.
5. En **Productos**, presiona **Importar CSV...** y elige el archivo.
6. Revisa la vista previa:
   - productos nuevos,
   - productos que se actualizarán,
   - proveedores nuevos y reutilizados,
   - unidades de stock inicial,
   - errores por fila.
7. Si hay errores, corrígelos en Excel y vuelve a importar. **No se importa
   nada hasta que el archivo esté sin errores.**
8. Presiona **Importar**.

Reglas de la importación:

- El archivo completo se valida antes de escribir. Si una fila tiene un error,
  no se guarda ninguna.
- Un SKU repetido dentro del archivo es un error. Corrige o elimina la fila.
- Si el SKU ya existe, se actualizan nombre, descripción, precio, costo y
  stock mínimo, pero **no** se cambia su stock. Así puedes volver a importar
  para corregir precios sin duplicar existencias.
- El **stock inicial** de un producto nuevo queda registrado como una entrada
  de inventario con referencia **Importación inicial**.
- Los proveedores se crean si su teléfono no existe; si ya existe, se
  reutiliza. La importación no vincula el producto con el proveedor (esa
  relación se usa en **Compras**).
- Acepta acentos y textos entre comillas, incluso con comas o saltos de línea.
- Puedes importar productos sin proveedor y proveedores sin productos.

Ejemplo de archivo:

```csv
sku,nombre,descripcion,precio_venta,costo,stock_minimo,stock_inicial,proveedor,proveedor_telefono,proveedor_email,proveedor_direccion
ABC-123,Refresco 600 ml,Botella de vidrio,18.50,12.00,6,24,Distribuidora del Norte,555-123-4567,ventas@proveedor.com,Av. Principal 123
ABC-124,Agua 1 L,,12.00,7.50,6,36,,,,
```

---

## 11. Inventario

> Solo para **Administrador**.

Entra a **Inventario**. A la izquierda buscas y seleccionas el producto; a la
derecha registras el movimiento.

### Registrar una entrada o salida

1. Busca el producto y selecciónalo. Verás **Stock actual** y **Mínimo**.
2. En **Registrar movimiento**, elige **Tipo**:
   - **Entrada:** llega mercancía (por ejemplo, carga inicial o devolución de
     cliente).
   - **Salida:** sale mercancía sin venta (merma, pérdida, rotura, uso
     interno).
   - **Ajuste:** corrige el stock después de un conteo físico.
3. Elige el **Motivo**.
4. Captura la **Cantidad** (entrada/salida) o el **Stock contado** (ajuste).
5. Presiona **Registrar**.

En un **Ajuste** escribes el número real contado. El sistema calcula la
diferencia y deja el stock en ese valor.

### Historial de movimientos

- Filtra por **Desde** y **Hasta** y presiona **Buscar**.
- **Ver todos** quita el filtro del producto seleccionado.
- La tabla muestra producto, tipo, cantidad, stock anterior y resultante,
  referencia, fecha y usuario.
- **Exportar CSV** guarda la **página visible** en un archivo que Excel abre
  con acentos correctos.

---

## 12. Compras

> Solo para **Administrador**.

### Registrar una compra

1. Entra a **Compras**.
2. Elige el **Proveedor** (solo aparecen los activos).
3. Captura el **Comprobante** (factura o nota de remisión; opcional).
4. En **Producto**, elige el artículo, la **Cantidad** y el **Costo
   unitario**.
5. Presiona **Agregar línea**. Repite para cada producto.
6. Revisa **Líneas de la compra** y el **Total**. Usa **Quitar línea** si te
   equivocaste.
7. Presiona **Registrar compra**.

Al registrar una compra:

- El stock de cada producto **aumenta**.
- El **Costo** del producto se actualiza al costo de la compra.
- Queda un movimiento de tipo **Compra** en el inventario.

### Historial y anulación

- Filtra por **Desde** y **Hasta** y presiona **Buscar**.
- Selecciona una compra para ver su **Detalle**.
- **Anular compra** revierte el stock y deja la compra marcada como anulada.
  Si el stock ya se vendió y no alcanza para revertir, el sistema no permite
  la anulación.
- **Exportar CSV** guarda la página visible del historial.

---

## 13. Historial de ventas

> Solo para **Administrador**.

Entra a **Ventas** para consultar todas las ventas.

Filtros (se combinan entre sí):

- **Desde** y **Hasta**.
- **Método:** efectivo, tarjeta, transferencia o mixto.
- **Cajero:** quién realizó la venta.
- **Buscar:** número de ticket o cajero.
- **Ver anuladas:** incluye ventas totalmente devueltas.
- Botón **Buscar**.

La tabla muestra **Ticket** (`V-000123`), **Fecha**, **Cajero**, **Método**,
**Estado** y **Total**. Si hubo devoluciones, debajo del total aparece
**Devuelto** y el total neto.

Acciones:

- **Ver ticket** (o doble clic en la fila): reabre el ticket para imprimirlo.
- **Devolver:** abre el checklist de devolución.
- **Exportar CSV:** exporta **solo la página visible**, respetando todos los
  filtros. Desde la página 2 el archivo agrega `_paginaN` al nombre.
- **Paginación:** elige 10, 25 o 50 registros por página y usa
  **Primera/Anterior/Siguiente/Última**.

---

## 14. Reportes

> Solo para **Administrador**.

Entra a **Reportes** para responder las preguntas del negocio sin salir de la
aplicación.

1. Elige **Desde** y **Hasta**, o usa un rango rápido: **Hoy**, **Últimos 7
   días**, **Este mes** o **Mes anterior**.
2. Elige **Agrupar por**: **Día**, **Semana** (inicia en lunes) o **Mes**.
3. Presiona **Generar reporte** (o **Actualizar**).

Verás:

- **Ventas netas**, **Utilidad**, **Costo de lo vendido** y cantidad de
  **Ventas**, ya descontando devoluciones.
- **Ventas por período:** tickets, neto, devuelto, utilidad y una barra de
  tendencia.
- **Ventas por método:** efectivo, tarjeta y transferencia con monto,
  cantidad y porcentaje.
- **Productos más vendidos:** top 10 por unidades netas, con SKU, ventas y
  utilidad.

La utilidad usa el costo guardado al momento de cada venta, de modo que el
histórico no cambia si después actualizas el costo del producto.

---

## 15. Usuarios y roles

> Solo para **Administrador**.

Entra a **Usuarios** para crear y mantener las cuentas del personal.

### Crear un usuario

1. Presiona **Limpiar** para asegurarte de estar en modo creación.
2. Captura **Nombre de usuario** (mínimo 3 caracteres), **Nombre**, **Rol**,
   **Contraseña** (mínimo 8) y **Confirmar contraseña**.
3. Presiona **Guardar**.

### Editar, activar y desactivar

- Selecciona un usuario de la lista para cargar sus datos. Puedes cambiar
  nombre de usuario, nombre y rol.
- La contraseña **no** se cambia desde la edición: usa **Restablecer
  contraseña**.
- **Desactivar** impide que el usuario entre, pero conserva su historial.
- **Activar** lo habilita de nuevo.
- **Ver desactivados** muestra las cuentas inactivas.

Reglas de seguridad:

- Debe quedar **al menos un administrador activo**.
- No puedes desactivar tu propio usuario.
- No puede haber dos usuarios con el mismo nombre de usuario.

### Restablecer contraseña

1. Selecciona el usuario.
2. Presiona **Restablecer contraseña**.
3. Captura y confirma la nueva contraseña (mínimo 8 caracteres).
4. Presiona **Guardar**.

La contraseña se guarda protegida con hash; nadie puede consultarla en texto
plano.

---

## 16. Respaldos

El respaldo es una copia completa de la base de datos. Es lo que permite
recuperar la información si la computadora falla, se daña el archivo o se
borra algo por error.

> Solo un **Administrador** puede crear y restaurar respaldos.

### 16.1 Respaldos automáticos

Vienen **activados** de fábrica y se crean:

- Al **iniciar sesión**, máximo uno por día (si ya existe el del día, no se
  repite).
- Al **cerrar la caja**.

Se guardan en la carpeta `Respaldos` junto a la base, con el prefijo
`respaldo_auto_`. La **retención** (por defecto 30 días) solo elimina los
respaldos automáticos viejos; **nunca** borra los manuales.

Para cambiar la configuración:

1. Entra a **Configuración**.
2. En **Respaldos automáticos** marca o desmarca **Crear respaldos
   automáticos**.
3. Elige cuántos días conservar (de 1 a 365).
4. Presiona **Guardar configuración**.

### 16.2 Crear un respaldo manual

1. Entra a **Configuración → Respaldos**.
2. Revisa **Carpeta de destino**. Por defecto es:
   `%LOCALAPPDATA%\SistemaInventario\Respaldos`.
3. Si quieres guardarlo en otro lado (USB, carpeta de red), presiona
   **Elegir carpeta…** y selecciona el destino.
4. Presiona **Crear respaldo**.
5. El sistema verifica el archivo y muestra la ruta en **Último respaldo**.

Cada respaldo se valida antes de darlo por bueno: se comprueba que el archivo
abra, que pase la revisión de integridad de SQLite y que tenga las tablas del
sistema. Si la verificación falla, el respaldo se descarta y se avisa.

> Elegir una carpeta **no** crea el respaldo: solo cambia el destino del
> próximo. El archivo se crea hasta que presionas **Crear respaldo**.

### 16.3 Dónde están los archivos

```text
%LOCALAPPDATA%\SistemaInventario\
├── SistemaInventario.db          base de datos activa
├── configuracion.json            ajustes de respaldos automáticos
├── Respaldos\
│   ├── respaldo_20261113_201500_ab12cd34.db       manual
│   └── respaldo_auto_20261113_090000_ef56gh78.db  automático
└── logs\                          registros de errores
```

**Plan mínimo de respaldo:** copia la carpeta `Respaldos` a una USB o a la
nube al menos una vez por semana, y antes de cualquier cambio grande
(actualización, restauración, cambio de computadora).

---

## 17. Recuperación

### 17.1 Restaurar desde la aplicación (recomendado)

1. Entra a **Configuración → Restaurar respaldo**.
2. Presiona **Seleccionar respaldo…**.
3. Elige un archivo `.db` de la carpeta de respaldos.
4. El sistema valida el archivo. Si está dañado o no es un respaldo
   compatible, muestra el error y no continúa.
5. Revisa la confirmación: nombre del archivo, fecha de modificación y tamaño.
6. Presiona **Confirmar**.
7. El sistema crea automáticamente un **respaldo de seguridad** de la base
   actual antes de reemplazarla.
8. Al terminar, avisa dónde quedó ese respaldo de seguridad y **reinicia la
   aplicación** para leer la base restaurada.

> **La restauración reemplaza toda la información actual** por la del
> respaldo. Las ventas, compras o cambios hechos después de ese respaldo se
> pierden. Cierra la caja y termina las operaciones en curso antes de
> restaurar.

### 17.2 Si la aplicación no abre

1. **No borres ni sobrescribas nada** todavía.
2. Copia la carpeta `%LOCALAPPDATA%\SistemaInventario` completa a una USB.
   Así conservas la base dañada y los respaldos.
3. Cierra cualquier ventana de la aplicación.
4. Renombra la base actual, por ejemplo:
   `SistemaInventario.db` → `SistemaInventario.danada.db`.
5. Copia el respaldo más reciente de `Respaldos` a la carpeta y renómbralo
   como `SistemaInventario.db`.
6. Abre la aplicación.
7. Si sigue sin abrir, revisa el log más reciente en
   `%LOCALAPPDATA%\SistemaInventario\logs` y compártelo con soporte técnico
   junto con la copia de la carpeta.

> Este procedimiento manual no valida el respaldo ni crea copia de seguridad,
> por eso se recomienda usar **Configuración → Restaurar respaldo** siempre
> que la aplicación abra.

### 17.3 Mover la información a otra computadora

1. En la computadora **vieja**, cierra la aplicación.
2. Copia la carpeta `%LOCALAPPDATA%\SistemaInventario` completa a una USB.
3. Instala la aplicación en la computadora **nueva** y ábrela una vez para que
   cree su carpeta de datos; luego ciérrala.
4. Reemplaza la carpeta `%LOCALAPPDATA%\SistemaInventario` de la computadora
   nueva con la copia de la USB.
5. Abre la aplicación y verifica que estén tus usuarios, productos y ventas.

---

## 18. Atajos de teclado

En **Caja** el cursor entra automáticamente en la búsqueda. Al cerrar un
diálogo o el ticket, regresa a la búsqueda para seguir escaneando.

| Atajo | Acción |
|---|---|
| **F1** | Ayuda de atajos del contexto actual |
| **F2** | Enfocar búsqueda / SKU |
| **F3** | Seleccionar productos |
| **F4** o **Ctrl+Enter** | Cobrar |
| **F5** | Actualizar caja |
| **F6** | Enfocar descuento |
| **F7** | Enfocar monto de pago |
| **F8** | Devolver venta |
| **F9** | Registrar movimiento de efectivo |
| **F10** | Cerrar caja |
| **Esc** | Limpiar venta o cerrar diálogo |
| **Enter** en búsqueda | Agregar producto por SKU |
| **Enter** en pago | Cobrar |
| **Delete** | Quitar la línea seleccionada |
| **+ / −** | Cambiar cantidad de la línea seleccionada |
| **↑/↓ + Enter** | Navegar y agregar en el selector de productos |
| **←/→** | Cambiar de página en el selector de productos |
| **Ctrl+P** | Imprimir ticket o corte |
| **Tab / Shift+Tab** | Navegar entre controles |

En el selector de productos, el primer resultado queda seleccionado al
filtrar y **Enter** lo agrega. Los botones **◀/▶** también cambian de página.

---

## 19. Problemas comunes

| Mensaje o situación | Qué hacer |
|---|---|
| **"Usuario o contraseña incorrectos."** | Revisa mayúsculas, minúsculas y Bloq Mayús. Si persiste, pide a un administrador que restablezca tu contraseña. |
| **"No hay una caja abierta para vender."** | Entra a **Caja** y presiona **Abrir caja**. |
| **"Ya hay una caja abierta en el sistema."** | Otro usuario la abrió. Trabaja en esa caja o pídele que la cierre. |
| **"El producto no tiene stock."** | Registra una **Entrada** en Inventario, una **Compra**, o revisa que el producto correcto esté dado de alta. |
| **"No hay más stock disponible de ese producto."** | La cantidad en el carrito ya iguala el stock. Reduce unidades o carga inventario. |
| **"No se puede archivar un producto con stock."** | Registra una **Salida** o un **Ajuste** para dejar el stock en 0 y vuelve a archivar. |
| **"No hay efectivo suficiente en la caja para ese retiro."** | El retiro supera el efectivo esperado. Retira un monto menor o revisa los movimientos. |
| **"No hay una caja abierta para reembolsar el efectivo."** | La devolución incluye efectivo: abre la caja y vuelve a intentar. |
| **La impresora no imprime o no hay impresora.** | La venta ya quedó registrada. Conecta o configura la impresora predeterminada de Windows y reimprime desde **Ventas → Ver ticket** o desde el corte. |
| **Windows muestra "Windows protegió tu PC".** | Presiona **Más información → Ejecutar de todas formas**. La firma de código aún está en trámite. |
| **"Sistema de inventario ya está abierto".** | Ya hay una ventana abierta. Ciérrala y vuelve a intentar; el sistema permite una sola instancia. |
| **"No se pudo iniciar la base de datos".** | Sigue la sección [Si la aplicación no abre](#172-si-la-aplicación-no-abre) y comparte el log con soporte. |
| **El CSV no trae todos los registros.** | El botón **Exportar CSV** guarda solo la **página visible**. Cambia a otra página y exporta de nuevo, o sube los registros por página a 50. |
| **No veo los menús de Productos, Ventas o Configuración.** | Tu usuario es **Cajero**. Pide a un administrador que consulte esa información o que cambie tu rol; después vuelve a iniciar sesión. |
| **Olvidé la contraseña del único administrador.** | Necesitas asistencia técnica; la aplicación no permite recuperarla sin otro administrador activo. |

---

## 20. Rutina diaria

### Al abrir

- [ ] Enciende la computadora e inicia sesión con tu usuario.
- [ ] Verifica la fecha y hora de Windows (afectan tickets y reportes).
- [ ] Cuenta el fondo de caja y presiona **Abrir caja** con el **Monto
      inicial** correcto.
- [ ] Revisa **Inicio → Stock bajo** y **Últimas compras**.
- [ ] Verifica que la impresora tenga papel.

### Durante el día

- [ ] Escanea o busca cada producto y confirma la cantidad en pantalla.
- [ ] Revisa el **Total** y el **Cambio** antes de cobrar.
- [ ] Entrega el ticket al cliente.
- [ ] Registra cada retiro o ingreso de efectivo con una **Descripción**.
- [ ] Si un cliente devuelve algo, usa **Devolver venta** y marca solo las
      unidades que regresan.

### Al cerrar

- [ ] Termina o cancela las ventas a medias.
- [ ] Cuenta el efectivo físico.
- [ ] Presiona **Cerrar caja**, captura el **Monto contado** y explica
      cualquier diferencia en **Observaciones**.
- [ ] Imprime el **Corte de caja**.
- [ ] Confirma que se generó el respaldo automático al cerrar.
- [ ] Copia la carpeta de respaldos a la USB o a la nube (al menos semanal).
- [ ] Cierra sesión si otra persona usará la computadora.

---

## 21. Glosario

- **Base de datos:** archivo `SistemaInventario.db` donde vive toda la
  información del negocio.
- **Caja:** turno de trabajo entre una apertura y un cierre.
- **Corte de caja:** comprobante con las ventas y el conteo de efectivo del
  turno.
- **Efectivo esperado:** dinero que debería haber en el cajón según el sistema.
- **Faltante / Sobrante:** diferencia entre lo contado y lo esperado.
- **Mixto:** venta pagada con más de un método (por ejemplo, efectivo +
  tarjeta).
- **Respaldo:** copia de seguridad completa de la base de datos.
- **SKU:** código único del producto; también se usa como código de barras.
- **Stock:** cantidad disponible en inventario.
- **Ticket:** comprobante de una venta (`V-000123`).

---

Si algo no coincide con lo que ves en pantalla, revisa primero la sección
[Problemas comunes](#19-problemas-comunes) y luego comparte el mensaje exacto
y el archivo de log más reciente con soporte técnico.

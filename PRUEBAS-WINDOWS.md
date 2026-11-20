# Matriz de pruebas en Windows

Plan de verificación visual y funcional de la interfaz en distintas
resoluciones, escalados DPI y versiones de Windows.

- **Objetivo:** confirmar que la interfaz no se rompe, no oculta controles y
  permite operar el punto de venta completo en las configuraciones soportadas.
- **Criterio de aceptación principal:** la interfaz funciona en **1366x768** y
  en **1920x1080 con 125 % de escala**.
- **Versión evaluada:** 1.0.x.
- **Ámbito:** ventana principal, todos los módulos, diálogos, ticket, corte y
  mensajes.
- **Fuera de ámbito:** rendimiento, impresión ESC/POS directa, apertura de
  cajón, tablets y pantallas táctiles.

> Para la prueba funcional completa en Windows 11, incluida instalación,
> migración, respaldos, impresión y evidencias, usa
> [`PRUEBAS-WINDOWS-11.md`](PRUEBAS-WINDOWS-11.md).

---

## 1. Entornos de prueba

| ID | Windows | Resolución | Escala | Uso |
|---|---|---|---|---|
| **E1** | Windows 11 (24H2 o superior) | 1366x768 | 100 % | Equipo POS económico |
| **E2** | Windows 11 (24H2 o superior) | 1920x1080 | 125 % | Configuración recomendada |
| **E3** | Windows 10 22H2 | 1366x768 | 100 % | Equipo antiguo |
| **E4** | Windows 10 22H2 | 1920x1080 | 125 % | Equipo de oficina |
| **E5** | Windows 11 (24H2 o superior) | 1920x1080 | 100 % | Referencia |
| **E6** | Windows 11 (24H2 o superior) | 2560x1440 | 150 % | Opcional, monitor grande |
| **E7** | Windows 11 con dos monitores de distinto DPI | 1920x1080 + 1366x768 | 125 % / 100 % | Opcional, cambio de DPI en caliente |

Requisitos por entorno:

- Barra de tareas abajo, sin ocultar automáticamente.
- Aplicación instalada con el instalador de la release.
- Una cuenta **Administrador** y una **Cajero** activas.
- Al menos 10 productos con stock, 2 proveedores y ventas de prueba.
- Impresora predeterminada configurada (puede ser "Microsoft Print to PDF").

### Cómo cambiar la escala en Windows

1. **Configuración → Sistema → Pantalla → Escala**.
2. Elige 100 %, 125 % o 150 %.
3. Cierra sesión de Windows o cierra y vuelve a abrir la aplicación para que
   el nuevo escalado se aplique.

### Cómo cambiar la resolución

1. **Configuración → Sistema → Pantalla → Resolución de pantalla**.
2. Selecciona `1366 x 768` o `1920 x 1080`.
3. Confirma el cambio y vuelve a abrir la aplicación.

---

## 2. Criterios de evaluación

Cada caso se marca como:

- **PASA:** la pantalla se ve completa, sin controles cortados ni superpuestos,
  y la acción principal es alcanzable con mouse y teclado.
- **FALLA:** hay controles inaccesibles, texto encimado, botones fuera de la
  pantalla, ventanas que no se pueden mover/cerrar o errores visuales.
- **BLOQUEADO:** no se pudo ejecutar por falta de un requisito.

Severidad:

| Nivel | Descripción | Ejemplo |
|---|---|---|
| **Crítica** | Impide vender, cobrar, cerrar caja o recuperar datos. | Botón **Realizar venta** fuera de pantalla. |
| **Alta** | Obliga a usar un rodeo o impide una tarea administrativa. | Diálogo de cierre sin botón **Cerrar caja**. |
| **Media** | Molesta pero no bloquea. | Texto cortado en una etiqueta. |
| **Baja** | Detalle estético. | Espaciado desigual. |

Una prueba se considera aprobada solo si:

- No hay fallas **críticas** ni **altas**.
- Las fallas medias tienen solución o se documentan como pendientes.
- Se adjunta evidencia (captura de pantalla) de cada entorno.

---

## 3. Matriz resumida

Marca cada celda con `PASA`, `FALLA`, `N/A` o `BLOQUEADO` y anota el folio de
evidencia.

| ID | Caso | E1 1366 | E2 1080@125 | E3 Win10 1366 | E4 Win10 1080@125 | E5 1080@100 | Evidencia |
|---|---|---|---|---|---|---|---|
| WIN-01 | Instalación y primer arranque | ☐ | ☐ | ☐ | ☐ | ☐ | |
| WIN-02 | Configuración inicial y login | ☐ | ☐ | ☐ | ☐ | ☐ | |
| WIN-03 | Ventana principal y menú lateral | ☐ | ☐ | ☐ | ☐ | ☐ | |
| WIN-04 | Inicio: tarjetas y listas | ☐ | ☐ | ☐ | ☐ | ☐ | |
| WIN-05 | Caja: apertura y resumen | ☐ | ☐ | ☐ | ☐ | ☐ | |
| WIN-06 | Caja: búsqueda y carrito | ☐ | ☐ | ☐ | ☐ | ☐ | |
| WIN-07 | Caja: descuento y pagos | ☐ | ☐ | ☐ | ☐ | ☐ | |
| WIN-08 | Selector de productos | ☐ | ☐ | ☐ | ☐ | ☐ | |
| WIN-09 | Ticket: completo y largo | ☐ | ☐ | ☐ | ☐ | ☐ | |
| WIN-10 | Devolución desde caja | ☐ | ☐ | ☐ | ☐ | ☐ | |
| WIN-11 | Movimiento de efectivo | ☐ | ☐ | ☐ | ☐ | ☐ | |
| WIN-12 | Cierre de caja con diferencia | ☐ | ☐ | ☐ | ☐ | ☐ | |
| WIN-13 | Corte de caja y reimpresión | ☐ | ☐ | ☐ | ☐ | ☐ | |
| WIN-14 | Productos y proveedores | ☐ | ☐ | ☐ | ☐ | ☐ | |
| WIN-15 | Inventario y compras | ☐ | ☐ | ☐ | ☐ | ☐ | |
| WIN-16 | Historial de ventas y filtros | ☐ | ☐ | ☐ | ☐ | ☐ | |
| WIN-17 | Reportes | ☐ | ☐ | ☐ | ☐ | ☐ | |
| WIN-18 | Usuarios y configuración | ☐ | ☐ | ☐ | ☐ | ☐ | |
| WIN-19 | Mensajes, confirmaciones y ayuda F1 | ☐ | ☐ | ☐ | ☐ | ☐ | |
| WIN-20 | Respaldos manuales y restauración | ☐ | ☐ | ☐ | ☐ | ☐ | |
| WIN-21 | Cambio de DPI con la app abierta | ☐ | ☐ | ☐ | ☐ | ☐ | |
| WIN-22 | Segundo monitor con otra escala | ☐ | ☐ | ☐ | ☐ | ☐ | |

---

## 4. Casos detallados

### WIN-01 — Instalación y primer arranque

1. Instala la aplicación con el instalador de la release.
2. Abre la aplicación desde el menú Inicio.
3. Revisa que la ventana de **Configuración inicial** se vea completa.

**Esperado:** el asistente cabe en pantalla, todos los campos y los botones
**Salir** y **Crear administrador** son visibles y alcanzables. No hay texto
cortado ni controles superpuestos.

### WIN-02 — Configuración inicial y login

1. Crea el primer administrador.
2. Cierra sesión y vuelve a entrar.
3. Prueba una contraseña incorrecta.
4. Abre la ventana **Iniciar sesión** en cada entorno.

**Esperado:** las ventanas se centran, se ven completas y muestran el mensaje
de error sin cortarse.

### WIN-03 — Ventana principal y menú lateral

1. Abre la aplicación con la cuenta administrador.
2. Verifica el tamaño inicial de la ventana.
3. Recorre todos los elementos del menú lateral.
4. Redimensiona la ventana al mínimo (`MinWidth=900`, `MinHeight=560`).
5. Restaura el tamaño y prueba **Cerrar sesión**.

**Esperado:**

- La ventana principal se abre en **pantalla completa**; **F11** alterna entre
  pantalla completa y maximizada. Todo el menú queda visible.
- En 1920x1080 al 125 % la ventana conserva un tamaño cómodo y centrado.
- El panel **Comandos** no tapa el nombre del usuario ni el botón **Cerrar
  sesión**; si hace falta, el menú lateral tiene desplazamiento propio.
- No aparece una barra horizontal en el menú.

### WIN-04 — Inicio: tarjetas y listas

1. Entra a **Inicio**.
2. Revisa las tarjetas **Ventas de hoy**, **Ticket promedio**, **Caja** y
   **Stock bajo**.
3. Baja hasta **Últimas ventas**, **Stock bajo** y **Últimas compras**.
4. Redimensiona la ventana y observa el reacomodo de las tarjetas.

**Esperado:** las tarjetas pasan de 4 a 2 columnas cuando la ventana se
angosta; los textos largos se ajustan o se recortan con puntos suspensivos,
sin encimarse; todo el contenido es alcanzable con la barra vertical.

### WIN-05 — Caja: apertura y resumen

1. Entra a **Caja** sin caja abierta.
2. Revisa el recuadro **Abrir caja**, captura el **Monto inicial** y abre.
3. Verifica **Caja abierta**, **Efectivo esperado** y **Movimientos**.

**Esperado:** los campos y botones se ven completos; el monto esperado no se
corta; la pantalla se puede desplazar verticalmente sin perder el botón
**Abrir caja**.

### WIN-06 — Caja: búsqueda y carrito

1. Escanea o escribe un SKU y presiona **Enter**.
2. Agrega varios productos.
3. Revisa las columnas **Producto, SKU, Precio, Cantidad, Subtotal** y
   **Acciones**.
4. Usa **+**, **−** y **✕**.
5. Redimensiona la ventana al mínimo.

**Esperado:** la búsqueda y los botones **Agregar** y **Seleccionar
productos** permanecen visibles; la tabla del carrito no desborda la ventana;
los botones de acción son alcanzables; si el ancho no alcanza, la tabla se
desplaza horizontalmente sin romper el resto de la pantalla.

### WIN-07 — Caja: descuento y pagos

1. Aplica un **Descuento**.
2. Prueba **Efectivo** con montos rápidos y verifica **Cambio** y
   **Faltante**.
3. Prueba **Tarjeta** y **Transferencia**.
4. Prueba **Mixto** con los tres montos.
5. Usa **F4** y **Ctrl+Enter** para cobrar.

**Esperado:** los campos de pago y los botones **Realizar venta** y
**Limpiar** son visibles sin desplazamiento horizontal; los mensajes de
faltante/cambio se leen completos; el flujo por teclado funciona.

### WIN-08 — Selector de productos

1. Presiona **F3** o **Seleccionar productos**.
2. Busca un producto, navega con **↑/↓**, cambia de página con **←/→**.
3. Agrega un producto y cierra el selector.

**Esperado:** la ventana (680x560) cabe en 1366x768; el buscador, la lista, la
paginación y los botones se ven completos; no hay controles fuera de pantalla.

### WIN-09 — Ticket: completo y largo

1. Realiza una venta con un solo producto.
2. Realiza una venta con 20 o más productos.
3. Cambia el ancho de papel entre **58 mm** y **80 mm**.
4. Presiona **Imprimir** y luego **Cerrar**.

**Esperado:** el detalle largo se desplaza dentro de la ventana; los botones
**Imprimir** y **Cerrar** siguen visibles; el ticket no se sale de la pantalla
en ninguno de los dos anchos.

### WIN-10 — Devolución desde caja

1. Presiona **Devolver venta** (**F8**).
2. Selecciona una venta con varios productos y unidades.
3. Abre un grupo, marca unidades y revisa **Reembolso estimado**.
4. Confirma la devolución.

**Esperado:** el checklist se desplaza; **Unidades a devolver**,
**Reembolso estimado** y los botones **Cerrar** y **Devolver** son
alcanzables; el desglose de reembolso no se corta.

### WIN-11 — Movimiento de efectivo

1. Presiona **F9** o **Registrar movimiento de efectivo**.
2. Registra un **Ingreso** y un **Retiro** con descripción.
3. Intenta un retiro mayor al efectivo esperado.

**Esperado:** todos los campos y botones se ven completos; el mensaje de error
se lee entero.

### WIN-12 — Cierre de caja con diferencia

1. Presiona **Cerrar caja** (**F10**) con ventas y movimientos.
2. Captura un **Monto contado** distinto al esperado.
3. Revisa **Faltante** o **Sobrante** y escribe **Observaciones**.
4. Confirma el cierre.

**Esperado:** el resumen y los botones **Cancelar** y **Cerrar caja** se ven
completos; el diálogo de confirmación no se sale de la pantalla; las
observaciones largas no rompen el diseño.

### WIN-13 — Corte de caja y reimpresión

1. Al cerrar la caja, revisa el **Corte de caja**.
2. Con muchas ventas, verifica el desplazamiento interno.
3. Imprime el corte en 58 mm y 80 mm.
4. Entra a **Historial de cajas cerradas**, abre una caja y reimprime.

**Esperado:** los totales, el desglose y la diferencia se leen completos; el
selector de ancho y los botones **Imprimir corte** y **Cerrar** permanecen
visibles; la ventana de historial se desplaza y no se sale de la pantalla.

### WIN-14 — Productos y proveedores

1. Entra a **Productos**.
2. Revisa la lista y el formulario **Detalle**.
3. Escanea un SKU, guarda, archiva y restaura un producto.
4. Repite con **Proveedores**.

**Esperado:** en ventanas angostas, el panel de lista se reduce y el detalle
conserva espacio útil; el formulario y los botones **Guardar**, **Limpiar**,
**Archivar/Restaurar** son alcanzables sin desplazamiento horizontal.

### WIN-15 — Inventario y compras

1. En **Inventario**, registra una entrada, una salida y un ajuste.
2. Revisa el historial paginado.
3. En **Compras**, registra una compra con varias líneas.
4. Selecciona una compra y revisa **Anular compra**.

**Esperado:** los paneles se adaptan; las tablas se desplazan internamente si
hace falta; los botones **Registrar**, **Exportar CSV**, **Registrar compra**
y **Anular compra** son visibles y utilizables.

### WIN-16 — Historial de ventas y filtros

1. Entra a **Ventas**.
2. Revisa **Desde**, **Hasta**, **Método**, **Cajero**, **Buscar**, **Ver
   anuladas** y **Buscar**.
3. Con 50 registros por página, baja hasta la paginación.
4. Abre **Ver ticket** y **Devolver**.

**Esperado:** la pantalla se desplaza verticalmente; los filtros no se
enciman; la tabla, la paginación y los botones **Ver ticket**, **Devolver** y
**Exportar CSV** son alcanzables tanto en 1366x768 como en 1920x1080 al 125 %.

### WIN-17 — Reportes

1. Entra a **Reportes**.
2. Genera un reporte por día, semana y mes.
3. Revisa las tarjetas de totales y las tres tablas.
4. Baja hasta **Productos más vendidos**.

**Esperado:** las tarjetas se reacomodan; las tablas se desplazan dentro de la
pantalla; no hay columnas cortadas ni texto encimado.

### WIN-18 — Usuarios y configuración

1. Entra a **Usuarios**, crea un cajero, edítalo y restablece su contraseña.
2. Entra a **Configuración**, revisa la ruta de la base y cambia la retención.
3. Crea un respaldo manual.

**Esperado:** formularios, selectores y botones se ven completos; los textos
largos de rutas se ajustan; los botones de acción son alcanzables.

### WIN-19 — Mensajes, confirmaciones y ayuda F1

1. Presiona **F1** en Inicio, Caja, Ventas y Configuración.
2. Provoca un error de validación en Caja (por ejemplo, cobrar sin productos).
3. Abre una confirmación de eliminación de línea.

**Esperado:** los mensajes largos se desplazan; los botones **Cerrar**,
**Cancelar** y **Confirmar** son visibles; la ventana no excede la pantalla.

### WIN-20 — Respaldos manuales y restauración

1. Crea un respaldo manual en una carpeta distinta.
2. Valida un archivo dañado y confirma que se rechaza.
3. Restaura un respaldo válido.

**Esperado:** las rutas largas se ajustan; el selector de archivos de Windows
se abre completo; la confirmación muestra nombre, fecha y tamaño sin cortarse;
al terminar, la aplicación se reinicia correctamente.

### WIN-21 — Cambio de DPI con la aplicación abierta

1. Con la aplicación abierta, cambia la escala de Windows (por ejemplo, de
   100 % a 125 %).
2. Cierra y vuelve a abrir la aplicación.
3. Repite el recorrido de Caja e Inicio.

**Esperado:** la interfaz se redibuja con el nuevo tamaño; no hay textos
borrosos, controles fuera de pantalla ni ventanas más grandes que el área de
trabajo. Si Windows requiere reiniciar sesión, se hace y se repite.

### WIN-22 — Segundo monitor con otra escala

1. Conecta un segundo monitor con resolución o escala distinta.
2. Mueve la ventana principal al segundo monitor.
3. Abre Caja, un ticket y el corte.

**Esperado:** la ventana se adapta al área de trabajo del monitor; no queda
parte de la ventana fuera de la pantalla; los diálogos se centran en el
monitor donde está la ventana principal.

---

## 5. Registro de resultados

Copia esta tabla por cada corrida y guárdala junto con las capturas.

| Campo | Valor |
|---|---|
| Fecha | |
| Versión | |
| Entorno (E1–E7) | |
| Windows | |
| Resolución / escala | |
| Tester | |
| Casos ejecutados | |
| Casos aprobados | |
| Fallas críticas | |
| Fallas altas | |
| Evidencia (carpeta o enlace) | |

Para cada falla registra:

| Campo | Valor |
|---|---|
| ID del caso | |
| Entorno | |
| Severidad | |
| Descripción | |
| Pasos para reproducir | |
| Resultado esperado | |
| Resultado obtenido | |
| Captura | |
| Estado | Abierta / Corregida / Verificada |

---

## 6. Ajustes aplicados para esta matriz

Estos cambios buscan que la interfaz cumpla el criterio en pantallas pequeñas.
Aun así, **deben validarse visualmente en Windows real** antes de cerrar el
ítem 4.7 de `PENDIENTES.md`.

| Ajuste | Motivo |
|---|---|
| La ventana principal se abre maximizada cuando su tamaño de diseño (1280x800) no cabe en el área de trabajo. | Evita que en 1366x768 la ventana quede más alta que la pantalla y se pierdan controles. |
| **Ventas** ahora tiene desplazamiento vertical. | Era el único módulo sin `ScrollViewer`; con muchos filtros y 50 registros se cortaba en 1366x768. |
| Se redujo la altura máxima de ticket, devolución, cancelación de venta, movimientos, historial de cajas y corte. | Sus ventanas podían superar el alto disponible en 1366x768; ahora el contenido interno se desplaza y los botones quedan visibles. |
| Pruebas unitarias de `AjusteVentana`. | Fijan el cálculo de cuándo maximizar, incluyendo 1366x768, 1920x1080 al 100 % y al 125 %. |

### Limitaciones conocidas

- La validación visual final requiere una sesión interactiva de Windows; no se
  puede sustituir por las pruebas unitarias.
- El escalado DPI cambia el tamaño lógico de las ventanas; los casos WIN-21 y
  WIN-22 deben ejecutarse en hardware real con dos monitores.
- Las impresoras térmicas reales pueden variar el ancho útil del papel; la
  matriz valida el diseño en pantalla, no la calibración física.

---

## 7. Evidencia mínima para cerrar el ítem

- [ ] Captura de **Inicio** en E1 y E2.
- [ ] Captura de **Caja** con carrito y pagos en E1 y E2.
- [ ] Captura de **Ticket** largo en E1.
- [ ] Captura de **Cierre de caja** y **Corte** en E1.
- [ ] Captura de **Ventas** con 50 registros y filtros en E1.
- [ ] Captura de **Productos**, **Inventario** y **Compras** en E2.
- [ ] Captura de **Reportes** en E2.
- [ ] Capturas de las ventanas de **mensaje**, **confirmación** y **ayuda F1**.
- [ ] Registro de resultados firmado por quien ejecutó las pruebas.

# Pendientes para llevar el sistema a un negocio real

> Estado base: v1 funcional en `dev` (noviembre 2026), 129 tests pasando.
> Este documento ordena lo que falta de forma cronológica: primero lo que
> desbloquea decisiones, después lo que permite operar, luego lo que da
> confianza, y al final lo que convierte la app en un producto vendible.

## Cómo usar este documento

- Cada ítem se trabaja en **una rama desde `dev`**, con commits separados.
- No se marca como terminado sin: build Debug/Release, tests, prueba manual y
  documentación actualizada.
- Los ítems de una fase se pueden hacer en paralelo solo si no comparten
  archivos; si comparten, se hacen en orden.
- Al cerrar un ítem, cambia `- [ ]` por `- [x]` y agrega el hash del commit.

**Tamaño estimado:** S = hasta 1 día · M = 2 a 4 días · L = 1 a 2 semanas · XL = más de 2 semanas.

---

## Ya está resuelto

- [x] Modelo de datos, migraciones y servicios de dominio.
- [x] Usuarios, Productos, Proveedores, Inventario, Compras, Caja/POS, Inicio y Configuración.
- [x] Roles Administrador/Cajero y restricciones de acceso.
- [x] Caja única compartida, apertura, movimientos y cierre con diferencia.
- [x] Ventas con stock, efectivo/tarjeta/transferencia, ticket en pantalla y anulación.
- [x] Descuento sobre el subtotal.
- [x] Búsqueda y alta por SKU en Caja (compatible con lector de código de barras).
- [x] Historial global de ventas con filtros y reimpresión de ticket.
- [x] Desglose por método de pago en cierre e historial de caja.
- [x] Devoluciones totales y parciales de ventas, con reembolso en efectivo
  desde la caja abierta y sin recalcular cajas cerradas.
- [x] Respaldos manuales con `BackupDatabase`.
- [x] Manejo global de errores y logs en archivo.
- [x] Instancia única con bloqueo de archivo.
- [x] Textos en español de México.
- [x] Suite de tests de servicios y build de Windows en CI.

## Restricciones conocidas hoy

- Una sola PC y una sola base SQLite local.
- Una sola caja abierta a la vez.
- Cantidades y stock enteros.
- Una sola forma de pago por venta.
- El reembolso en efectivo de una devolución requiere una caja abierta; con
  tarjeta o transferencia la devolución se registra de todos modos.
- Ticket solo en pantalla.
- Respaldo manual, sin restauración desde la app.
- Sin facturación ni impuestos.
- Sin clientes ni ventas a crédito.

---

## Fase 0 — Decisiones que destraban el resto

- [ ] **0.1 Definir el cliente objetivo y el alcance de la v1 comercial**
  - **Qué:** negocio de un solo mostrador, una PC Windows, productos con código
    de barras, cantidades enteras, sin facturación ni fiado.
  - **Criterio:** la decisión queda escrita en este archivo o en el README.
  - **Tamaño:** S.

- [ ] **0.2 Resolver la versión remota**
  - **Problema:** el repo es privado y la API de GitHub responde 404; la app
    siempre muestra la versión local.
  - **Opciones:** repo público, `version.txt` que viaje con el ejecutable, o
    `GITHUB_TOKEN` (poco práctico para el cliente final).
  - **Criterio:** la app muestra la versión real sin pedir token al usuario.
  - **Tamaño:** S.

- [x] **0.3 Definir la política de devoluciones después del cierre de caja**
  - **Qué:** si se permite, quién puede hacerla y cómo impacta el efectivo de
    una caja ya cerrada.
  - **Criterio:** regla contable documentada antes de programar.
  - **Política (implementada en `feat/devoluciones-venta`):** las devoluciones
    pueden ser totales o parciales y solo las hace el vendedor original o un
    administrador. La caja cerrada es inmutable (sus montos no se recalculan):
    el reembolso en efectivo sale de la caja abierta actual como movimiento
    `AnulacionVenta` que referencia la venta; si no hay caja abierta no se
    reembolsa efectivo, pero las devoluciones con tarjeta o transferencia sí
    se registran. El descuento se prorratea por unidad y, al devolver todo lo
    pendiente, se reembolsa el saldo exacto. En ventas con pagos mixtos el
    reembolso se reparte proporcionalmente entre los pagos originales y cada
    uno acumula su `MontoDevuelto`; solo la parte en efectivo sale de la caja.
    La venta queda `Anulada` solo cuando ya no tiene unidades pendientes, y
    los reportes (Inicio, cierre e historiales) se muestran netos de
    devoluciones.
  - **Tamaño:** S.

- [x] **0.4 Confirmar si los pagos mixtos entran en la v1 comercial**
  - **Qué:** una venta pagada parte en efectivo y parte con tarjeta.
  - **Criterio:** decisión tomada; si es sí, se agenda el ítem 1.4.
  - **Decisión:** sí; se implementaron en el ítem 1.4 (rama
    `feat/pagos-mixtos`).
  - **Tamaño:** S.

---

## Fase 1 — Operación diaria en el mostrador

- [x] **1.1 Impresión de ticket**
  - **Por qué:** un negocio no puede entregar la pantalla al cliente.
  - **Alcance mínimo:** botón Imprimir en el ticket; diseño para 58 mm y 80 mm;
    si la impresora falla, la venta ya quedó registrada y solo se avisa.
  - **Después:** evaluar ESC/POS directo y apertura de cajón.
  - **Criterio:** ticket legible impreso desde una venta y desde una
    reimpresión; la app no se bloquea si no hay impresora.
  - **Tests:** generación del contenido y manejo de error de impresión.
  - **Hecho en:** rama `feat/impresion-ticket` (f01e6d3): selector de 58/80 mm,
    impresión por la API de Windows o CUPS, y aviso en la ventana si falla.
  - **Tamaño:** M.

- [x] **1.2 Reimprimir ticket desde el detalle de una caja cerrada**
  - **Qué:** que las ventas listadas en `CajaHistorialWindow` abran su ticket.
  - **Criterio:** doble clic o botón abre el ticket de esa venta.
  - **Hecho en:** rama `feat/impresion-ticket` (742f1f0): botón "Ver ticket"
    y doble clic en cada venta, incluidas las canceladas.
  - **Tamaño:** S.

- [x] **1.3 Devoluciones después del cierre de caja**
  - **Qué:** revertir stock y registrar el impacto en efectivo de una venta de
    una caja cerrada.
  - **Reglas:** solo administrador o vendedor original; no se puede devolver dos
    veces; queda movimiento de inventario y de caja.
  - **Criterio:** una venta vieja se puede devolver sin reabrir la caja y el
    historial muestra la devolución.
  - **Tests:** permisos, doble devolución, stock y efectivo.
  - **Depende de:** 0.3.
  - **Hecho en:** rama `feat/devoluciones-venta` (5b35701 y la ventana en
    4a5ea92).
  - **Tamaño:** L.

- [x] **1.4 Pagos mixtos**
  - **Qué:** registrar varios pagos por venta (efectivo + tarjeta, por ejemplo).
  - **Modelo:** nueva entidad o columnas de pagos; migración de EF.
  - **UI:** agregar pagos hasta cubrir el total; el cambio solo aplica al
    efectivo.
  - **Criterio:** ticket y cierre muestran el desglose correcto; el efectivo
    esperado de la caja coincide.
  - **Tests:** suma de pagos, vuelto, cierre y ticket.
  - **Depende de:** 0.4.
  - **Hecho en:** rama `feat/pagos-mixtos` (c774529 y 6524a3a): tabla
    `PagoVenta`, migración con backfill, selector Mixto con los montos de
    efectivo/tarjeta/transferencia, desglose en ticket/cierre y devoluciones
    por método.
  - **Tamaño:** L.

- [ ] **1.5 Atajos de teclado y foco en el POS**
  - **Qué:** F2 buscar, F4 cobrar, Esc limpiar, foco automático en la búsqueda
    al entrar a Caja.
  - **Criterio:** se puede vender sin tocar el mouse.
  - **Tamaño:** S.

- [ ] **1.6 Confirmación al cerrar caja con diferencia**
  - **Qué:** si el monto contado no coincide con el esperado, pedir
    confirmación y mostrar la diferencia.
  - **Criterio:** no se cierra una caja con diferencia por error.
  - **Tamaño:** S.

- [ ] **1.7 Apertura de cajón de dinero (si el cliente lo usa)**
  - **Qué:** disparar el cajón desde la impresora al cobrar en efectivo.
  - **Depende de:** 1.1 y del modelo de impresora.
  - **Tamaño:** S/M.

---

## Fase 2 — Datos, confianza y soporte

- [ ] **2.1 Restaurar respaldo desde la app**
  - **Qué:** elegir un archivo, validarlo, respaldar la base actual y
    reemplazarla con confirmación.
  - **Reglas:** solo administrador; la app se reinicia al terminar.
  - **Criterio:** se puede recuperar una base desde un respaldo sin tocar
    archivos a mano.
  - **Tests:** respaldo válido, archivo corrupto, restauración completa.
  - **Tamaño:** M.

- [ ] **2.2 Respaldo automático**
  - **Qué:** respaldar al cerrar caja y/o una vez por día, con retención
    configurable y aviso si falla.
  - **Criterio:** existe al menos un respaldo reciente sin intervención del
    usuario.
  - **Tamaño:** M.

- [ ] **2.3 Tests críticos faltantes**
  - **Qué:** cierre de caja con diferencia, permisos de anulación de venta
    (vendedor, otro cajero, administrador) y respaldo con la base abierta.
  - **Criterio:** esos tres casos quedan cubiertos por la suite.
  - **Tamaño:** S.

- [ ] **2.4 Abrir la carpeta de logs desde Configuración**
  - **Qué:** botón para abrir la carpeta de logs y ver el último archivo.
  - **Criterio:** el usuario puede enviar el log sin buscar rutas a mano.
  - **Tamaño:** S.

- [ ] **2.5 Verificación de integridad del respaldo**
  - **Qué:** comprobar que el archivo generado abre y tiene las tablas
    esperadas antes de darlo por bueno.
  - **Criterio:** un respaldo corrupto se detecta al crearlo.
  - **Tamaño:** S.

- [ ] **2.6 Actualizaciones de la app**
  - **Qué:** avisar cuando hay una versión nueva y descargarla/instalarla de
    forma segura.
  - **Depende de:** 0.2 y 4.1.
  - **Criterio:** el cliente puede actualizar sin borrar datos ni reinstalar a
    mano.
  - **Tamaño:** L.

---

## Fase 3 — Administración y reportes

- [ ] **3.1 Corte de caja imprimible**
  - **Qué:** imprimir el resumen de cierre con ventas, métodos, esperado,
    contado y diferencia.
  - **Depende de:** 1.1.
  - **Criterio:** el dueño se lleva el corte en papel.
  - **Tamaño:** M.

- [ ] **3.2 Exportar a CSV**
  - **Qué:** ventas, inventario y compras en un rango de fechas.
  - **Criterio:** el archivo abre en Excel y coincide con lo mostrado.
  - **Tamaño:** M.

- [ ] **3.3 Reportes de administración**
  - **Qué:** ventas por período, por método, productos más vendidos y utilidad
    (precio de venta menos costo).
  - **Criterio:** el dueño puede ver sus números sin exportar a otro sistema.
  - **Tamaño:** L.

- [ ] **3.4 Paginación o carga progresiva del historial de ventas**
  - **Qué:** hoy hay un límite de 500 ventas por consulta; agregar páginas o
    “cargar más”.
  - **Criterio:** se puede consultar un año completo sin truncar datos.
  - **Tamaño:** S/M.

- [ ] **3.5 Persistencia de tamaño y posición de la ventana**
  - **Criterio:** la app abre donde y como se cerró.
  - **Tamaño:** S.

- [ ] **3.6 Filtros adicionales en el historial de ventas**
  - **Qué:** filtrar por método de pago y por cajero.
  - **Criterio:** los filtros se combinan con fecha y búsqueda.
  - **Tamaño:** S.

---

## Fase 4 — Distribución y producto vendible

- [ ] **4.1 Instalador para Windows**
  - **Qué:** Inno Setup o MSIX; acceso directo, desinstalador y datos en
    `LocalApplicationData`.
  - **Criterio:** una instalación limpia en Windows funciona sin pasar por la
    terminal.
  - **Tamaño:** M.

- [ ] **4.2 Firma de código**
  - **Qué:** firmar el ejecutable y el instalador para evitar advertencias de
    SmartScreen.
  - **Criterio:** Windows no muestra “editor desconocido”.
  - **Tamaño:** S (requiere comprar certificado).

- [ ] **4.3 Release con instalador adjunto**
  - **Qué:** que el workflow publique el instalador y no solo el ZIP.
  - **Criterio:** cada release tiene un único archivo instalable.
  - **Tamaño:** S.

- [ ] **4.4 Manual de usuario**
  - **Qué:** guía de instalación, apertura de caja, venta, cierre, respaldos y
    recuperación.
  - **Criterio:** una persona nueva puede operar sin acompañamiento.
  - **Tamaño:** M.

- [ ] **4.5 Licenciamiento o modelo de cobro**
  - **Qué:** decidir si se vende licencia, suscripción o servicio de
    implementación.
  - **Criterio:** mecanismo definido y aplicable.
  - **Tamaño:** M.

- [ ] **4.6 Términos y aviso de privacidad**
  - **Qué:** documentos legales según el modelo de venta y los datos que
    guarda la app.
  - **Criterio:** el cliente recibe los documentos al contratar.
  - **Tamaño:** S.

- [ ] **4.7 Matriz de pruebas en Windows**
  - **Qué:** distintas resoluciones, escalado DPI y versiones de Windows.
  - **Criterio:** la interfaz no se rompe en 1366x768 ni en 1920x1080 con 125%
    de escala.
  - **Tamaño:** M.

- [ ] **4.8 Migración de datos desde Excel u otro sistema**
  - **Qué:** importar productos, proveedores y stock inicial.
  - **Criterio:** un negocio nuevo arranca sin cargar todo a mano.
  - **Tamaño:** M.

---

## Fase 5 — Módulos según el cliente (no universales)

- [ ] **5.1 Productos por peso o unidades decimales**
  - **Qué:** cantidades y stock decimales, precios por kg.
  - **Cuándo:** carnicería, frutería, abarrotes a granel.
  - **Tamaño:** L.

- [ ] **5.2 Clientes y ventas a crédito**
  - **Qué:** clientes, límite de crédito, cuenta por cobrar y abonos.
  - **Cuándo:** el negocio fía.
  - **Tamaño:** L.

- [ ] **5.3 IVA y facturación (CFDI)**
  - **Qué:** desglose de impuestos y timbrado fiscal.
  - **Cuándo:** el negocio necesita facturar en México.
  - **Tamaño:** XL.

- [ ] **5.4 Multi-PC o multi-caja**
  - **Qué:** base de datos central o servidor local.
  - **Cuándo:** hay más de una caja o una PC en oficina.
  - **Tamaño:** XL.

---

## Fase 6 — Cierre y entrega

- [x] **6.1 Actualizar documentación**
  - **Qué:** README, `ARQUITECTURA-BASE.md` y `COLABORACION.md` con los módulos,
    logs, instancia única y el repositorio correcto.
  - **Tamaño:** S.

- [ ] **6.2 Limpiar ramas y datos de prueba**
  - **Qué:** borrar ramas viejas y decidir si se restaura la base usada en
    pruebas.
  - **Tamaño:** S.

- [ ] **6.3 Checklist de entrega al cliente**
  - **Qué:** instalación, respaldo inicial, capacitación y contacto de soporte.
  - **Tamaño:** S.

---

## Ruta recomendada de ejecución

1. **Semana 1:** 0.1, 0.2, 0.3, 0.4, 1.1, 1.2, 1.5, 1.6.
2. **Semana 2 y 3:** 1.3, 1.4.
3. **Semana 4:** 2.1, 2.2, 2.3, 2.4, 2.5.
4. **Semana 5:** 3.1, 3.2, 3.4, 3.5, 3.6.
5. **Semana 6:** 4.1, 4.2, 4.3.
6. **Semana 7:** 4.4, 4.5, 4.6, 4.7, 4.8, 2.6.
7. **Después:** 3.3 y los módulos de la fase 5 según el cliente.

> Los tiempos son orientativos. La prioridad real es esta: ticket, devoluciones,
> pagos mixtos, respaldos, instalador y reportes.

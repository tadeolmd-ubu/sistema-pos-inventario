# Checklist de entrega al cliente

> Documento para quien instala y entrega el sistema. Complétalo con el cliente
> presente y conserva una copia firmada. El manual de operación es
> [`MANUAL-USUARIO.md`](MANUAL-USUARIO.md).

## Datos de la entrega

| Campo | Valor |
|---|---|
| Cliente / negocio | |
| Fecha de entrega | |
| Versión entregada | |
| Archivo instalador | `PosInventario-____-win-x64-installer.exe` |
| Equipo / Windows | |
| Entregado por | |
| Recibido por | |
| Carpeta de respaldos | `%LOCALAPPDATA%\SistemaInventario\Respaldos` |

## 1. Antes de salir

- [ ] Confirmé la versión y descargué el instalador desde la release oficial:
      <https://github.com/tadeolmd-ubu/sistema-pos-inventario/releases/latest>
- [ ] El instalador abre y completa una instalación de prueba.
- [ ] La aplicación abre, crea el primer administrador y permite iniciar sesión.
- [ ] `dotnet build PosInventario.sln` termina sin errores.
- [ ] `dotnet test PosInventario.sln` termina con todas las pruebas correctas.
- [ ] No entrego una base con datos de prueba del desarrollador.
- [ ] Tengo el manual de usuario y este checklist.
- [ ] Tengo definidos el contacto, horario y canal de soporte.
- [ ] Tengo una USB o carpeta de nube para el respaldo inicial.

## 2. Instalación en el equipo del cliente

- [ ] Windows 10 u 11 de 64 bits, con al menos 500 MB libres.
- [ ] Fecha y hora de Windows correctas.
- [ ] Instalador ejecutado con el usuario de Windows del negocio.
- [ ] Instalación **por usuario**, sin pedir permisos de administrador.
- [ ] Acceso directo en el menú Inicio y, si se acordó, en el escritorio.
- [ ] Primer arranque: ventana **Configuración inicial**.
- [ ] Administrador creado con usuario y contraseña seguros.
- [ ] Contraseña del administrador guardada en un lugar seguro.
- [ ] Inicio de sesión correcto con el administrador.
- [ ] Versión visible en la aplicación coincide con la entregada.
- [ ] Impresora térmica configurada, si el cliente la tiene.
- [ ] Ticket de prueba visible en pantalla y, si aplica, impreso.

> La aplicación se instala en
> `%LOCALAPPDATA%\Programs\Sistema de inventario`. Los datos viven en
> `%LOCALAPPDATA%\SistemaInventario`.

## 3. Respaldo inicial

- [ ] **Configuración → Respaldos automáticos** está activado.
- [ ] Retención definida según el cliente (por defecto 30 días).
- [ ] **Carpeta de destino** revisada.
- [ ] Respaldo manual creado desde **Configuración → Crear respaldo**.
- [ ] El sistema confirmó que el respaldo es válido.
- [ ] Copié el archivo `.db` a una USB o a la nube del cliente.
- [ ] Anoté la ruta y el nombre del respaldo inicial.
- [ ] Le expliqué al cliente que los respaldos automáticos se crean al iniciar
      sesión y al cerrar la caja.
- [ ] Le expliqué que debe copiar la carpeta `Respaldos` a otro medio al menos
      una vez por semana.

Ruta del respaldo inicial: ______________________________________________

## 4. Configuración inicial del negocio

- [ ] Productos capturados o importados desde CSV.
- [ ] Precios, costos y stock inicial revisados.
- [ ] Productos por pieza y por peso configurados correctamente.
- [ ] Proveedores capturados.
- [ ] Clientes capturados, si usa crédito.
- [ ] Límites de crédito definidos por cliente.
- [ ] Usuarios y cajeros creados.
- [ ] Roles revisados: el cajero no entra a módulos administrativos.
- [ ] Caja inicial abierta con el fondo correcto.
- [ ] Datos de la empresa y operación revisados con el responsable.

## 5. Capacitación

Tiempo sugerido: 60 a 90 minutos, con práctica del cliente.

- [ ] Iniciar sesión, cerrar sesión y cambiar de usuario.
- [ ] Abrir caja y contar el fondo inicial.
- [ ] Vender a **Público** sin seleccionar cliente.
- [ ] Seleccionar un cliente y vender a contado.
- [ ] Vender con **Efectivo**, **Tarjeta**, **Transferencia** y **Mixto**.
- [ ] Vender a **Crédito** y explicar el límite de crédito.
- [ ] Capturar una venta de producto por **peso (kg)**, si aplica.
- [ ] Modificar cantidades, aplicar descuento y cobrar.
- [ ] Imprimir o reimprimir un ticket.
- [ ] Registrar una devolución total o parcial.
- [ ] Registrar un abono de una venta a crédito, si aplica.
- [ ] Registrar una entrada, salida o ajuste de inventario.
- [ ] Registrar una compra y revisar el costo actualizado.
- [ ] Consultar el historial de ventas.
- [ ] Generar reportes y exportarlos a Excel.
- [ ] Crear un respaldo manual y localizar la carpeta de respaldos.
- [ ] Cerrar caja, capturar el monto contado e imprimir el corte.
- [ ] Revisar **Problemas comunes** del manual.
- [ ] Acordar el canal para pedir soporte.

El cliente debe realizar al menos una venta, una devolución y un cierre de caja
con sus propias manos antes de firmar.

## 6. Prueba de aceptación

Marca cada punto o escribe **N/A** si el cliente no usa esa función.

- [ ] Venta de contado a público.
- [ ] Venta de contado con cliente seleccionado.
- [ ] Venta a crédito dentro del límite.
- [ ] Venta a crédito rechazada por límite insuficiente.
- [ ] Abono en efectivo y actualización del saldo.
- [ ] Abono con tarjeta o transferencia.
- [ ] Venta de producto por peso, si aplica.
- [ ] Devolución parcial y regreso de stock.
- [ ] Ticket y corte de caja impresos o visibles.
- [ ] Reporte generado y exportado a Excel.
- [ ] Respaldo manual creado y validado.
- [ ] Cierre y reapertura de la aplicación sin perder datos.
- [ ] Usuario cajero sin acceso a módulos administrativos.

Resultado: ☐ Aprobado  ☐ Aprobado con observaciones  ☐ No aprobado

Observaciones:

__________________________________________________________________________

__________________________________________________________________________

## 7. Contacto de soporte

| Campo | Valor |
|---|---|
| Proveedor / responsable | |
| Teléfono | |
| WhatsApp | |
| Correo | |
| Horario de atención | |
| Soporte remoto | Sí / No |
| Tiempo de respuesta acordado | |

Cuando el cliente reporte un problema, pedirle:

- [ ] Versión de la aplicación.
- [ ] Captura de pantalla o mensaje exacto.
- [ ] Pasos que realizó antes del error.
- [ ] Si el problema es de datos, el respaldo más reciente.
- [ ] El log más reciente de
      `%LOCALAPPDATA%\SistemaInventario\logs\app-YYYYMMDD.log`.

No pedir la base de datos activa ni contraseñas por mensajería. Para revisar
datos, trabajar sobre una copia del respaldo.

## 8. Recomendaciones permanentes al cliente

- [ ] No borrar la carpeta `%LOCALAPPDATA%\SistemaInventario`.
- [ ] No compartir la contraseña del administrador.
- [ ] Crear respaldos manuales antes de una actualización o cambio grande.
- [ ] Copiar los respaldos a USB o nube cada semana.
- [ ] Cerrar la caja al terminar el turno.
- [ ] Usar un usuario de Windows del negocio, no una cuenta compartida.
- [ ] Contactar a soporte antes de restaurar una base o mover la instalación.
- [ ] Revisar las actualizaciones y el manual cuando cambie la versión.

## 9. Cierre de la entrega

- [ ] Manual de usuario entregado.
- [ ] Este checklist firmado.
- [ ] Respaldo inicial entregado y verificado.
- [ ] Contacto de soporte registrado.
- [ ] El cliente confirmó que puede vender y cerrar caja.

Entregado por: __________________________  Fecha: ______________

Recibido por: ___________________________  Fecha: ______________

Firma del cliente: ______________________

Observaciones finales:

__________________________________________________________________________

__________________________________________________________________________

# Alcance de las pantallas operativas de Roca Maya

## Decisión de arquitectura

- Mantener WPF sobre .NET 8 y los paquetes actuales; no agregar paquetes.
- Usar colecciones y modelos en memoria para este prototipo. Los datos se reinician al cerrar la aplicación.
- Mantener las secciones y pantallas modales dentro del patrón actual de navegación.
- Las ventanas para crear productos, seleccionar/ver clientes y abrir/cerrar caja usarán `ShowDialog`, `ResizeMode="NoResize"` y la paleta `ColorPalette.xaml`.

## Alcance funcional

### Navegación y resumen

- Agregar a la barra lateral las secciones Compras, Crédito, Proveedores y Ventas.
- Resumen presenta datos básicos de la sesión, estado de caja, actividad reciente y productos próximos a vencer.
- El indicador de vencimiento se basa en la fecha de vencimiento de cada lote de compra; esa fecha es opcional al registrar una compra.

### Compras e inventario

- Registrar una compra con proveedor y varias líneas de producto/cantidad.
- Permitir crear un producto desde una ventana modal fija con nombre, ID, precio de venta y cantidad inicial.
- Cada línea de compra conserva cantidad y fecha de compra; si se indicó vencimiento, conserva también esa fecha.
- Inventario muestra los productos y un historial por producto con las entradas, cantidades y fechas.
- El costo de compra se conserva en la línea de compra para mostrar el historial del proveedor; el precio del producto es su precio de venta.

### Clientes y crédito

- Clientes se reemplaza por una lista sencilla con búsqueda por nombre y acción para crear.
- Un cliente tiene nombre, teléfono, tipo Persona o Empresa y un identificador: identidad para Persona, RTN para Empresa.
- Ver/crear clientes se realiza en ventana modal fija. La ficha del cliente muestra sus datos e historial.
- Crédito lista clientes con saldo, movimientos de crédito y abonos; permite consultar el historial del cliente.

### Proveedores

- Lista con búsqueda y creación de proveedores con nombre, teléfono y contacto; la ficha muestra sus compras.
- El historial de compras enlaza proveedor, fecha, productos y cantidades.

### Caja, ventas y cierre

- Caja puede abrirse con un monto inicial mediante conteo de denominaciones y muestra la suma acumulada.
- El cierre usa el mismo conteo, registra el resumen de la sesión y aparece en Ventas.
- Si se intenta cerrar la aplicación con una caja abierta, se ofrece cerrar caja o cancelar el cierre de la aplicación. Si se cancela el cierre de caja, la aplicación permanece abierta.
- Ventas muestra las ventas y cierres de caja de la sesión, con consulta de detalle.
- Una venta a contado usa la ventana de cobro ya solicitada. Una venta a crédito necesita un cliente seleccionado y registra el saldo en la pantalla Crédito.

### Configuración y distribución

- Configuración se organiza en secciones, incluyendo Usuarios y Roles, manteniendo datos de demostración.
- Abrir la aplicación al iniciar Windows se deja para la configuración del instalador de distribución, no se activa desde las pantallas del prototipo.

## Criterios de aceptación

- Las operaciones de compra aumentan existencias y añaden una línea al historial del producto y del proveedor.
- Los clientes se pueden buscar por nombre, crear por tipo y abrir en una ficha modal.
- Los créditos y abonos actualizan el saldo visible y aparecen en el historial del cliente.
- Los conteos de caja actualizan en vivo el total; el cierre queda visible en Ventas y bloquea el cierre de la aplicación hasta cerrar caja o cancelar.
- La interfaz conserva la paleta Roca Maya y no añade dependencias.

## Fuera de alcance

- Guardar datos en disco, sincronización o base de datos.
- Autenticación real, impresión de facturas, conexión con hardware de caja o cumplimiento fiscal.
- Configurar inicio automático en Windows antes de elegir un instalador de distribución.

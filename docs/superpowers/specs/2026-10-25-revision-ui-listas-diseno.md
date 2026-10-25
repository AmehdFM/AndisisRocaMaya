# Revisión de UI: listas y superficies

## Objetivo

Mejorar la lectura y el trabajo con decenas de registros en todas las pantallas del prototipo, reduciendo paneles con apariencia de tarjeta sin perder jerarquía visual ni la paleta Roca Maya.

## Enfoque aprobado

Se conserva la cuadrícula de tarjetas compactas en Caja, donde cada producto es una opción de selección. En las demás pantallas se prefieren filas, encabezados, divisores y fondos tonales; superficies blancas quedan para tablas o áreas que necesitan delimitarse. Los resúmenes se presentan como columnas alineadas sin contenedores repetidos.

Se añaden filtros en listas de operación extensas. La búsqueda de compras filtra tanto el catálogo de productos de la captura como el historial por proveedor, número o fecha. Las acciones que descartan datos o elementos se agrupan primero y usan rojo; confirmar o guardar conserva el azul/negro de marca.

## Límites

- Mantener los controles y el comportamiento de negocio existentes; solo ajustar navegación visual, filtros y ubicación/color de acciones destructivas.
- No agregar paquetes ni persistencia.
- Mantener paleta definida en `Assets/ColorPalette.xaml`, usando tonos de esa paleta y tonos neutros derivados.
- No ejecutar la aplicación ni agregar pruebas automatizadas.

## Criterios de aceptación

- Caja conserva sus tarjetas de producto.
- Clientes, crédito, inventario, proveedores, compras, ventas, resumen y configuración usan listas/filas compactas y jerarquía basada en tipografía, separadores y color.
- Las listas de clientes, productos, proveedores, compras y ventas se pueden filtrar mientras se escribe por un campo útil.
- `Vaciar` carrito y quitar un artículo se reconocen como acciones destructivas por su color rojo y aparecen antes que acciones positivas en su grupo.
- XAML compila y parsea; el proyecto no cambia dependencias.

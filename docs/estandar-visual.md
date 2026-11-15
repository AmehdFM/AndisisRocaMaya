# Estándar visual de Roca Maya

La interfaz usa la identidad existente y la estructura de los reportes. Los recursos reutilizables están en `Assets/ColorPalette.xaml`, `Assets/AppPresentation.xaml` y `Assets/ReportPresentation.xaml`.

## Colores y tipografía

| Uso | Color |
| --- | --- |
| Acción principal, selección y gráficos | Azul #3888C0 |
| Marca y acentos | Celeste #58B8E8 |
| Fondo general | #F3F8FB |
| Encabezados y secciones | #EAF5FB |
| Tablas y campos | Blanco |
| Texto principal | #080818 |
| Texto secundario | #526A7A |
| Bordes | #C8DCE8 |
| Acción destructiva / error | #B42318 sobre #FEE4E2 |
| Estado favorable | #287553 sobre #E5F2E9 |
| Advertencia / referencia | #9A5B23 sobre #FFF0CC |
| Navegación lateral | #193A52 |

Segoe UI. Título de pantalla 21 px seminegrita; botones y campos 12 px; encabezados de tabla 11 px. Las cantidades y totales conservan su jerarquía. El color no sustituye la etiqueta de una acción o estado.

## Distribución

- Margen exterior 16 px horizontal y 12 px vertical, sin limitar el ancho del contenido.
- Encabezado común con título a la izquierda y logo de 108 × 44 px a la derecha. Relleno 12 × 8 px; separación inferior 8 px.
- Filtros y búsquedas debajo del encabezado. Exportar PDF e Imprimir al extremo derecho de esa fila, en ese orden.
- Separación entre secciones 8 px y entre columnas 12 px. Los formularios mantienen 4 px entre etiqueta y campo.
- Preferir fondo continuo; usar blanco para tablas, listas y formularios que requieren contraste.
- Las pestañas de proveedores heredan el encabezado de Compras: no duplicar el logo dentro de la pestaña.
- Tablas y listas ocupan el espacio restante con desplazamiento. No centrar verticalmente las pantallas de trabajo.

## Acciones

- `AppPrimaryButton`: crear, registrar, guardar, confirmar y cobrar. Fondo azul y texto blanco.
- `AppButton`: consultar, cancelar, volver, exportar e imprimir. Fondo blanco, texto y borde azules.
- `AppDangerButton`: vaciar, quitar o eliminar. Rojo suave y etiqueta explícita.
- Altura habitual 34 px y relleno lateral 12 px. Espacio entre botones 8 px. Acciones principales de caja pueden medir 40 px para facilitar el uso repetido.
- Botones de icono 28–32 px con descripción accesible. Los botones que representan filas completas conservan su selección y ancho.
- Estados comunes de hover, pulsación, foco de teclado y deshabilitado. No depender del estilo predeterminado de Windows.
- Formularios modales: título y logo arriba, contenido debajo, cancelar a la izquierda de confirmar en el pie derecho.

## Aplicación y verificación

Mantener los enlaces, permisos y comandos existentes. Revisar vistas y pestañas a 1366 × 768 y al mínimo de ventana de 1024 × 640; los gráficos y reportes conservan sus unidades y valores. El acceso conserva su composición propia e incorpora la misma marca. Los PDF mantienen su plantilla independiente de impresión.

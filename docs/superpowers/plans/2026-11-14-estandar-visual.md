# Plan del estándar visual

**Objetivo:** Unificar las pantallas y aprovechar el espacio disponible en desarrollo.

**Arquitectura:** Diccionario WPF compartido para encabezados, acciones, campos, pestañas y tablas. Migrar las vistas existentes sin alterar comandos ni datos.

**Especificación:** `docs/estandar-visual.md`. Se mantiene la paleta y el patrón de contabilidad solicitado por el usuario.

- [x] Centralizar recursos de presentación y documentar tamaños, colores y ubicación de acciones.
- [x] Aplicar encabezados y márgenes compactos a las pantallas y formularios.
- [x] Sustituir estilos locales de botones y tablas por estilos compartidos conservando las excepciones de navegación y selección.
- [x] Compilar y cargar vistas/pestañas a dos resoluciones; renderizar muestras para inspeccionar espaciado, marca y controles.
- [x] Corregir desbordamientos y revisar el diff. Verificación final: 67 renderizaciones sin errores de XAML, conservando comandos, eventos y enlaces de datos.

# Login azul para Sistema Roca Maya

- **Intención:** reemplazar la ventana WPF vacía por una pantalla de inicio de sesión clara y pulida, adecuada al proyecto Sistema Roca Maya.
- **Diseño elegido:** tarjeta centrada con un detalle gráfico discreto, paleta azul, título de bienvenida, campos para usuario y contraseña y botón principal para iniciar sesión. Mantener el diseño legible dentro de la ventana de escritorio existente.
- **Enfoque:** construir la vista directamente en `MainWindow.xaml`, usando estilos y recursos XAML locales. No agregar paquetes ni imágenes externas.
- **Alternativas consideradas:** panel dividido con marca lateral (más expresivo, pero ocupa espacio en la ventana de 800 × 450); formulario sobrio sin detalle gráfico (más simple, pero menos distintivo). Se eligió la tarjeta centrada por equilibrio entre claridad y personalidad.
- **Alcance:** presentación visual de acceso solamente; no existe autenticación ni conexión a datos en este proyecto, así que el botón no validará credenciales.
- **Restricciones:** conservar WPF y .NET 8 existentes; mantener la ventana principal como punto de entrada.
- **Criterio de éxito:** al abrir la app se ve una pantalla azul cuidada, con campos claramente identificados y jerarquía visual consistente.

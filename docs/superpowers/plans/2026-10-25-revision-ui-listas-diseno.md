# Revisión UI de listas y superficies — Plan

**Spec:** `docs/superpowers/specs/2026-10-25-revision-ui-listas-diseno.md`

## Tareas

1. [x] Normalizar superficies de Resumen, Crédito, Inventario, Ventas y Configuración: eliminar contenedores-card redundantes en métricas/detalles/listas y mantener alineación con tonos Roca Maya.
2. [x] Afinar Clientes, Proveedores y Compras: conservar tablas/filas y añadir filtro instantáneo a productos de compra e historial general y por proveedor.
3. [x] Ajustar acciones en Caja: dar tratamiento rojo y ubicación inicial a Vaciar y quitar artículo; ampliar búsquedas en listas e historiales largos.
4. [x] Verificar estáticamente XAML, bindings, `git diff --check`, compilación y ausencia de cambios de paquetes.

## Restricciones

- No ejecutar la aplicación.
- No agregar/ejecutar tests automatizados (instrucción del entorno).
- Mantener tarjetas de producto en Caja.
- Verificación: `dotnet build --no-restore` → 0 errores; 22 XAML parseados; `git diff --check` limpio.
- La advertencia NU1701 de `SkiaSharp.Views.WPF` es preexistente; no se modificó el archivo de proyecto ni se añadieron paquetes.

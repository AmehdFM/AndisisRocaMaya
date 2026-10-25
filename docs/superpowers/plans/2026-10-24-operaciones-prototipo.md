# Operaciones del prototipo de Roca Maya — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax.

**Goal:** Completar las pantallas operativas solicitadas con datos compartidos en memoria y ventanas modales fijas, sin agregar paquetes.

**Architecture:** Un `DemoStore` compartido es la fuente de verdad durante la sesión para productos, lotes, compras, clientes, proveedores, créditos, ventas y caja. Vistas WPF enfocadas por módulo consumen ViewModels; las altas y consultas que el usuario pidió como emergentes se hacen en ventanas `ShowDialog` no redimensionables.

**Tech Stack:** WPF, .NET 8, CommunityToolkit.Mvvm ya existente, colecciones observables en memoria.

**Spec:** `docs/superpowers/specs/2026-10-24-operaciones-prototipo.md`

## Global Constraints

- Mantener datos en memoria; al cerrar el proceso se reinician.
- No agregar paquetes ni usar persistencia.
- Usar `Assets/ColorPalette.xaml` para colores de marca.
- Ventanas modales solicitadas usan `ShowDialog` y `ResizeMode="NoResize"`.
- No agregar ni ejecutar pruebas automatizadas; no abrir la aplicación sin solicitud.
- El conteo de caja admite denominaciones en lempiras y calcula subtotales y total en vivo.

## Review Focus

- La búsqueda de clientes/proveedores/productos con texto vacío devuelve la lista completa.
- Un producto con cantidades de varios lotes muestra existencia agregada e historial por fecha.
- Una venta a crédito requiere cliente y refleja cargo/abono y saldo en Crédito.
- Cancelar cierre de caja no cambia el estado; cancelar cierre de aplicación deja la ventana abierta.
- Reabrir secciones durante la sesión conserva compras, caja, ventas, clientes y movimientos.

## Plan Map

Las tareas son secuenciales porque comparten datos: primero modelos y almacenamiento de sesión; después se conectan clientes, proveedores, compras e inventario; luego créditos, caja y ventas; finalmente resumen, ajustes y navegación completa.

### Task 1: Datos operativos compartidos en memoria

**Files:**
- Create: `SistemaRocaMaya/Models/OperationalRecords.cs`
- Create: `SistemaRocaMaya/Services/OperationalDemoStore.cs`
- Modify: `SistemaRocaMaya/ViewModels/ShellViewModel.cs`

**Interfaces:**
- `OperationalDemoStore.Instance` expone colecciones observables y métodos para crear clientes, productos, proveedores, compras, ventas, créditos/abonos y sesiones de caja.
- Cada compra conserva líneas, costo unitario, cantidad, fecha y vencimiento opcional por lote; cada producto expone existencia agregada.

- [x] Definir modelos y enums de clientes, productos, lotes/movimientos, proveedores, compras, ventas, crédito y sesiones de caja.
- [x] Crear `OperationalDemoStore` con registros de muestra coherentes, contadores de IDs y operaciones que actualicen historiales y saldos relacionados.
- [x] Mantener las colecciones de sesión accesibles desde `ShellViewModel` sin añadir infraestructura externa.
- [x] Revisar formato y consistencia de datos iniciales de las pantallas de demostración existentes.

### Task 2: Clientes y consulta de crédito

**Files:**
- Create: `SistemaRocaMaya/ViewModels/CustomersViewModel.cs`
- Create: `SistemaRocaMaya/Views/CustomersView.xaml` y `.xaml.cs`
- Create: `SistemaRocaMaya/Views/CustomerEditWindow.xaml` y `.xaml.cs`
- Create: `SistemaRocaMaya/ViewModels/CreditsViewModel.cs`
- Create: `SistemaRocaMaya/Views/CreditsView.xaml` y `.xaml.cs`
- Modify: `SistemaRocaMaya/ViewModels/ShellViewModel.cs`, `SistemaRocaMaya/Views/ShellWindow.xaml`

**Interfaces:**
- Clientes: búsqueda por nombre, lista y alta modal con tipo Persona/Empresa y campo de identidad/RTN correspondiente.
- Ficha modal fija: datos del cliente, cargos y abonos recientes.
- Crédito: resumen de saldo, lista de clientes con saldo y detalle de movimientos; registrar abono actualiza el mismo libro de movimientos.

- [x] Sustituir la pantalla actual de Clientes y conectar búsqueda/selección/alta a `OperationalDemoStore`.
- [x] Implementar las ventanas modales fijas para crear y ver clientes.
- [x] Implementar pantalla de crédito y registro de abonos con historial.

### Task 3: Proveedores, compras e inventario

**Files:**
- Create: `SistemaRocaMaya/ViewModels/SuppliersViewModel.cs`
- Create: `SistemaRocaMaya/Views/SuppliersView.xaml` y `.xaml.cs`
- Create: `SistemaRocaMaya/Views/SupplierEditWindow.xaml` y `.xaml.cs`
- Create: `SistemaRocaMaya/ViewModels/PurchasesViewModel.cs`
- Create: `SistemaRocaMaya/Views/PurchasesView.xaml` y `.xaml.cs`
- Create: `SistemaRocaMaya/Views/ProductEditWindow.xaml` y `.xaml.cs`
- Create: `SistemaRocaMaya/ViewModels/InventoryViewModel.cs`
- Create: `SistemaRocaMaya/Views/InventoryView.xaml` y `.xaml.cs`
- Modify: `SistemaRocaMaya/ViewModels/ShellViewModel.cs`, `SistemaRocaMaya/Views/ShellWindow.xaml`

**Interfaces:**
- Proveedores: búsqueda/alta modal fija y ficha de compras.
- Compras: proveedor, fecha y múltiples líneas de producto/cantidad/costo; producto nuevo se crea en modal fija.
- Inventario: existencia por producto e historial de entradas con cantidades y fechas; lotes con vencimiento alimentan el resumen.

- [x] Implementar proveedores e historial enlazado a compras.
- [x] Implementar creación de productos y registro de compras con múltiples líneas.
- [x] Implementar inventario con historial por producto y filtro.
- [x] Conectar cada compra al historial de proveedor, al lote de inventario y a la existencia agregada.

### Task 4: Caja, ventas, créditos y protección de cierre

**Files:**
- Create: `SistemaRocaMaya/Views/CashSessionWindow.xaml` y `.xaml.cs`
- Create: `SistemaRocaMaya/ViewModels/SalesViewModel.cs`
- Create: `SistemaRocaMaya/Views/SalesView.xaml` y `.xaml.cs`
- Modify: `SistemaRocaMaya/ViewModels/PosViewModel.cs`, `SistemaRocaMaya/Views/PosView.xaml`, `SistemaRocaMaya/Views/PosView.xaml.cs`, `SistemaRocaMaya/Views/ShellWindow.xaml.cs`, `SistemaRocaMaya/ViewModels/ShellViewModel.cs`

**Interfaces:**
- Abrir/cerrar caja usa conteo por denominación, calcula totales y guarda la sesión de demostración.
- Ventas muestra ventas completadas y cierres; crédito requiere cliente y registra el cargo.
- El evento de cierre de la ventana cancela el cierre mientras haya caja abierta, salvo que se confirme un cierre exitoso.

- [x] Integrar el inicio/fin de sesión de caja y conteo por denominación.
- [x] Registrar ventas al contado confirmadas y ventas a crédito ligadas al cliente.
- [x] Implementar historial de ventas y cierres consultable.
- [x] Interceptar cierre de aplicación con caja abierta; continuar solo tras cierre exitoso.

### Task 5: Resumen, navegación y configuración

**Files:**
- Create: `SistemaRocaMaya/ViewModels/SettingsViewModel.cs`
- Create: `SistemaRocaMaya/Views/SettingsView.xaml` y `.xaml.cs`
- Modify: `SistemaRocaMaya/ViewModels/ShellViewModel.cs`, `SistemaRocaMaya/Views/ShellWindow.xaml`
- Modify: `docs/superpowers/specs/2026-10-24-operaciones-prototipo.md`

**Interfaces:**
- Sidebar incluye Resumen, Caja, Clientes, Inventario, Compras, Crédito, Proveedores, Ventas y Configuración.
- Configuración tiene secciones Usuarios y Roles de demostración.
- Resumen obtiene datos de ventas/caja y lotes próximos a vencer del store compartido.
- Inicio automático queda documentado para la configuración del instalador futuro.

- [x] Sustituir placeholders por Resumen y Configuración conectados a datos de sesión.
- [x] Añadir secciones de Usuarios y Roles dentro de Configuración.
- [x] Verificar navegación, estados vacíos y comportamiento de modales mediante revisión estática; no lanzar la aplicación.

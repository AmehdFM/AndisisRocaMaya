# Convenciones de ID en Sistema Roca Maya

SQLite guarda claves internas de tipo `INTEGER PRIMARY KEY` para enlazar registros. Las vistas y los reportes usan folios o códigos de texto independientes para identificar las operaciones ante el usuario.

## Formatos y dónde se ven

| Registro | Campo en SQLite | Formato y ejemplo | Dónde se muestra |
|---|---|---|---|
| Venta | `ventas.ticket` | `VT-AAAAMMDD-NNNN` · `VT-20261118-0003` | Actividad reciente de Inicio, columna Ticket de Ventas en efectivo por turno y su PDF. Aparece como origen en los documentos del Diario y del Mayor. |
| Compra | `compras.numero` | `CP-AAAAMMDD-NNNN` · `CP-20261118-0002` | Compras recientes, historial detallado por proveedor y su PDF. Aparece como origen en los documentos del Diario y del Mayor. |
| Factura del proveedor | `compras.referencia_factura` | Texto opcional copiado de la factura, por ejemplo `FAC-8931` | Registro de compras e historial detallado por proveedor y su PDF. El proveedor asigna esta referencia. |
| Turno de caja | `turnos_caja.folio` | `CJ-AAAAMMDD-NNNN` · `CJ-20261118-0001` | Caja, lista de Ventas en efectivo por turno y encabezado de su PDF. Se asigna con la fecha de apertura y se conserva al cerrar. |
| Conteo de caja | `turnos_caja.folio_apertura` y `turnos_caja.folio_cierre` | Folio del turno con `-AP` o `-CI` · `CJ-20261118-0001-AP` y `CJ-20261118-0001-CI` | Detalle de caja y PDF de Ventas en efectivo por turno. Cada renglón de billetes o monedas se relaciona por turno, momento y denominación. |
| Abono | `movimientos_credito.folio` | `AB-AAAAMMDD-NNNN` · `AB-20261118-0004` | Confirmación e historial de Clientes y movimientos en Crédito. Aparece como origen en los documentos del Diario y del Mayor. El cargo por una venta a crédito usa el ticket de esa venta. |
| Lote | `lotes_inventario.folio` | `LT-AAAAMMDD-NNNN` · `LT-20261118-0005` | Detalle de lotes en Inventario y resumen de existencias por lote al pie del Kardex y en su PDF. Un folio identifica cada lote. |
| Asiento contable | `asientos_contables.numero` | `AS-AAAAMMDD-NNNN` · `AS-20261118-0012` | Columna Documento del PDF o impresión del Diario y columna Documento del PDF del Mayor. Una venta con costo genera dos asientos con folios propios y el mismo ticket de origen. |
| Producto | `productos.codigo` | `PR-NNNNNN` · `PR-000125` | Inventario y búsqueda o selección de productos por código en Compras. |

## Cómo se generan

`AAAAMMDD` es la fecha de la operación en orden año, mes y día. `NNNN` es un consecutivo por prefijo y fecha, con un mínimo de cuatro dígitos. El código de producto usa un consecutivo general con un mínimo de seis dígitos. SQLite conserva cada consecutivo en `secuencias_folio`. Los conteos heredan el folio del turno y agregan el sufijo del momento.

`DatabaseWrite.Folio` obtiene el consecutivo dentro de la misma transacción que guarda la operación. `OperationalDemoStore` solicita el folio al registrar productos, compras, ventas, abonos, turnos y lotes. `DatabaseWrite.Post` asigna el de cada asiento. Los reportes contables enlazan el asiento con su origen mediante las claves internas y muestran ambas referencias juntas cuando corresponde, por ejemplo `AS-20261118-0012 · VT-20261118-0003`.

## Código relacionado

- Definición de tablas, campos, restricciones y sufijos de conteo. [schema.sql](../SistemaRocaMaya/Database/schema.sql)
- Folios, secuencias y asientos. [RocaMayaDatabase.cs](../SistemaRocaMaya/Services/RocaMayaDatabase.cs)
- Registro de operaciones y asignación de códigos. [OperationalDemoStore.cs](../SistemaRocaMaya/Services/OperationalDemoStore.cs)
- Referencias del Diario, el Mayor y los lotes del Kardex. [AccountingViewModel.cs](../SistemaRocaMaya/ViewModels/AccountingViewModel.cs)
- Encabezado y referencias de caja en el reporte de ventas. [SalesViewModel.cs](../SistemaRocaMaya/ViewModels/SalesViewModel.cs)

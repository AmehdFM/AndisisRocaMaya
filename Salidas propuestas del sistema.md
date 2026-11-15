# 1 Introducción 

El presente documento constituye el avance formal del diseño de salidas para el sistema de información desarrollado por la firma consultora Syntax Error para la empresa rural Pulpería Santa Lucía/Consulfarma. El objetivo central de este avance es definir las especificaciones técnicas, operativas y estratégicas de las ocho salidas requeridas por el sistema. Teniendo en cuenta que la solución informática se concibe con una naturaleza de sistema contable y de control estricto de existencias, cada reporte se ha diseñado para reemplazar los registros manuales e intuitivos por flujos de información confiables, oportunos y adaptados a las capacidades tecnológicas del establecimiento. 

# 2 Matriz de Determinación de las Salidas del Sistema 

La siguiente matriz de salida del sistema, está pensada para brindar información consistente, concisa y oportuna a la alta gerencia. Para ello se harán uso de 4 reportes detallados basados en los libros típicos de la teoría contable y las hojas de Kardex que detallan las entradas, salidas, costos y ventas de los productos en stock. También se cuenta con con 3 reportes sintetizados que buscan, resumir la información contable, resumen de cuentas por cobrar y ratios financieros explicados, siendo estos objetivos principales del sistema. Por último, se cuenta con un reporte ad hoc, que filtrará por proveedor para ver el historial de entradas y salidas de dicho proveedor, como información útil a la hora de discutir precios a la hora de negociar.

# 2.1 Matriz de Especificación de Salidas de Información

|   |   |   |   |
|---|---|---|---|
|**Categoría**|**Nombre del Reporte / Salida**|**Método de Salida**|**Propósito Operativo**|
|Detallado|Libro Diario Contable|Pantalla interactiva con opción de exportación a PDF|Registrar cronológicamente cada transacción financiera en partida doble (ventas, compras a proveedores, pagos de servicios, abonos y retiros).|
|Detallado|Libro Mayor / Auxiliar de Cuentas|Pantalla interactiva de consulta por cuenta|Desglosar el historial individual de cargos, abonos y saldos acumulados de cada cuenta contable (Caja, Cuentas por Cobrar, Inventario).|
|Detallado|Reporte de Transacciones Diarias de Venta en Efectivo|Pantalla de cierre de caja.|Conciliar operativamente el dinero físico en el cajón de cobro contra las ventas registradas en mostrador al final del turno.|
|Detallado|Reporte de Kardex de Inventario|Pantalla con filtros de búsqueda e impresión de informe|Rastrear el flujo físico y financiero de entradas y salidas de mercancía por lote y costo unitario para garantizar la rotación PEPS.|
|Sintetizado|Estados Financieros Consolidados|Pantalla ejecutiva e informe impreso estándar|Presentar los saldos netos consolidados de activos, pasivos, patrimonio, ingresos, costos y utilidad neta para la toma de decisiones.|
|Sintetizado|Reporte de Antigüedad de Saldos de Cuentas por Cobrar|Pantalla interactiva con semáforo de mora e informe para campo|Consolidar los créditos informales pendientes clasificados por cliente y rangos de morosidad (vigentes, 30, 60 y 90+ días) para agilizar cobros.|
|Sintetizado|Reporte de Ratios e Indicadores Financieros|Tablero visual con explicaciones interpretativas|Evaluar la salud financiera mediante el cálculo automático de liquidez, margen de utilidad y rotación de inventarios con guías conceptuales.|
|Ad hoc|Consulta Eventual de Historial de Compras por Proveedor|Pantalla de consulta dinámica a demanda con exportación a PDF|Responder a necesidades eventuales e imprevistas de negociación, filtrando compras, volúmenes e historial de precios por proveedor específico.|

Tabla 2.1. _Categoría, método de salida y propósito operativo._

|   |   |   |   |   |   |
|---|---|---|---|---|---|
|**Categoría**|**Nombre del Reporte / Salida**|**Usuario / Destinatario**|**Frecuencia**|**Lugar de Disponibilida d**|**Periodo de Almacenami ento**|
|Detallado|Libro Diario Contable|Propietario / Gerente y Analista Contable|Diario / Revisión Semanal|Módulo de Contabilidad General en escritorio|5 Años|
|Detallado|Libro Mayor / Auxiliar de Cuentas|Propietario / Gerente y Auxiliar Contable|Mensual o a demanda|Módulo de Contabilidad General|5 Años|
|Detallado|Reporte de Transacciones Diarias de Venta en Efectivo|Encargado de Caja / Mostrador y Propietario|Diaria al cierre del turno|Estación de trabajo en Punto de Venta|1 Año|
|Detallado|Reporte de Kardex de Inventario|Encargado de Bodega y Propietario|Diario / Semanal|Módulo de Inventario y Control de Bodega|3 Años|
|Sintetizado|Estados Financieros Consolidados|Propietario / Gerencia General|Mensual, Trimestral y Anual|Módulo de Gerencia General|Permanente|
|Sintetizado|Reporte de Antigüedad de Saldos de Cuentas por Cobrar|Propietario / Gestor de Créditos|Semanal / Quincenal|Módulo de Gestión de Créditos|3 Años|
|Sintetizado|Reporte de Ratios e Indicadores Financieros|Propietario / Gerente General|Mensual y Trimestral|Módulo Gerencial / Estación Principal|5 Años|
|Ad hoc|Consulta Eventual de Historial de Compras por Proveedor|Propietario / Encargado de Compras|Eventual|Módulo de Compras e Inventario|3 Años|

Tabla 2.2. _Destinatarios, frecuencia, disponibilidad y almacenamiento._

# 3.  Descripción y Fundamentación del Reporte Ad Hoc

**3.1**  **Origen de la Necesidad de Información**

En el entorno operativo de la pulpería, las relaciones con los distribuidores mayoristas de víveres y medicamentos ocurren de forma periódica mediante visitas presenciales de agentes de ventas a la tienda. En estas reuniones imprevistas, los proveedores suelen anunciar incrementos porcentuales en los costos de insumos básicos o proponer descuentos especiales condicionados a compras por volumen.

Bajo la gestión manual en papel, el propietario carecía de herramientas inmediatas para verificar si los precios del proveedor habían aumentado previamente o para calcular el volumen total comprado en meses anteriores. El Reporte Ad hoc de Consulta Eventual de Historial de Compras y Variación de Costos por Proveedor nace para resolver esta brecha de información en el instante exacto en que ocurre la negociación.

# 3.2  Funcionamiento Dinámico de la Salida

A diferencia de los reportes rutinarios que poseen estructuras fijas y programadas, esta salida opera mediante un formulario de consulta en pantalla donde el usuario selecciona a un proveedor específico y establece un rango de fechas personalizado (por ejemplo, los últimos cuatro o seis meses). El sistema consulta las tablas relacionales de la base de datos local y genera en segundos una vista consolidada que presenta:

•         El catálogo de productos adquiridos a ese proveedor en el período seleccionado.

•         El desglose de cada factura ingresada con su fecha y volumen correspondiente.

•         La evolución histórica del costo unitario de cada producto, calculando la variación porcentual entre compras.

•         El monto total acumulado invertido en dicho proveedor.


Esta capacidad analítica le otorga al propietario un respaldo cuantitativo inmediato para exigir descuentos por volumen o rechazar incrementos injustificados de costo durante la visita del distribuidor.
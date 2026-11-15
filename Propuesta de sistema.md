**Es un sistema contable con un pequeño modulo de venta e inventario**

# Introducción

En el entorno comercial, la transición desde esquemas operativos informales hacia la digitalización centralizada representa un factor determinante para la supervivencia y escalabilidad de las micro, pequeñas y medianas empresas. Un Sistema de Información no solo optimiza el procesamiento transaccional en el punto de venta, sino que se convierte en la columna vertebral estratégica para el control de activos, la reducción de costos operativos y la toma de decisiones fundamentadas en datos reales.

El presente informe documenta el diagnóstico, análisis de factibilidad, diseño e ingeniería de implementación de un Sistema Contable y Punto de Venta desarrollado para el establecimiento comercial Consulfarma Rocamaya / Pulpería Santa Lucía, ubicado en la Aldea Santa Lucía, Yoro. A pesar de experimentar una expansión constante hacia diversos rubros (farmacia, abarrotes, combustibles y servicios financieros), la empresa ha mantenido una gestión empírica e informal, generando cuellos de botella críticos como la falta de centralización de precios, pérdidas financieras por cuentas por cobrar no registradas, mermas de productos por vencimiento y un desconocimiento de la utilidad neta real del negocio. Para abordar esta situación, el equipo de consultoría en Ingeniería en Sistemas SYNTAX ERROR ha estructurado un abordaje metódico orientado a la modernización de sus procesos.

# Resumen Ejecutivo

El documento técnico del grupo consultor Syntax Error presenta la propuesta integral para la automatización de la empresa rural Consulfarma Rocamaya, conocida también como Pulpería Santa Lucía. La organización evolucionó desde la venta inicial de medicamentos hasta convertirse en un establecimiento comercial diversificado gestionado por sus propietarios. El diagnóstico reveló que el negocio opera bajo procedimientos informales e intuitivos, destacando problemas como la dificultad para consultar precios en la atención al cliente, la pérdida de ingresos por ventas al crédito no registradas, el desconocimiento de las utilidades reales y la falta de control sobre los costos de inventario. Como oportunidad estratégica, se plantea la transformación futura del negocio en una bodega distribuidora mayorista para surtir a comunidades vecinas.

Para resolver estas deficiencias, el proyecto establece el desarrollo de un sistema contable e inventario a la medida que centralice el control del negocio y reemplace los registros manuales en papel. Los alcances obligatorios del software incluyen un módulo de punto de venta con búsqueda rápida de artículos, la gestión de cuentas por cobrar con saldos actualizados al instante, un módulo contable simplificado que genere de forma automática el libro diario, el libro mayor y los estados financieros, y un control de existencias bajo el método de Primeras en Entrar, Primeras en Salir. Tras evaluar opciones como sitios web de consulta o paquetes comerciales, se seleccionó el desarrollo en casa por ser la única opción que satisface todos los requerimientos locales sin generar pagos recurrentes por licencias.

El estudio de viabilidad confirmó la factibilidad técnica, económica y operativa de la solución. En el aspecto técnico, la empresa cuenta con infraestructura de respaldo eléctrico mediante planta a gasolina y sistema de alimentación ininterrumpida, así como conectividad por internet satelital Starlink, requiriéndose únicamente el reemplazo de una computadora personal de escritorio dedicada. En el ámbito económico, se determinó una inversión inicial de veinte mil Lempiras por servicios profesionales de desarrollo y entre siete mil y diez mil Lempiras para la adquisición de la computadora, la cual se optimiza gracias al reuso de periféricos existentes valorados en veinte mil Lempiras. Los beneficios tangibles anuales suman treinta y dos mil Lempiras derivados de la recuperación de cuentas por cobrar y la disminución de pérdidas por productos vencidos, logrando la recuperación de la inversión en aproximadamente un año de operación. En la parte operativa, la experiencia previa de la gerencia y una interfaz intuitiva garantizan una adopción sin resistencia.

Por último, el informe organiza la ejecución del proyecto a través de una planificación temporal estructurada en un diagrama de Gantt e integra a los cinco miembros del equipo consultor Syntax Error con roles definidos. El equipo está conformado por un gestor de proyectos que coordina la comunicación y planificación, un arquitecto de infraestructura responsable de la continuidad técnica local, un administrador de base de datos relacional, un desarrollador enfocado en la interfaz y experiencia de usuario de baja fricción, y un analista de sistemas encargado de traducir las necesidades comerciales en especificaciones técnicas.

# 1           La Organización Como Sistema

## 1.1         Historia

Surge inicialmente como un pequeño negocio orientado a la venta de medicamentos, aprovechando productos que se encontraban disponibles a partir de una iniciativa previa relacionada con servicios de salud.

En sus primeros momentos, también se contempló la posibilidad de complementar esta actividad mediante la atención de pacientes por parte de personal de enfermería fuera del horario del centro de salud. Sin embargo, debido a las dificultades para establecer este tipo de colaboración, el negocio continuó desarrollándose principalmente mediante la comercialización de medicamentos.

 Con el paso del tiempo, las necesidades de los habitantes de la comunidad comenzaron a orientar el crecimiento del establecimiento. Los propios clientes solicitaban productos adicionales, especialmente artículos de la canasta básica, lo que llevó a ampliar progresivamente la oferta hasta convertir el negocio en una tienda con una variedad cada vez mayor de productos.

Posteriormente, se incorporaron nuevas líneas de venta poco habituales en este tipo de establecimientos, como gasolina, ropa (tanto nueva como usada). Más adelante también se añadieron servicios relacionados con la entrega de dinero enviado desde otras zonas del país o desde el extranjero, cobrando una comisión por la gestión realizada.

El crecimiento del negocio se produjo dentro de un entorno competitivo, ya que existían otros pequeños comercios en la misma comunidad. Frente a esta situación, la empresa logró diferenciarse principalmente por ofrecer una mayor variedad de productos y por brindar una atención rápida y cercana a los clientes. Estos factores permitieron fortalecer su posición dentro de la comunidad y ampliar progresivamente sus actividades comerciales.

## 1.2         Misión

Facilitar la vida cotidiana de nuestra comunidad ofreciendo productos de abarrotes de calidad y sin interrupciones, brindando una atención cercana y moderna, asegurando comodidad y soluciones a cada cliente.

## 1.3         Visión

Consolidarnos como el principal proveedor de abarrotes de la región, siendo el aliado estratégico de las familias y negocios que buscan la comodidad y facilidad de productor y crecer de manera segura.

## 1.4         Valores centrales

·         **Responsabilidad:** siempre ha sido prioridad surtido de productos en toda el área, velando por las necesidades de los clientes y siendo atentos a lo que demandan con el fin de ofrecer diversidad de productos atendiendo de manera veloz otorgando facilidad de acceso en tanto los clientes puedan conseguir todo lo que necesitan sin tener que preocuparse por viajar grandes distancias garantizándoles productos de buena calidad.

·         **Amabilidad:** los clientes son tratados con cordialidad, otorgándoles el trato más humano posible creando una amistad vendedor/cliente que permite así una comunicación efectiva entre ambas partes generando confianza del cliente hacia el local.

·         **Confianza:** el local cuenta con un sistema de ventas al crédito para sus clientes, asegurándose que el cliente pueda comprar al momento que lo necesite y pagar con el pasar del tiempo solventando su necesidad esto es gracias a la confianza que se genera entre ambas partes.

·         **Solidaridad:** Se prioriza siempre apoyar a la comunidad otorgándoles ayudandoles en situaciones donde lo necesiten sin poner por encima la esperanza de remuneración, ya sea reparando bicicletas o brindando primeros auxilios a pacientes espontáneos.

·         **Constancia y Trabajo:** La cultura y trabajo siempre serán una demostración de gran fuerza y esfuerzo en la comunidad. La constancia y entrega hacen mella en los corazones de los clientes, que observan como se trabaja en ser mejor día a día.

## 1.5         Objetivos organizacionales

**Adquisición de un local propio y expansión del espacio**: Debido a que la empresa opera actualmente en una propiedad arrendada, uno de los objetivos principales es contar con un inmueble propio que permita ampliar el local y, a futuro, convertir el negocio en un centro de abastos al por mayor y menor.

**Diversificación hacia el rubro agroveterinario:** Aprovechando que la comunidad se ubica en una zona productora de ganado, granos básicos y café, se busca extender la oferta comercial hacia la agroveterinaria para abastecer de insumos esenciales a los productores locales.

**Optimización de la relación con proveedores de medicamentos:** Se pretende ampliar el catálogo de productos farmacéuticos, obtener mejores precios de compra y negociar condiciones que permitan la devolución o cambio de medicamentos próximos a vencer para evitar pérdidas financieras.

**Mejora del control interno e inventarios:** Implementar un sistema de registro y organización de productos que facilite la ubicación rápida de la mercadería y el control exacto de las existencias sin necesidad de realizar búsquedas manuales.

## 1.6         Cultura organizacional

### 1.6.1        Filosofía de Servicio y Dinámica en Punto de Venta

El establecimiento combina el modelo tradicional de comercio de abarrotes con un enfoque diferenciado de estancia y servicio al cliente, ofreciendo un trato personalizado y condiciones de infraestructura atípicas para la zona (continuidad eléctrica y conectividad). La implementación del sistema busca preservar esta cercanía: las interfaces de cobro se diseñarán para una interacción mínima y de baja fricción, asegurando que la digitalización no dilate el tiempo de atención en mostrador ni mecanice la relación con la comunidad.

### 1.6.2        Operatividad

La toma de decisiones estratégicas, compras y negociación con proveedores está centralizada exclusivamente en la alta gerencia propietaria. La labor operativa (almacén, reposición y despacho) recae en personal de mostrador y apoyo temporal. Por tanto, la arquitectura de software adoptará un esquema de permisos estrictamente segregado:

·         **Nivel Operativo (Caja / Bodega):** Lista de precios de mercancías e interfaces de cobro.

·         **Nivel Administrativo (Gerencia):** Acceso a la información contable y a la información analítica del rendimiento general de la empresa, así como el registro de transacciones

### 1.6.3        Resistencia tecnológica

Dado el arraigo de los métodos analógicos propios de los comercios locales del sector, existe una barrera cultural natural frente a la automatización de procesos. No obstante, la apertura de la gerencia hacia la tecnificación facilita el proceso. Para mitigar la fricción en el personal operativo y temporal cuya rotación exige curvas de aprendizaje mínimas, la adopción se ejecutará en etapas, priorizando la capacitación ejecutiva de los administradores y acompañando al personal de mostrador con flujos asistidos y validaciones visuales claras.

# 2           Definición Del Problema

## 2.1         Identificación de Problemas, Oportunidades y Objetivos de la Organización:

### 2.1.1        Declaración diagnostica de la problemática y Oportunidad.

Cuando una organización enfrenta una situación operativa adversa que impide el incumplimiento de las metas o cuando identifican oportunidades clave para innovar y expandirse. En el caso de Pulpería Santa Lucía/Consulfarma Rocamaya, a pesar de experimentar un crecimiento sostenido en su comunidad rural, la gestión administrativa y comercial se ha mantenido bajo procedimientos tradicionales, informales e intuitivos.

Esto ha generado algunos cuellos de botella críticos, como la falta de inventario centralizado de precios, el olvido recurrente en el cobro de ventas al crédito, mermas de productos vencidos en la sección de la farmacia y un desconocimiento absoluto de la utilidad neta y situación financiera real del negocio.

A su vez, el negocio enfrenta una oportunidad de mercado estratégica, que es consolidarse como un centro de abasto como un centro de abasto y bodega mayorista para surtir a más de 30 comunidades y caseríos vecinos. Para capitalizar esta expansión e incorporar los nuevos rubos, la empresa requiere sustituir el manejo empírico por un sistema de información contable y de punto de venta rápida, teniendo que ser capaz de automatizar el control de las existencias bajo el método PEPS, crear partidas contables mediante los datos de caja y de generar los estados financieros y ratios para una buena toma de decisiones fundamentándose en datos reales.

### 2.1.2        Matriz de Problema - Oportunidad - Objetivo – Restricción

La matriz diagnostica que vinculara cada causa y efecto detectado como las metas del software y las restricciones invariables de su entorno.

|Problema / Oportunidad|Causas y/o Efectos|Objetivos del Sistema|Limitaciones / Restricciones del Sistema|
|---|---|---|---|
|1. Problema: la dificultad para consultar precios de venta al momento y la imposibilidad de recibir apoyo de personal externo en caja por estar ocupados.|Causas: Inexistencia de un control del inventario no se sabe que sale y que entra con exactitud; los precios están anotados en hojas dispersas que solo la gerencia conoce.  <br>Efectos: Lentitud en la atención, generación de colas en mostrador y dependencia exclusiva de los dueños para cobrar.|Implementar un módulo de Punto de Venta rápido con búsqueda por código o nombre del producto y cálculo automático de cambio para agilizar la atención.|El módulo será operado mediante una interfaz sumamente fácil de entender, que no requiera una gran explicación o complejidad para el personal de apoyo.|
|2. Problema: Pérdida de ingresos por cuentas por cobrar no cobradas.|Causas: Registros manuales, informal o de palabra de los productos otorgados por fiado a los vecinos de la aldea.  <br>Efectos: cuentas morosas acumulándose, se extravían las notas y hay pérdida directa de capital de trabajo.|Crear un módulo de control de Cuentas por Cobrar que permita visualizar fielmente los saldos pendientes, clientes morosos e historial de créditos.|El sistema debe mantener la flexibilidad del crédito comunitario sin alterar la relación de confianza con los clientes ni exigir integración bancaria compleja.|
|3. Problema: Desconocimiento de la rentabilidad real, utilidad neta y estructura financiera de la empresa.|Causas: No hay registros contables formales; las decisiones se toman por intuición y no por datos.  <br>Efectos: Desconocimiento sobre el patrimonio, imposibilitando evaluar márgenes netos y los riesgos de descapitalización.|Diseñar un sistema de partidas contables simplificadas (Libro Diario y Mayor automáticos), estados de situación financiera, estados de resultados e indicadores (ratios).|Poco alcance: Todas las transacciones se registrarán bajo la modalidad de caja (efectivo), excluyendo facturación electrónica fiscal y las conciliaciones bancarias más complejas.|
|4. Problema: Imposibilidad de conocer el costo de sus activos corrientes.|Causas: La bodega se maneja por un sistema empírico de “lo que haga falta”, por lo tanto, no solo se desconoce el valor de los activos, si no que tampoco se conoce el flujo de los mismos.|Incorporar un control de inventario valorado mediante el método PEPS que permita conocer el valor de la mercadería y el flujo de los misma.|Facilidad: Para que no se convierta en una molestia el sistema debe permitir entradas eficientes.|
|5. Oportunidad: Conversión estratégica a Bodega Comercial Mayorista para surtir a 30 comunidades y diversificación agro veterinaria.|Causas: Creciente demanda de pequeños comercios en aldeas vecinas y presencia de productores ganaderos y cafetaleros en la zona.  <br>Efectos: Oportunidad de incrementar el volumen de ventas por paquetes y diversificar ingresos.|Escalar el sistema para gestionar precios mayoristas por paquete, volúmenes de venta masivos e integrar la futura línea de productos agro veterinarios.|Restricción técnica e infraestructura: El software debe ser liviano y operar de manera ininterrumpida apoyado por la red Starlink, la batería acumuladora (8-10 hrs) y la planta eléctrica ante apagones.|
### 3.1.1        Propuesta #3 Sistema contable

Sistema contable con módulos de libros diario y mayor, con sistema de generación automática de estados financieros y ratios financieros, además de control de inventario utilizando el método PEPS

Ventajas:

Brinda un control de las finanzas de la empresa y permite inventariar correctamente los productos de la empresa utilizando métodos fiables y precisos para calcular costos y determinar la utilidad en base a los ingresos.

Desventajas:

Dado que no hay presencia de personal capacitado en la contabilidad, el sistema debe abstraer la información lo suficiente para que sea utilizado por la gerencia.


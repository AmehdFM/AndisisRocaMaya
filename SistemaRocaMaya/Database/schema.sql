PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS roles (id INTEGER PRIMARY KEY, nombre TEXT NOT NULL UNIQUE);
CREATE TABLE IF NOT EXISTS permisos (id INTEGER PRIMARY KEY, codigo TEXT NOT NULL UNIQUE, descripcion TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS rol_permisos (
    rol_id INTEGER NOT NULL REFERENCES roles(id), permiso_id INTEGER NOT NULL REFERENCES permisos(id),
    PRIMARY KEY (rol_id, permiso_id)
);
CREATE TABLE IF NOT EXISTS usuarios (
    id INTEGER PRIMARY KEY, nombre_usuario TEXT NOT NULL COLLATE NOCASE UNIQUE, contrasena_hash TEXT NOT NULL,
    rol_id INTEGER NOT NULL REFERENCES roles(id), activo INTEGER NOT NULL DEFAULT 1 CHECK(activo IN (0,1))
);
CREATE TABLE IF NOT EXISTS tipos_cliente (id INTEGER PRIMARY KEY, codigo TEXT NOT NULL UNIQUE, nombre TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS clientes (
    id INTEGER PRIMARY KEY, nombre TEXT NOT NULL, telefono TEXT, tipo_cliente_id INTEGER NOT NULL REFERENCES tipos_cliente(id),
    identidad_o_rtn TEXT, activo INTEGER NOT NULL DEFAULT 1 CHECK(activo IN (0,1))
);
CREATE TABLE IF NOT EXISTS proveedores (
    id INTEGER PRIMARY KEY, nombre TEXT NOT NULL, telefono TEXT, contacto TEXT, activo INTEGER NOT NULL DEFAULT 1 CHECK(activo IN (0,1))
);
CREATE TABLE IF NOT EXISTS productos (
    id INTEGER PRIMARY KEY, codigo TEXT NOT NULL UNIQUE, nombre TEXT NOT NULL,
    precio_venta_centavos INTEGER NOT NULL CHECK(precio_venta_centavos >= 0), activo INTEGER NOT NULL DEFAULT 1 CHECK(activo IN (0,1))
);
CREATE TABLE IF NOT EXISTS compras (
    id INTEGER PRIMARY KEY, numero TEXT NOT NULL UNIQUE, proveedor_id INTEGER NOT NULL REFERENCES proveedores(id),
    usuario_id INTEGER NOT NULL REFERENCES usuarios(id), fecha_hora TEXT NOT NULL, referencia_factura TEXT
);
CREATE TABLE IF NOT EXISTS compra_detalles (
    id INTEGER PRIMARY KEY, compra_id INTEGER NOT NULL REFERENCES compras(id), producto_id INTEGER NOT NULL REFERENCES productos(id),
    cantidad INTEGER NOT NULL CHECK(cantidad > 0), costo_unitario_centavos INTEGER NOT NULL CHECK(costo_unitario_centavos >= 0)
);
CREATE TABLE IF NOT EXISTS turnos_caja (
    id INTEGER PRIMARY KEY, folio TEXT NOT NULL UNIQUE, abierto_por_usuario_id INTEGER NOT NULL REFERENCES usuarios(id),
    cerrado_por_usuario_id INTEGER REFERENCES usuarios(id), apertura_fecha_hora TEXT NOT NULL, cierre_fecha_hora TEXT,
    folio_apertura TEXT GENERATED ALWAYS AS (folio || '-AP') VIRTUAL,
    folio_cierre TEXT GENERATED ALWAYS AS (folio || '-CI') VIRTUAL,
    CHECK ((cierre_fecha_hora IS NULL) = (cerrado_por_usuario_id IS NULL)),
    CHECK (cierre_fecha_hora IS NULL OR cierre_fecha_hora >= apertura_fecha_hora)
);
CREATE UNIQUE INDEX IF NOT EXISTS un_turno_abierto ON turnos_caja((1)) WHERE cierre_fecha_hora IS NULL;
CREATE TABLE IF NOT EXISTS momentos_conteo_caja (id INTEGER PRIMARY KEY, codigo TEXT NOT NULL UNIQUE, nombre TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS conteos_caja (
    turno_id INTEGER NOT NULL REFERENCES turnos_caja(id), momento_id INTEGER NOT NULL REFERENCES momentos_conteo_caja(id),
    denominacion_centavos INTEGER NOT NULL CHECK(denominacion_centavos > 0), cantidad INTEGER NOT NULL CHECK(cantidad >= 0),
    PRIMARY KEY(turno_id,momento_id,denominacion_centavos)
);
CREATE TABLE IF NOT EXISTS ventas (
    id INTEGER PRIMARY KEY, ticket TEXT NOT NULL UNIQUE, turno_id INTEGER NOT NULL REFERENCES turnos_caja(id),
    usuario_id INTEGER NOT NULL REFERENCES usuarios(id), cliente_id INTEGER REFERENCES clientes(id),
    fecha_hora TEXT NOT NULL, tipo_pago TEXT NOT NULL CHECK(tipo_pago IN ('CONTADO','CREDITO')),
    CHECK(tipo_pago != 'CREDITO' OR cliente_id IS NOT NULL)
);
CREATE TABLE IF NOT EXISTS venta_detalles (
    id INTEGER PRIMARY KEY, venta_id INTEGER NOT NULL REFERENCES ventas(id), producto_id INTEGER NOT NULL REFERENCES productos(id),
    cantidad INTEGER NOT NULL CHECK(cantidad > 0), precio_unitario_centavos INTEGER NOT NULL CHECK(precio_unitario_centavos >= 0)
);
CREATE TABLE IF NOT EXISTS lotes_inventario (
    id INTEGER PRIMARY KEY, folio TEXT NOT NULL UNIQUE, producto_id INTEGER NOT NULL REFERENCES productos(id),
    compra_detalle_id INTEGER REFERENCES compra_detalles(id), fecha_ingreso TEXT NOT NULL, fecha_vencimiento TEXT,
    cantidad_ingresada INTEGER NOT NULL CHECK(cantidad_ingresada > 0), costo_unitario_centavos INTEGER NOT NULL CHECK(costo_unitario_centavos >= 0)
);
CREATE TABLE IF NOT EXISTS consumos_lote (
    id INTEGER PRIMARY KEY, venta_detalle_id INTEGER NOT NULL REFERENCES venta_detalles(id),
    lote_id INTEGER NOT NULL REFERENCES lotes_inventario(id), cantidad INTEGER NOT NULL CHECK(cantidad > 0),
    UNIQUE(venta_detalle_id,lote_id)
);
CREATE TABLE IF NOT EXISTS costeos_venta_lote (
    id INTEGER PRIMARY KEY, venta_detalle_id INTEGER NOT NULL REFERENCES venta_detalles(id),
    lote_id INTEGER NOT NULL REFERENCES lotes_inventario(id), cantidad INTEGER NOT NULL CHECK(cantidad > 0),
    costo_unitario_centavos INTEGER NOT NULL CHECK(costo_unitario_centavos >= 0), UNIQUE(venta_detalle_id,lote_id)
);
CREATE TABLE IF NOT EXISTS tipos_movimiento_credito (id INTEGER PRIMARY KEY, codigo TEXT NOT NULL UNIQUE, nombre TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS movimientos_credito (
    id INTEGER PRIMARY KEY, folio TEXT UNIQUE, cliente_id INTEGER NOT NULL REFERENCES clientes(id),
    venta_id INTEGER REFERENCES ventas(id), usuario_id INTEGER NOT NULL REFERENCES usuarios(id), fecha_hora TEXT NOT NULL,
    tipo_movimiento_id INTEGER NOT NULL REFERENCES tipos_movimiento_credito(id), importe_centavos INTEGER NOT NULL CHECK(importe_centavos > 0),
    fecha_vencimiento TEXT, concepto TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS cuentas_contables (
    id INTEGER PRIMARY KEY, codigo TEXT NOT NULL UNIQUE, nombre TEXT NOT NULL UNIQUE,
    tipo TEXT NOT NULL CHECK(tipo IN ('ACTIVO','PASIVO','PATRIMONIO','INGRESO','COSTO','GASTO')),
    naturaleza TEXT NOT NULL CHECK(naturaleza IN ('DEUDORA','ACREEDORA')), activa INTEGER NOT NULL DEFAULT 1 CHECK(activa IN (0,1))
);
CREATE TABLE IF NOT EXISTS asientos_contables (
    id INTEGER PRIMARY KEY, numero TEXT NOT NULL UNIQUE, fecha_hora TEXT NOT NULL, concepto TEXT NOT NULL,
    usuario_id INTEGER NOT NULL REFERENCES usuarios(id), compra_id INTEGER REFERENCES compras(id),
    venta_id INTEGER REFERENCES ventas(id), movimiento_credito_id INTEGER REFERENCES movimientos_credito(id),
    CHECK((compra_id IS NOT NULL)+(venta_id IS NOT NULL)+(movimiento_credito_id IS NOT NULL)<=1)
);
CREATE TABLE IF NOT EXISTS asiento_detalles (
    id INTEGER PRIMARY KEY, asiento_id INTEGER NOT NULL REFERENCES asientos_contables(id),
    cuenta_id INTEGER NOT NULL REFERENCES cuentas_contables(id), orden INTEGER NOT NULL, concepto TEXT,
    debito_centavos INTEGER NOT NULL DEFAULT 0 CHECK(debito_centavos >= 0), credito_centavos INTEGER NOT NULL DEFAULT 0 CHECK(credito_centavos >= 0),
    CHECK((debito_centavos > 0) != (credito_centavos > 0)), UNIQUE(asiento_id,orden)
);

CREATE TABLE IF NOT EXISTS secuencias_folio (
    tipo TEXT NOT NULL, fecha TEXT NOT NULL,
    ultimo INTEGER NOT NULL CHECK(ultimo > 0), PRIMARY KEY(tipo,fecha)
);
CREATE TABLE IF NOT EXISTS configuracion (clave TEXT PRIMARY KEY, valor TEXT NOT NULL);
CREATE INDEX IF NOT EXISTS compras_proveedor_fecha ON compras(proveedor_id,fecha_hora);
CREATE INDEX IF NOT EXISTS compras_detalles_compra ON compra_detalles(compra_id);
CREATE INDEX IF NOT EXISTS ventas_turno_fecha ON ventas(turno_id,fecha_hora);
CREATE INDEX IF NOT EXISTS ventas_detalles_venta ON venta_detalles(venta_id);
CREATE INDEX IF NOT EXISTS lotes_producto_fecha ON lotes_inventario(producto_id,fecha_ingreso);
CREATE INDEX IF NOT EXISTS consumos_por_lote ON consumos_lote(lote_id);
CREATE INDEX IF NOT EXISTS costeos_por_lote ON costeos_venta_lote(lote_id);
CREATE INDEX IF NOT EXISTS credito_cliente_fecha ON movimientos_credito(cliente_id,fecha_hora);
CREATE INDEX IF NOT EXISTS asiento_fecha ON asientos_contables(fecha_hora);
CREATE INDEX IF NOT EXISTS asiento_detalle_cuenta ON asiento_detalles(cuenta_id);

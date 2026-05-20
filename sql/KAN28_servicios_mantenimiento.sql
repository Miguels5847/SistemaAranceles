-- KAN-28: Servicios + Mantenimiento (RF-MI-01)
-- Hoja "8 Mantenimiento": Servicios Básicos (B8:B12) + Mantenimiento (B15:B20)

CREATE TABLE IF NOT EXISTS mantenimiento_servicio (
    id                       SERIAL PRIMARY KEY,
    carrera_id               INTEGER NOT NULL REFERENCES carrera(id),
    tipo_rubro               VARCHAR(30) NOT NULL,
    nombre_rubro             VARCHAR(150) NOT NULL,
    costo_anual_universidad  NUMERIC(18,2) NOT NULL DEFAULT 0,
    creado_en                TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    creado_por_usuario_id    INTEGER,
    actualizado_en           TIMESTAMPTZ,
    actualizado_por_usuario_id INTEGER,
    esta_activo              BOOLEAN NOT NULL DEFAULT TRUE,
    eliminado_en             TIMESTAMPTZ,
    eliminado_por_usuario_id INTEGER
);

CREATE INDEX IF NOT EXISTS ix_mantenimiento_servicio_carrera_tipo
    ON mantenimiento_servicio (carrera_id, tipo_rubro)
    WHERE esta_activo = TRUE;

-- Seed: valores del Excel "8 Mantenimiento" para carrera_id = 1
-- Servicios Básicos (B8:B11 → Total B12 = $1,058,000)
INSERT INTO mantenimiento_servicio (carrera_id, tipo_rubro, nombre_rubro, costo_anual_universidad)
VALUES
    (1, 'ServicioBasico', 'Agua',               300000),
    (1, 'ServicioBasico', 'Internet',            150000),
    (1, 'ServicioBasico', 'Energía Eléctrica',   500000),
    (1, 'ServicioBasico', 'Comunicaciones',       108000)
ON CONFLICT DO NOTHING;

-- Mantenimiento (B15:B19 → Total B20 = $2,959,400)
INSERT INTO mantenimiento_servicio (carrera_id, tipo_rubro, nombre_rubro, costo_anual_universidad)
VALUES
    (1, 'Mantenimiento', 'Limpieza',              480000),
    (1, 'Mantenimiento', 'Seguros',               720000),
    (1, 'Mantenimiento', 'Seguridad',             960000),
    (1, 'Mantenimiento', 'Refacciones',           399400),
    (1, 'Mantenimiento', 'Infraestructura',       400000)
ON CONFLICT DO NOTHING;

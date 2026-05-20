-- KAN-31: Activos Diferidos — Amortización (RF-MI-04)
-- Hoja "Amortización": permisos legales al 20% anual

CREATE TABLE IF NOT EXISTS activo_diferido (
    id                         SERIAL PRIMARY KEY,
    carrera_id                 INTEGER NOT NULL REFERENCES carrera(id),
    nombre_rubro               VARCHAR(150) NOT NULL,
    valor                      NUMERIC(18,2) NOT NULL DEFAULT 0,
    tasa_amortizacion_anual    NUMERIC(6,4)  NOT NULL DEFAULT 0.2000,
    creado_en                  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    creado_por_usuario_id      INTEGER,
    actualizado_en             TIMESTAMPTZ,
    actualizado_por_usuario_id INTEGER,
    esta_activo                BOOLEAN NOT NULL DEFAULT TRUE,
    eliminado_en               TIMESTAMPTZ,
    eliminado_por_usuario_id   INTEGER
);

CREATE INDEX IF NOT EXISTS ix_activo_diferido_carrera
    ON activo_diferido (carrera_id)
    WHERE esta_activo = TRUE;

-- Seed: placeholders ($0) — el administrador carga los valores reales
-- una vez obtenidos de los organismos municipales (KAN-31: ⚠️ datos faltantes)
INSERT INTO activo_diferido (carrera_id, nombre_rubro, valor, tasa_amortizacion_anual)
VALUES
    (1, 'Permiso Municipal',    0, 0.2000),
    (1, 'Permiso de Bomberos',  0, 0.2000)
ON CONFLICT DO NOTHING;

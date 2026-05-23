-- =============================================================================
-- KAN-31: Activos Diferidos — Amortización (RF-MI-04)
-- Hoja "Amortización": permisos legales al 20% anual.
-- Ejecutar en Supabase SQL Editor.
-- Idempotente: crea o repara columnas faltantes sin eliminar datos.
-- =============================================================================

BEGIN;

CREATE TABLE IF NOT EXISTS public.activo_diferido (
    id                         SERIAL PRIMARY KEY,
    carrera_id                 INTEGER,
    escenario_proyeccion_id    INTEGER,
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

-- Compatibilidad con versiones anteriores de la tabla.
ALTER TABLE public.activo_diferido ADD COLUMN IF NOT EXISTS carrera_id INTEGER;
ALTER TABLE public.activo_diferido ADD COLUMN IF NOT EXISTS escenario_proyeccion_id INTEGER;
ALTER TABLE public.activo_diferido ADD COLUMN IF NOT EXISTS nombre_rubro VARCHAR(150);
ALTER TABLE public.activo_diferido ADD COLUMN IF NOT EXISTS valor NUMERIC(18,2);
ALTER TABLE public.activo_diferido ADD COLUMN IF NOT EXISTS tasa_amortizacion_anual NUMERIC(6,4);
ALTER TABLE public.activo_diferido ADD COLUMN IF NOT EXISTS creado_en TIMESTAMPTZ;
ALTER TABLE public.activo_diferido ADD COLUMN IF NOT EXISTS creado_por_usuario_id INTEGER;
ALTER TABLE public.activo_diferido ADD COLUMN IF NOT EXISTS actualizado_en TIMESTAMPTZ;
ALTER TABLE public.activo_diferido ADD COLUMN IF NOT EXISTS actualizado_por_usuario_id INTEGER;
ALTER TABLE public.activo_diferido ADD COLUMN IF NOT EXISTS esta_activo BOOLEAN;
ALTER TABLE public.activo_diferido ADD COLUMN IF NOT EXISTS eliminado_en TIMESTAMPTZ;
ALTER TABLE public.activo_diferido ADD COLUMN IF NOT EXISTS eliminado_por_usuario_id INTEGER;

-- Reparar defaults en columnas que ya existían sin DEFAULT por versiones anteriores.
ALTER TABLE public.activo_diferido ALTER COLUMN valor SET DEFAULT 0;
ALTER TABLE public.activo_diferido ALTER COLUMN tasa_amortizacion_anual SET DEFAULT 0.2000;
ALTER TABLE public.activo_diferido ALTER COLUMN creado_en SET DEFAULT NOW();
ALTER TABLE public.activo_diferido ALTER COLUMN esta_activo SET DEFAULT TRUE;

-- Para KAN-31 el escenario es opcional: los activos diferidos aplican por carrera,
-- y pueden especializarse por escenario más adelante sin romper la carga actual.
ALTER TABLE public.activo_diferido ALTER COLUMN escenario_proyeccion_id DROP NOT NULL;
ALTER TABLE public.activo_diferido ALTER COLUMN escenario_proyeccion_id DROP DEFAULT;

-- Reparar datos existentes con NULL antes de aplicar NOT NULL.
UPDATE public.activo_diferido
SET valor = 0
WHERE valor IS NULL;

UPDATE public.activo_diferido
SET tasa_amortizacion_anual = 0.2000
WHERE tasa_amortizacion_anual IS NULL;

UPDATE public.activo_diferido
SET creado_en = NOW()
WHERE creado_en IS NULL;

UPDATE public.activo_diferido
SET esta_activo = TRUE
WHERE esta_activo IS NULL;

UPDATE public.activo_diferido
SET nombre_rubro = 'Activo diferido ' || id::TEXT
WHERE nombre_rubro IS NULL OR BTRIM(nombre_rubro) = '';

-- Si había registros antiguos sin carrera, asignarlos a la primera carrera existente.
UPDATE public.activo_diferido ad
SET carrera_id = c.id
FROM (
    SELECT id
    FROM public.carrera
    ORDER BY id
    LIMIT 1
) c
WHERE ad.carrera_id IS NULL;

ALTER TABLE public.activo_diferido ALTER COLUMN carrera_id SET NOT NULL;
ALTER TABLE public.activo_diferido ALTER COLUMN nombre_rubro SET NOT NULL;
ALTER TABLE public.activo_diferido ALTER COLUMN valor SET NOT NULL;
ALTER TABLE public.activo_diferido ALTER COLUMN tasa_amortizacion_anual SET NOT NULL;
ALTER TABLE public.activo_diferido ALTER COLUMN creado_en SET NOT NULL;
ALTER TABLE public.activo_diferido ALTER COLUMN esta_activo SET NOT NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_activo_diferido_carrera'
    ) THEN
        ALTER TABLE public.activo_diferido
        ADD CONSTRAINT fk_activo_diferido_carrera
        FOREIGN KEY (carrera_id)
        REFERENCES public.carrera(id)
        ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_activo_diferido_escenario'
    ) THEN
        ALTER TABLE public.activo_diferido
        ADD CONSTRAINT fk_activo_diferido_escenario
        FOREIGN KEY (escenario_proyeccion_id)
        REFERENCES public.escenario_proyeccion(id)
        ON DELETE SET NULL;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_activo_diferido_carrera
    ON public.activo_diferido (carrera_id)
    WHERE esta_activo = TRUE;

CREATE INDEX IF NOT EXISTS ix_activo_diferido_escenario
    ON public.activo_diferido (escenario_proyeccion_id)
    WHERE esta_activo = TRUE;

CREATE UNIQUE INDEX IF NOT EXISTS ux_activo_diferido_activo
    ON public.activo_diferido (carrera_id, COALESCE(escenario_proyeccion_id, 0), nombre_rubro)
    WHERE esta_activo = TRUE;

-- Seed por carrera. Quedan en 0 hasta que el administrador cargue valores reales.
-- escenario_proyeccion_id queda NULL para que sea configuración general de la carrera.
INSERT INTO public.activo_diferido
    (carrera_id, escenario_proyeccion_id, nombre_rubro, valor, tasa_amortizacion_anual, creado_en, esta_activo)
SELECT c.id, NULL, v.nombre_rubro, 0::NUMERIC, 0.2000::NUMERIC, NOW(), TRUE
FROM public.carrera c
CROSS JOIN (VALUES
    ('Permiso Municipal'),
    ('Permiso de Bomberos')
) AS v(nombre_rubro)
WHERE NOT EXISTS (
    SELECT 1
    FROM public.activo_diferido ad
    WHERE ad.carrera_id = c.id
      AND ad.escenario_proyeccion_id IS NULL
      AND ad.nombre_rubro = v.nombre_rubro
      AND ad.esta_activo = TRUE
);

COMMIT;

-- Verificación
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'activo_diferido'
ORDER BY ordinal_position;

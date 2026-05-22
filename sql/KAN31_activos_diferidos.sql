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
ALTER TABLE public.activo_diferido
ADD COLUMN IF NOT EXISTS carrera_id INTEGER;

ALTER TABLE public.activo_diferido
ADD COLUMN IF NOT EXISTS nombre_rubro VARCHAR(150);

ALTER TABLE public.activo_diferido
ADD COLUMN IF NOT EXISTS valor NUMERIC(18,2) NOT NULL DEFAULT 0;

ALTER TABLE public.activo_diferido
ADD COLUMN IF NOT EXISTS tasa_amortizacion_anual NUMERIC(6,4) NOT NULL DEFAULT 0.2000;

ALTER TABLE public.activo_diferido
ADD COLUMN IF NOT EXISTS creado_en TIMESTAMPTZ NOT NULL DEFAULT NOW();

ALTER TABLE public.activo_diferido
ADD COLUMN IF NOT EXISTS creado_por_usuario_id INTEGER;

ALTER TABLE public.activo_diferido
ADD COLUMN IF NOT EXISTS actualizado_en TIMESTAMPTZ;

ALTER TABLE public.activo_diferido
ADD COLUMN IF NOT EXISTS actualizado_por_usuario_id INTEGER;

ALTER TABLE public.activo_diferido
ADD COLUMN IF NOT EXISTS esta_activo BOOLEAN NOT NULL DEFAULT TRUE;

ALTER TABLE public.activo_diferido
ADD COLUMN IF NOT EXISTS eliminado_en TIMESTAMPTZ;

ALTER TABLE public.activo_diferido
ADD COLUMN IF NOT EXISTS eliminado_por_usuario_id INTEGER;

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

ALTER TABLE public.activo_diferido
ALTER COLUMN carrera_id SET NOT NULL;

ALTER TABLE public.activo_diferido
ALTER COLUMN nombre_rubro SET NOT NULL;

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
END $$;

CREATE INDEX IF NOT EXISTS ix_activo_diferido_carrera
    ON public.activo_diferido (carrera_id)
    WHERE esta_activo = TRUE;

CREATE UNIQUE INDEX IF NOT EXISTS ux_activo_diferido_activo
    ON public.activo_diferido (carrera_id, nombre_rubro)
    WHERE esta_activo = TRUE;

-- Seed por carrera. Quedan en 0 hasta que el administrador cargue valores reales.
INSERT INTO public.activo_diferido (carrera_id, nombre_rubro, valor, tasa_amortizacion_anual)
SELECT c.id, v.nombre_rubro, 0::NUMERIC, 0.2000::NUMERIC
FROM public.carrera c
CROSS JOIN (VALUES
    ('Permiso Municipal'),
    ('Permiso de Bomberos')
) AS v(nombre_rubro)
WHERE NOT EXISTS (
    SELECT 1
    FROM public.activo_diferido ad
    WHERE ad.carrera_id = c.id
      AND ad.nombre_rubro = v.nombre_rubro
      AND ad.esta_activo = TRUE
);

COMMIT;

-- Verificación
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'activo_diferido'
ORDER BY ordinal_position;

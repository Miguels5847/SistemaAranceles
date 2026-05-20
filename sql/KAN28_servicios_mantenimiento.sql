-- =============================================================================
-- KAN-28: Servicios + Mantenimiento (RF-MI-01)
-- Hoja "8 Mantenimiento": Servicios Básicos + Mantenimiento
-- Ejecutar en Supabase SQL Editor.
-- Idempotente. No elimina datos existentes.
-- =============================================================================

BEGIN;

CREATE TABLE IF NOT EXISTS public.mantenimiento_servicio (
    id                            SERIAL PRIMARY KEY,
    carrera_id                    INTEGER NOT NULL,
    escenario_proyeccion_id       INTEGER,
    sede                          VARCHAR(100) NOT NULL DEFAULT 'General',
    tipo_rubro                    VARCHAR(30) NOT NULL,
    nombre_rubro                  VARCHAR(150) NOT NULL,
    costo_anual_universidad       NUMERIC(18,2) NOT NULL DEFAULT 0,
    creado_en                     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    creado_por_usuario_id         INTEGER,
    actualizado_en                TIMESTAMPTZ,
    actualizado_por_usuario_id    INTEGER,
    esta_activo                   BOOLEAN NOT NULL DEFAULT TRUE,
    eliminado_en                  TIMESTAMPTZ,
    eliminado_por_usuario_id      INTEGER,

    CONSTRAINT fk_mantenimiento_servicio_carrera
        FOREIGN KEY (carrera_id)
        REFERENCES public.carrera(id)
        ON DELETE RESTRICT,

    CONSTRAINT fk_mantenimiento_servicio_escenario
        FOREIGN KEY (escenario_proyeccion_id)
        REFERENCES public.escenario_proyeccion(id)
        ON DELETE SET NULL,

    CONSTRAINT ck_mantenimiento_servicio_tipo
        CHECK (tipo_rubro IN ('ServicioBasico', 'Mantenimiento')),

    CONSTRAINT ck_mantenimiento_servicio_costo_no_negativo
        CHECK (costo_anual_universidad >= 0)
);

-- Compatibilidad si la tabla ya existía por una versión anterior del script.
ALTER TABLE public.mantenimiento_servicio
ADD COLUMN IF NOT EXISTS escenario_proyeccion_id INTEGER;

ALTER TABLE public.mantenimiento_servicio
ADD COLUMN IF NOT EXISTS sede VARCHAR(100) NOT NULL DEFAULT 'General';

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_mantenimiento_servicio_escenario'
    ) THEN
        ALTER TABLE public.mantenimiento_servicio
        ADD CONSTRAINT fk_mantenimiento_servicio_escenario
        FOREIGN KEY (escenario_proyeccion_id)
        REFERENCES public.escenario_proyeccion(id)
        ON DELETE SET NULL;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_mantenimiento_servicio_carrera_tipo
    ON public.mantenimiento_servicio (carrera_id, tipo_rubro)
    WHERE esta_activo = TRUE;

CREATE INDEX IF NOT EXISTS ix_mantenimiento_servicio_escenario
    ON public.mantenimiento_servicio (escenario_proyeccion_id)
    WHERE esta_activo = TRUE;

-- Evita duplicar semillas si se ejecuta varias veces.
CREATE UNIQUE INDEX IF NOT EXISTS ux_mantenimiento_servicio_activo
    ON public.mantenimiento_servicio (carrera_id, COALESCE(escenario_proyeccion_id, 0), tipo_rubro, nombre_rubro)
    WHERE esta_activo = TRUE;

-- Seed base por cada carrera existente. Se deja escenario_proyeccion_id NULL para que aplique
-- como configuración general de la carrera, sin amarrarla a un escenario específico.
INSERT INTO public.mantenimiento_servicio
    (carrera_id, escenario_proyeccion_id, sede, tipo_rubro, nombre_rubro, costo_anual_universidad)
SELECT c.id, NULL, 'General', v.tipo_rubro, v.nombre_rubro, v.costo_anual_universidad
FROM public.carrera c
CROSS JOIN (VALUES
    ('ServicioBasico', 'Agua',                300000::NUMERIC),
    ('ServicioBasico', 'Internet',            150000::NUMERIC),
    ('ServicioBasico', 'Energía Eléctrica',   500000::NUMERIC),
    ('ServicioBasico', 'Comunicaciones',      108000::NUMERIC),
    ('Mantenimiento',  'Limpieza',            480000::NUMERIC),
    ('Mantenimiento',  'Seguros',             720000::NUMERIC),
    ('Mantenimiento',  'Seguridad',           960000::NUMERIC),
    ('Mantenimiento',  'Refacciones',         399400::NUMERIC),
    ('Mantenimiento',  'Infraestructura',     400000::NUMERIC)
) AS v(tipo_rubro, nombre_rubro, costo_anual_universidad)
WHERE NOT EXISTS (
    SELECT 1
    FROM public.mantenimiento_servicio ms
    WHERE ms.carrera_id = c.id
      AND ms.escenario_proyeccion_id IS NULL
      AND ms.tipo_rubro = v.tipo_rubro
      AND ms.nombre_rubro = v.nombre_rubro
      AND ms.esta_activo = TRUE
);

COMMIT;

-- Verificación
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'mantenimiento_servicio'
ORDER BY ordinal_position;

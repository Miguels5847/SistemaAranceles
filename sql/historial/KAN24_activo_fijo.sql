-- =============================================================================
-- KAN-24: CRUD Activos Fijos (bloque 1 hoja Excel "3 Recursos fisicos")
-- Ejecutar en Supabase SQL Editor. Idempotente. No borra datos existentes.
--
-- NOTA: la tabla "activo_fijo" YA existia desde KAN-03 (otro modulo).
-- KAN-24 usa su propia tabla "recurso_activo_fijo" para evitar colision.
-- =============================================================================

BEGIN;

-- 0. Limpieza: quitar columnas que un intento previo agrego por error a la
--    tabla legacy "activo_fijo" (vacias, sin uso). Restaura su esquema KAN-03.
ALTER TABLE public.activo_fijo DROP COLUMN IF EXISTS carrera_id;
ALTER TABLE public.activo_fijo DROP COLUMN IF EXISTS categoria;
ALTER TABLE public.activo_fijo DROP COLUMN IF EXISTS unidad_medida;
ALTER TABLE public.activo_fijo DROP COLUMN IF EXISTS vida_util_anios;
ALTER TABLE public.activo_fijo DROP COLUMN IF EXISTS porcentaje_residual;
ALTER TABLE public.activo_fijo DROP COLUMN IF EXISTS fecha_adquisicion;

-- 1. Tabla propia de KAN-24
CREATE TABLE IF NOT EXISTS public.recurso_activo_fijo (
    id                          SERIAL PRIMARY KEY,
    carrera_id                  INTEGER NOT NULL REFERENCES public.carrera(id) ON DELETE RESTRICT,
    descripcion                 VARCHAR(200) NOT NULL,
    categoria                   VARCHAR(40) NOT NULL,
    cantidad                    NUMERIC(18,4) NOT NULL,
    unidad_medida               VARCHAR(20) NOT NULL DEFAULT 'UNI',
    valor_unitario              NUMERIC(18,2) NOT NULL,
    vida_util_anios             INTEGER NOT NULL,
    porcentaje_residual         NUMERIC(6,4) NOT NULL DEFAULT 0.05,
    fecha_adquisicion           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    creado_en                   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    creado_por_usuario_id       INTEGER,
    actualizado_en              TIMESTAMPTZ,
    actualizado_por_usuario_id  INTEGER,
    esta_activo                 BOOLEAN NOT NULL DEFAULT TRUE,
    eliminado_en                TIMESTAMPTZ,
    eliminado_por_usuario_id    INTEGER
);

CREATE INDEX IF NOT EXISTS "IX_recurso_activo_fijo_carrera_id_categoria"
    ON public.recurso_activo_fijo (carrera_id, categoria);

-- 2. Marcar la migracion EF como aplicada
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260516000000_KAN24_ActivoFijo', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;

-- Verificacion
SELECT to_regclass('public.recurso_activo_fijo') AS tabla_creada;
SELECT column_name FROM information_schema.columns
 WHERE table_name = 'recurso_activo_fijo' ORDER BY ordinal_position;

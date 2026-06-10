-- =============================================================================
-- KAN-24 (extension): TipoCalculoCantidad en recurso_activo_fijo +
-- catalogo_activo_base global + siembra por carrera.
-- Ejecutar en Supabase SQL Editor. Idempotente. No borra datos.
-- Ejecutar despues de KAN24_activo_fijo.sql.
-- =============================================================================

BEGIN;

-- 1. Columnas nuevas en recurso_activo_fijo
ALTER TABLE public.recurso_activo_fijo
    ADD COLUMN IF NOT EXISTS tipo_calculo_cantidad VARCHAR(30) NOT NULL DEFAULT 'Manual';
ALTER TABLE public.recurso_activo_fijo
    ADD COLUMN IF NOT EXISTS factor_multiplicador NUMERIC(18,4) NOT NULL DEFAULT 1;
ALTER TABLE public.recurso_activo_fijo
    ADD COLUMN IF NOT EXISTS offset_cantidad NUMERIC(18,4) NOT NULL DEFAULT 0;

-- 2. Catalogo institucional global (sin carrera_id)
CREATE TABLE IF NOT EXISTS public.catalogo_activo_base (
    id                          SERIAL PRIMARY KEY,
    descripcion                 VARCHAR(200) NOT NULL,
    categoria                   VARCHAR(40) NOT NULL,
    tipo_calculo_cantidad       VARCHAR(30) NOT NULL DEFAULT 'Manual',
    cantidad_default            NUMERIC(18,4) NOT NULL DEFAULT 0,
    unidad_medida               VARCHAR(20) NOT NULL DEFAULT 'UNI',
    valor_unitario              NUMERIC(18,2) NOT NULL DEFAULT 0,
    factor_multiplicador        NUMERIC(18,4) NOT NULL DEFAULT 1,
    offset_cantidad             NUMERIC(18,4) NOT NULL DEFAULT 0,
    vida_util_anios             INTEGER NOT NULL,
    porcentaje_residual         NUMERIC(6,4) NOT NULL DEFAULT 0.05,
    creado_en                   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    creado_por_usuario_id       INTEGER,
    actualizado_en              TIMESTAMPTZ,
    actualizado_por_usuario_id  INTEGER,
    esta_activo                 BOOLEAN NOT NULL DEFAULT TRUE,
    eliminado_en                TIMESTAMPTZ,
    eliminado_por_usuario_id    INTEGER
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_catalogo_activo_base_descripcion"
    ON public.catalogo_activo_base (descripcion);

-- 3. Plantillas por defecto (Operatividad EVEA, Licencias Zoom)
INSERT INTO public.catalogo_activo_base
    (descripcion, categoria, tipo_calculo_cantidad, cantidad_default, unidad_medida,
     valor_unitario, factor_multiplicador, offset_cantidad, vida_util_anios, porcentaje_residual)
VALUES
    ('Operatividad EVEA', 'LaboratoriosEquipos', 'PorEstudiante', 0, 'UNI', 7.50, 1, 0, 10, 0.05),
    ('Licencias Zoom',    'LaboratoriosEquipos', 'PorDocente',    0, 'UNI', 13.50, 1, 10, 10, 0.05)
ON CONFLICT (descripcion) DO NOTHING;

-- 4. Marcar migracion EF como aplicada
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260517000000_KAN24_CatalogoActivoBase', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;

-- Verificacion
SELECT to_regclass('public.catalogo_activo_base') AS catalogo_creado;
SELECT descripcion, tipo_calculo_cantidad, valor_unitario, offset_cantidad
  FROM public.catalogo_activo_base ORDER BY descripcion;
SELECT column_name FROM information_schema.columns
 WHERE table_name = 'recurso_activo_fijo'
   AND column_name IN ('tipo_calculo_cantidad','factor_multiplicador','offset_cantidad')
 ORDER BY column_name;

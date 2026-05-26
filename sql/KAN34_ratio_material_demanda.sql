-- =====================================================================
-- KAN-34: Ratios de consumo de materiales por demanda (Épica 9)
-- =====================================================================
-- Tabla nueva: ratio_material_demanda.
-- Define cuánto consume un estudiante de cada material/insumo, por
-- carrera (o global si carrera_id es NULL).
--
-- Los precios unitarios NO se duplican aquí: vienen de
-- item_material_insumo (Capital de Trabajo, KAN-29) cuando se vincula
-- via item_material_insumo_id. Si el ratio no está vinculado a un item,
-- se considera ratio puro y el precio se gestionará por separado.
--
-- Cálculo posterior (en CalcularMaterialesPorPeriodoQuery):
--   cantidad_periodo  = estudiantes_periodo × ratio_consumo × (meses_operativos si unidad='por_estudiante_mes', sino 1)
--   precio_unitario   = item_material_insumo.precio_unitario (si vinculado) o 0
--   factor_inflacion  = factor acumulado desde anio_base_proyeccion al año del periodo
--   costo_monetario   = cantidad_periodo × precio_unitario × factor_inflacion (si aplica_inflacion=true)
-- =====================================================================

BEGIN;

CREATE TABLE IF NOT EXISTS public.ratio_material_demanda (
    id                         SERIAL PRIMARY KEY,
    carrera_id                 INTEGER NULL REFERENCES public.carrera(id) ON DELETE CASCADE,
    categoria                  VARCHAR(60)  NOT NULL,
    concepto                   VARCHAR(140) NOT NULL,
    item_material_insumo_id    INTEGER NULL REFERENCES public.item_material_insumo(id) ON DELETE SET NULL,
    ratio_consumo              NUMERIC(12,6) NOT NULL DEFAULT 0,
    unidad_ratio               VARCHAR(40)  NOT NULL DEFAULT 'por_estudiante',
    meses_operativos           INTEGER      NOT NULL DEFAULT 6,
    aplica_inflacion           BOOLEAN      NOT NULL DEFAULT TRUE,

    creado_en                  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    creado_por_usuario_id      INTEGER NULL REFERENCES public.usuario(id) ON DELETE SET NULL,
    actualizado_en             TIMESTAMPTZ NULL,
    actualizado_por_usuario_id INTEGER NULL REFERENCES public.usuario(id) ON DELETE SET NULL,
    esta_activo                BOOLEAN      NOT NULL DEFAULT TRUE,
    eliminado_en               TIMESTAMPTZ NULL,
    eliminado_por_usuario_id   INTEGER NULL REFERENCES public.usuario(id) ON DELETE SET NULL
);

-- Defensa idempotente
ALTER TABLE public.ratio_material_demanda
    ADD COLUMN IF NOT EXISTS unidad_ratio     VARCHAR(40)  NOT NULL DEFAULT 'por_estudiante';
ALTER TABLE public.ratio_material_demanda
    ADD COLUMN IF NOT EXISTS meses_operativos INTEGER      NOT NULL DEFAULT 6;
ALTER TABLE public.ratio_material_demanda
    ADD COLUMN IF NOT EXISTS aplica_inflacion BOOLEAN      NOT NULL DEFAULT TRUE;

-- Constraints
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_ratio_material_demanda_ratio_consumo') THEN
        ALTER TABLE public.ratio_material_demanda
            ADD CONSTRAINT "CK_ratio_material_demanda_ratio_consumo"
            CHECK (ratio_consumo >= 0);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_ratio_material_demanda_meses') THEN
        ALTER TABLE public.ratio_material_demanda
            ADD CONSTRAINT "CK_ratio_material_demanda_meses"
            CHECK (meses_operativos > 0 AND meses_operativos <= 12);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_ratio_material_demanda_unidad') THEN
        ALTER TABLE public.ratio_material_demanda
            ADD CONSTRAINT "CK_ratio_material_demanda_unidad"
            CHECK (unidad_ratio IN ('por_estudiante', 'por_estudiante_mes'));
    END IF;
END $$;

-- Índices
CREATE UNIQUE INDEX IF NOT EXISTS "IX_ratio_material_demanda_carrera_categoria_concepto"
    ON public.ratio_material_demanda (COALESCE(carrera_id, 0), categoria, concepto)
    WHERE esta_activo = TRUE;

CREATE INDEX IF NOT EXISTS "IX_ratio_material_demanda_carrera"
    ON public.ratio_material_demanda (carrera_id);

CREATE INDEX IF NOT EXISTS "IX_ratio_material_demanda_item"
    ON public.ratio_material_demanda (item_material_insumo_id);

COMMIT;

-- =====================================================================
-- VERIFICACIÓN
-- =====================================================================
-- SELECT column_name, data_type, column_default
-- FROM information_schema.columns
-- WHERE table_schema='public' AND table_name='ratio_material_demanda'
-- ORDER BY ordinal_position;
--
-- SELECT conname FROM pg_constraint
-- WHERE conrelid = 'public.ratio_material_demanda'::regclass;

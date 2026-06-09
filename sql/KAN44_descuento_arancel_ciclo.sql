-- =====================================================================
-- KAN-44: Descuentos de arancel por ciclo (Demanda e Ingresos / Análisis Financiero)
-- =====================================================================
-- Tabla nueva: descuento_arancel_ciclo.
-- El arancel base se mantiene; el descuento solo afecta el valor cobrado
-- por ciclo para calcular ingresos (y el arancel óptimo financiero).
-- Configurable por carrera y opcionalmente por escenario (NULL = global de carrera).
-- Idempotente. Los solapamientos de rangos se validan en la aplicación.
-- =====================================================================

BEGIN;

CREATE TABLE IF NOT EXISTS public.descuento_arancel_ciclo (
    id                          SERIAL PRIMARY KEY,
    carrera_id                  INTEGER NOT NULL REFERENCES public.carrera(id) ON DELETE CASCADE,
    escenario_proyeccion_id     INTEGER NULL REFERENCES public.escenario_proyeccion(id) ON DELETE SET NULL,
    ciclo_desde                 INTEGER NOT NULL,
    ciclo_hasta                 INTEGER NOT NULL,
    porcentaje_descuento        NUMERIC(6,2) NOT NULL DEFAULT 0,
    creado_en                   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    creado_por_usuario_id       INTEGER NULL REFERENCES public.usuario(id) ON DELETE SET NULL,
    actualizado_en              TIMESTAMPTZ NULL,
    actualizado_por_usuario_id  INTEGER NULL REFERENCES public.usuario(id) ON DELETE SET NULL,
    esta_activo                 BOOLEAN NOT NULL DEFAULT TRUE,
    eliminado_en                TIMESTAMPTZ NULL,
    eliminado_por_usuario_id    INTEGER NULL REFERENCES public.usuario(id) ON DELETE SET NULL
);

-- Defensa idempotente por si la tabla existe con esquema parcial
ALTER TABLE public.descuento_arancel_ciclo
    ADD COLUMN IF NOT EXISTS ciclo_desde INTEGER NOT NULL DEFAULT 1;
ALTER TABLE public.descuento_arancel_ciclo
    ADD COLUMN IF NOT EXISTS ciclo_hasta INTEGER NOT NULL DEFAULT 1;
ALTER TABLE public.descuento_arancel_ciclo
    ADD COLUMN IF NOT EXISTS porcentaje_descuento NUMERIC(6,2) NOT NULL DEFAULT 0;

-- Constraints
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'CK_descuento_arancel_ciclo_rango'
    ) THEN
        ALTER TABLE public.descuento_arancel_ciclo
            ADD CONSTRAINT "CK_descuento_arancel_ciclo_rango"
            CHECK (ciclo_desde > 0 AND ciclo_hasta >= ciclo_desde);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'CK_descuento_arancel_ciclo_porc'
    ) THEN
        ALTER TABLE public.descuento_arancel_ciclo
            ADD CONSTRAINT "CK_descuento_arancel_ciclo_porc"
            CHECK (porcentaje_descuento >= 0 AND porcentaje_descuento <= 100);
    END IF;
END $$;

-- Índices
CREATE INDEX IF NOT EXISTS "IX_descuento_arancel_ciclo_carrera_escenario"
    ON public.descuento_arancel_ciclo (carrera_id, escenario_proyeccion_id);

CREATE INDEX IF NOT EXISTS "IX_descuento_arancel_ciclo_carrera_escenario_activo"
    ON public.descuento_arancel_ciclo (carrera_id, escenario_proyeccion_id, esta_activo);

COMMIT;

-- =====================================================================
-- VERIFICACIÓN
-- =====================================================================
-- SELECT * FROM public.descuento_arancel_ciclo ORDER BY carrera_id, ciclo_desde;

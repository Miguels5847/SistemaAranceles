-- =====================================================================
-- KAN-35: Parámetros institucionales para Demanda e Ingresos (Épica 9)
-- =====================================================================
-- Amplía datos_institucionales con parámetros globales que alimentan
-- a KAN-32 (% matrícula default), KAN-33 (% becas, semestres/año) y
-- KAN-34 (presupuestos, seguro, año base inflación).
--
-- IMPORTANTE: el arancel NO se agrega aquí (se queda por carrera en
-- configuracion_arancel_carrera, KAN-32). Esta tabla solo guarda
-- parámetros globales institucionales.
-- =====================================================================

BEGIN;

ALTER TABLE public.datos_institucionales
    ADD COLUMN IF NOT EXISTS porcentaje_matricula_default        NUMERIC(7,4)  NOT NULL DEFAULT 10.0000,
    ADD COLUMN IF NOT EXISTS porcentaje_becas_institucionales    NUMERIC(7,4)  NOT NULL DEFAULT 10.0000,
    ADD COLUMN IF NOT EXISTS semestres_por_anio                  INTEGER       NOT NULL DEFAULT 2,
    ADD COLUMN IF NOT EXISTS meses_operativos_ciclo              INTEGER       NOT NULL DEFAULT 6,
    ADD COLUMN IF NOT EXISTS presupuesto_anual_capacitacion      NUMERIC(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS presupuesto_anual_internacionalizacion NUMERIC(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS presupuesto_anual_marketing         NUMERIC(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS poliza_seguro_estudiantil_anual     NUMERIC(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS fuente_inflacion                    VARCHAR(80)   NULL,
    ADD COLUMN IF NOT EXISTS anio_base_proyeccion                INTEGER       NULL;

-- Constraints defensivos
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_di_porcentaje_matricula_default') THEN
        ALTER TABLE public.datos_institucionales
            ADD CONSTRAINT "CK_di_porcentaje_matricula_default"
            CHECK (porcentaje_matricula_default >= 0 AND porcentaje_matricula_default <= 100);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_di_porcentaje_becas') THEN
        ALTER TABLE public.datos_institucionales
            ADD CONSTRAINT "CK_di_porcentaje_becas"
            CHECK (porcentaje_becas_institucionales >= 0 AND porcentaje_becas_institucionales <= 100);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_di_semestres_por_anio') THEN
        ALTER TABLE public.datos_institucionales
            ADD CONSTRAINT "CK_di_semestres_por_anio"
            CHECK (semestres_por_anio > 0 AND semestres_por_anio <= 4);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_di_meses_operativos_ciclo') THEN
        ALTER TABLE public.datos_institucionales
            ADD CONSTRAINT "CK_di_meses_operativos_ciclo"
            CHECK (meses_operativos_ciclo > 0 AND meses_operativos_ciclo <= 12);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_di_presupuesto_capacitacion') THEN
        ALTER TABLE public.datos_institucionales
            ADD CONSTRAINT "CK_di_presupuesto_capacitacion"
            CHECK (presupuesto_anual_capacitacion >= 0);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_di_presupuesto_internacionalizacion') THEN
        ALTER TABLE public.datos_institucionales
            ADD CONSTRAINT "CK_di_presupuesto_internacionalizacion"
            CHECK (presupuesto_anual_internacionalizacion >= 0);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_di_presupuesto_marketing') THEN
        ALTER TABLE public.datos_institucionales
            ADD CONSTRAINT "CK_di_presupuesto_marketing"
            CHECK (presupuesto_anual_marketing >= 0);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_di_poliza_seguro') THEN
        ALTER TABLE public.datos_institucionales
            ADD CONSTRAINT "CK_di_poliza_seguro"
            CHECK (poliza_seguro_estudiantil_anual >= 0);
    END IF;
END $$;

COMMIT;

-- =====================================================================
-- VERIFICACIÓN
-- =====================================================================
-- SELECT column_name, data_type, column_default
-- FROM information_schema.columns
-- WHERE table_schema='public' AND table_name='datos_institucionales'
--   AND column_name IN (
--     'porcentaje_matricula_default','porcentaje_becas_institucionales',
--     'semestres_por_anio','meses_operativos_ciclo',
--     'presupuesto_anual_capacitacion','presupuesto_anual_internacionalizacion',
--     'presupuesto_anual_marketing','poliza_seguro_estudiantil_anual',
--     'fuente_inflacion','anio_base_proyeccion'
--   )
-- ORDER BY column_name;

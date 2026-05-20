-- =============================================================================
-- KAN-25: Inversiones Futuras / Proyeccion de necesidades
-- Tabla: inversion_futura
-- Ejecutar en Supabase SQL Editor despues de los scripts KAN-24.
-- Idempotente. No borra datos.
-- =============================================================================

BEGIN;

CREATE TABLE IF NOT EXISTS public.inversion_futura (
    id                          SERIAL PRIMARY KEY,
    activo_fijo_id              INTEGER NOT NULL,
    anio                        INTEGER NOT NULL,
    semestre                    INTEGER NOT NULL,
    cantidad_proyectada         NUMERIC(18,4) NOT NULL DEFAULT 0,
    creado_en                   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    creado_por_usuario_id       INTEGER,
    actualizado_en              TIMESTAMPTZ,
    actualizado_por_usuario_id  INTEGER,
    esta_activo                 BOOLEAN NOT NULL DEFAULT TRUE,
    eliminado_en                TIMESTAMPTZ,
    eliminado_por_usuario_id    INTEGER,

    CONSTRAINT fk_inversion_futura_activo_fijo
        FOREIGN KEY (activo_fijo_id)
        REFERENCES public.recurso_activo_fijo(id)
        ON DELETE RESTRICT,

    CONSTRAINT ck_inversion_futura_semestre
        CHECK (semestre IN (1, 2)),

    CONSTRAINT ck_inversion_futura_anio
        CHECK (anio BETWEEN 2000 AND 2200),

    CONSTRAINT ck_inversion_futura_cantidad_no_negativa
        CHECK (cantidad_proyectada >= 0)
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_inversion_futura_activo_periodo
    ON public.inversion_futura (activo_fijo_id, anio, semestre)
    WHERE esta_activo = TRUE;

CREATE INDEX IF NOT EXISTS ix_inversion_futura_activo_fijo
    ON public.inversion_futura (activo_fijo_id);

-- Marcar migracion logica como aplicada para trazabilidad manual.
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260518000000_KAN25_InversionFutura', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;

-- Verificacion
SELECT to_regclass('public.inversion_futura') AS inversion_futura_creada;
SELECT column_name
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'inversion_futura'
ORDER BY ordinal_position;

-- ============================================================================
-- Migración: KAN_OverrideHorasPeriodo
-- CU-ES-04: persiste override de horas (docencia/práctica) por período por proyección.
-- Ejecutar en Supabase SQL Editor (idempotente).
-- ============================================================================

BEGIN;

CREATE TABLE IF NOT EXISTS public.override_horas_periodo (
    id                          SERIAL PRIMARY KEY,
    proyeccion_id               INTEGER NOT NULL REFERENCES public.proyeccion_estudiantes(id) ON DELETE CASCADE,
    periodo                     SMALLINT NOT NULL CHECK (periodo BETWEEN 1 AND 20),
    horas_docencia              NUMERIC(10,2),
    horas_practica              NUMERIC(10,2),
    creado_en                   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    creado_por_usuario_id       INTEGER,
    actualizado_en              TIMESTAMPTZ,
    actualizado_por_usuario_id  INTEGER,
    esta_activo                 BOOLEAN NOT NULL DEFAULT TRUE,
    eliminado_en                TIMESTAMPTZ,
    eliminado_por_usuario_id    INTEGER
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_override_horas_periodo_proyeccion_id_periodo"
    ON public.override_horas_periodo (proyeccion_id, periodo);

INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260510205433_KAN_OverrideHorasPeriodo', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;

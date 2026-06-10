-- =============================================================
-- KAN14 Fase 5 - Lote 02 (RLS) - PRECHECK
-- Objetivo:
--   1) Verificar tablas criticas para RLS.
--   2) Levantar estado actual de RLS y politicas.
--   3) Detectar roles candidatos de conexion para aplicar politicas.
-- =============================================================

SET lock_timeout = '5s';
SET statement_timeout = '120s';

SELECT current_database() AS db, current_user AS usuario_actual, now() AS ejecutado_en;

WITH tablas(tabla) AS (
    VALUES
        ('usuario'),
        ('rol'),
        ('permiso'),
        ('rol_permiso'),
        ('usuario_permiso_override'),
        ('sesion_usuario'),
        ('auditoria_log')
)
SELECT
    t.tabla,
    CASE WHEN c.oid IS NULL THEN false ELSE true END AS existe,
    c.relrowsecurity AS rls_habilitado,
    c.relforcerowsecurity AS rls_forzado
FROM tablas t
LEFT JOIN pg_class c ON c.relname = t.tabla
LEFT JOIN pg_namespace n ON n.oid = c.relnamespace AND n.nspname = 'public'
ORDER BY t.tabla;

-- Politicas actuales por tabla
SELECT
    schemaname,
    tablename,
    policyname,
    permissive,
    roles,
    cmd
FROM pg_policies
WHERE schemaname = 'public'
  AND tablename IN ('usuario','rol','permiso','rol_permiso','usuario_permiso_override','sesion_usuario','auditoria_log')
ORDER BY tablename, policyname;

-- Roles candidatos (los existentes del set base + current_user)
WITH candidatos(nombre) AS (
    VALUES ('authenticated'), ('service_role'), ('postgres'), (current_user)
)
SELECT DISTINCT c.nombre AS rol_candidato, r.oid IS NOT NULL AS existe_en_bd
FROM candidatos c
LEFT JOIN pg_roles r ON r.rolname = c.nombre
ORDER BY c.nombre;

-- Gate precheck (debe retornar 0 para continuar)
WITH tablas(tabla) AS (
    VALUES
        ('usuario'),
        ('rol'),
        ('permiso'),
        ('rol_permiso'),
        ('usuario_permiso_override'),
        ('sesion_usuario'),
        ('auditoria_log')
)
SELECT COUNT(*) AS tablas_faltantes
FROM tablas t
LEFT JOIN pg_class c ON c.relname = t.tabla
LEFT JOIN pg_namespace n ON n.oid = c.relnamespace AND n.nspname = 'public'
WHERE c.oid IS NULL;

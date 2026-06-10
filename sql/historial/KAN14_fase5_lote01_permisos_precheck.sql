-- =============================================================
-- KAN14 Fase 5 - Lote 01 (Permisos) - PRECHECK
-- Objetivo:
--   1) Verificar existencia/estado de permisos CFG.*, REP.* y CA.ELIMINAR.
--   2) Verificar asignacion de esos permisos al rol Administrador.
--   3) Levantar estado base de RLS en tablas criticas (solo lectura).
-- =============================================================

SET lock_timeout = '5s';
SET statement_timeout = '120s';

-- Contexto de ejecucion
SELECT current_database() AS db, current_user AS usuario_actual, now() AS ejecutado_en;

-- Permisos objetivo
WITH objetivos(codigo, modulo_nombre, accion_nombre) AS (
    VALUES
        ('CFG.VER', 'Configuracion', 'VER'),
        ('CFG.EDITAR', 'Configuracion', 'EDITAR'),
        ('REP.VER', 'Reportes', 'VER'),
        ('REP.EXPORTAR', 'Reportes', 'EXPORTAR'),
        ('CA.ELIMINAR', 'Carreras', 'ELIMINAR')
)
SELECT
    o.codigo,
    o.modulo_nombre AS modulo_esperado,
    o.accion_nombre AS accion_esperada,
    p.id AS permiso_id,
    p.modulo_nombre AS modulo_actual,
    p.accion_nombre AS accion_actual,
    p.esta_activo,
    CASE WHEN p.id IS NULL THEN 'FALTANTE' ELSE 'OK' END AS estado
FROM objetivos o
LEFT JOIN public.permiso p ON p.codigo = o.codigo
ORDER BY o.codigo;

-- Asignacion al rol Administrador
WITH admin AS (
    SELECT id
    FROM public.rol
    WHERE nombre = 'Administrador'
    LIMIT 1
), objetivos(codigo) AS (
    VALUES ('CFG.VER'), ('CFG.EDITAR'), ('REP.VER'), ('REP.EXPORTAR'), ('CA.ELIMINAR')
)
SELECT
    o.codigo,
    p.id AS permiso_id,
    CASE WHEN rp.rol_id IS NULL THEN false ELSE true END AS asignado_a_admin
FROM objetivos o
LEFT JOIN public.permiso p ON p.codigo = o.codigo
LEFT JOIN admin a ON true
LEFT JOIN public.rol_permiso rp ON rp.rol_id = a.id AND rp.permiso_id = p.id
ORDER BY o.codigo;

-- Gate del lote 01 permisos
WITH objetivos(codigo) AS (
    VALUES ('CFG.VER'), ('CFG.EDITAR'), ('REP.VER'), ('REP.EXPORTAR'), ('CA.ELIMINAR')
), admin AS (
    SELECT id
    FROM public.rol
    WHERE nombre = 'Administrador'
    LIMIT 1
)
SELECT
    SUM(CASE WHEN p.id IS NULL THEN 1 ELSE 0 END) AS permisos_faltantes,
    SUM(CASE WHEN p.id IS NOT NULL AND lower(coalesce(p.esta_activo::text, 'false')) NOT IN ('1','t','true') THEN 1 ELSE 0 END) AS permisos_inactivos,
    SUM(CASE WHEN p.id IS NOT NULL AND rp.rol_id IS NULL THEN 1 ELSE 0 END) AS permisos_no_asignados_a_admin
FROM objetivos o
LEFT JOIN public.permiso p ON p.codigo = o.codigo
LEFT JOIN admin a ON true
LEFT JOIN public.rol_permiso rp ON rp.rol_id = a.id AND rp.permiso_id = p.id;

-- Estado RLS actual (diagnostico, no bloqueante)
WITH tablas_criticas(tabla) AS (
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
    c.relrowsecurity AS rls_habilitado,
    c.relforcerowsecurity AS rls_forzado
FROM tablas_criticas t
JOIN pg_class c ON c.relname = t.tabla
JOIN pg_namespace n ON n.oid = c.relnamespace AND n.nspname = 'public'
ORDER BY t.tabla;

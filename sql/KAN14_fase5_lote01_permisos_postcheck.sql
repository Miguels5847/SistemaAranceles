-- =============================================================
-- KAN14 Fase 5 - Lote 01 (Permisos) - POSTCHECK
-- Objetivo: confirmar existencia y asignacion efectiva de permisos.
-- =============================================================

SET lock_timeout = '5s';
SET statement_timeout = '120s';

-- Estado de permisos objetivo
WITH objetivos(codigo) AS (
    VALUES ('CFG.VER'), ('CFG.EDITAR'), ('REP.VER'), ('REP.EXPORTAR'), ('CA.ELIMINAR')
)
SELECT
    o.codigo,
    p.id AS permiso_id,
    p.modulo_nombre,
    p.accion_nombre,
    p.esta_activo
FROM objetivos o
LEFT JOIN public.permiso p ON p.codigo = o.codigo
ORDER BY o.codigo;

-- Asignacion al Administrador
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
    CASE WHEN rp.rol_id IS NULL THEN false ELSE true END AS asignado_a_admin
FROM objetivos o
LEFT JOIN public.permiso p ON p.codigo = o.codigo
LEFT JOIN admin a ON true
LEFT JOIN public.rol_permiso rp ON rp.rol_id = a.id AND rp.permiso_id = p.id
ORDER BY o.codigo;

-- Gate postcheck: debe retornar 0 para cerrar lote 01
WITH objetivos(codigo) AS (
    VALUES ('CFG.VER'), ('CFG.EDITAR'), ('REP.VER'), ('REP.EXPORTAR'), ('CA.ELIMINAR')
), admin AS (
    SELECT id
    FROM public.rol
    WHERE nombre = 'Administrador'
    LIMIT 1
)
SELECT
    SUM(CASE WHEN p.id IS NULL THEN 1 ELSE 0 END)
  + SUM(CASE WHEN p.id IS NOT NULL AND lower(coalesce(p.esta_activo::text, 'false')) NOT IN ('1','t','true') THEN 1 ELSE 0 END)
  + SUM(CASE WHEN p.id IS NOT NULL AND rp.rol_id IS NULL THEN 1 ELSE 0 END)
    AS total_errores_lote01_permisos
FROM objetivos o
LEFT JOIN public.permiso p ON p.codigo = o.codigo
LEFT JOIN admin a ON true
LEFT JOIN public.rol_permiso rp ON rp.rol_id = a.id AND rp.permiso_id = p.id;

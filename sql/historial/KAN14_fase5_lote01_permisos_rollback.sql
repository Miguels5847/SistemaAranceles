-- =============================================================
-- KAN14 Fase 5 - Lote 01 (Permisos) - ROLLBACK
-- Objetivo: deshacer de forma conservadora los cambios del lote 01.
-- NOTA: no elimina CFG.VER/REP.VER para evitar romper menus ya activos.
--       Revierte solo capacidades nuevas de edicion/exportacion/eliminacion.
-- =============================================================

BEGIN;

SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '300s';

-- 1) Retirar asignaciones al Administrador de permisos de capacidad alta
WITH admin AS (
    SELECT id
    FROM public.rol
    WHERE nombre = 'Administrador'
    LIMIT 1
), objetivos AS (
    SELECT id
    FROM public.permiso
    WHERE codigo IN ('CFG.EDITAR', 'REP.EXPORTAR', 'CA.ELIMINAR')
)
DELETE FROM public.rol_permiso rp
USING admin a, objetivos o
WHERE rp.rol_id = a.id
  AND rp.permiso_id = o.id;

-- 2) Desactivar CA.ELIMINAR (sin borrar fisicamente)
UPDATE public.permiso
SET esta_activo = FALSE,
    actualizado_en = NOW()
WHERE codigo = 'CA.ELIMINAR';

COMMIT;

-- =============================================================
-- KAN14 Fase 5 - Lote 01 (Permisos) - APPLY
-- Objetivo:
--   1) Completar permisos faltantes de Administrador (CFG.*, REP.*).
--   2) Crear CA.ELIMINAR y asignar minimo al Administrador.
-- Nota: No aplica RLS en este lote para evitar bloqueo funcional.
-- =============================================================

BEGIN;

SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '300s';

-- 1) Upsert de permisos objetivo
INSERT INTO public.permiso (codigo, modulo_nombre, accion_nombre, descripcion, creado_en, esta_activo)
VALUES
  ('CFG.VER', 'Configuracion', 'VER', 'Ver configuraciones del sistema', NOW(), TRUE),
  ('CFG.EDITAR', 'Configuracion', 'EDITAR', 'Editar configuraciones del sistema', NOW(), TRUE),
  ('REP.VER', 'Reportes', 'VER', 'Ver reportes', NOW(), TRUE),
  ('REP.EXPORTAR', 'Reportes', 'EXPORTAR', 'Exportar reportes', NOW(), TRUE),
  ('CA.ELIMINAR', 'Carreras', 'ELIMINAR', 'Eliminar carreras', NOW(), TRUE)
ON CONFLICT (codigo) DO UPDATE
SET
  modulo_nombre = EXCLUDED.modulo_nombre,
  accion_nombre = EXCLUDED.accion_nombre,
  descripcion = EXCLUDED.descripcion,
  esta_activo = TRUE,
  actualizado_en = NOW();

-- 2) Asignar permisos objetivo al rol Administrador
WITH admin AS (
    SELECT id
    FROM public.rol
    WHERE nombre = 'Administrador'
    LIMIT 1
), objetivos AS (
    SELECT id
    FROM public.permiso
    WHERE codigo IN ('CFG.VER', 'CFG.EDITAR', 'REP.VER', 'REP.EXPORTAR', 'CA.ELIMINAR')
)
INSERT INTO public.rol_permiso (rol_id, permiso_id)
SELECT a.id, o.id
FROM admin a
CROSS JOIN objetivos o
ON CONFLICT DO NOTHING;

COMMIT;

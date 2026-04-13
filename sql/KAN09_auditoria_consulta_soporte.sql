-- =============================================================
-- KAN-09: Soporte de consulta AuditLog (RF-US-04)
--
-- PROPÓSITO:
--   1) Crear permiso AUD.VER
--   2) Asignar AUD.VER solo a rol Administrador
--   3) Asegurar índices para filtros/paginación rápida
--
-- EJECUTAR en: Supabase SQL Editor
-- IDEMPOTENTE: sí
-- =============================================================

BEGIN;

-- 0) Normalizar el tipo de fecha de auditoría si sigue como texto
DO $$
BEGIN
  IF EXISTS (
    SELECT 1
    FROM information_schema.columns
    WHERE table_schema = 'public'
      AND table_name = 'auditoria_log'
      AND column_name = 'evento_en'
      AND (data_type IN ('text', 'character varying') OR udt_name IN ('text', 'varchar'))
  ) THEN
    ALTER TABLE public.auditoria_log
    ALTER COLUMN evento_en TYPE timestamp without time zone
    USING (NULLIF(evento_en, '')::timestamptz AT TIME ZONE 'UTC');
  END IF;
END $$;

-- 1) Permiso de consulta de auditoría
INSERT INTO public.permiso (codigo, modulo_nombre, accion_nombre, descripcion, creado_en, esta_activo)
VALUES ('AUD.VER', 'Auditoria', 'VER', 'Consultar logs de auditoría con filtros', NOW(), TRUE)
ON CONFLICT (codigo) DO NOTHING;

-- 2) Asignar solo a Administrador
INSERT INTO public.rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id
FROM public.rol r
JOIN public.permiso p ON p.codigo = 'AUD.VER'
WHERE r.nombre = 'Administrador'
ON CONFLICT DO NOTHING;

-- 3) Limpiar asignaciones en otros roles (si existieran por datos previos)
DELETE FROM public.rol_permiso rp
USING public.rol r, public.permiso p
WHERE rp.rol_id = r.id
  AND rp.permiso_id = p.id
  AND p.codigo = 'AUD.VER'
  AND r.nombre <> 'Administrador';

-- 4) Índices de soporte para filtros (usuario, fechas, módulo, acción)
CREATE INDEX IF NOT EXISTS ix_auditoria_log_evento_desc
ON public.auditoria_log (evento_en DESC);

CREATE INDEX IF NOT EXISTS ix_auditoria_log_usuario_evento_desc
ON public.auditoria_log (ejecutado_por_usuario_id, evento_en DESC);

CREATE INDEX IF NOT EXISTS ix_auditoria_log_modulo_accion_evento_desc
ON public.auditoria_log (modulo_nombre, accion_nombre, evento_en DESC);

COMMIT;

-- ------------------------------------------------------------
-- Verificación post-deploy
-- ------------------------------------------------------------

-- A) Permiso AUD.VER y su asignación por rol
SELECT r.nombre AS rol, p.codigo AS permiso
FROM public.rol_permiso rp
JOIN public.rol r ON r.id = rp.rol_id
JOIN public.permiso p ON p.id = rp.permiso_id
WHERE p.codigo = 'AUD.VER'
ORDER BY r.nombre;

-- B) Últimos registros
SELECT id, evento_en, modulo_nombre, entidad_nombre, accion_nombre
FROM public.auditoria_log
ORDER BY evento_en DESC
LIMIT 100;

-- C) Filtro por usuario
SELECT *
FROM public.auditoria_log
WHERE ejecutado_por_usuario_id = 1
ORDER BY evento_en DESC;

-- D) Filtro por módulo y fecha
SELECT *
FROM public.auditoria_log
WHERE modulo_nombre = 'Usuarios'
  AND evento_en::timestamp >= NOW() - INTERVAL '7 days';

-- E) Sanidad
SELECT COUNT(*)
FROM public.auditoria_log
WHERE modulo_nombre IS NULL
   OR entidad_nombre IS NULL
   OR accion_nombre IS NULL;

-- F) Tipo real de evento_en (debe ser timestamp without time zone)
SELECT data_type, udt_name
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'auditoria_log'
  AND column_name = 'evento_en';

-- =============================================================================
-- KAN-23: Permisos faltantes en el catalogo
--   - Tasa de Retencion y Graduacion (TRE.*)  -> antes solo via PR.VER/admin
--   - Proyeccion de Estudiantes        (ES.*)  -> antes solo admin
--
-- Hace que ambos modulos aparezcan en "Permisos de Acceso por Modulo"
-- y se puedan conceder/denegar por usuario.
--
-- Idempotente. Solo INSERTA filas nuevas; NO modifica permisos, roles
-- ni overrides existentes. Ejecutar en Supabase SQL Editor.
--
-- Nomenclatura alineada a KAN-11: modulo_nombre INITCAP/sin acentos,
-- accion_nombre en minuscula.
-- =============================================================================

BEGIN;

-- 1. Permisos nuevos
INSERT INTO public.permiso (codigo, modulo_nombre, accion_nombre, descripcion, creado_en, esta_activo)
VALUES
  ('TRE.VER',    'Tasa De Retencion Y Graduacion', 'ver',      'Ver tasa de retencion y graduacion',        NOW(), TRUE),
  ('TRE.EDITAR', 'Tasa De Retencion Y Graduacion', 'editar',   'Configurar tasa de retencion y graduacion', NOW(), TRUE),
  ('ES.VER',     'Proyeccion De Estudiantes',      'ver',      'Ver proyeccion de estudiantes',             NOW(), TRUE),
  ('ES.EJECUTAR','Proyeccion De Estudiantes',      'ejecutar', 'Ejecutar proyeccion de estudiantes',        NOW(), TRUE)
ON CONFLICT (codigo) DO NOTHING;

-- 2. Administrador -> los 4 (mantiene "acceso total")
INSERT INTO public.rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id
  FROM public.rol r
  JOIN public.permiso p ON p.codigo IN ('TRE.VER','TRE.EDITAR','ES.VER','ES.EJECUTAR')
 WHERE r.nombre = 'Administrador'
ON CONFLICT DO NOTHING;

-- 3. Analista -> los 4 (hoy ya accede a estos modulos via PR.VER; se preserva)
INSERT INTO public.rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id
  FROM public.rol r
  JOIN public.permiso p ON p.codigo IN ('TRE.VER','TRE.EDITAR','ES.VER','ES.EJECUTAR')
 WHERE r.nombre = 'Analista'
ON CONFLICT DO NOTHING;

-- 4. Visualizador -> solo lectura (preserva su acceso de solo-ver actual)
INSERT INTO public.rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id
  FROM public.rol r
  JOIN public.permiso p ON p.codigo IN ('TRE.VER','ES.VER')
 WHERE r.nombre = 'Visualizador'
ON CONFLICT DO NOTHING;

COMMIT;

-- =============================================================================
-- Verificacion
-- =============================================================================
SELECT codigo, modulo_nombre, accion_nombre
  FROM public.permiso
 WHERE codigo IN ('TRE.VER','TRE.EDITAR','ES.VER','ES.EJECUTAR')
 ORDER BY codigo;

SELECT r.nombre AS rol, p.codigo
  FROM public.rol_permiso rp
  JOIN public.rol r     ON r.id = rp.rol_id
  JOIN public.permiso p ON p.id = rp.permiso_id
 WHERE p.codigo IN ('TRE.VER','TRE.EDITAR','ES.VER','ES.EJECUTAR')
 ORDER BY r.nombre, p.codigo;

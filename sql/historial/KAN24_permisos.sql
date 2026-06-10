-- =============================================================================
-- KAN-24: Permisos del modulo Recursos Fisicos y Depreciacion (RD.*)
-- Hace que el modulo aparezca en "Permisos de Acceso por Modulo" y se pueda
-- conceder/denegar por usuario.
--
-- Idempotente. Solo INSERTA; NO modifica permisos/roles/overrides existentes.
-- Nomenclatura alineada a KAN-11/KAN-23: modulo INITCAP sin acentos, accion minuscula.
-- =============================================================================

BEGIN;

-- 1. Permisos nuevos
INSERT INTO public.permiso (codigo, modulo_nombre, accion_nombre, descripcion, creado_en, esta_activo)
VALUES
  ('RD.VER',      'Recursos Fisicos Y Depreciacion', 'ver',      'Ver activos fijos y depreciacion',    NOW(), TRUE),
  ('RD.CREAR',    'Recursos Fisicos Y Depreciacion', 'crear',    'Crear activos fijos',                 NOW(), TRUE),
  ('RD.EDITAR',   'Recursos Fisicos Y Depreciacion', 'editar',   'Editar activos fijos',                NOW(), TRUE),
  ('RD.ELIMINAR', 'Recursos Fisicos Y Depreciacion', 'eliminar', 'Eliminar (logico) activos fijos',     NOW(), TRUE)
ON CONFLICT (codigo) DO NOTHING;

-- 2. Administrador -> los 4
INSERT INTO public.rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id
  FROM public.rol r
  JOIN public.permiso p ON p.codigo IN ('RD.VER','RD.CREAR','RD.EDITAR','RD.ELIMINAR')
 WHERE r.nombre = 'Administrador'
ON CONFLICT DO NOTHING;

-- 3. Analista -> los 4
INSERT INTO public.rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id
  FROM public.rol r
  JOIN public.permiso p ON p.codigo IN ('RD.VER','RD.CREAR','RD.EDITAR','RD.ELIMINAR')
 WHERE r.nombre = 'Analista'
ON CONFLICT DO NOTHING;

-- 4. Visualizador -> solo lectura
INSERT INTO public.rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id
  FROM public.rol r
  JOIN public.permiso p ON p.codigo = 'RD.VER'
 WHERE r.nombre = 'Visualizador'
ON CONFLICT DO NOTHING;

COMMIT;

-- Verificacion
SELECT codigo, modulo_nombre, accion_nombre
  FROM public.permiso
 WHERE codigo IN ('RD.VER','RD.CREAR','RD.EDITAR','RD.ELIMINAR')
 ORDER BY codigo;

SELECT r.nombre AS rol, p.codigo
  FROM public.rol_permiso rp
  JOIN public.rol r     ON r.id = rp.rol_id
  JOIN public.permiso p ON p.id = rp.permiso_id
 WHERE p.codigo IN ('RD.VER','RD.CREAR','RD.EDITAR','RD.ELIMINAR')
 ORDER BY r.nombre, p.codigo;

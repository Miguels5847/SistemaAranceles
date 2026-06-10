-- =============================================================================
-- KAN-22: Permisos para Datos Institucionales y Aporte Planta Central
-- Ejecutar en Supabase SQL Editor (despues de KAN22_DatosInstitucionales.sql).
-- Idempotente.
-- =============================================================================

BEGIN;

-- 1. Permisos KAN-22
INSERT INTO public.permiso (codigo, modulo_nombre, accion_nombre, descripcion, creado_en, esta_activo)
VALUES
  ('DI.VER',    'Datos Institucionales', 'VER',    'Ver datos institucionales (poblacion y planta central)', NOW(), TRUE),
  ('DI.EDITAR', 'Datos Institucionales', 'EDITAR', 'Configurar datos institucionales (admin)',                NOW(), TRUE),
  ('PC.VER',    'Planta Central',        'VER',    'Ver calculo de aporte de carrera a planta central',      NOW(), TRUE)
ON CONFLICT (codigo) DO NOTHING;

-- 2. Asignar los 3 permisos al rol Administrador
INSERT INTO public.rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id
  FROM public.rol r
  JOIN public.permiso p ON p.codigo IN ('DI.VER', 'DI.EDITAR', 'PC.VER')
 WHERE r.nombre = 'Administrador'
ON CONFLICT DO NOTHING;

COMMIT;

-- Verificacion
SELECT codigo, modulo_nombre, accion_nombre
  FROM public.permiso
 WHERE codigo IN ('DI.VER', 'DI.EDITAR', 'PC.VER')
 ORDER BY codigo;

SELECT r.nombre AS rol, p.codigo
  FROM public.rol_permiso rp
  JOIN public.rol r     ON r.id = rp.rol_id
  JOIN public.permiso p ON p.id = rp.permiso_id
 WHERE p.codigo IN ('DI.VER', 'DI.EDITAR', 'PC.VER')
 ORDER BY r.nombre, p.codigo;

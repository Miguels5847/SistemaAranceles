-- =============================================================
-- KAN-12: Asignar permisos mínimos al rol Visualizador
--
-- Objetivo:
--   Garantizar que el rol Visualizador tenga acceso de lectura
--   a módulos principales (Usuarios, Carreras, Inflación, etc.)
--   para evitar que usuarios cambien a Visualizador quedando
--   sin permisos efectivos.
--
-- Idempotente: Sí (usa ON CONFLICT DO NOTHING)
-- Ejecutar en: Supabase SQL Editor
-- =============================================================

BEGIN;

-- 1) Obtener ID del rol Visualizador
WITH rol_visualizador AS (
    SELECT id
    FROM rol
    WHERE nombre = 'Visualizador'
      AND esta_activo = true
    LIMIT 1
)

-- 2) Insertar permisos VER para módulos principales
INSERT INTO rol_permiso (rol_id, permiso_id)
SELECT
    rv.id AS rol_id,
    p.id AS permiso_id
FROM rol_visualizador rv
CROSS JOIN permiso p
WHERE p.esta_activo = true
  AND p.accion_nombre = 'ver'
  AND p.modulo_nombre IN (
      'Usuarios',
      'Carreras', 
      'Inflacion',
      'Proyecciones',
      'Analisis Financiero',
      'Reportes'
  )
ON CONFLICT DO NOTHING;

-- 3) Verificación: Permisos asignados al Visualizador
SELECT
    r.nombre AS rol_nombre,
    COUNT(rp.permiso_id) AS total_permisos,
    STRING_AGG(DISTINCT p.modulo_nombre || ' - ' || p.accion_nombre, ', ' ORDER BY p.modulo_nombre || ' - ' || p.accion_nombre) AS permisos_asignados
FROM rol r
LEFT JOIN rol_permiso rp ON rp.rol_id = r.id
LEFT JOIN permiso p ON p.id = rp.permiso_id
WHERE r.nombre = 'Visualizador'
  AND r.esta_activo = true
GROUP BY r.id, r.nombre;

COMMIT;

-- =============================================================
-- KAN-12: Asignar permisos mínimos al rol Visualizador
--
-- Objetivo:
--   Garantizar que el rol Visualizador tenga acceso de lectura
--   a módulos principales y dependientes (solo lectura)
--   para evitar que usuarios cambien a Visualizador quedando
--   sin permisos efectivos.
--
-- Idempotente: Sí (usa ON CONFLICT DO NOTHING)
-- Ejecutar en: Supabase SQL Editor
-- =============================================================

BEGIN;

CREATE EXTENSION IF NOT EXISTS unaccent;

-- 0) Validación: qué módulos esperados tienen permiso VER activo.
WITH esperados(modulo) AS (
  VALUES
    ('Usuarios'),
    ('Carreras'),
    ('Inflacion'),
    ('Proyecciones'),
    ('Analisis Financiero'),
    ('Reportes'),
    ('Configuracion'),
    ('Sueldos'),
    ('Demanda'),
    ('Mantenimiento'),
    ('Costos y Gastos')
)
SELECT
  e.modulo,
  COUNT(p.id) AS permisos_ver_activos
FROM esperados e
LEFT JOIN permiso p
  ON lower(unaccent(trim(p.modulo_nombre))) = lower(unaccent(trim(e.modulo)))
   AND lower(unaccent(trim(p.accion_nombre))) = 'ver'
   AND lower(coalesce(p.esta_activo::text, '0')) IN ('1', 't', 'true')
GROUP BY e.modulo
ORDER BY e.modulo;

-- 1) Obtener ID del rol Visualizador
WITH rol_visualizador AS (
    SELECT id
    FROM rol
    WHERE nombre = 'Visualizador'
    AND lower(coalesce(esta_activo::text, '0')) IN ('1', 't', 'true')
    LIMIT 1
)

-- 2) Insertar permisos VER para módulos principales
INSERT INTO rol_permiso (rol_id, permiso_id)
SELECT
    rv.id AS rol_id,
    p.id AS permiso_id
FROM rol_visualizador rv
CROSS JOIN permiso p
WHERE lower(coalesce(p.esta_activo::text, '0')) IN ('1', 't', 'true')
  AND lower(unaccent(trim(p.accion_nombre))) = 'ver'
  AND lower(unaccent(trim(p.modulo_nombre))) IN (
      lower(unaccent('Usuarios')),
      lower(unaccent('Carreras')),
      lower(unaccent('Inflacion')),
      lower(unaccent('Proyecciones')),
      lower(unaccent('Analisis Financiero')),
      lower(unaccent('Reportes')),
      lower(unaccent('Configuracion')),
      lower(unaccent('Sueldos')),
      lower(unaccent('Demanda')),
      lower(unaccent('Mantenimiento')),
      lower(unaccent('Costos y Gastos')),
      lower(unaccent('Costos')),
      lower(unaccent('Gastos'))
  )
ON CONFLICT DO NOTHING;

-- 2.a) Limpiar permisos VER no permitidos para Visualizador
--      (en especial Auditoria, que debe quedar solo para Administrador).
WITH rol_visualizador AS (
    SELECT id
    FROM rol
    WHERE nombre = 'Visualizador'
      AND lower(coalesce(esta_activo::text, '0')) IN ('1', 't', 'true')
    LIMIT 1
)
DELETE FROM rol_permiso rp
USING rol_visualizador rv, permiso p
WHERE rp.rol_id = rv.id
  AND rp.permiso_id = p.id
  AND lower(unaccent(trim(p.accion_nombre))) = 'ver'
  AND lower(unaccent(trim(p.modulo_nombre))) NOT IN (
      lower(unaccent('Usuarios')),
      lower(unaccent('Carreras')),
      lower(unaccent('Inflacion')),
      lower(unaccent('Proyecciones')),
      lower(unaccent('Analisis Financiero')),
      lower(unaccent('Reportes')),
      lower(unaccent('Configuracion')),
      lower(unaccent('Sueldos')),
      lower(unaccent('Demanda')),
      lower(unaccent('Mantenimiento')),
      lower(unaccent('Costos y Gastos')),
      lower(unaccent('Costos')),
      lower(unaccent('Gastos'))
  );

-- 2.a.1) Safety net explícito: retirar AUD.VER de Visualizador.
WITH rol_visualizador AS (
    SELECT id
    FROM rol
    WHERE nombre = 'Visualizador'
      AND lower(coalesce(esta_activo::text, '0')) IN ('1', 't', 'true')
    LIMIT 1
)
DELETE FROM rol_permiso rp
USING rol_visualizador rv, permiso p
WHERE rp.rol_id = rv.id
  AND rp.permiso_id = p.id
  AND p.codigo = 'AUD.VER';

-- 2.b) Verificación puntual de módulos dependientes reales
SELECT
    p.modulo_nombre,
    COUNT(*) AS total_ver_asignables
FROM permiso p
WHERE lower(coalesce(p.esta_activo::text, '0')) IN ('1', 't', 'true')
  AND lower(unaccent(trim(p.accion_nombre))) = 'ver'
  AND lower(unaccent(trim(p.modulo_nombre))) IN (
      lower(unaccent('Sueldos')),
      lower(unaccent('Demanda')),
      lower(unaccent('Mantenimiento')),
      lower(unaccent('Costos y Gastos')),
      lower(unaccent('Costos')),
      lower(unaccent('Gastos'))
  )
GROUP BY p.modulo_nombre
ORDER BY p.modulo_nombre;

-- 3) Verificación: Permisos asignados al Visualizador
SELECT
    r.nombre AS rol_nombre,
    COUNT(rp.permiso_id) AS total_permisos,
    STRING_AGG(DISTINCT p.modulo_nombre || ' - ' || p.accion_nombre, ', ' ORDER BY p.modulo_nombre || ' - ' || p.accion_nombre) AS permisos_asignados
FROM rol r
LEFT JOIN rol_permiso rp ON rp.rol_id = r.id
LEFT JOIN permiso p ON p.id = rp.permiso_id
WHERE r.nombre = 'Visualizador'
  AND lower(coalesce(r.esta_activo::text, '0')) IN ('1', 't', 'true')
GROUP BY r.id, r.nombre;

COMMIT;

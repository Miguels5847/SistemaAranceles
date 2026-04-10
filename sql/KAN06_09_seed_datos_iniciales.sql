-- =============================================================
-- KAN-06..09: Seed de datos iniciales para Épica 2
-- Inserta: Roles, Permisos, Usuario Administrador inicial
--
-- NOTA: esta_activo es INTEGER (1/0) no BOOLEAN — heredado de
-- la migración KAN-03 que usó estilo SQLite.
--
-- EJECUTAR en: Supabase SQL Editor
-- ORDEN: Ejecutar después de KAN05_mark_migration_applied.sql
-- =============================================================

-- ------------------------------------------------------------
-- 1. ROLES  (esta_activo = INTEGER)
-- ------------------------------------------------------------
INSERT INTO public.rol (nombre, descripcion, creado_en, esta_activo)
VALUES
  ('Administrador', 'Acceso total al sistema: usuarios, configuración y todos los módulos.', NOW(), 1),
  ('Analista',      'Acceso a módulos financieros y de proyección. Sin gestión de usuarios.', NOW(), 1),
  ('Visualizador',  'Solo lectura en todos los módulos permitidos.', NOW(), 1)
ON CONFLICT (nombre) DO NOTHING;

-- ------------------------------------------------------------
-- 2. PERMISOS  (esta_activo = INTEGER)
-- ------------------------------------------------------------
INSERT INTO public.permiso (codigo, modulo_nombre, accion_nombre, descripcion, creado_en, esta_activo)
VALUES
  -- Usuarios
  ('US.VER',    'Usuarios', 'VER',      'Ver lista de usuarios',          NOW(), 1),
  ('US.CREAR',  'Usuarios', 'CREAR',    'Crear nuevos usuarios',          NOW(), 1),
  ('US.EDITAR', 'Usuarios', 'EDITAR',   'Editar datos de usuarios',       NOW(), 1),
  ('US.ELIMINAR','Usuarios', 'ELIMINAR', 'Dar de baja usuarios',           NOW(), 1),
  -- Carreras
  ('CA.VER',    'Carreras', 'VER',      'Ver catálogo de carreras',       NOW(), 1),
  ('CA.CREAR',  'Carreras', 'CREAR',    'Crear carreras',                 NOW(), 1),
  ('CA.EDITAR', 'Carreras', 'EDITAR',   'Editar carreras',                NOW(), 1),
  -- Inflación
  ('INF.VER',   'Inflacion', 'VER',     'Ver datos de inflación',         NOW(), 1),
  ('INF.ED',    'Inflacion', 'EDITAR',  'Editar parámetros de inflación', NOW(), 1),
  -- Proyecciones
  ('PR.VER',    'Proyecciones', 'VER',       'Ver proyecciones',          NOW(), 1),
  ('PR.EJEC',   'Proyecciones', 'EJECUTAR',  'Ejecutar proyecciones',     NOW(), 1),
  -- Análisis Financiero
  ('AF.VER',    'AnalisisFinanciero', 'VER',      'Ver análisis',         NOW(), 1),
  ('AF.EJEC',   'AnalisisFinanciero', 'EJECUTAR', 'Ejecutar análisis',    NOW(), 1),
  -- Auditoría
  ('AUD.VER',   'Auditoria', 'VER',              'Consultar audit log',   NOW(), 1)
ON CONFLICT (codigo) DO NOTHING;

-- ------------------------------------------------------------
-- 3. ROL-PERMISO: Administrador → todos los permisos
-- ------------------------------------------------------------
INSERT INTO public.rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id
FROM public.rol r
CROSS JOIN public.permiso p
WHERE r.nombre = 'Administrador'
ON CONFLICT DO NOTHING;

-- ------------------------------------------------------------
-- 4. ROL-PERMISO: Analista → sin gestión de usuarios
-- ------------------------------------------------------------
INSERT INTO public.rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id
FROM public.rol r
JOIN public.permiso p ON p.codigo NOT IN ('US.CREAR','US.EDITAR','US.ELIMINAR','AUD.VER')
WHERE r.nombre = 'Analista'
ON CONFLICT DO NOTHING;

-- ------------------------------------------------------------
-- 5. ROL-PERMISO: Visualizador → solo VER
-- ------------------------------------------------------------
INSERT INTO public.rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id
FROM public.rol r
JOIN public.permiso p ON p.accion_nombre = 'VER' AND p.codigo <> 'AUD.VER'
WHERE r.nombre = 'Visualizador'
ON CONFLICT DO NOTHING;

-- ------------------------------------------------------------
-- 6. USUARIO ADMINISTRADOR INICIAL
-- Contraseña: Admin2026#   (¡CAMBIAR en primer login!)
-- pgcrypto genera hash BCrypt compatible con BCrypt.Net-Next
-- esta_activo = INTEGER (1)
-- ------------------------------------------------------------
CREATE EXTENSION IF NOT EXISTS pgcrypto;

DO $$
DECLARE
    v_hash      TEXT;
    v_usuario_id INT;
    v_rol_id    INT;
BEGIN
    v_hash := crypt('Admin2026#', gen_salt('bf', 11));

    INSERT INTO public.usuario (
        nombre_completo,
        correo_institucional,
        hash_contrasena,
        estado,
        creado_en,
        esta_activo
    )
    VALUES (
        'Administrador del Sistema',
        'admin@ucacue.edu.ec',
        v_hash,
        'Activo',
        NOW(),
        1
    )
    ON CONFLICT (correo_institucional) DO NOTHING
    RETURNING id INTO v_usuario_id;

    IF v_usuario_id IS NULL THEN
        SELECT id INTO v_usuario_id
        FROM public.usuario
        WHERE correo_institucional = 'admin@ucacue.edu.ec';
    END IF;

    SELECT id INTO v_rol_id FROM public.rol WHERE nombre = 'Administrador';

    INSERT INTO public.usuario_rol (usuario_id, rol_id)
    VALUES (v_usuario_id, v_rol_id)
    ON CONFLICT DO NOTHING;

    RAISE NOTICE 'Admin OK — id: %, hash prefix: %', v_usuario_id, left(v_hash, 20);
END$$;

-- ------------------------------------------------------------
-- VERIFICACIÓN FINAL
-- ------------------------------------------------------------
SELECT u.id, u.nombre_completo, u.correo_institucional, u.estado,
       r.nombre AS rol
FROM public.usuario u
JOIN public.usuario_rol ur ON ur.usuario_id = u.id
JOIN public.rol r           ON r.id = ur.rol_id
WHERE u.correo_institucional = 'admin@ucacue.edu.ec';

SELECT r.nombre AS rol, COUNT(rp.permiso_id) AS total_permisos
FROM public.rol r
LEFT JOIN public.rol_permiso rp ON rp.rol_id = r.id
GROUP BY r.nombre
ORDER BY r.nombre;

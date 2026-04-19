-- =============================================================
-- KAN-13: Seed de datos iniciales para Carreras y Escenarios
-- Para: Épica 4 (Tasa de Retención)
--
-- EJECUTAR EN: Supabase SQL Editor
-- ORDEN: Ejecutar después de KAN06_09_seed_datos_iniciales.sql
-- =============================================================

-- ============================================================
-- 0. FIX SCHEMA: es_predeterminado quedó como integer por
--    migraciones generadas para SQLite. La entidad EF lo mapea
--    como bool -> Npgsql lanza:
--    "Reading as 'System.Boolean' is not supported for fields
--     having DataTypeName 'integer'".
--    Convertimos a boolean (idempotente).
-- ============================================================
DO $$
DECLARE
    v_tipo text;
BEGIN
    SELECT data_type INTO v_tipo
    FROM information_schema.columns
    WHERE table_schema = 'public'
      AND table_name   = 'escenario_proyeccion'
      AND column_name  = 'es_predeterminado';

    IF v_tipo = 'integer' THEN
        EXECUTE 'ALTER TABLE public.escenario_proyeccion
                 ALTER COLUMN es_predeterminado DROP DEFAULT';
        EXECUTE 'ALTER TABLE public.escenario_proyeccion
                 ALTER COLUMN es_predeterminado TYPE boolean
                 USING (es_predeterminado <> 0)';
        EXECUTE 'ALTER TABLE public.escenario_proyeccion
                 ALTER COLUMN es_predeterminado SET DEFAULT FALSE';
    END IF;
END $$;

-- ============================================================
-- 1. CARRERAS (catálogo base para simulación de retención)
--    esta_activo es boolean en Supabase -> TRUE/FALSE
-- ============================================================
INSERT INTO public.carrera (codigo, nombre, facultad_nombre, total_ciclos, creado_en, creado_por_usuario_id, esta_activo)
VALUES
  ('ADM-EMPRESAS',    'Administración de Empresas',          'Ciencias Administrativas', 8,  NOW(), 1, TRUE),
  ('ING-CIVIL',       'Ingeniería Civil',                    'Ingeniería y Tecnología',  10, NOW(), 1, TRUE),
  ('ING-SOFTWARE',    'Ingeniería en Software',              'Ingeniería y Tecnología',  8,  NOW(), 1, TRUE),
  ('ING-INDUSTRIAL',  'Ingeniería Industrial',               'Ingeniería y Tecnología',  10, NOW(), 1, TRUE),
  ('ARQUITECTURA',    'Arquitectura',                        'Ingeniería y Tecnología',  10, NOW(), 1, TRUE),
  ('MEDICINA',        'Medicina',                            'Ciencias de la Salud',     12, NOW(), 1, TRUE),
  ('ODONTOLOGIA',     'Odontología',                         'Ciencias de la Salud',     10, NOW(), 1, TRUE),
  ('ENFERMERIA',      'Enfermería',                          'Ciencias de la Salud',     8,  NOW(), 1, TRUE),
  ('PSICO-CLINICA',   'Psicología Clínica',                  'Ciencias de la Salud',     8,  NOW(), 1, TRUE),
  ('DERECHO',         'Derecho',                             'Ciencias Jurídicas',       9,  NOW(), 1, TRUE),
  ('CONT-AUD',        'Contabilidad y Auditoría',            'Ciencias Administrativas', 8,  NOW(), 1, TRUE),
  ('EDUC-INICIAL',    'Educación Inicial',                   'Ciencias de la Educación', 8,  NOW(), 1, TRUE),
  ('EDUC-BASICA',     'Educación Básica',                    'Ciencias de la Educación', 8,  NOW(), 1, TRUE),
  ('TRABAJO-SOCIAL',  'Trabajo Social',                      'Ciencias Sociales',        8,  NOW(), 1, TRUE),
  ('ROBOTICA',        'Ingeniería en Robótica',              'Ingeniería y Tecnología',  8,  NOW(), 1, TRUE),
  ('BIO-SISTEMAS',    'Ingeniería en Sistemas Biomédicos',   'Ingeniería y Tecnología',  8,  NOW(), 1, TRUE),
  ('VIDEOJUEGOS',     'Ingeniería en Videojuegos',           'Ingeniería y Tecnología',  8,  NOW(), 1, TRUE),
  ('VETERINARIA',     'Medicina Veterinaria',                'Ciencias de la Salud',     10, NOW(), 1, TRUE),
  ('DISENO-GRAFICO',  'Diseño Gráfico',                      'Arte y Diseño',            8,  NOW(), 1, TRUE)
ON CONFLICT DO NOTHING;


-- ============================================================
-- 2. ESCENARIOS DE PROYECCIÓN
--    es_predeterminado ya es boolean tras el paso 0 -> TRUE/FALSE
--    esta_activo es boolean -> TRUE/FALSE
--    NO existe anio_base (nunca se usa)
-- ============================================================
INSERT INTO public.escenario_proyeccion
  (carrera_id, nombre, descripcion, es_predeterminado, creado_en, creado_por_usuario_id, esta_activo)
SELECT c.id,
       'Escenario Base 2026',
       'Proyección con inflación normal (3.5% anual promedio histórico)',
       TRUE,
       NOW(),
       1,
       TRUE
FROM public.carrera c
WHERE c.codigo = 'ADM-EMPRESAS'
  AND NOT EXISTS (
    SELECT 1 FROM public.escenario_proyeccion ep
    WHERE ep.carrera_id = c.id AND ep.nombre = 'Escenario Base 2026'
  );

INSERT INTO public.escenario_proyeccion
  (carrera_id, nombre, descripcion, es_predeterminado, creado_en, creado_por_usuario_id, esta_activo)
SELECT c.id,
       'Escenario Pesimista',
       'Proyección con inflación alta (6% anual) — recesión económica',
       FALSE,
       NOW(),
       1,
       TRUE
FROM public.carrera c
WHERE c.codigo = 'ING-CIVIL'
  AND NOT EXISTS (
    SELECT 1 FROM public.escenario_proyeccion ep
    WHERE ep.carrera_id = c.id AND ep.nombre = 'Escenario Pesimista'
  );

INSERT INTO public.escenario_proyeccion
  (carrera_id, nombre, descripcion, es_predeterminado, creado_en, creado_por_usuario_id, esta_activo)
SELECT c.id,
       'Escenario Optimista',
       'Proyección con inflación baja (2% anual) — crecimiento controlado',
       FALSE,
       NOW(),
       1,
       TRUE
FROM public.carrera c
WHERE c.codigo = 'ING-SOFTWARE'
  AND NOT EXISTS (
    SELECT 1 FROM public.escenario_proyeccion ep
    WHERE ep.carrera_id = c.id AND ep.nombre = 'Escenario Optimista'
  );

INSERT INTO public.escenario_proyeccion
  (carrera_id, nombre, descripcion, es_predeterminado, creado_en, creado_por_usuario_id, esta_activo)
SELECT c.id,
       'Escenario Histórico',
       'Proyección basada en datos reales BCE 2012-2025 sin cambios',
       FALSE,
       NOW(),
       1,
       TRUE
FROM public.carrera c
WHERE c.codigo = 'DERECHO'
  AND NOT EXISTS (
    SELECT 1 FROM public.escenario_proyeccion ep
    WHERE ep.carrera_id = c.id AND ep.nombre = 'Escenario Histórico'
  );

-- ============================================================
-- 3. PERMISOS TRE.* -> se entregan en KAN-17 (no tocar aquí)
-- ============================================================

-- ============================================================
-- VERIFICACIÓN POST-SEED
-- ============================================================
-- SELECT COUNT(*) AS total_carreras   FROM public.carrera              WHERE esta_activo = TRUE;
-- SELECT COUNT(*) AS total_escenarios FROM public.escenario_proyeccion WHERE esta_activo = TRUE;
-- -> Esperado: 20 carreras, 4 escenarios

-- Chequeo rápido del tipo corregido:
-- SELECT data_type FROM information_schema.columns
--  WHERE table_name='escenario_proyeccion' AND column_name='es_predeterminado';
-- -> Esperado: boolean

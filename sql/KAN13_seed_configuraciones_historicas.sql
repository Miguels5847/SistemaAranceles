-- =============================================================
-- KAN-13: Seed de escenarios y configuraciones históricas
-- Para: Épica 4 (Tasa de Retención y Graduación)
--
-- EJECUTAR EN: Supabase SQL Editor
-- ORDEN: Ejecutar después de KAN13_seed_carreras_escenarios.sql
-- =============================================================

-- ============================================================
-- 1. ESCENARIOS — Histórico / Optimista / Pesimista
--    Un set por carrera activa (idempotente)
-- ============================================================
INSERT INTO public.escenario_proyeccion
    (carrera_id, nombre, descripcion, es_predeterminado,
     creado_en, creado_por_usuario_id, esta_activo)
SELECT
    c.id,
    e.nombre,
    e.descripcion,
    e.es_predeterminado,
    NOW(), 1, TRUE
FROM public.carrera c
CROSS JOIN (VALUES
    ('Histórico', 'Datos reales de periodos anteriores. Base de referencia para proyecciones.', TRUE),
    ('Optimista',  'Proyección con mejora: retención +5 pp, graduación +8 pp, estudiantes ×1.20.', FALSE),
    ('Pesimista',  'Proyección adversa: retención −10 pp, graduación −15 pp, estudiantes ×0.80.', FALSE)
) AS e(nombre, descripcion, es_predeterminado)
WHERE c.esta_activo = TRUE
  AND NOT EXISTS (
      SELECT 1 FROM public.escenario_proyeccion ep
      WHERE ep.carrera_id = c.id
        AND ep.nombre     = e.nombre
        AND ep.esta_activo = TRUE
  );

-- ============================================================
-- 2. CONFIGURACIONES HISTÓRICAS (idempotente por carrera)
--    Un registro por carrera — escenario Histórico
--    Datos: conservadores basados en rangos reales Ecuador
-- ============================================================
INSERT INTO public.configuracion_retencion
    (carrera_id, escenario_proyeccion_id,
     total_ciclos,
     tasa_retencion_porcentaje, tasa_graduacion_porcentaje,
     estudiantes_periodo_1, estudiantes_periodo_2,
     paralelos_periodo_1, paralelos_periodo_2,
     creado_en, creado_por_usuario_id, esta_activo)
SELECT
    c.id,
    ep.id,
    c.total_ciclos,
    v.tasa_ret::numeric(9,4),
    v.tasa_grad::numeric(9,4),
    v.est_p1::numeric(9,4),
    v.est_p2::numeric(9,4),
    v.par_p1,
    v.par_p2,
    NOW(), 1, TRUE
FROM public.carrera c
JOIN public.escenario_proyeccion ep
    ON  ep.carrera_id  = c.id
    AND ep.nombre      = 'Histórico'
    AND ep.esta_activo = TRUE
JOIN (VALUES
    -- codigo,          tasa_ret, tasa_grad, est_p1, est_p2, par_p1, par_p2
    ('ADM-EMPRESAS',    78.5,     58.0,      40,     38,     2,      2),
    ('ING-CIVIL',       72.0,     52.0,      35,     32,     1,      1),
    ('ING-SOFTWARE',    80.0,     60.0,      38,     35,     2,      2),
    ('ING-INDUSTRIAL',  74.0,     54.0,      35,     33,     1,      1),
    ('ARQUITECTURA',    70.0,     50.0,      30,     28,     1,      1),
    ('MEDICINA',        88.0,     70.0,      25,     24,     1,      1),
    ('ODONTOLOGIA',     85.0,     65.0,      28,     27,     1,      1),
    ('ENFERMERIA',      76.0,     56.0,      35,     33,     1,      1),
    ('PSICO-CLINICA',   75.0,     55.0,      35,     33,     1,      1),
    ('DERECHO',         68.0,     48.0,      45,     42,     2,      2),
    ('CONT-AUD',        77.0,     57.0,      40,     38,     2,      2),
    ('EDUC-INICIAL',    82.0,     62.0,      35,     33,     1,      1),
    ('EDUC-BASICA',     80.0,     60.0,      35,     33,     1,      1),
    ('TRABAJO-SOCIAL',  73.0,     53.0,      35,     32,     1,      1),
    ('ROBOTICA',        79.0,     59.0,      30,     28,     1,      1),
    ('BIO-SISTEMAS',    78.0,     58.0,      28,     26,     1,      1),
    ('VIDEOJUEGOS',     76.0,     56.0,      30,     28,     1,      1),
    ('VETERINARIA',     83.0,     63.0,      30,     28,     1,      1),
    ('DISENO-GRAFICO',  74.0,     54.0,      32,     30,     1,      1)
) AS v(codigo, tasa_ret, tasa_grad, est_p1, est_p2, par_p1, par_p2)
    ON c.codigo = v.codigo
WHERE c.esta_activo = TRUE
  AND NOT EXISTS (
      SELECT 1
      FROM public.configuracion_retencion cr
      WHERE cr.carrera_id = c.id
        AND cr.escenario_proyeccion_id = ep.id
        AND cr.esta_activo = TRUE
  );

-- ============================================================
-- VERIFICACIÓN POST-SEED
-- ============================================================
-- SELECT c.codigo, ep.nombre, cr.tasa_retencion_porcentaje
-- FROM public.configuracion_retencion cr
-- JOIN public.carrera c ON c.id = cr.carrera_id
-- JOIN public.escenario_proyeccion ep ON ep.id = cr.escenario_proyeccion_id
-- WHERE cr.esta_activo = TRUE
-- ORDER BY c.codigo;
-- -> Esperado: 19 filas, todas con escenario 'Histórico'

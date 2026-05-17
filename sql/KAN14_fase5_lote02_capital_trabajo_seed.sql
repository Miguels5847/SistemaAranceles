-- KAN14_fase5_lote02_capital_trabajo_seed.sql
-- Carga de datos maestros: Catálogo de Cargos, Catálogo de Materiales y Parámetros
-- Datos para "6 Capital de Trabajo" — 15 cargos + 16 ítems de materiales
-- Ejecutar: psql -U usuario -d supabase_db < KAN14_fase5_lote02_capital_trabajo_seed.sql
-- O en pgAdmin: copiar y ejecutar en el editor SQL

-- ============================================================================
-- SECCIÓN 1: Cargos para la Carrera Base (carrera_id = 1)
-- A.1 Personal Administrativo (Cantidad fija)
-- ============================================================================

INSERT INTO public.cargos_facultad (carrera_id, nombre_cargo, sueldo_base_mensual, es_cargo_docente, activo, fecha_creacion)
VALUES
    (1, 'Decano', 3880.00, false, true, NOW()),
    (1, 'Subdecano', 2880.00, false, true, NOW()),
    (1, 'Director de Carrera', 2200.00, false, true, NOW()),
    (1, 'Secretario', 1000.00, false, true, NOW()),
    (1, 'Auxiliar de Secretaria', 850.00, false, true, NOW()),
    (1, 'Coordinador', 900.00, false, true, NOW()),
    (1, 'Bienestar Estudiantil', 900.00, false, true, NOW()),
    (1, 'Bibliotecario', 800.00, false, true, NOW()),
    (1, 'Auxiliar de Servicio', 450.00, false, true, NOW()),
    (1, 'Guardia', 650.00, false, true, NOW())
ON CONFLICT DO NOTHING;

-- A.2 Personal Docente (Cantidad variable)
INSERT INTO public.cargos_facultad (carrera_id, nombre_cargo, sueldo_base_mensual, es_cargo_docente, activo, fecha_creacion)
VALUES
    (1, 'Tiempo Completo PhD', 2800.00, true, true, NOW()),
    (1, 'Tiempo Completo Mgs.', 1800.00, true, true, NOW()),
    (1, 'Medio Tiempo', 900.00, true, true, NOW()),
    (1, 'Tiempo Parcial', 432.00, true, true, NOW()),
    (1, 'Ocasional Tipo 2 (Técnico Docente)', 1350.00, true, true, NOW())
ON CONFLICT DO NOTHING;

-- ============================================================================
-- SECCIÓN 2: Materiales y Suministros (7 ítems)
-- ============================================================================

INSERT INTO public.items_material_insumo (nombre_item, categoria_nombre, precio_unitario, cantidad_sugerida, activo, fecha_creacion)
VALUES
    ('Resma de papel bond de 75 gramos', 'MATERIALES_SUMINISTROS', 3.25, 6.0, true, NOW()),
    ('Cartuchos de impresora (Color)', 'MATERIALES_SUMINISTROS', 50.00, 0.6, true, NOW()),
    ('Cartuchos de impresora (Negro)', 'MATERIALES_SUMINISTROS', 40.00, 0.6, true, NOW()),
    ('Carpetas de cartón', 'MATERIALES_SUMINISTROS', 1.00, 60.0, true, NOW()),
    ('Porta files', 'MATERIALES_SUMINISTROS', 2.00, 30.0, true, NOW()),
    ('Esferos, micro minas, lápiz, borradores, correctores', 'MATERIALES_SUMINISTROS', 0.25, 30.0, true, NOW()),
    ('Grapas, clips (CAJA)', 'MATERIALES_SUMINISTROS', 1.00, 0.6, true, NOW())
ON CONFLICT (nombre_item) DO NOTHING;

-- ============================================================================
-- SECCIÓN 3: Suministros de Aseo y Limpieza (7 ítems)
-- ============================================================================

INSERT INTO public.items_material_insumo (nombre_item, categoria_nombre, precio_unitario, cantidad_sugerida, activo, fecha_creacion)
VALUES
    ('Desinfectante (Galón)', 'ASEO_LIMPIEZA', 4.00, 0.9, true, NOW()),
    ('Jabón Líquido (Galón)', 'ASEO_LIMPIEZA', 3.00, 1.08, true, NOW()),
    ('Papel Higiénico (Rollo Grande)', 'ASEO_LIMPIEZA', 10.50, 23.4, true, NOW()),
    ('Escoba', 'ASEO_LIMPIEZA', 10.50, 2.0, true, NOW()),
    ('Paquete de Fundas de Basura', 'ASEO_LIMPIEZA', 0.80, 6.0, true, NOW()),
    ('Trapeador', 'ASEO_LIMPIEZA', 2.50, 3.0, true, NOW()),
    ('Cloro (Galón)', 'ASEO_LIMPIEZA', 2.50, 0.9, true, NOW())
ON CONFLICT (nombre_item) DO NOTHING;

-- ============================================================================
-- SECCIÓN 4: Accesorios y Materiales (2 ítems activos + espacios vacíos reservados)
-- ============================================================================

INSERT INTO public.items_material_insumo (nombre_item, categoria_nombre, precio_unitario, cantidad_sugerida, activo, fecha_creacion)
VALUES
    ('Grapadora', 'ACCESORIOS_MATERIALES', 15.00, 5.25, true, NOW()),
    ('Perforadora', 'ACCESORIOS_MATERIALES', 10.00, 5.25, true, NOW())
ON CONFLICT (nombre_item) DO NOTHING;

-- ============================================================================
-- SECCIÓN 5: Parámetros Globales (si existen en tabla de configuración)
-- Nota: Ajusta según tu estructura real de tabla de parámetros
-- ============================================================================

-- Ejemplo si tienes tabla parametros_sistema:
-- INSERT INTO public.parametros_sistema (clave, valor, descripcion, tipo_dato, activo, fecha_creacion)
-- VALUES
--     ('VALOR_HORA_CLASE', '12.00', 'Valor unitario de hora clase para tiempo parcial', 'decimal', true, NOW()),
--     ('HORAS_CLASE_MES', '36', 'Horas de clase por mes (9 horas/semana × 4 semanas)', 'integer', true, NOW()),
--     ('HORAS_CLASE_SEMANA', '9', 'Horas de clase por semana', 'integer', true, NOW()),
--     ('SEMANAS_MES', '4', 'Semanas por mes', 'integer', true, NOW()),
--     ('DIVISOR_MENSUALIZACION', '6', 'Divisor para convertir total semestral a mensual', 'integer', true, NOW()),
--     ('ESTUDIANTES_UA', '285', 'Estudiantes de otras carreras (Unidad Académica)', 'integer', true, NOW()),
--     ('TASA_FONDO_RESERVA', '0.0833', 'Tasa de fondo de reserva (8.33%)', 'decimal', true, NOW()),
--     ('TASA_APORTE_PATRONAL', '0.1115', 'Tasa de aporte patronal (11.15%)', 'decimal', true, NOW()),
--     ('DECIMO_IV_ANUAL', '450.00', 'Décimo IV anual en USD', 'decimal', true, NOW())
-- ON CONFLICT (clave) DO NOTHING;

-- ============================================================================
-- Validación final
-- ============================================================================

-- Contar registros cargados:
SELECT 'CARGOS_FACULTAD' as tabla, COUNT(*) as total FROM public.cargos_facultad WHERE carrera_id = 1 AND es_cargo_docente = false
UNION ALL
SELECT 'CARGOS_DOCENTES', COUNT(*) FROM public.cargos_facultad WHERE carrera_id = 1 AND es_cargo_docente = true
UNION ALL
SELECT 'MATERIALES_SUMINISTROS', COUNT(*) FROM public.items_material_insumo WHERE categoria_nombre = 'MATERIALES_SUMINISTROS'
UNION ALL
SELECT 'ASEO_LIMPIEZA', COUNT(*) FROM public.items_material_insumo WHERE categoria_nombre = 'ASEO_LIMPIEZA'
UNION ALL
SELECT 'ACCESORIOS_MATERIALES', COUNT(*) FROM public.items_material_insumo WHERE categoria_nombre = 'ACCESORIOS_MATERIALES';

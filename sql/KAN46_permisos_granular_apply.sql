-- =====================================================================
-- KAN-46 SQL-C: PERMISOS GRANULARES — correctivo idempotente (re-ejecutable).
-- Ejecutar DESPUÉS de revisar el diagnóstico (SQL-B).
-- No borra nada: el .exe anterior sigue funcionando durante la transición.
-- Los usuarios deben CERRAR SESIÓN y volver a entrar para ver los cambios.
-- =====================================================================
BEGIN;

-- 1) Sembrar permisos nuevos / faltantes (solo códigos que el C# consume)
INSERT INTO public.permiso (codigo, modulo_nombre, accion_nombre, descripcion, creado_en, esta_activo)
VALUES
    ('AMO.VER',        'Amortizacion',              'VER',      'Acceso al módulo Amortización',                NOW(), TRUE),
    ('AMO.EDITAR',     'Amortizacion',              'EDITAR',   'Editar financiamiento y amortización',         NOW(), TRUE),
    ('SC.VER',         'Sueldos Carrera',           'VER',      'Acceso al módulo Sueldos Carrera',             NOW(), TRUE),
    ('CT.VER',         'Capital de Trabajo',        'VER',      'Acceso al módulo Capital de Trabajo',          NOW(), TRUE),
    ('AF.EDITAR',      'Analisis Financiero',       'EDITAR',   'Aplicar arancel óptimo y editar análisis',     NOW(), TRUE),
    ('MI.VER',         'Mantenimiento e Inversion', 'VER',      'Acceso a Mantenimiento e Inversión',           NOW(), TRUE),
    ('MI.CREAR',       'Mantenimiento e Inversion', 'CREAR',    'Crear servicios/activos diferidos',            NOW(), TRUE),
    ('MI.EDITAR',      'Mantenimiento e Inversion', 'EDITAR',   'Editar servicios/activos diferidos',           NOW(), TRUE),
    ('MI.ELIMINAR',    'Mantenimiento e Inversion', 'ELIMINAR', 'Eliminar servicios/activos diferidos',         NOW(), TRUE),
    ('DI_NG.VER',      'Demanda e Ingresos',        'VER',      'Acceso al módulo Demanda e Ingresos',          NOW(), TRUE),
    ('DI_NG.EDITAR',   'Demanda e Ingresos',        'EDITAR',   'Editar configuración de arancel y ratios',     NOW(), TRUE),
    ('DI_NG.CALCULAR', 'Demanda e Ingresos',        'CALCULAR', 'Refrescar matrices de ingresos y materiales',  NOW(), TRUE),
    ('CG.VER',         'Costos y Gastos',           'VER',      'Acceso al módulo Costos y Gastos',             NOW(), TRUE),
    ('REP.VER',        'Reportes',                  'VER',      'Ver reportes',                                 NOW(), TRUE),
    ('REP.EXPORTAR',   'Reportes',                  'EXPORTAR', 'Exportar reportes a PDF',                      NOW(), TRUE)
ON CONFLICT (codigo) DO UPDATE
    SET esta_activo   = TRUE,
        modulo_nombre = EXCLUDED.modulo_nombre;

-- 2) Desactivar permisos huérfanos de Configuración (el módulo ya no existe)
UPDATE public.permiso SET esta_activo = FALSE WHERE codigo IN ('CFG.VER', 'CFG.EDITAR');

-- 3) Normalizar nombres de módulo existentes (para la UI de permisos de usuario)
UPDATE public.permiso SET modulo_nombre = 'Analisis Financiero' WHERE codigo LIKE 'AF.%';
UPDATE public.permiso SET modulo_nombre = 'Demanda e Ingresos'  WHERE codigo LIKE 'DI_NG.%';

-- 4) Migrar asignaciones de ROL copiando las del permiso que reemplazan:
--    AF.VER -> SC.VER y CT.VER | DI.VER -> AMO.VER | DI.EDITAR -> AMO.EDITAR | DI_NG.EDITAR -> AF.EDITAR
INSERT INTO public.rol_permiso (rol_id, permiso_id)
SELECT rp.rol_id, pn.id
FROM (VALUES ('AF.VER','SC.VER'),
             ('AF.VER','CT.VER'),
             ('DI.VER','AMO.VER'),
             ('DI.EDITAR','AMO.EDITAR'),
             ('DI_NG.EDITAR','AF.EDITAR')) AS m(codigo_viejo, codigo_nuevo)
JOIN public.permiso pv ON pv.codigo = m.codigo_viejo
JOIN public.permiso pn ON pn.codigo = m.codigo_nuevo
JOIN public.rol_permiso rp ON rp.permiso_id = pv.id
ON CONFLICT (rol_id, permiso_id) DO NOTHING;

-- 5) Migrar OVERRIDES por usuario con la misma regla (se copia 'concedido' tal cual)
INSERT INTO public.usuario_permiso_override (usuario_id, permiso_id, concedido)
SELECT upo.usuario_id, pn.id, upo.concedido
FROM (VALUES ('AF.VER','SC.VER'),
             ('AF.VER','CT.VER'),
             ('DI.VER','AMO.VER'),
             ('DI.EDITAR','AMO.EDITAR'),
             ('DI_NG.EDITAR','AF.EDITAR')) AS m(codigo_viejo, codigo_nuevo)
JOIN public.permiso pv ON pv.codigo = m.codigo_viejo
JOIN public.permiso pn ON pn.codigo = m.codigo_nuevo
JOIN public.usuario_permiso_override upo ON upo.permiso_id = pv.id
WHERE NOT EXISTS (
    SELECT 1 FROM public.usuario_permiso_override x
    WHERE x.usuario_id = upo.usuario_id AND x.permiso_id = pn.id);

-- 6) Asignar a roles los permisos posiblemente nunca sembrados:
--    Administrador y Analista: todo; Visualizador: solo VER.
INSERT INTO public.rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id
FROM public.rol r CROSS JOIN public.permiso p
WHERE r.nombre IN ('Administrador','Analista')
  AND p.codigo IN ('MI.VER','MI.CREAR','MI.EDITAR','MI.ELIMINAR',
                   'CG.VER','DI_NG.VER','DI_NG.EDITAR','DI_NG.CALCULAR',
                   'REP.VER','REP.EXPORTAR',
                   'AMO.VER','AMO.EDITAR','SC.VER','CT.VER','AF.EDITAR')
ON CONFLICT (rol_id, permiso_id) DO NOTHING;

INSERT INTO public.rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id
FROM public.rol r CROSS JOIN public.permiso p
WHERE r.nombre = 'Visualizador'
  AND p.codigo IN ('MI.VER','CG.VER','DI_NG.VER','REP.VER','AMO.VER','SC.VER','CT.VER')
ON CONFLICT (rol_id, permiso_id) DO NOTHING;

COMMIT;

-- =====================================================================
-- VERIFICACIÓN
-- =====================================================================
SELECT codigo, modulo_nombre, esta_activo
FROM public.permiso
ORDER BY modulo_nombre, codigo;

SELECT r.nombre, COUNT(*) AS total_permisos
FROM public.rol_permiso rp
JOIN public.rol r ON r.id = rp.rol_id
JOIN public.permiso p ON p.id = rp.permiso_id AND p.esta_activo
GROUP BY r.nombre
ORDER BY r.nombre;

SELECT codigo, esta_activo FROM public.permiso WHERE codigo LIKE 'CFG.%';

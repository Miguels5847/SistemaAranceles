-- =====================================================================
-- KAN-46 SQL-B: DIAGNÓSTICO de permisos (solo lectura).
-- Ejecutar y reportar los resultados ANTES de aplicar el correctivo SQL-C.
-- =====================================================================

-- 1) Inventario completo de permisos y su estado
SELECT id, codigo, modulo_nombre, accion_nombre, esta_activo
FROM public.permiso
ORDER BY modulo_nombre, codigo;

-- 2) Códigos que el código C# usa: ¿existen, están inactivos o faltan?
WITH esperados(codigo) AS (
    VALUES ('US.VER'),('US.CREAR'),('US.EDITAR'),('US.ELIMINAR'),
           ('CA.VER'),('CA.CREAR'),('CA.EDITAR'),('CA.ELIMINAR'),
           ('INF.VER'),('INF.EDITAR'),
           ('TRE.VER'),('TRE.CREAR'),('TRE.EDITAR'),('TRE.ELIMINAR'),
           ('ES.VER'),('ES.CREAR'),('ES.EDITAR'),('ES.ELIMINAR'),
           ('DI.VER'),('DI.EDITAR'),
           ('PC.VER'),
           ('RD.VER'),('RD.CREAR'),('RD.EDITAR'),('RD.ELIMINAR'),
           ('MI.VER'),('MI.CREAR'),('MI.EDITAR'),('MI.ELIMINAR'),
           ('AF.VER'),
           ('DI_NG.VER'),('DI_NG.EDITAR'),('DI_NG.CALCULAR'),
           ('CG.VER'),
           ('REP.VER'),('REP.EXPORTAR'),
           ('AUD.VER'),
           ('CFG.VER'),('CFG.EDITAR')
)
SELECT e.codigo,
       CASE WHEN p.id IS NULL THEN 'FALTA EN BD'
            WHEN NOT p.esta_activo THEN 'EXISTE INACTIVO'
            ELSE 'OK ACTIVO' END AS estado
FROM esperados e
LEFT JOIN public.permiso p ON p.codigo = e.codigo
ORDER BY estado, e.codigo;

-- 3) Asignaciones rol -> permiso (para saber a quién copiar los nuevos)
SELECT r.nombre AS rol, p.codigo, p.esta_activo
FROM public.rol_permiso rp
JOIN public.rol r     ON r.id = rp.rol_id
JOIN public.permiso p ON p.id = rp.permiso_id
ORDER BY r.nombre, p.codigo;

-- 4) Lista de roles existentes (¿hay 'Planificador' u otros?)
SELECT id, nombre, esta_activo FROM public.rol ORDER BY id;

-- 5) Overrides por usuario que afecten códigos a migrar
SELECT upo.usuario_id, p.codigo, upo.concedido
FROM public.usuario_permiso_override upo
JOIN public.permiso p ON p.id = upo.permiso_id
WHERE p.codigo IN ('AF.VER','DI.VER','DI.EDITAR','DI_NG.EDITAR','CFG.VER','CFG.EDITAR')
ORDER BY upo.usuario_id, p.codigo;

-- 6) (Tarea 3) ¿Existe el CHECK del modo de arancel?
SELECT conname, pg_get_constraintdef(oid)
FROM pg_constraint
WHERE conrelid = 'public.configuracion_arancel_carrera'::regclass;

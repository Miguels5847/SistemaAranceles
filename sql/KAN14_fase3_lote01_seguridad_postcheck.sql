-- =============================================================
-- KAN14 Fase 3 - Lote 01 (Seguridad/Core) - POSTCHECK
-- Objetivo: validar tipos finales y sanidad basica tras APPLY.
-- =============================================================

SET lock_timeout = '5s';
SET statement_timeout = '120s';

-- 1) Verificacion de tipos finales esperados
SELECT
    c.table_name,
    c.column_name,
    c.data_type,
    c.udt_name
FROM information_schema.columns c
WHERE c.table_schema = 'public'
  AND (
      (c.table_name, c.column_name) IN (
          ('usuario', 'ultimo_acceso_en'),
          ('usuario', 'creado_en'),
          ('usuario', 'actualizado_en'),
          ('usuario', 'eliminado_en'),
          ('sesion_usuario', 'emitido_en'),
          ('sesion_usuario', 'expira_en'),
          ('sesion_usuario', 'revocado_en'),
          ('rol', 'creado_en'),
          ('rol', 'actualizado_en'),
          ('rol', 'eliminado_en'),
          ('permiso', 'creado_en'),
          ('permiso', 'actualizado_en'),
          ('permiso', 'eliminado_en'),
          ('usuario_permiso_override', 'creado_en'),
          ('auditoria_log', 'evento_en')
      )
  )
ORDER BY c.table_name, c.column_name;

-- 2) Conteo de filas por tabla (comparar con precheck)
SELECT 'usuario' AS tabla, COUNT(*) AS filas FROM public.usuario
UNION ALL
SELECT 'sesion_usuario', COUNT(*) FROM public.sesion_usuario
UNION ALL
SELECT 'rol', COUNT(*) FROM public.rol
UNION ALL
SELECT 'permiso', COUNT(*) FROM public.permiso
UNION ALL
SELECT 'usuario_permiso_override', COUNT(*) FROM public.usuario_permiso_override
UNION ALL
SELECT 'auditoria_log', COUNT(*) FROM public.auditoria_log
ORDER BY tabla;

-- 3) Muestras rapidas de coherencia temporal
SELECT id, ultimo_acceso_en, creado_en, actualizado_en
FROM public.usuario
ORDER BY id DESC
LIMIT 10;

SELECT id, emitido_en, expira_en, revocado_en
FROM public.sesion_usuario
ORDER BY id DESC
LIMIT 10;

SELECT id, evento_en, modulo_nombre, accion_nombre
FROM public.auditoria_log
ORDER BY evento_en DESC
LIMIT 20;

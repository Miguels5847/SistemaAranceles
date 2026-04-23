-- =============================================================
-- KAN-11: Normalización y unificación de permisos RBAC
--
-- Objetivo:
--   1) Estandarizar modulo_nombre y accion_nombre para evitar
--      duplicados por acentos/mayúsculas.
--   2) Unificar permisos duplicados por (módulo, acción) normalizados.
--   3) Migrar referencias en rol_permiso y usuario_permiso_override.
--
-- Ejemplo que corrige:
--   'Inflación' + 'VER' y 'Inflacion' + 'ver' -> un solo permiso canónico.
--
-- Idempotente: Sí
-- Ejecutar en: Supabase SQL Editor
-- =============================================================

BEGIN;

CREATE EXTENSION IF NOT EXISTS unaccent;

-- 1) Normalizar nombres de módulo y acción para todos los permisos activos.
UPDATE public.permiso p
SET
    modulo_nombre = INITCAP(unaccent(trim(p.modulo_nombre))),
    accion_nombre = lower(unaccent(trim(p.accion_nombre))),
    actualizado_en = NOW()
WHERE lower(coalesce(p.esta_activo::text, '0')) IN ('1', 't', 'true')
  AND (
      p.modulo_nombre IS DISTINCT FROM INITCAP(unaccent(trim(p.modulo_nombre)))
      OR p.accion_nombre IS DISTINCT FROM lower(unaccent(trim(p.accion_nombre)))
  );

-- 1.b) Forzar nombres canónicos de módulo por prefijo de código
--      (evita resultados como "Analisisfinanciero").
UPDATE public.permiso p
SET
    modulo_nombre = CASE
        WHEN p.codigo LIKE 'US.%' THEN 'Usuarios'
        WHEN p.codigo LIKE 'CA.%' THEN 'Carreras'
        WHEN p.codigo LIKE 'INF.%' THEN 'Inflacion'
        WHEN p.codigo LIKE 'PR.%' THEN 'Proyecciones'
        WHEN p.codigo LIKE 'AF.%' THEN 'Analisis Financiero'
        WHEN p.codigo LIKE 'AUD.%' THEN 'Auditoria'
        WHEN p.codigo LIKE 'CFG.%' THEN 'Configuracion'
        WHEN p.codigo LIKE 'REP.%' THEN 'Reportes'
        ELSE p.modulo_nombre
    END,
    actualizado_en = NOW()
WHERE lower(coalesce(p.esta_activo::text, '0')) IN ('1', 't', 'true')
  AND p.modulo_nombre IS DISTINCT FROM CASE
        WHEN p.codigo LIKE 'US.%' THEN 'Usuarios'
        WHEN p.codigo LIKE 'CA.%' THEN 'Carreras'
        WHEN p.codigo LIKE 'INF.%' THEN 'Inflacion'
        WHEN p.codigo LIKE 'PR.%' THEN 'Proyecciones'
        WHEN p.codigo LIKE 'AF.%' THEN 'Analisis Financiero'
        WHEN p.codigo LIKE 'AUD.%' THEN 'Auditoria'
        WHEN p.codigo LIKE 'CFG.%' THEN 'Configuracion'
        WHEN p.codigo LIKE 'REP.%' THEN 'Reportes'
        ELSE p.modulo_nombre
    END;

-- 2) Unificar duplicados por (modulo_nombre, accion_nombre) ya normalizados.
DO $$
DECLARE
    r RECORD;
BEGIN
    FOR r IN
        WITH normalizados AS (
            SELECT
                id,
                modulo_nombre,
                accion_nombre,
                codigo,
                MIN(id) OVER (PARTITION BY modulo_nombre, accion_nombre) AS id_canonico,
                COUNT(*) OVER (PARTITION BY modulo_nombre, accion_nombre) AS total
            FROM public.permiso
            WHERE lower(coalesce(esta_activo::text, '0')) IN ('1', 't', 'true')
        )
        SELECT id, id_canonico, modulo_nombre, accion_nombre, codigo
        FROM normalizados
        WHERE total > 1 AND id <> id_canonico
        ORDER BY modulo_nombre, accion_nombre, id
    LOOP
        -- rol_permiso
        INSERT INTO public.rol_permiso (rol_id, permiso_id)
        SELECT rp.rol_id, r.id_canonico
        FROM public.rol_permiso rp
        WHERE rp.permiso_id = r.id
        ON CONFLICT DO NOTHING;

        DELETE FROM public.rol_permiso
        WHERE permiso_id = r.id;

        -- usuario_permiso_override
        INSERT INTO public.usuario_permiso_override (
            usuario_id,
            permiso_id,
            concedido,
            creado_en,
            creado_por_usuario_id
        )
        SELECT
            upo.usuario_id,
            r.id_canonico,
            upo.concedido,
            upo.creado_en,
            upo.creado_por_usuario_id
        FROM public.usuario_permiso_override upo
        WHERE upo.permiso_id = r.id
        ON CONFLICT (usuario_id, permiso_id)
        DO UPDATE SET
            concedido = EXCLUDED.concedido,
            creado_en = EXCLUDED.creado_en,
            creado_por_usuario_id = EXCLUDED.creado_por_usuario_id;

        DELETE FROM public.usuario_permiso_override
        WHERE permiso_id = r.id;

        -- permiso duplicado
        DELETE FROM public.permiso
        WHERE id = r.id;

        RAISE NOTICE 'Permiso duplicado fusionado: id=% -> id_canonico=% (% / %).',
            r.id, r.id_canonico, r.modulo_nombre, r.accion_nombre;
    END LOOP;
END $$;

COMMIT;

-- =============================================================
-- Verificaciones sugeridas
-- =============================================================

-- A) Permisos normalizados
SELECT id, codigo, modulo_nombre, accion_nombre
FROM public.permiso
WHERE lower(coalesce(esta_activo::text, '0')) IN ('1', 't', 'true')
ORDER BY modulo_nombre, accion_nombre, codigo;

-- B) Debe devolver 0 filas: duplicados por módulo + acción
SELECT modulo_nombre, accion_nombre, COUNT(*) AS total
FROM public.permiso
WHERE lower(coalesce(esta_activo::text, '0')) IN ('1', 't', 'true')
GROUP BY modulo_nombre, accion_nombre
HAVING COUNT(*) > 1
ORDER BY total DESC, modulo_nombre, accion_nombre;

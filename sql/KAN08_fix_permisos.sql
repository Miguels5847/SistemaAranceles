-- =============================================================
-- KAN-08: Migración de permisos — menú dinámico
--
-- PROPÓSITO:
--   1. Unificar US.ELIM y US.ELIMINAR en un único código canónico
--   2. Verificar consistencia de rol-permiso tras el renombramiento
--
-- EJECUTAR en: Supabase SQL Editor
-- IDEMPOTENTE: sí
-- ORDEN: Ejecutar después de KAN06_09_seed_datos_iniciales.sql
-- =============================================================

BEGIN;

DO $$
DECLARE
	v_old_id INTEGER;
	v_new_id INTEGER;
BEGIN
	SELECT id INTO v_old_id
	FROM public.permiso
	WHERE codigo = 'US.ELIM'
	LIMIT 1;

	SELECT id INTO v_new_id
	FROM public.permiso
	WHERE codigo = 'US.ELIMINAR'
	LIMIT 1;

	-- Caso A: no existe el código antiguo -> nada que migrar
	IF v_old_id IS NULL THEN
		RAISE NOTICE 'No existe US.ELIM. Nada que migrar.';
		RETURN;
	END IF;

	-- Caso B: existe US.ELIM pero no existe US.ELIMINAR -> renombrar directo
	IF v_new_id IS NULL THEN
		UPDATE public.permiso
		SET codigo = 'US.ELIMINAR'
		WHERE id = v_old_id;

		RAISE NOTICE 'Permiso US.ELIM renombrado a US.ELIMINAR (id=%).', v_old_id;
		RETURN;
	END IF;

	-- Caso C: existen ambos -> migrar referencias de old_id -> new_id y eliminar old_id

	-- 1) rol_permiso (evitar duplicados por PK compuesta)
	INSERT INTO public.rol_permiso (rol_id, permiso_id)
	SELECT rp.rol_id, v_new_id
	FROM public.rol_permiso rp
	WHERE rp.permiso_id = v_old_id
	ON CONFLICT DO NOTHING;

	DELETE FROM public.rol_permiso
	WHERE permiso_id = v_old_id;

	-- 2) usuario_permiso_override (evitar duplicados por (usuario_id, permiso_id))
	INSERT INTO public.usuario_permiso_override (
		usuario_id,
		permiso_id,
		concedido,
		creado_en,
		creado_por_usuario_id
	)
	SELECT
		upo.usuario_id,
		v_new_id,
		upo.concedido,
		upo.creado_en,
		upo.creado_por_usuario_id
	FROM public.usuario_permiso_override upo
	WHERE upo.permiso_id = v_old_id
	ON CONFLICT (usuario_id, permiso_id)
	DO UPDATE SET
		concedido = EXCLUDED.concedido,
		creado_en = EXCLUDED.creado_en,
		creado_por_usuario_id = EXCLUDED.creado_por_usuario_id;

	DELETE FROM public.usuario_permiso_override
	WHERE permiso_id = v_old_id;

	-- 3) eliminar permiso viejo
	DELETE FROM public.permiso
	WHERE id = v_old_id;

	RAISE NOTICE 'Migración completada. old_id=% -> new_id=%', v_old_id, v_new_id;
END $$;

COMMIT;

-- ------------------------------------------------------------
-- 2. Verificación post-migración
-- ------------------------------------------------------------

-- 2a) No debe existir US.ELIM
SELECT codigo
FROM public.permiso
WHERE codigo IN ('US.ELIM', 'US.ELIMINAR');

-- 2b) Matriz rol-permiso completa
SELECT r.nombre AS rol, p.codigo
FROM public.rol r
JOIN public.rol_permiso rp ON rp.rol_id = r.id
JOIN public.permiso p      ON p.id = rp.permiso_id
ORDER BY r.nombre, p.codigo;

-- 2c) Permisos módulo Usuarios
SELECT codigo, modulo_nombre, accion_nombre
FROM public.permiso
WHERE modulo_nombre = 'Usuarios'
ORDER BY codigo;

-- 2d) Overrides vigentes
SELECT usuario_id, permiso_id, concedido, creado_en
FROM public.usuario_permiso_override
ORDER BY usuario_id, permiso_id;

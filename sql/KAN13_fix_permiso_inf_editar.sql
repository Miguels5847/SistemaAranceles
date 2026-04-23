-- =============================================================
-- KAN-13: Migracion segura de permiso INF.ED -> INF.EDITAR
-- Objetivo:
--   - Mantener idempotencia
--   - Evitar conflictos si existen ambos codigos
--   - Preservar asignaciones por rol y overrides de usuario
-- =============================================================

BEGIN;

DO $$
DECLARE
    v_old_id INT;
    v_new_id INT;
BEGIN
    SELECT id INTO v_old_id FROM public.permiso WHERE codigo = 'INF.ED';
    SELECT id INTO v_new_id FROM public.permiso WHERE codigo = 'INF.EDITAR';

    -- Caso 1: existe INF.ED y no existe INF.EDITAR -> rename directo
    IF v_old_id IS NOT NULL AND v_new_id IS NULL THEN
        UPDATE public.permiso
        SET codigo = 'INF.EDITAR',
            modulo_nombre = 'Inflacion',
            accion_nombre = 'EDITAR',
            descripcion = COALESCE(descripcion, 'Editar parámetros de inflación')
        WHERE id = v_old_id;

    -- Caso 2: existen ambos -> consolidar referencias en INF.EDITAR y eliminar INF.ED
    ELSIF v_old_id IS NOT NULL AND v_new_id IS NOT NULL THEN
        -- Consolidar rol_permiso sin duplicar PK compuesta
        INSERT INTO public.rol_permiso (rol_id, permiso_id)
        SELECT rp.rol_id, v_new_id
        FROM public.rol_permiso rp
        WHERE rp.permiso_id = v_old_id
        ON CONFLICT DO NOTHING;

        DELETE FROM public.rol_permiso WHERE permiso_id = v_old_id;

        -- Consolidar overrides de usuario sin duplicar PK compuesta
        INSERT INTO public.usuario_permiso_override (usuario_id, permiso_id, concedido, motivo, creado_en)
        SELECT uo.usuario_id, v_new_id, uo.concedido, uo.motivo, uo.creado_en
        FROM public.usuario_permiso_override uo
        WHERE uo.permiso_id = v_old_id
        ON CONFLICT (usuario_id, permiso_id)
        DO UPDATE SET
            concedido = EXCLUDED.concedido,
            motivo = EXCLUDED.motivo;

        DELETE FROM public.usuario_permiso_override WHERE permiso_id = v_old_id;

        DELETE FROM public.permiso WHERE id = v_old_id;

        UPDATE public.permiso
        SET modulo_nombre = 'Inflacion',
            accion_nombre = 'EDITAR',
            descripcion = COALESCE(descripcion, 'Editar parámetros de inflación')
        WHERE id = v_new_id;

    -- Caso 3: ya esta migrado (solo INF.EDITAR)
    ELSIF v_old_id IS NULL AND v_new_id IS NOT NULL THEN
        UPDATE public.permiso
        SET modulo_nombre = 'Inflacion',
            accion_nombre = 'EDITAR',
            descripcion = COALESCE(descripcion, 'Editar parámetros de inflación')
        WHERE id = v_new_id;
    END IF;
END $$;

COMMIT;

-- Verificacion rapida post-migracion:
-- SELECT id, codigo, modulo_nombre, accion_nombre FROM public.permiso WHERE codigo IN ('INF.ED','INF.EDITAR');
-- SELECT r.nombre, p.codigo FROM public.rol_permiso rp JOIN public.rol r ON r.id = rp.rol_id JOIN public.permiso p ON p.id = rp.permiso_id WHERE p.codigo='INF.EDITAR' ORDER BY r.nombre;

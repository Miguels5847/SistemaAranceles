-- =============================================================
-- KAN-13 Rollback: INF.EDITAR -> INF.ED
-- Uso exclusivo si Fase 1 requiere reversa funcional inmediata.
-- =============================================================

BEGIN;

DO $$
DECLARE
    v_old_id INT;
    v_new_id INT;
BEGIN
    SELECT id INTO v_new_id FROM public.permiso WHERE codigo = 'INF.EDITAR';
    SELECT id INTO v_old_id FROM public.permiso WHERE codigo = 'INF.ED';

    -- Caso 1: existe INF.EDITAR y no INF.ED -> rename directo
    IF v_new_id IS NOT NULL AND v_old_id IS NULL THEN
        UPDATE public.permiso
        SET codigo = 'INF.ED',
            modulo_nombre = 'Inflacion',
            accion_nombre = 'EDITAR'
        WHERE id = v_new_id;

    -- Caso 2: existen ambos -> consolidar en INF.ED y eliminar INF.EDITAR
    ELSIF v_new_id IS NOT NULL AND v_old_id IS NOT NULL THEN
        INSERT INTO public.rol_permiso (rol_id, permiso_id)
        SELECT rp.rol_id, v_old_id
        FROM public.rol_permiso rp
        WHERE rp.permiso_id = v_new_id
        ON CONFLICT DO NOTHING;

        DELETE FROM public.rol_permiso WHERE permiso_id = v_new_id;

        INSERT INTO public.usuario_permiso_override (usuario_id, permiso_id, concedido, motivo, creado_en)
        SELECT uo.usuario_id, v_old_id, uo.concedido, uo.motivo, uo.creado_en
        FROM public.usuario_permiso_override uo
        WHERE uo.permiso_id = v_new_id
        ON CONFLICT (usuario_id, permiso_id)
        DO UPDATE SET
            concedido = EXCLUDED.concedido,
            motivo = EXCLUDED.motivo;

        DELETE FROM public.usuario_permiso_override WHERE permiso_id = v_new_id;

        DELETE FROM public.permiso WHERE id = v_new_id;
    END IF;
END $$;

COMMIT;

-- Verificacion rapida post-rollback:
-- SELECT id, codigo, modulo_nombre, accion_nombre FROM public.permiso WHERE codigo IN ('INF.ED','INF.EDITAR');

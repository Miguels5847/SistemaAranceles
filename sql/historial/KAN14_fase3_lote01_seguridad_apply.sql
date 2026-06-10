-- =============================================================
-- KAN14 Fase 3 - Lote 01 (Seguridad/Core) - APPLY
-- Objetivo: migrar columnas de fecha a timestamptz de forma segura.
-- Requisito: ejecutar precheck y confirmar total_invalidos_lote01 = 0.
-- =============================================================

BEGIN;

SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '300s';

DO $$
DECLARE
    r record;
    v_type text;
    v_invalid bigint;
    v_default text;
BEGIN
    FOR r IN
        SELECT *
        FROM (VALUES
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
            ('usuario_permiso_override', 'creado_en')
        ) AS t(tabla, columna)
    LOOP
        SELECT c.data_type
          INTO v_type
          FROM information_schema.columns c
         WHERE c.table_schema = 'public'
           AND c.table_name = r.tabla
           AND c.column_name = r.columna;

        IF v_type IS NULL THEN
            RAISE NOTICE 'KAN14 lote01: %.% no existe; se omite.', r.tabla, r.columna;
            CONTINUE;
        END IF;

        IF v_type IN ('text', 'character varying') THEN
                        SELECT pg_get_expr(ad.adbin, ad.adrelid)
                            INTO v_default
                            FROM pg_attribute a
                            LEFT JOIN pg_attrdef ad
                                ON ad.adrelid = a.attrelid
                             AND ad.adnum = a.attnum
                         WHERE a.attrelid = format('public.%I', r.tabla)::regclass
                             AND a.attname = r.columna;

                        IF v_default IS NOT NULL THEN
                                EXECUTE format(
                                        'ALTER TABLE public.%I ALTER COLUMN %I DROP DEFAULT',
                                        r.tabla, r.columna
                                );
                        END IF;

            EXECUTE format(
                'SELECT COUNT(*)
                   FROM public.%I
                  WHERE NULLIF(btrim(%I), '''') IS NOT NULL
                    AND (
                        CASE
                            WHEN NULLIF(btrim(%I), '''') IS NULL THEN NULL
                            ELSE NULLIF(btrim(%I), '''')::timestamptz
                        END
                    ) IS NULL',
                r.tabla, r.columna, r.columna, r.columna
            ) INTO v_invalid;

            IF COALESCE(v_invalid, 0) > 0 THEN
                RAISE EXCEPTION 'KAN14 lote01 abortado: %.% tiene % valores no convertibles.', r.tabla, r.columna, v_invalid;
            END IF;

            EXECUTE format(
                'ALTER TABLE public.%I
                   ALTER COLUMN %I TYPE timestamptz
                   USING CASE
                            WHEN NULLIF(btrim(%I), '''') IS NULL THEN NULL
                            ELSE NULLIF(btrim(%I), '''')::timestamptz
                         END',
                r.tabla, r.columna, r.columna, r.columna
            );

            IF v_default IS NOT NULL THEN
                EXECUTE format(
                    'ALTER TABLE public.%I ALTER COLUMN %I SET DEFAULT now()',
                    r.tabla, r.columna
                );
            END IF;

            RAISE NOTICE 'KAN14 lote01: %.% convertido a timestamptz.', r.tabla, r.columna;
        ELSE
            RAISE NOTICE 'KAN14 lote01: %.% ya no es text/varchar (tipo=%), se omite.', r.tabla, r.columna, v_type;
        END IF;
    END LOOP;

    -- Migracion especial de auditoria_log.evento_en
    SELECT c.data_type
      INTO v_type
      FROM information_schema.columns c
     WHERE c.table_schema = 'public'
       AND c.table_name = 'auditoria_log'
       AND c.column_name = 'evento_en';

    IF v_type IS NULL THEN
        RAISE NOTICE 'KAN14 lote01: auditoria_log.evento_en no existe; se omite.';
    ELSIF v_type = 'timestamp without time zone' THEN
        ALTER TABLE public.auditoria_log
            ALTER COLUMN evento_en TYPE timestamptz
            USING evento_en AT TIME ZONE 'UTC';
        RAISE NOTICE 'KAN14 lote01: auditoria_log.evento_en timestamp->timestamptz (UTC) aplicado.';
    ELSIF v_type IN ('text', 'character varying') THEN
        ALTER TABLE public.auditoria_log
            ALTER COLUMN evento_en TYPE timestamptz
            USING CASE
                     WHEN NULLIF(btrim(evento_en), '') IS NULL THEN NULL
                     ELSE NULLIF(btrim(evento_en), '')::timestamptz
                  END;
        RAISE NOTICE 'KAN14 lote01: auditoria_log.evento_en text->timestamptz aplicado.';
    ELSE
        RAISE NOTICE 'KAN14 lote01: auditoria_log.evento_en ya esta en tipo %, sin cambios.', v_type;
    END IF;
END;
$$;

COMMIT;

-- =====================================================================
-- KAN-32: Configuración de Arancel por Carrera (Épica 9 — Demanda e Ingresos)
-- =====================================================================
-- Tabla nueva: configuracion_arancel_carrera.
-- Reemplaza funcionalmente a la tabla 'configuracion_arancel' (KAN-03)
-- que queda como obsoleta (sin uso desde la app). No se borra para
-- preservar datos históricos.
--
-- Diferencias clave:
--   - escenario_proyeccion_id NULLABLE (config global por carrera)
--   - modo_calculo_arancel: 'Manual' | 'AutomaticoCostoCarrera'
--   - arancel_manual + porcentaje_matricula (overrides por carrera)
--   - usa_porcentaje_matricula_institucional: si false, usa porcentaje_matricula
-- =====================================================================

BEGIN;

CREATE TABLE IF NOT EXISTS public.configuracion_arancel_carrera (
    id                                       SERIAL PRIMARY KEY,
    carrera_id                               INTEGER NOT NULL REFERENCES public.carrera(id) ON DELETE CASCADE,
    escenario_proyeccion_id                  INTEGER NULL REFERENCES public.escenario_proyeccion(id) ON DELETE SET NULL,
    modo_calculo_arancel                     VARCHAR(30) NOT NULL DEFAULT 'Manual',
    arancel_manual                           NUMERIC(18,2) NULL,
    porcentaje_matricula                     NUMERIC(7,4) NULL,
    usa_porcentaje_matricula_institucional   BOOLEAN NOT NULL DEFAULT TRUE,
    creado_en                                TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    creado_por_usuario_id                    INTEGER NULL REFERENCES public.usuario(id) ON DELETE SET NULL,
    actualizado_en                           TIMESTAMPTZ NULL,
    actualizado_por_usuario_id               INTEGER NULL REFERENCES public.usuario(id) ON DELETE SET NULL,
    esta_activo                              BOOLEAN NOT NULL DEFAULT TRUE,
    eliminado_en                             TIMESTAMPTZ NULL,
    eliminado_por_usuario_id                 INTEGER NULL REFERENCES public.usuario(id) ON DELETE SET NULL
);

-- Defensa idempotente por si la tabla existe con esquema parcial
ALTER TABLE public.configuracion_arancel_carrera
    ADD COLUMN IF NOT EXISTS modo_calculo_arancel VARCHAR(30) NOT NULL DEFAULT 'Manual';
ALTER TABLE public.configuracion_arancel_carrera
    ADD COLUMN IF NOT EXISTS arancel_manual NUMERIC(18,2) NULL;
ALTER TABLE public.configuracion_arancel_carrera
    ADD COLUMN IF NOT EXISTS porcentaje_matricula NUMERIC(7,4) NULL;
ALTER TABLE public.configuracion_arancel_carrera
    ADD COLUMN IF NOT EXISTS usa_porcentaje_matricula_institucional BOOLEAN NOT NULL DEFAULT TRUE;

-- Constraints
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'CK_configuracion_arancel_carrera_modo'
    ) THEN
        ALTER TABLE public.configuracion_arancel_carrera
            ADD CONSTRAINT "CK_configuracion_arancel_carrera_modo"
            CHECK (modo_calculo_arancel IN ('Manual', 'AutomaticoCostoCarrera'));
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'CK_configuracion_arancel_carrera_porc_matricula'
    ) THEN
        ALTER TABLE public.configuracion_arancel_carrera
            ADD CONSTRAINT "CK_configuracion_arancel_carrera_porc_matricula"
            CHECK (porcentaje_matricula IS NULL OR (porcentaje_matricula >= 0 AND porcentaje_matricula <= 100));
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'CK_configuracion_arancel_carrera_arancel_manual'
    ) THEN
        ALTER TABLE public.configuracion_arancel_carrera
            ADD CONSTRAINT "CK_configuracion_arancel_carrera_arancel_manual"
            CHECK (arancel_manual IS NULL OR arancel_manual >= 0);
    END IF;
END $$;

-- Índices
CREATE UNIQUE INDEX IF NOT EXISTS "IX_configuracion_arancel_carrera_carrera_escenario"
    ON public.configuracion_arancel_carrera (carrera_id, COALESCE(escenario_proyeccion_id, 0))
    WHERE esta_activo = TRUE;

CREATE INDEX IF NOT EXISTS "IX_configuracion_arancel_carrera_escenario"
    ON public.configuracion_arancel_carrera (escenario_proyeccion_id);

-- =====================================================================
-- Permisos Demanda e Ingresos (DI_NG)
-- =====================================================================
INSERT INTO public.permiso (codigo, modulo_nombre, accion_nombre, descripcion, esta_activo, creado_en)
VALUES
    ('DI_NG.VER',      'DemandaIngresos', 'VER',      'Acceso al módulo Demanda e Ingresos',                TRUE, NOW()),
    ('DI_NG.EDITAR',   'DemandaIngresos', 'EDITAR',   'Crear/editar/eliminar config arancel y ratios',      TRUE, NOW()),
    ('DI_NG.CALCULAR', 'DemandaIngresos', 'CALCULAR', 'Refrescar matrices de ingresos y materiales',        TRUE, NOW())
ON CONFLICT (codigo) DO NOTHING;

-- Asignar a Administrador (todos)
INSERT INTO public.rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id
FROM public.rol r
CROSS JOIN public.permiso p
WHERE r.nombre = 'Administrador'
  AND p.codigo IN ('DI_NG.VER', 'DI_NG.EDITAR', 'DI_NG.CALCULAR')
ON CONFLICT (rol_id, permiso_id) DO NOTHING;

-- Asignar a Analista (VER + EDITAR + CALCULAR)
INSERT INTO public.rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id
FROM public.rol r
CROSS JOIN public.permiso p
WHERE r.nombre = 'Analista'
  AND p.codigo IN ('DI_NG.VER', 'DI_NG.EDITAR', 'DI_NG.CALCULAR')
ON CONFLICT (rol_id, permiso_id) DO NOTHING;

-- Asignar a Visualizador (solo VER)
INSERT INTO public.rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id
FROM public.rol r
CROSS JOIN public.permiso p
WHERE r.nombre = 'Visualizador'
  AND p.codigo = 'DI_NG.VER'
ON CONFLICT (rol_id, permiso_id) DO NOTHING;

COMMIT;

-- =====================================================================
-- VERIFICACIÓN
-- =====================================================================
-- SELECT * FROM public.configuracion_arancel_carrera;
-- SELECT codigo, modulo_nombre, accion_nombre FROM public.permiso WHERE codigo LIKE 'DI_NG.%';

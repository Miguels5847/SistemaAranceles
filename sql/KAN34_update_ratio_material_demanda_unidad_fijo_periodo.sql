BEGIN;

ALTER TABLE public.ratio_material_demanda
DROP CONSTRAINT IF EXISTS "CK_ratio_material_demanda_unidad";

ALTER TABLE public.ratio_material_demanda
ADD CONSTRAINT "CK_ratio_material_demanda_unidad"
CHECK (
    unidad_ratio IN (
        'por_estudiante',
        'por_estudiante_mes',
        'fijo_periodo'
    )
);

COMMIT;

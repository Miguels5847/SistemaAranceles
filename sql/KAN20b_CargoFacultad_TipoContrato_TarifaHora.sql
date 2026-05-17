-- ============================================================================
-- Migración: KAN20b_CargoFacultad_TipoContrato_TarifaHora
-- Ejecutar en Supabase SQL Editor (idempotente, segura de re-ejecutar)
-- ============================================================================

BEGIN;

-- 1) Schema: agregar columnas nuevas + limpiar drift de tipo_cargo
ALTER TABLE IF EXISTS public.cargo_facultad
    ADD COLUMN IF NOT EXISTS cantidad_default NUMERIC(9,2) NOT NULL DEFAULT 1;

ALTER TABLE IF EXISTS public.cargo_facultad
    ADD COLUMN IF NOT EXISTS tarifa_hora NUMERIC(10,4) NOT NULL DEFAULT 0;

ALTER TABLE IF EXISTS public.cargo_facultad
    ADD COLUMN IF NOT EXISTS tipo_contrato VARCHAR(30) NOT NULL DEFAULT 'Administrativo';

ALTER TABLE IF EXISTS public.cargo_facultad
    DROP COLUMN IF EXISTS tipo_cargo;

-- 2) Backfill: clasificar cargos existentes por nombre
UPDATE public.cargo_facultad SET tipo_contrato = 'PhD'
    WHERE tipo_contrato = 'Administrativo'
      AND LOWER(nombre_cargo) LIKE '%phd%';

UPDATE public.cargo_facultad SET tipo_contrato = 'Mgs'
    WHERE tipo_contrato = 'Administrativo'
      AND (LOWER(nombre_cargo) LIKE '%mgs%' OR LOWER(nombre_cargo) LIKE '%magister%');

UPDATE public.cargo_facultad SET tipo_contrato = 'MedioTiempo'
    WHERE tipo_contrato = 'Administrativo'
      AND LOWER(nombre_cargo) LIKE '%medio tiempo%';

UPDATE public.cargo_facultad SET tipo_contrato = 'Tecnico'
    WHERE tipo_contrato = 'Administrativo'
      AND (LOWER(nombre_cargo) LIKE '%tecnico%' OR LOWER(nombre_cargo) LIKE '%técnico%');

-- TP: convertir sueldo mensual a tarifa horaria, cero el sueldo
UPDATE public.cargo_facultad
    SET tipo_contrato       = 'TiempoParcial',
        tarifa_hora         = ROUND(sueldo_base_mensual / (12 * 4), 4),
        sueldo_base_mensual = 0
    WHERE tipo_contrato = 'Administrativo'
      AND LOWER(nombre_cargo) LIKE '%parcial%';

-- 3) Marcar la migración como aplicada en EF (no la vuelva a correr `dotnet ef database update`)
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260510204418_KAN20b_CargoFacultad_TipoContrato_TarifaHora', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;

-- ============================================================================
-- Verificación (opcional, ejecutar después)
-- ============================================================================
-- SELECT id, nombre_cargo, tipo_contrato, sueldo_base_mensual, tarifa_hora
-- FROM public.cargo_facultad ORDER BY carrera_id, nombre_cargo;
--
-- Esperado para CarreraId=1 (Administración):
--   Tiempo Parcial            → tipo_contrato='TiempoParcial', sueldo=0,    tarifa=9.0000
--   Tiempo Completo PhD       → tipo_contrato='PhD',           sueldo=2800
--   Tiempo Completo Mgs.      → tipo_contrato='Mgs',           sueldo=1800
--   Medio Tiempo              → tipo_contrato='MedioTiempo',   sueldo=900
--   Ocasional Tipo 2 (...)    → tipo_contrato='Tecnico',       sueldo=1350
--   resto (Decano, Subdecano,etc) → tipo_contrato='Administrativo'
----RESULTADO----
[
  {
    "id": 5,
    "nombre_cargo": "Auxiliar de Secretaria",
    "tipo_contrato": "Administrativo",
    "sueldo_base_mensual": "850.00",
    "tarifa_hora": "0.0000"
  },
  {
    "id": 9,
    "nombre_cargo": "Auxiliar de Servicio",
    "tipo_contrato": "Administrativo",
    "sueldo_base_mensual": "450.00",
    "tarifa_hora": "0.0000"
  },
  {
    "id": 8,
    "nombre_cargo": "Bibliotecario",
    "tipo_contrato": "Administrativo",
    "sueldo_base_mensual": "800.00",
    "tarifa_hora": "0.0000"
  },
  {
    "id": 7,
    "nombre_cargo": "Bienestar Estudiantil",
    "tipo_contrato": "Administrativo",
    "sueldo_base_mensual": "900.00",
    "tarifa_hora": "0.0000"
  },
  {
    "id": 6,
    "nombre_cargo": "Coordinador",
    "tipo_contrato": "Administrativo",
    "sueldo_base_mensual": "900.00",
    "tarifa_hora": "0.0000"
  },
  {
    "id": 1,
    "nombre_cargo": "Decano",
    "tipo_contrato": "Administrativo",
    "sueldo_base_mensual": "3880.00",
    "tarifa_hora": "0.0000"
  },
  {
    "id": 3,
    "nombre_cargo": "Director de Carrera",
    "tipo_contrato": "Administrativo",
    "sueldo_base_mensual": "2200.00",
    "tarifa_hora": "0.0000"
  },
  {
    "id": 14,
    "nombre_cargo": "Docente Tiempo Parcial",
    "tipo_contrato": "TiempoParcial",
    "sueldo_base_mensual": "0.00",
    "tarifa_hora": "9.0000"
  },
  {
    "id": 10,
    "nombre_cargo": "Guardia",
    "tipo_contrato": "Administrativo",
    "sueldo_base_mensual": "650.00",
    "tarifa_hora": "0.0000"
  },
  {
    "id": 13,
    "nombre_cargo": "Medio Tiempo",
    "tipo_contrato": "MedioTiempo",
    "sueldo_base_mensual": "900.00",
    "tarifa_hora": "0.0000"
  },
  {
    "id": 15,
    "nombre_cargo": "Ocasional Tipo 2 (Técnico Docente)",
    "tipo_contrato": "Tecnico",
    "sueldo_base_mensual": "1350.00",
    "tarifa_hora": "0.0000"
  },
  {
    "id": 19,
    "nombre_cargo": "Prueba",
    "tipo_contrato": "Administrativo",
    "sueldo_base_mensual": "670.00",
    "tarifa_hora": "0.0000"
  },
  {
    "id": 20,
    "nombre_cargo": "Prueba2",
    "tipo_contrato": "Administrativo",
    "sueldo_base_mensual": "999.00",
    "tarifa_hora": "0.0000"
  },
  {
    "id": 4,
    "nombre_cargo": "Secretario",
    "tipo_contrato": "Administrativo",
    "sueldo_base_mensual": "1000.00",
    "tarifa_hora": "0.0000"
  },
  {
    "id": 2,
    "nombre_cargo": "Subdecano",
    "tipo_contrato": "Administrativo",
    "sueldo_base_mensual": "2880.00",
    "tarifa_hora": "0.0000"
  },
  {
    "id": 12,
    "nombre_cargo": "Tiempo Completo Mgs.",
    "tipo_contrato": "Mgs",
    "sueldo_base_mensual": "1800.00",
    "tarifa_hora": "0.0000"
  },
  {
    "id": 11,
    "nombre_cargo": "Tiempo Completo PhD",
    "tipo_contrato": "PhD",
    "sueldo_base_mensual": "2800.00",
    "tarifa_hora": "0.0000"
  }
]
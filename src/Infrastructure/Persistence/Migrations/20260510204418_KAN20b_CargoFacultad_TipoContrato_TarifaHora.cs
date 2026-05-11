using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaAranceles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class KAN20b_CargoFacultad_TipoContrato_TarifaHora : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotente: si ya existen, no romper. Reconcilia drift de migraciones SQL previas.
            migrationBuilder.Sql(@"
                ALTER TABLE IF EXISTS public.cargo_facultad
                    ADD COLUMN IF NOT EXISTS cantidad_default NUMERIC(9,2) NOT NULL DEFAULT 1;

                ALTER TABLE IF EXISTS public.cargo_facultad
                    ADD COLUMN IF NOT EXISTS tarifa_hora NUMERIC(10,4) NOT NULL DEFAULT 0;

                ALTER TABLE IF EXISTS public.cargo_facultad
                    ADD COLUMN IF NOT EXISTS tipo_contrato VARCHAR(30) NOT NULL DEFAULT 'Administrativo';

                ALTER TABLE IF EXISTS public.cargo_facultad
                    DROP COLUMN IF EXISTS tipo_cargo;
            ");

            // Backfill: clasificar cargos existentes por nombre y convertir TP a tarifa horaria.
            migrationBuilder.Sql(@"
                UPDATE public.cargo_facultad SET tipo_contrato = 'PhD'
                    WHERE tipo_contrato = 'Administrativo' AND LOWER(nombre_cargo) LIKE '%phd%';

                UPDATE public.cargo_facultad SET tipo_contrato = 'Mgs'
                    WHERE tipo_contrato = 'Administrativo'
                      AND (LOWER(nombre_cargo) LIKE '%mgs%' OR LOWER(nombre_cargo) LIKE '%magister%');

                UPDATE public.cargo_facultad SET tipo_contrato = 'MedioTiempo'
                    WHERE tipo_contrato = 'Administrativo' AND LOWER(nombre_cargo) LIKE '%medio tiempo%';

                UPDATE public.cargo_facultad SET tipo_contrato = 'Tecnico'
                    WHERE tipo_contrato = 'Administrativo'
                      AND (LOWER(nombre_cargo) LIKE '%tecnico%' OR LOWER(nombre_cargo) LIKE '%técnico%');

                UPDATE public.cargo_facultad
                    SET tipo_contrato = 'TiempoParcial',
                        tarifa_hora = ROUND(sueldo_base_mensual / (12 * 4), 4),
                        sueldo_base_mensual = 0
                    WHERE tipo_contrato = 'Administrativo' AND LOWER(nombre_cargo) LIKE '%parcial%';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE IF EXISTS public.cargo_facultad DROP COLUMN IF EXISTS tarifa_hora;
                ALTER TABLE IF EXISTS public.cargo_facultad DROP COLUMN IF EXISTS tipo_contrato;
                ALTER TABLE IF EXISTS public.cargo_facultad
                    ADD COLUMN IF NOT EXISTS tipo_cargo VARCHAR(60) NOT NULL DEFAULT 'No especificado';
            ");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaAranceles.Infrastructure.Persistence.Migrations
{
    public partial class RemoveTipoCargo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop column from singular table
            migrationBuilder.Sql(@"ALTER TABLE IF EXISTS public.cargo_facultad DROP COLUMN IF EXISTS tipo_cargo;");
            // Drop column from plural helper table if exists
            migrationBuilder.Sql(@"ALTER TABLE IF EXISTS public.cargos_facultad DROP COLUMN IF EXISTS tipo_cargo;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate column (note: uses TEXT, nullable=false as before; adjust if needed)
            migrationBuilder.Sql(@"ALTER TABLE IF EXISTS public.cargo_facultad ADD COLUMN IF NOT EXISTS tipo_cargo TEXT NOT NULL DEFAULT 'No especificado';");
            migrationBuilder.Sql(@"ALTER TABLE IF EXISTS public.cargos_facultad ADD COLUMN IF NOT EXISTS tipo_cargo TEXT;");
        }
    }
}

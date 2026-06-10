-- =============================================================
-- KAN-05: Marcar migración como aplicada en __EFMigrationsHistory
-- CONTEXTO: Las políticas RLS se aplicaron manualmente vía SQL
-- antes de que dotnet ef database update pudiese ejecutar la
-- migración 20260405211732_KAN05_RlsPostgrestHardening.
-- Este script la registra como aplicada para sincronizar EF.
--
-- EJECUTAR en: Supabase SQL Editor (con postgres/service_role)
-- PRECONDICIÓN: Las políticas ya deben existir (validado por
--   pg_policies query que las confirmó).
-- =============================================================

INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260405211732_KAN05_RlsPostgrestHardening', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

-- Verificación
SELECT "MigrationId", "ProductVersion"
FROM public."__EFMigrationsHistory"
ORDER BY "MigrationId";

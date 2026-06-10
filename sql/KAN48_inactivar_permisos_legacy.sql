-- KAN-48: inactivar permisos legacy sin uso en el código C#
-- (detectados en el diagnóstico KAN-46; ningún ViewModel/MainViewModel los consulta).
-- Idempotente. No borra nada: solo esta_activo = FALSE para que no aparezcan
-- en la pantalla de permisos de usuario.

BEGIN;

UPDATE public.permiso
SET esta_activo = FALSE
WHERE codigo IN ('AF.EJEC', 'PR.VER', 'PR.EJEC', 'ES.EJECUTAR')
  AND esta_activo = TRUE;

-- Verificación
SELECT codigo, nombre, modulo_nombre, esta_activo
FROM public.permiso
WHERE codigo IN ('AF.EJEC', 'PR.VER', 'PR.EJEC', 'ES.EJECUTAR')
ORDER BY codigo;

COMMIT;

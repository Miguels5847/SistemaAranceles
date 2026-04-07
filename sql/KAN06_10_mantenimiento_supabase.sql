-- =============================================================
-- Mantenimiento mínimo para Supabase/PostgreSQL
-- Objetivo: reducir latencia en login/logout y evitar degradación
-- Tablas críticas: usuario, usuario_rol, rol, sesion_usuario, auditoria_log
-- =============================================================

-- 1) Índices recomendados para búsquedas y joins frecuentes
CREATE UNIQUE INDEX IF NOT EXISTS ix_usuario_correo_institucional
ON public.usuario (correo_institucional);

CREATE INDEX IF NOT EXISTS ix_usuario_rol_usuario_id
ON public.usuario_rol (usuario_id);

CREATE INDEX IF NOT EXISTS ix_usuario_rol_rol_id
ON public.usuario_rol (rol_id);

CREATE UNIQUE INDEX IF NOT EXISTS ix_sesion_usuario_token_sesion
ON public.sesion_usuario (token_sesion);

CREATE INDEX IF NOT EXISTS ix_sesion_usuario_usuario_id
ON public.sesion_usuario (usuario_id);

CREATE INDEX IF NOT EXISTS ix_auditoria_log_evento_modulo
ON public.auditoria_log (evento_en, modulo_nombre);

CREATE INDEX IF NOT EXISTS ix_rol_nombre
ON public.rol (nombre);

-- 2) Ajustes de autovacuum para tablas pequeñas/activas
-- Útiles porque tus tablas son chicas y cambian poco, pero se benefician de análisis más frecuente.
ALTER TABLE public.sesion_usuario SET (
    autovacuum_vacuum_scale_factor = 0.05,
    autovacuum_analyze_scale_factor = 0.02,
    autovacuum_vacuum_threshold = 2,
    autovacuum_analyze_threshold = 2
);

ALTER TABLE public.auditoria_log SET (
    autovacuum_vacuum_scale_factor = 0.05,
    autovacuum_analyze_scale_factor = 0.02,
    autovacuum_vacuum_threshold = 2,
    autovacuum_analyze_threshold = 2
);

ALTER TABLE public.usuario_rol SET (
    autovacuum_vacuum_scale_factor = 0.05,
    autovacuum_analyze_scale_factor = 0.02,
    autovacuum_vacuum_threshold = 2,
    autovacuum_analyze_threshold = 2
);

-- 3) Verificación rápida
SELECT relname, n_live_tup, n_dead_tup, vacuum_count, autovacuum_count, analyze_count, autoanalyze_count
FROM pg_stat_user_tables
WHERE relname IN ('rol','usuario_rol','usuario','auditoria_log','sesion_usuario')
ORDER BY relname;

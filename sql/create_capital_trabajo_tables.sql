-- create_capital_trabajo_tables.sql
-- DDL helper: crea las tablas mínimas usadas por los scripts de seed
-- Ejecutar en Supabase / psql antes de ejecutar KAN-20 seed si las tablas no existen.

-- Tabla: cargos_facultad
CREATE TABLE IF NOT EXISTS public.cargos_facultad (
    id BIGSERIAL PRIMARY KEY,
    carrera_id INTEGER NOT NULL,
    nombre_cargo TEXT NOT NULL,
    sueldo_base_mensual NUMERIC(12,2),
    es_cargo_docente BOOLEAN DEFAULT false,
    activo BOOLEAN DEFAULT true,
    fecha_creacion TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE (carrera_id, nombre_cargo)
);

-- Tabla: items_material_insumo
CREATE TABLE IF NOT EXISTS public.items_material_insumo (
    id BIGSERIAL PRIMARY KEY,
    nombre_item TEXT NOT NULL,
    categoria_nombre TEXT,
    precio_unitario NUMERIC(12,2),
    cantidad_sugerida NUMERIC(12,4),
    activo BOOLEAN DEFAULT true,
    fecha_creacion TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE (nombre_item)
);

-- Opcional: índices para consultas por categoria
CREATE INDEX IF NOT EXISTS idx_items_material_insumo_categoria ON public.items_material_insumo (categoria_nombre);
CREATE INDEX IF NOT EXISTS idx_cargos_facultad_carrera ON public.cargos_facultad (carrera_id);

-- Instrucciones rápidas (psql):
-- psql "postgresql://<user>:<pass>@<host>:<port>/<db>?sslmode=require" -f create_capital_trabajo_tables.sql
-- psql "postgresql://<user>:<pass>@<host>:<port>/<db>?sslmode=require" -f KAN-20_capital_trabajo_seed.sql

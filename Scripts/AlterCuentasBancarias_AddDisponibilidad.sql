-- Script de migracion: Agregar columnas de disponibilidad por modulo
-- Ejecutar en la base de datos PostgreSQL de Railway

-- Agregar columnas de disponibilidad si no existen
ALTER TABLE cuentas_bancarias_frontoffice
ADD COLUMN IF NOT EXISTS disponible_compra_divisas BOOLEAN DEFAULT false;

ALTER TABLE cuentas_bancarias_frontoffice
ADD COLUMN IF NOT EXISTS disponible_pack_alimentos BOOLEAN DEFAULT false;

ALTER TABLE cuentas_bancarias_frontoffice
ADD COLUMN IF NOT EXISTS disponible_billetes_avion BOOLEAN DEFAULT false;

ALTER TABLE cuentas_bancarias_frontoffice
ADD COLUMN IF NOT EXISTS disponible_pack_viajes BOOLEAN DEFAULT false;

-- Verificar que las columnas se agregaron
SELECT column_name, data_type, column_default
FROM information_schema.columns
WHERE table_name = 'cuentas_bancarias_frontoffice'
ORDER BY ordinal_position;

-- Script para crear la tabla de cuentas bancarias del FrontOffice
-- Ejecutar en la base de datos PostgreSQL de Railway

-- Crear la tabla cuentas_bancarias_frontoffice
CREATE TABLE IF NOT EXISTS cuentas_bancarias_frontoffice (
    id_cuenta SERIAL PRIMARY KEY,
    nombre_banco VARCHAR(100) NOT NULL,
    titular VARCHAR(200) NOT NULL,
    entidad VARCHAR(20),
    iban VARCHAR(50) NOT NULL,
    -- Campos de disponibilidad por modulo
    disponible_compra_divisas BOOLEAN DEFAULT false,
    disponible_pack_alimentos BOOLEAN DEFAULT false,
    disponible_billetes_avion BOOLEAN DEFAULT false,
    disponible_pack_viajes BOOLEAN DEFAULT false,
    -- Campos de control
    activo BOOLEAN DEFAULT true,
    fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    fecha_modificacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Crear indice para mejorar rendimiento
CREATE INDEX IF NOT EXISTS idx_cuentas_frontoffice_activo
ON cuentas_bancarias_frontoffice(activo);

-- Trigger para actualizar fecha_modificacion
CREATE OR REPLACE FUNCTION actualizar_fecha_modificacion_cuentas_frontoffice()
RETURNS TRIGGER AS $$
BEGIN
    NEW.fecha_modificacion = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trigger_actualizar_fecha_cuentas_frontoffice ON cuentas_bancarias_frontoffice;
CREATE TRIGGER trigger_actualizar_fecha_cuentas_frontoffice
    BEFORE UPDATE ON cuentas_bancarias_frontoffice
    FOR EACH ROW
    EXECUTE FUNCTION actualizar_fecha_modificacion_cuentas_frontoffice();

-- Comentarios
COMMENT ON TABLE cuentas_bancarias_frontoffice IS 'Cuentas bancarias mostradas en el FrontOffice para los usuarios';
COMMENT ON COLUMN cuentas_bancarias_frontoffice.nombre_banco IS 'Nombre del banco';
COMMENT ON COLUMN cuentas_bancarias_frontoffice.titular IS 'Titular de la cuenta';
COMMENT ON COLUMN cuentas_bancarias_frontoffice.entidad IS 'Codigo de entidad bancaria';
COMMENT ON COLUMN cuentas_bancarias_frontoffice.iban IS 'IBAN completo de la cuenta';
COMMENT ON COLUMN cuentas_bancarias_frontoffice.disponible_compra_divisas IS 'Visible en modulo Compra de Divisas';
COMMENT ON COLUMN cuentas_bancarias_frontoffice.disponible_pack_alimentos IS 'Visible en modulo Pack de Alimentos';
COMMENT ON COLUMN cuentas_bancarias_frontoffice.disponible_billetes_avion IS 'Visible en modulo Billetes de Avion';
COMMENT ON COLUMN cuentas_bancarias_frontoffice.disponible_pack_viajes IS 'Visible en modulo Pack de Viajes';

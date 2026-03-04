-- Tabla para almacenar contraseñas de administración del FrontOffice por comercio
-- Permite que cada comercio tenga una contraseña única para acceder a paneles específicos

CREATE TABLE IF NOT EXISTS contrasenas_admin_front (
    id_contrasena SERIAL PRIMARY KEY,
    id_comercio INTEGER NOT NULL,
    contrasena_hash TEXT NOT NULL,
    contrasena_visible TEXT NOT NULL, -- Almacenamos la contraseña sin hash para poder mostrarla
    fecha_creacion TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    fecha_ultima_modificacion TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    creado_por INTEGER REFERENCES administradores_allva(id_administrador),
    modificado_por INTEGER REFERENCES administradores_allva(id_administrador),
    activo BOOLEAN DEFAULT TRUE,

    CONSTRAINT fk_comercio FOREIGN KEY (id_comercio) REFERENCES comercios(id_comercio) ON DELETE CASCADE,
    CONSTRAINT uq_contrasena_comercio UNIQUE (id_comercio)
);

-- Índices para mejorar el rendimiento
CREATE INDEX IF NOT EXISTS idx_contrasenas_admin_front_comercio ON contrasenas_admin_front(id_comercio);
CREATE INDEX IF NOT EXISTS idx_contrasenas_admin_front_activo ON contrasenas_admin_front(activo);

-- Comentarios
COMMENT ON TABLE contrasenas_admin_front IS 'Contraseñas de acceso al panel de administración FrontOffice por comercio';
COMMENT ON COLUMN contrasenas_admin_front.contrasena_hash IS 'Hash de la contraseña para validación de acceso';
COMMENT ON COLUMN contrasenas_admin_front.contrasena_visible IS 'Contraseña en texto plano para poder mostrarla al administrador';
COMMENT ON COLUMN contrasenas_admin_front.id_comercio IS 'Comercio al que pertenece esta contraseña';

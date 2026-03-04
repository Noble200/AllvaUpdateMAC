-- =============================================
-- Tabla: preguntas_frecuentes
-- Descripcion: Almacena las preguntas del FAQ
-- =============================================
CREATE TABLE IF NOT EXISTS preguntas_frecuentes (
    id_pregunta SERIAL PRIMARY KEY,
    pregunta TEXT NOT NULL,
    orden INT DEFAULT 0,
    activo BOOLEAN DEFAULT TRUE,
    fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    fecha_modificacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- =============================================
-- Tabla: respuestas_pregunta
-- Descripcion: Almacena las respuestas de cada pregunta
-- Una pregunta puede tener multiples respuestas
-- =============================================
CREATE TABLE IF NOT EXISTS respuestas_pregunta (
    id_respuesta SERIAL PRIMARY KEY,
    id_pregunta INT NOT NULL REFERENCES preguntas_frecuentes(id_pregunta) ON DELETE CASCADE,
    respuesta TEXT NOT NULL,
    orden INT DEFAULT 0,
    activo BOOLEAN DEFAULT TRUE,
    fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Indices para mejorar rendimiento
CREATE INDEX IF NOT EXISTS idx_respuestas_pregunta ON respuestas_pregunta(id_pregunta);
CREATE INDEX IF NOT EXISTS idx_preguntas_activas ON preguntas_frecuentes(activo);
CREATE INDEX IF NOT EXISTS idx_preguntas_orden ON preguntas_frecuentes(orden);

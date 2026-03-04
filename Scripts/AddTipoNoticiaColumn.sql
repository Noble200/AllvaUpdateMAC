-- =============================================
-- Script para agregar campo tipo_noticia
-- Sistema: AllvaSystem
-- Descripción: Añade la columna tipo_noticia para distinguir
--              entre noticias cuadradas, carrusel y comunicaciones
-- =============================================

-- Agregar la columna tipo_noticia si no existe
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_name='noticias' AND column_name='tipo_noticia') THEN
        ALTER TABLE noticias ADD COLUMN tipo_noticia VARCHAR(20) DEFAULT 'cuadrada';
    END IF;
END $$;

-- Crear índice para mejorar el rendimiento
CREATE INDEX IF NOT EXISTS idx_noticias_tipo ON noticias(tipo_noticia);

-- Comentario en la columna
COMMENT ON COLUMN noticias.tipo_noticia IS 'Tipo de noticia: cuadrada (grid 2x2), carrusel (rotativo vertical), comunicacion (alertas del sistema)';

-- Actualizar noticias existentes destacadas como cuadradas por defecto
UPDATE noticias
SET tipo_noticia = 'cuadrada'
WHERE es_destacada = true AND tipo_noticia IS NULL;

-- Actualizar noticias existentes no destacadas como carrusel por defecto
UPDATE noticias
SET tipo_noticia = 'carrusel'
WHERE es_destacada = false AND tipo_noticia IS NULL;

-- Resultado
SELECT 'Campo tipo_noticia agregado correctamente' AS resultado;

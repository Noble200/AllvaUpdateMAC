-- =============================================
-- Script para crear la tabla de Noticias
-- Sistema: AllvaSystem
-- Descripción: Tabla para gestionar noticias del Front Office
-- =============================================

-- Crear la tabla noticias si no existe
CREATE TABLE IF NOT EXISTS noticias (
    id SERIAL PRIMARY KEY,
    titulo VARCHAR(255) NOT NULL,
    categoria VARCHAR(100),
    descripcion TEXT,
    contenido TEXT,
    imagen_url VARCHAR(500),
    imagen_data BYTEA,
    -- Campos para el recorte/encuadre de imagen
    imagen_crop_x DOUBLE PRECISION DEFAULT 0,
    imagen_crop_y DOUBLE PRECISION DEFAULT 0,
    imagen_crop_width DOUBLE PRECISION DEFAULT 0,
    imagen_crop_height DOUBLE PRECISION DEFAULT 0,
    estado VARCHAR(20) DEFAULT 'Activa',
    es_destacada BOOLEAN DEFAULT false,
    orden INTEGER DEFAULT 1,
    fecha_publicacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    fecha_modificacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Crear índices para mejorar el rendimiento
CREATE INDEX IF NOT EXISTS idx_noticias_estado ON noticias(estado);
CREATE INDEX IF NOT EXISTS idx_noticias_destacada ON noticias(es_destacada);
CREATE INDEX IF NOT EXISTS idx_noticias_fecha_pub ON noticias(fecha_publicacion DESC);
CREATE INDEX IF NOT EXISTS idx_noticias_orden ON noticias(orden);

-- Insertar noticias de ejemplo (opcional - comentar si no se desea)
INSERT INTO noticias (titulo, categoria, descripcion, contenido, imagen_url, estado, es_destacada, orden, fecha_publicacion)
VALUES
    ('Titulo de la publicación 1',
     'Fri Jun 19 2020 | Category',
     'Sample small text. Lorem ipsum dolor sit amet.',
     'Contenido completo de la noticia 1. Lorem ipsum dolor sit amet, consectetur adipiscing elit.',
     '',
     'Activa',
     true,
     1,
     CURRENT_TIMESTAMP),

    ('Titulo de la publicación 2',
     'Fri Jun 19 2020 | Category',
     'Sample small text. Lorem ipsum dolor sit amet.',
     'Contenido completo de la noticia 2. Lorem ipsum dolor sit amet, consectetur adipiscing elit.',
     '',
     'Activa',
     true,
     2,
     CURRENT_TIMESTAMP),

    ('Titulo de la publicación 3',
     'Fri Jun 19 2020 | Category',
     'Sample small text. Lorem ipsum dolor sit amet.',
     'Contenido completo de la noticia 3. Lorem ipsum dolor sit amet, consectetur adipiscing elit.',
     '',
     'Activa',
     true,
     3,
     CURRENT_TIMESTAMP),

    ('Titulo de la publicación 4',
     'Fri Jun 19 2020 | Category',
     'Sample small text. Lorem ipsum dolor sit amet.',
     'Contenido completo de la noticia 4. Lorem ipsum dolor sit amet, consectetur adipiscing elit.',
     '',
     'Activa',
     true,
     4,
     CURRENT_TIMESTAMP),

    ('Noticia adicional 1',
     'Mon Jul 20 2020 | News',
     'Descripción de noticia adicional.',
     'Contenido de noticia adicional.',
     '',
     'Activa',
     false,
     5,
     CURRENT_TIMESTAMP),

    ('Noticia adicional 2',
     'Tue Jul 21 2020 | Updates',
     'Otra noticia interesante.',
     'Más contenido relevante.',
     '',
     'Activa',
     false,
     6,
     CURRENT_TIMESTAMP)
ON CONFLICT DO NOTHING;

-- Comentarios en la tabla
COMMENT ON TABLE noticias IS 'Tabla para almacenar las noticias del Front Office';
COMMENT ON COLUMN noticias.id IS 'Identificador único de la noticia';
COMMENT ON COLUMN noticias.titulo IS 'Título de la noticia';
COMMENT ON COLUMN noticias.categoria IS 'Categoría o etiqueta de la noticia (ej: fecha | categoría)';
COMMENT ON COLUMN noticias.descripcion IS 'Descripción breve para la vista previa';
COMMENT ON COLUMN noticias.contenido IS 'Contenido completo de la noticia';
COMMENT ON COLUMN noticias.imagen_url IS 'URL o ruta de la imagen de la noticia';
COMMENT ON COLUMN noticias.imagen_data IS 'Datos binarios de la imagen (opcional)';
COMMENT ON COLUMN noticias.estado IS 'Estado de la noticia: Activa, Inactiva, Borrador';
COMMENT ON COLUMN noticias.es_destacada IS 'Indica si la noticia aparece en la sección destacada (máximo 4)';
COMMENT ON COLUMN noticias.orden IS 'Orden de visualización de la noticia';
COMMENT ON COLUMN noticias.fecha_publicacion IS 'Fecha de publicación de la noticia';
COMMENT ON COLUMN noticias.fecha_creacion IS 'Fecha de creación del registro';
COMMENT ON COLUMN noticias.fecha_modificacion IS 'Fecha de última modificación';

-- =============================================
-- MÓDULO BILLETES DE AVIÓN - Tablas y datos de prueba
-- =============================================

-- Tabla de aeropuertos
CREATE TABLE IF NOT EXISTS aeropuertos (
    id SERIAL PRIMARY KEY,
    codigo VARCHAR(10) NOT NULL UNIQUE,
    ciudad VARCHAR(100) NOT NULL,
    pais VARCHAR(100) NOT NULL,
    nombre_aeropuerto VARCHAR(255) NOT NULL
);

-- Tabla de vuelos disponibles
CREATE TABLE IF NOT EXISTS vuelos (
    id SERIAL PRIMARY KEY,
    origen_codigo VARCHAR(10) NOT NULL REFERENCES aeropuertos(codigo),
    destino_codigo VARCHAR(10) NOT NULL REFERENCES aeropuertos(codigo),
    aerolinea VARCHAR(100) NOT NULL,
    numero_vuelo VARCHAR(20) NOT NULL,
    hora_salida VARCHAR(10) NOT NULL,
    hora_llegada VARCHAR(10) NOT NULL,
    duracion VARCHAR(30) NOT NULL,
    escalas INTEGER DEFAULT 0,
    ciudad_escala VARCHAR(100),
    tiempo_conexion VARCHAR(30),
    terminal_origen VARCHAR(50),
    terminal_destino VARCHAR(50),
    avion VARCHAR(100),
    precio_turista DECIMAL(10,2),
    precio_turista_premium DECIMAL(10,2),
    precio_business DECIMAL(10,2),
    disponible_turista BOOLEAN DEFAULT true,
    disponible_premium BOOLEAN DEFAULT false,
    disponible_business BOOLEAN DEFAULT false,
    equipaje_incluido VARCHAR(50) DEFAULT '23kg',
    estado VARCHAR(20) DEFAULT 'Activo',
    fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tabla de precios por día para el calendario
CREATE TABLE IF NOT EXISTS precios_calendario (
    id SERIAL PRIMARY KEY,
    origen_codigo VARCHAR(10) NOT NULL,
    destino_codigo VARCHAR(10) NOT NULL,
    fecha DATE NOT NULL,
    precio_minimo DECIMAL(10,2) NOT NULL
);

-- Índices
CREATE INDEX IF NOT EXISTS idx_vuelos_ruta ON vuelos(origen_codigo, destino_codigo);
CREATE INDEX IF NOT EXISTS idx_vuelos_estado ON vuelos(estado);
CREATE INDEX IF NOT EXISTS idx_precios_cal_ruta ON precios_calendario(origen_codigo, destino_codigo);
CREATE INDEX IF NOT EXISTS idx_precios_cal_fecha ON precios_calendario(fecha);

-- =============================================
-- DATOS DE PRUEBA: Aeropuertos
-- =============================================
INSERT INTO aeropuertos (codigo, ciudad, pais, nombre_aeropuerto) VALUES
    ('BCN', 'Barcelona', 'España', 'Barcelona-El Prat'),
    ('MAD', 'Madrid', 'España', 'Madrid Adolfo Suárez-Barajas'),
    ('SDQ', 'Santo Domingo', 'República Dominicana', 'Aeropuerto Internacional de Las Américas'),
    ('CDG', 'París', 'Francia', 'Charles de Gaulle'),
    ('LHR', 'Londres', 'Reino Unido', 'London Heathrow'),
    ('FCO', 'Roma', 'Italia', 'Leonardo da Vinci-Fiumicino'),
    ('MIA', 'Miami', 'Estados Unidos', 'Miami International Airport'),
    ('BOG', 'Bogotá', 'Colombia', 'Aeropuerto Internacional El Dorado'),
    ('JFK', 'Nueva York', 'Estados Unidos', 'John F. Kennedy International'),
    ('LIS', 'Lisboa', 'Portugal', 'Aeropuerto Humberto Delgado'),
    ('CUN', 'Cancún', 'México', 'Aeropuerto Internacional de Cancún'),
    ('GRU', 'São Paulo', 'Brasil', 'Aeroporto Internacional de Guarulhos')
ON CONFLICT (codigo) DO NOTHING;

-- =============================================
-- DATOS DE PRUEBA: Vuelos Barcelona - Santo Domingo
-- =============================================
INSERT INTO vuelos (origen_codigo, destino_codigo, aerolinea, numero_vuelo, hora_salida, hora_llegada, duracion, escalas, ciudad_escala, tiempo_conexion, terminal_origen, terminal_destino, avion, precio_turista, precio_turista_premium, precio_business, disponible_turista, disponible_premium, disponible_business) VALUES
    -- BCN → SDQ (con escala en MAD)
    ('BCN', 'SDQ', 'Iberia', 'IB428', '08:00', '12:35', '12h 35min', 1, 'Madrid', '2h 20min', 'Terminal 1', 'Terminal 4', 'Airbus A320', 3315.00, 4917.00, NULL, true, true, false),
    ('BCN', 'SDQ', 'Iberia', 'IB502', '10:00', '14:35', '12h 35min', 1, 'Madrid', '1h 45min', 'Terminal 1', 'Terminal 4', 'Airbus A321', 3315.00, NULL, NULL, true, false, false),
    ('BCN', 'SDQ', 'Air Europa', 'UX1024', '12:00', '16:50', '13h 50min', 1, 'Madrid', '2h 10min', 'Terminal 1', 'Terminal 2', 'Boeing 787', 4395.00, NULL, NULL, true, false, false),

    -- MAD → SDQ (directo)
    ('MAD', 'SDQ', 'Iberia', 'IB6500', '09:30', '14:00', '9h 30min', 0, NULL, NULL, 'Terminal 4', 'Terminal A', 'Airbus A330', 2890.00, 4200.00, 6500.00, true, true, true),
    ('MAD', 'SDQ', 'Air Europa', 'UX091', '11:00', '15:45', '9h 45min', 0, NULL, NULL, 'Terminal 2', 'Terminal A', 'Boeing 787-9', 2750.00, 3980.00, NULL, true, true, false),
    ('MAD', 'SDQ', 'Wamos Air', 'EB510', '22:00', '03:30', '10h 30min', 0, NULL, NULL, 'Terminal 1', 'Terminal A', 'Airbus A330-200', 2450.00, NULL, NULL, true, false, false),

    -- BCN → CDG (París)
    ('BCN', 'CDG', 'Vueling', 'VY8001', '07:15', '09:30', '2h 15min', 0, NULL, NULL, 'Terminal 1', 'Terminal 2E', 'Airbus A320', 89.00, 165.00, NULL, true, true, false),
    ('BCN', 'CDG', 'Air France', 'AF1149', '13:45', '16:00', '2h 15min', 0, NULL, NULL, 'Terminal 1', 'Terminal 2F', 'Airbus A319', 125.00, 245.00, 520.00, true, true, true),

    -- MAD → LHR (Londres)
    ('MAD', 'LHR', 'British Airways', 'BA461', '08:30', '10:15', '2h 45min', 0, NULL, NULL, 'Terminal 4S', 'Terminal 5', 'Airbus A320neo', 115.00, 280.00, 690.00, true, true, true),
    ('MAD', 'LHR', 'Iberia', 'IB3166', '15:00', '16:45', '2h 45min', 0, NULL, NULL, 'Terminal 4', 'Terminal 5', 'Airbus A321', 95.00, 210.00, 580.00, true, true, true),

    -- BCN → MIA
    ('BCN', 'MIA', 'American Airlines', 'AA67', '10:30', '16:00', '10h 30min', 0, NULL, NULL, 'Terminal 1', 'Terminal N', 'Boeing 777-200', 4200.00, 5800.00, 8900.00, true, true, true),
    ('BCN', 'MIA', 'Iberia', 'IB6123', '12:00', '21:30', '14h 30min', 1, 'Madrid', '3h 00min', 'Terminal 1', 'Terminal N', 'Airbus A350', 3600.00, 5200.00, NULL, true, true, false),

    -- MAD → BOG (Bogotá)
    ('MAD', 'BOG', 'Avianca', 'AV20', '09:00', '14:30', '10h 30min', 0, NULL, NULL, 'Terminal 4S', 'Terminal 1', 'Boeing 787-8', 3100.00, 4500.00, 7200.00, true, true, true),
    ('MAD', 'BOG', 'Iberia', 'IB6581', '23:30', '05:15', '10h 45min', 0, NULL, NULL, 'Terminal 4', 'Terminal 1', 'Airbus A330-300', 2950.00, 4300.00, 6800.00, true, true, true),

    -- BCN → FCO (Roma)
    ('BCN', 'FCO', 'Ryanair', 'FR5482', '06:30', '08:40', '2h 10min', 0, NULL, NULL, 'Terminal 2', 'Terminal 3', 'Boeing 737-800', 45.00, NULL, NULL, true, false, false),
    ('BCN', 'FCO', 'Vueling', 'VY6110', '14:20', '16:30', '2h 10min', 0, NULL, NULL, 'Terminal 1', 'Terminal 1', 'Airbus A320', 72.00, 145.00, NULL, true, true, false);

-- =============================================
-- DATOS DE PRUEBA: Precios calendario (BCN → SDQ, 2 meses)
-- =============================================
DO $$
DECLARE
    fecha_actual DATE := CURRENT_DATE;
    fecha_fin DATE := CURRENT_DATE + INTERVAL '60 days';
    precio_base DECIMAL(10,2);
    dia_semana INTEGER;
BEGIN
    WHILE fecha_actual <= fecha_fin LOOP
        dia_semana := EXTRACT(DOW FROM fecha_actual);
        -- Precios varían por día de semana
        CASE dia_semana
            WHEN 0 THEN precio_base := 1300.00;  -- Domingo
            WHEN 1 THEN precio_base := 1100.00;  -- Lunes
            WHEN 2 THEN precio_base := 1120.00;  -- Martes
            WHEN 3 THEN precio_base := 1105.00;  -- Miércoles
            WHEN 4 THEN precio_base := 1200.00;  -- Jueves
            WHEN 5 THEN precio_base := 1380.00;  -- Viernes
            WHEN 6 THEN precio_base := 1450.00;  -- Sábado
        END CASE;
        -- Añadir variación aleatoria ±15%
        precio_base := precio_base * (0.85 + random() * 0.30);

        INSERT INTO precios_calendario (origen_codigo, destino_codigo, fecha, precio_minimo)
        VALUES ('BCN', 'SDQ', fecha_actual, ROUND(precio_base, 2));

        fecha_actual := fecha_actual + INTERVAL '1 day';
    END LOOP;
END $$;

-- Precios calendario MAD → SDQ
DO $$
DECLARE
    fecha_actual DATE := CURRENT_DATE;
    fecha_fin DATE := CURRENT_DATE + INTERVAL '60 days';
    precio_base DECIMAL(10,2);
    dia_semana INTEGER;
BEGIN
    WHILE fecha_actual <= fecha_fin LOOP
        dia_semana := EXTRACT(DOW FROM fecha_actual);
        CASE dia_semana
            WHEN 0 THEN precio_base := 1100.00;
            WHEN 1 THEN precio_base := 950.00;
            WHEN 2 THEN precio_base := 980.00;
            WHEN 3 THEN precio_base := 920.00;
            WHEN 4 THEN precio_base := 1050.00;
            WHEN 5 THEN precio_base := 1200.00;
            WHEN 6 THEN precio_base := 1280.00;
        END CASE;
        precio_base := precio_base * (0.85 + random() * 0.30);

        INSERT INTO precios_calendario (origen_codigo, destino_codigo, fecha, precio_minimo)
        VALUES ('MAD', 'SDQ', fecha_actual, ROUND(precio_base, 2));

        fecha_actual := fecha_actual + INTERVAL '1 day';
    END LOOP;
END $$;

-- Precios calendario BCN → CDG (París, más baratos)
DO $$
DECLARE
    fecha_actual DATE := CURRENT_DATE;
    fecha_fin DATE := CURRENT_DATE + INTERVAL '60 days';
    precio_base DECIMAL(10,2);
    dia_semana INTEGER;
BEGIN
    WHILE fecha_actual <= fecha_fin LOOP
        dia_semana := EXTRACT(DOW FROM fecha_actual);
        CASE dia_semana
            WHEN 0 THEN precio_base := 95.00;
            WHEN 1 THEN precio_base := 72.00;
            WHEN 2 THEN precio_base := 68.00;
            WHEN 3 THEN precio_base := 75.00;
            WHEN 4 THEN precio_base := 85.00;
            WHEN 5 THEN precio_base := 110.00;
            WHEN 6 THEN precio_base := 120.00;
        END CASE;
        precio_base := precio_base * (0.85 + random() * 0.30);

        INSERT INTO precios_calendario (origen_codigo, destino_codigo, fecha, precio_minimo)
        VALUES ('BCN', 'CDG', fecha_actual, ROUND(precio_base, 2));

        fecha_actual := fecha_actual + INTERVAL '1 day';
    END LOOP;
END $$;

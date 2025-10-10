-- Script para configurar el brand bet30 con CORS apropiados
-- Ejecutar este script en la base de datos PostgreSQL

-- 1. Crear operador si no existe
INSERT INTO operators (id, name, status, created_at) 
VALUES (
    '11111111-1111-1111-1111-111111111111'::uuid, 
    'BET30 Operator', 
    'ACTIVE', 
    NOW()
) ON CONFLICT (id) DO NOTHING;

-- 2. Crear/actualizar brand bet30 con configuración de CORS
INSERT INTO brands (id, operator_id, code, name, domain, admin_domain, cors_origins, status, created_at) 
VALUES (
    '22222222-2222-2222-2222-222222222222'::uuid,
    '11111111-1111-1111-1111-111111111111'::uuid,
    'bet30',
    'BET30 Casino',
    'bet30.local',
    'admin.bet30.local',
    ARRAY[
        'http://localhost:5173',
        'http://localhost:3000',
        'http://admin.bet30.local:5173',
        'https://admin.bet30.local:5173',
        'http://bet30.local:5173',
        'https://bet30.local:5173',
        'http://127.0.0.1:5173',
        'http://localhost:7182',
        'https://localhost:7182',
        'http://admin.bet30.local:7182',
        'https://admin.bet30.local:7182'
    ],
    'ACTIVE',
    NOW()
) ON CONFLICT (code) DO UPDATE SET 
    domain = 'bet30.local',
    admin_domain = 'admin.bet30.local',
    cors_origins = ARRAY[
        'http://localhost:5173',
        'http://localhost:3000',
        'http://admin.bet30.local:5173',
        'https://admin.bet30.local:5173',
        'http://bet30.local:5173',
        'https://bet30.local:5173',
        'http://127.0.0.1:5173',
        'http://localhost:7182',
        'https://localhost:7182',
        'http://admin.bet30.local:7182',
        'https://admin.bet30.local:7182'
    ],
    status = 'ACTIVE';

-- 3. Verificar la configuración
SELECT 
    b.id,
    b.code,
    b.name,
    b.domain,
    b.admin_domain,
    b.cors_origins,
    b.status,
    o.name as operator_name
FROM brands b
LEFT JOIN operators o ON b.operator_id = o.id
WHERE b.code = 'bet30';

-- 4. Mostrar información de configuración
SELECT 
    'Brand configurado correctamente para:' as mensaje,
    domain as dominio_principal,
    admin_domain as dominio_admin,
    array_length(cors_origins, 1) as cantidad_cors_origins
FROM brands 
WHERE code = 'bet30';
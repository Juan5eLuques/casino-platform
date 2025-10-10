-- Script de verificación y corrección para CORS
-- Ejecutar este script para verificar y corregir la configuración del brand BET30

-- 1. Verificar la configuración actual del brand BET30
SELECT 
    'VERIFICACIÓN ACTUAL' as tipo,
    b.code as brand_code,
    b.domain,
    b.admin_domain,
    b.status::text,
    b.cors_origins,
    array_length(b.cors_origins, 1) as cors_count
FROM brands b 
WHERE b.code = 'bet30';

-- 2. Verificar si el operador existe
SELECT 
    'OPERADOR' as tipo,
    o.id::text as brand_code,
    o.name as domain,
    o.status::text as admin_domain,
    '' as status,
    null as cors_origins,
    0 as cors_count
FROM operators o 
WHERE o.id = '11111111-1111-1111-1111-111111111111'::uuid;

-- 3. Si el brand no existe o está mal configurado, ejecutar esta corrección:
-- (Descomenta las siguientes líneas si necesitas ejecutar la corrección)

/*
-- Crear operador si no existe
INSERT INTO operators (id, name, status, created_at) 
VALUES (
    '11111111-1111-1111-1111-111111111111'::uuid, 
    'BET30 Operator', 
    'ACTIVE', 
    NOW()
) ON CONFLICT (id) DO NOTHING;

-- Actualizar o crear brand BET30 con CORS específicos
INSERT INTO brands (id, operator_id, code, name, domain, admin_domain, cors_origins, status, created_at) 
VALUES (
    '22222222-2222-2222-2222-222222222222'::uuid,
    '11111111-1111-1111-1111-111111111111'::uuid,
    'bet30',
    'BET30 Casino',
    'bet30.local',
    'admin.bet30.local',
    ARRAY[
        'http://admin.bet30.local:5173',
        'https://admin.bet30.local:5173',
        'http://bet30.local:5173',
        'https://bet30.local:5173',
        'http://localhost:5173',
        'http://127.0.0.1:5173'
    ],
    'ACTIVE',
    NOW()
) ON CONFLICT (code) DO UPDATE SET 
    domain = 'bet30.local',
    admin_domain = 'admin.bet30.local',
    cors_origins = ARRAY[
        'http://admin.bet30.local:5173',
        'https://admin.bet30.local:5173',
        'http://bet30.local:5173',
        'https://bet30.local:5173',
        'http://localhost:5173',
        'http://127.0.0.1:5173'
    ],
    status = 'ACTIVE';
*/

-- 4. Verificar que el origin específico esté en la lista
SELECT 
    'VERIFICACIÓN CORS' as tipo,
    'http://admin.bet30.local:5173' as origin_buscado,
    CASE 
        WHEN 'http://admin.bet30.local:5173' = ANY(cors_origins) THEN 'SÍ ESTÁ'
        ELSE 'NO ESTÁ'
    END as esta_permitido,
    cors_origins
FROM brands 
WHERE code = 'bet30';

-- 5. Mostrar comandos SQL para ejecutar si hay problemas
SELECT 
    'Si el origin NO ESTÁ permitido, ejecuta este UPDATE:' as mensaje,
    $$ 
    UPDATE brands 
    SET cors_origins = ARRAY[
        'http://admin.bet30.local:5173',
        'https://admin.bet30.local:5173',
        'http://bet30.local:5173',
        'https://bet30.local:5173',
        'http://localhost:5173',
        'http://127.0.0.1:5173'
    ]
    WHERE code = 'bet30';
    $$ as comando_sql;
-- Script para configurar brand para desarrollo localhost
-- Ejecutar este script en la base de datos PostgreSQL

-- 1. Crear operador de desarrollo si no existe
INSERT INTO operators (id, name, status, created_at) 
VALUES (
    'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'::uuid, 
    'Development Operator', 
    'ACTIVE', 
    NOW()
) ON CONFLICT (id) DO NOTHING;

-- 2. Crear/actualizar brand para localhost
INSERT INTO brands (id, operator_id, code, name, domain, admin_domain, cors_origins, status, created_at) 
VALUES (
    'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'::uuid,
    'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'::uuid,
    'localhost-dev',
    'Development Casino',
    'localhost',
    'localhost',
    ARRAY[
        'http://localhost:5173',
        'http://localhost:3000',
        'http://127.0.0.1:5173',
        'http://127.0.0.1:3000',
        'https://localhost:5173',
        'https://localhost:7182'
    ],
    'ACTIVE',
    NOW()
) ON CONFLICT (code) DO UPDATE SET 
    domain = 'localhost',
    admin_domain = 'localhost',
    cors_origins = ARRAY[
        'http://localhost:5173',
        'http://localhost:3000',
        'http://127.0.0.1:5173',
        'http://127.0.0.1:3000',
        'https://localhost:5173',
        'https://localhost:7182'
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
WHERE b.code IN ('localhost-dev', 'bet30');

-- 4. Crear usuario admin de desarrollo
INSERT INTO backoffice_users (id, operator_id, username, password_hash, role, status, created_at) 
VALUES (
    gen_random_uuid(),
    'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'::uuid,
    'dev_admin',
    'AQAAAAEAACcQAAAAEInvCQI8gCJH+TQAyhX4w4X5m9hBGF1HY8D8X5vW7Cp2KqW0nL9pJ7yE6zF3uV8=', -- admin123
    'OPERATOR_ADMIN',
    'ACTIVE',
    NOW()
) ON CONFLICT (username) DO UPDATE SET 
    password_hash = 'AQAAAAEAACcQAAAAEInvCQI8gCJH+TQAyhX4w4X5m9hBGF1HY8D8X5vW7Cp2KqW0nL9pJ7yE6zF3uV8=',
    status = 'ACTIVE';

-- 5. Crear cajero de desarrollo
INSERT INTO backoffice_users (id, operator_id, username, password_hash, role, status, created_at) 
VALUES (
    gen_random_uuid(),
    'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'::uuid,
    'dev_cashier',
    'AQAAAAEAACcQAAAAEInvCQI8gCJH+TQAyhX4w4X5m9hBGF1HY8D8X5vW7Cp2KqW0nL9pJ7yE6zF3uV8=', -- admin123
    'CASHIER',
    'ACTIVE',
    NOW()
) ON CONFLICT (username) DO UPDATE SET 
    password_hash = 'AQAAAAEAACcQAAAAEInvCQI8gCJH+TQAyhX4w4X5m9hBGF1HY8D8X5vW7Cp2KqW0nL9pJ7yE6zF3uV8=',
    status = 'ACTIVE';

-- 6. Verificar usuarios creados
SELECT 
    u.username,
    u.role,
    u.status,
    o.name as operator_name
FROM backoffice_users u
LEFT JOIN operators o ON u.operator_id = o.id
WHERE u.username IN ('dev_admin', 'dev_cashier', 'juanse')
ORDER BY u.username;
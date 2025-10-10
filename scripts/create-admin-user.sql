-- Script para crear un usuario administrador nuevo
-- Ejecutar este script en la base de datos PostgreSQL

-- 1. Crear un operador de prueba (si no existe)
INSERT INTO operators (id, name, status, created_at) 
VALUES (
    '11111111-1111-1111-1111-111111111111'::uuid, 
    'Test Operator', 
    'ACTIVE', 
    NOW()
) ON CONFLICT (id) DO NOTHING;

-- 2. Crear usuario SUPER_ADMIN con password hasheado
-- Password: "admin123" - hasheado con ASP.NET Identity PasswordHasher
INSERT INTO backoffice_users (id, operator_id, username, password_hash, role, status, created_at) 
VALUES (
    gen_random_uuid(),
    '11111111-1111-1111-1111-111111111111'::uuid,
    'superadmin',
    'AQAAAAEAACcQAAAAEMxdHm+DJNYs1HvMYjxIzYYHSf5VZf5YmD5nJ8xH2v3wG7yF5qX9z8B4c6E2nM7pL1s=',
    'SUPER_ADMIN',
    'ACTIVE',
    NOW()
);

-- 3. Crear otro usuario OPERATOR_ADMIN para testing
INSERT INTO backoffice_users (id, operator_id, username, password_hash, role, status, created_at) 
VALUES (
    gen_random_uuid(),
    '11111111-1111-1111-1111-111111111111'::uuid,
    'operator_admin',
    'AQAAAAEAACcQAAAAEMxdHm+DJNYs1HvMYjxIzYYHSf5VZf5YmD5nJ8xH2v3wG7yF5qX9z8B4c6E2nM7pL1s=',
    'OPERATOR_ADMIN',
    'ACTIVE',
    NOW()
);

-- 4. Crear usuario CASHIER para testing
INSERT INTO backoffice_users (id, operator_id, username, password_hash, role, status, created_at) 
VALUES (
    gen_random_uuid(),
    '11111111-1111-1111-1111-111111111111'::uuid,
    'cashier_user',
    'AQAAAAEAACcQAAAAEMxdHm+DJNYs1HvMYjxIzYYHSf5VZf5YmD5nJ8xH2v3wG7yF5qX9z8B4c6E2nM7pL1s=',
    'CASHIER',
    'ACTIVE',
    NOW()
);

-- Verificar los usuarios creados
SELECT u.id, u.username, u.role, u.status, o.name as operator_name 
FROM backoffice_users u 
LEFT JOIN operators o ON u.operator_id = o.id 
WHERE u.username IN ('superadmin', 'operator_admin', 'cashier_user');

-- Información importante:
-- Todos los usuarios tienen la password: "admin123"
-- Para cambiar la password, usa el endpoint: POST /api/v1/admin/users/{userId}/password
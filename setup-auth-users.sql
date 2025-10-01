-- Script para crear usuarios de backoffice de ejemplo
-- Ejecutar después de aplicar migraciones

-- Verificar que exista el operador de ejemplo
INSERT INTO "Operators" ("Id", "Name", "Status", "CreatedAt") 
VALUES (
    '00000000-0000-0000-0000-000000000001'::uuid, 
    'Test Casino Group', 
    'ACTIVE', 
    NOW()
) ON CONFLICT ("Id") DO NOTHING;

-- Crear usuarios de backoffice de ejemplo
-- Contraseña para todos: "password123" (hasheada con BCrypt)
INSERT INTO "BackofficeUsers" ("Id", "OperatorId", "Username", "PasswordHash", "Role", "Status", "CreatedAt")
VALUES 
    -- Super Admin (acceso global)
    (
        'aaaaaaaa-bbbb-cccc-dddd-111111111111'::uuid,
        NULL, -- Super admin no está asociado a un operador específico
        'superadmin',
        '$2a$11$2vGHgA7EOsH8OO6RLZfWXOGq5P3OnQDmJLfxXL0jcmYcJwUY6YFOi', -- password123
        'SUPER_ADMIN',
        'ACTIVE',
        NOW()
    ),
    -- Operator Admin 1
    (
        'bbbbbbbb-cccc-dddd-eeee-222222222222'::uuid,
        '00000000-0000-0000-0000-000000000001'::uuid,
        'admin1',
        '$2a$11$2vGHgA7EOsH8OO6RLZfWXOGq5P3OnQDmJLfxXL0jcmYcJwUY6YFOi', -- password123
        'OPERATOR_ADMIN',
        'ACTIVE',
        NOW()
    ),
    -- Cashier 1
    (
        'cccccccc-dddd-eeee-ffff-333333333333'::uuid,
        '00000000-0000-0000-0000-000000000001'::uuid,
        'cashier1',
        '$2a$11$2vGHgA7EOsH8OO6RLZfWXOGq5P3OnQDmJLfxXL0jcmYcJwUY6YFOi', -- password123
        'CASHIER',
        'ACTIVE',
        NOW()
    ),
    -- Operator Admin 2 (otro operador hipotético)
    (
        'dddddddd-eeee-ffff-aaaa-444444444444'::uuid,
        '00000000-0000-0000-0000-000000000001'::uuid,
        'admin2',
        '$2a$11$2vGHgA7EOsH8OO6RLZfWXOGq5P3OnQDmJLfxXL0jcmYcJwUY6YFOi', -- password123
        'OPERATOR_ADMIN',
        'ACTIVE',
        NOW()
    )
ON CONFLICT ("Id") DO NOTHING;

-- Verificar la creación
SELECT 
    "Id",
    "Username", 
    "Role",
    "Status",
    CASE 
        WHEN "OperatorId" IS NULL THEN 'Global Access'
        ELSE "OperatorId"::text
    END as "OperatorScope",
    "CreatedAt"
FROM "BackofficeUsers"
ORDER BY "Role", "Username";

SELECT 'Backoffice users setup completed successfully!' as message;
SELECT 'Use password "password123" to login with any of these users:' as note;
SELECT 'superadmin, admin1, admin2, cashier1' as available_users;
-- =============================================
-- Setup LOCALHOST_DEV Brand for Development
-- =============================================
-- Este script configura el brand LOCALHOST_DEV para desarrollo local
-- El brand se comporta como cualquier otro brand con sus propios usuarios

-- =============================================
-- 1. Crear o Actualizar Brand LOCALHOST_DEV
-- =============================================

-- Eliminar brand existente si queremos recrearlo
-- DELETE FROM "Brands" WHERE "Code" = 'LOCALHOST_DEV';

-- Insertar o Actualizar brand LOCALHOST_DEV
INSERT INTO "Brands" (
    "Id", 
    "Code", 
    "Name", 
    "Locale", 
    "Domain", 
    "AdminDomain", 
    "CorsOrigins", 
    "Status", 
    "CreatedAt", 
    "UpdatedAt"
)
VALUES (
    gen_random_uuid(),
    'LOCALHOST_DEV',
    'Localhost Development',
    'en-US',
    'localhost',  -- Domain principal
    'localhost',  -- AdminDomain
    ARRAY[
        'http://localhost:5173',
        'http://localhost:5000',
        'http://localhost:3000',
        'http://127.0.0.1:5173',
        'http://127.0.0.1:5000',
        'http://127.0.0.1:3000'
    ]::TEXT[],
    'ACTIVE',
    NOW(),
    NOW()
)
ON CONFLICT ("Code") 
DO UPDATE SET
    "Name" = EXCLUDED."Name",
    "Domain" = EXCLUDED."Domain",
    "AdminDomain" = EXCLUDED."AdminDomain",
    "CorsOrigins" = EXCLUDED."CorsOrigins",
    "Status" = EXCLUDED."Status",
    "UpdatedAt" = NOW();

-- Obtener el ID del brand LOCALHOST_DEV para usarlo después
DO $$
DECLARE
    localhost_brand_id UUID;
    super_admin_id UUID;
    brand_admin_id UUID;
    cashier_id UUID;
BEGIN
    -- Obtener ID del brand
    SELECT "Id" INTO localhost_brand_id 
    FROM "Brands" 
    WHERE "Code" = 'LOCALHOST_DEV';
    
    RAISE NOTICE 'LOCALHOST_DEV Brand ID: %', localhost_brand_id;

    -- =============================================
    -- 2. Crear SUPER_ADMIN (sin brand específico)
    -- =============================================
    -- El SUPER_ADMIN puede acceder a TODOS los brands
    
    INSERT INTO "BackofficeUsers" (
        "Id",
        "BrandId",  -- NULL para SUPER_ADMIN (acceso global)
        "Username",
        "PasswordHash",  -- Password: "admin123"
        "Role",
        "Status",
        "WalletBalance",
        "CommissionPercent",
        "CreatedAt",
        "LastLoginAt"
    )
    VALUES (
        gen_random_uuid(),
        NULL,  -- Sin brand específico
        'superadmin',
        '$2a$11$YyK8gKVGLmQ5QqrN6Y1Nhu7xXEOCGQh5R6NXHLJBWvZJ3PqKXXZ.W',  -- admin123
        'SUPER_ADMIN',
        'ACTIVE',
        100000.00,
        0,
        NOW(),
        NULL
    )
    ON CONFLICT ("Username") 
    DO UPDATE SET
        "PasswordHash" = EXCLUDED."PasswordHash",
        "Status" = EXCLUDED."Status";

    -- =============================================
    -- 3. Crear BRAND_ADMIN para LOCALHOST_DEV
    -- =============================================
    
    INSERT INTO "BackofficeUsers" (
        "Id",
        "BrandId",  -- Asignado específicamente a LOCALHOST_DEV
        "Username",
        "PasswordHash",  -- Password: "admin123"
        "Role",
        "Status",
        "WalletBalance",
        "CommissionPercent",
        "CreatedAt",
        "CreatedByUserId",
        "CreatedByRole",
        "LastLoginAt"
    )
    VALUES (
        gen_random_uuid(),
        localhost_brand_id,
        'admin_localhost',
        '$2a$11$YyK8gKVGLmQ5QqrN6Y1Nhu7xXEOCGQh5R6NXHLJBWvZJ3PqKXXZ.W',  -- admin123
        'BRAND_ADMIN',
        'ACTIVE',
        50000.00,
        0,
        NOW(),
        NULL,  -- Creado manualmente (no por otro usuario)
        'SYSTEM',
        NULL
    )
    ON CONFLICT ("Username") 
    DO UPDATE SET
        "BrandId" = localhost_brand_id,
        "PasswordHash" = EXCLUDED."PasswordHash",
        "Status" = EXCLUDED."Status"
    RETURNING "Id" INTO brand_admin_id;

    RAISE NOTICE 'BRAND_ADMIN created: admin_localhost (ID: %)', brand_admin_id;

    -- =============================================
    -- 4. Crear CASHIER para LOCALHOST_DEV
    -- =============================================
    
    INSERT INTO "BackofficeUsers" (
        "Id",
        "BrandId",
        "Username",
        "PasswordHash",  -- Password: "cashier123"
        "Role",
        "Status",
        "WalletBalance",
        "CommissionPercent",
        "ParentCashierId",  -- NULL porque es cashier directo del brand
        "CreatedAt",
        "CreatedByUserId",
        "CreatedByRole",
        "LastLoginAt"
    )
    VALUES (
        gen_random_uuid(),
        localhost_brand_id,
        'cashier_localhost',
        '$2a$11$YyK8gKVGLmQ5QqrN6Y1Nhu7xXEOCGQh5R6NXHLJBWvZJ3PqKXXZ.W',  -- cashier123
        'CASHIER',
        'ACTIVE',
        10000.00,
        5.00,  -- 5% de comisión
        NULL,
        NOW(),
        brand_admin_id,  -- Creado por el BRAND_ADMIN
        'BRAND_ADMIN',
        NULL
    )
    ON CONFLICT ("Username") 
    DO UPDATE SET
        "BrandId" = localhost_brand_id,
        "PasswordHash" = EXCLUDED."PasswordHash",
        "Status" = EXCLUDED."Status",
        "CreatedByUserId" = brand_admin_id
    RETURNING "Id" INTO cashier_id;

    RAISE NOTICE 'CASHIER created: cashier_localhost (ID: %)', cashier_id;

    -- =============================================
    -- 5. Crear PLAYERS para LOCALHOST_DEV
    -- =============================================
    
    -- Player 1
    INSERT INTO "Players" (
        "Id",
        "BrandId",
        "Username",
        "Email",
        "Status",
        "WalletBalance",
        "CreatedAt",
        "CreatedByUserId",
        "LastLoginAt"
    )
    VALUES (
        gen_random_uuid(),
        localhost_brand_id,
        'player1_localhost',
        'player1@localhost.dev',
        'ACTIVE',
        1000.00,
        NOW(),
        cashier_id,  -- Creado por el cashier
        NULL
    )
    ON CONFLICT ("Username", "BrandId") 
    DO UPDATE SET
        "Status" = EXCLUDED."Status",
        "WalletBalance" = EXCLUDED."WalletBalance";

    -- Player 2
    INSERT INTO "Players" (
        "Id",
        "BrandId",
        "Username",
        "Email",
        "Status",
        "WalletBalance",
        "CreatedAt",
        "CreatedByUserId",
        "LastLoginAt"
    )
    VALUES (
        gen_random_uuid(),
        localhost_brand_id,
        'player2_localhost',
        'player2@localhost.dev',
        'ACTIVE',
        500.00,
        NOW(),
        cashier_id,
        NULL
    )
    ON CONFLICT ("Username", "BrandId") 
    DO UPDATE SET
        "Status" = EXCLUDED."Status";

    -- Player 3
    INSERT INTO "Players" (
        "Id",
        "BrandId",
        "Username",
        "Email",
        "Status",
        "WalletBalance",
        "CreatedAt",
        "CreatedByUserId",
        "LastLoginAt"
    )
    VALUES (
        gen_random_uuid(),
        localhost_brand_id,
        'player3_localhost',
        'player3@localhost.dev',
        'ACTIVE',
        2500.00,
        NOW(),
        brand_admin_id,  -- Creado directamente por BRAND_ADMIN
        NULL
    )
    ON CONFLICT ("Username", "BrandId") 
    DO UPDATE SET
        "Status" = EXCLUDED."Status";

    RAISE NOTICE 'Players created for LOCALHOST_DEV';

END $$;

-- =============================================
-- 6. Verificar la configuración
-- =============================================

-- Ver el brand creado
SELECT 
    "Id",
    "Code",
    "Name",
    "Domain",
    "AdminDomain",
    "Status"
FROM "Brands"
WHERE "Code" = 'LOCALHOST_DEV';

-- Ver usuarios de backoffice para LOCALHOST_DEV
SELECT 
    u."Id",
    u."Username",
    u."Role",
    u."Status",
    u."WalletBalance",
    b."Code" AS "BrandCode",
    u."CreatedByUserId",
    u."CreatedByRole"
FROM "BackofficeUsers" u
LEFT JOIN "Brands" b ON b."Id" = u."BrandId"
WHERE b."Code" = 'LOCALHOST_DEV' OR u."Username" = 'superadmin'
ORDER BY 
    CASE u."Role"
        WHEN 'SUPER_ADMIN' THEN 1
        WHEN 'BRAND_ADMIN' THEN 2
        WHEN 'CASHIER' THEN 3
    END;

-- Ver players para LOCALHOST_DEV
SELECT 
    p."Id",
    p."Username",
    p."Email",
    p."Status",
    p."WalletBalance",
    b."Code" AS "BrandCode",
    creator."Username" AS "CreatedByUsername"
FROM "Players" p
INNER JOIN "Brands" b ON b."Id" = p."BrandId"
LEFT JOIN "BackofficeUsers" creator ON creator."Id" = p."CreatedByUserId"
WHERE b."Code" = 'LOCALHOST_DEV'
ORDER BY p."CreatedAt";

-- =============================================
-- RESULTADO ESPERADO
-- =============================================
/*
USUARIOS CREADOS:

1. superadmin (SUPER_ADMIN) - Acceso global a todos los brands
   - Username: superadmin
   - Password: admin123
   - BrandId: NULL (acceso a todos)

2. admin_localhost (BRAND_ADMIN) - Admin del brand LOCALHOST_DEV
   - Username: admin_localhost
   - Password: admin123
   - BrandId: LOCALHOST_DEV

3. cashier_localhost (CASHIER) - Cajero del brand LOCALHOST_DEV
   - Username: cashier_localhost
   - Password: cashier123
   - BrandId: LOCALHOST_DEV
   - Comisión: 5%

4. player1_localhost (PLAYER)
   - Username: player1_localhost
   - BrandId: LOCALHOST_DEV
   - Creado por: cashier_localhost

5. player2_localhost (PLAYER)
   - Username: player2_localhost
   - BrandId: LOCALHOST_DEV
   - Creado por: cashier_localhost

6. player3_localhost (PLAYER)
   - Username: player3_localhost
   - BrandId: LOCALHOST_DEV
   - Creado por: admin_localhost

TESTING:

# Login superadmin (cualquier brand)
curl -X POST http://localhost:5000/api/v1/admin/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"superadmin","password":"admin123"}'

# Login admin_localhost (solo LOCALHOST_DEV)
curl -X POST http://localhost:5000/api/v1/admin/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin_localhost","password":"admin123"}'

# Login cashier_localhost (solo LOCALHOST_DEV)
curl -X POST http://localhost:5000/api/v1/admin/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"cashier_localhost","password":"cashier123"}'
*/

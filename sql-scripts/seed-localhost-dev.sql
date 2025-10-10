-- Seed script for localhost development environment
-- Creates a development brand and superadmin user with valid GUIDs

-- Step 1: Create localhost development brand
INSERT INTO "Brands" ("Id", "Code", "Name", "Locale", "Domain", "AdminDomain", "CorsOrigins", "Status", "CreatedAt", "UpdatedAt")
VALUES 
    ('11111111-1111-1111-1111-111111111111', 'LOCALHOST_DEV', 'Localhost Development Casino', 'en-US', 'localhost:3000', 'localhost:3001', 
     '["http://localhost:3000","https://localhost:3000","http://localhost:3001","https://localhost:3001","http://127.0.0.1:3000","https://127.0.0.1:3000","http://127.0.0.1:3001","https://127.0.0.1:3001"]', 
     'ACTIVE', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Step 2: Create superadmin user
-- Password hash for "hola1234" 
-- Using provided hash: AQAAAAEAACcQAAAAEKpIesLBYL3f0FDqkIis1PtyCJjXY1k0ZUrvP2L2dSj2eDk2F2GaYLCLF7njU9UgNw==
INSERT INTO "BackofficeUsers" ("Id", "BrandId", "Username", "PasswordHash", "Role", "Status", "CreatedAt", "ParentCashierId", "CommissionRate")
VALUES 
    ('22222222-2222-2222-2222-222222222222', NULL, 'superadmin', 'AQAAAAEAACcQAAAAEKpIesLBYL3f0FDqkIis1PtyCJjXY1k0ZUrvP2L2dSj2eDk2F2GaYLCLF7njU9UgNw==', 'SUPER_ADMIN', 'ACTIVE', CURRENT_TIMESTAMP, NULL, 0.00);

-- Step 3: Create some sample games for development
INSERT INTO "Games" ("Id", "Code", "Provider", "Name", "Enabled", "CreatedAt")
VALUES 
    ('33333333-3333-3333-3333-333333333333', 'DEV_SLOT_001', 'DEV_PROVIDER', 'Development Slot Machine', true, CURRENT_TIMESTAMP),
    ('44444444-4444-4444-4444-444444444444', 'DEV_BLACKJACK', 'DEV_PROVIDER', 'Development Blackjack', true, CURRENT_TIMESTAMP);

-- Step 4: Assign games to localhost brand
INSERT INTO "BrandGames" ("BrandId", "GameId", "Enabled", "DisplayOrder", "Tags")
VALUES 
    ('11111111-1111-1111-1111-111111111111', '33333333-3333-3333-3333-333333333333', true, 1, '["development","slots"]'),
    ('11111111-1111-1111-1111-111111111111', '44444444-4444-4444-4444-444444444444', true, 2, '["development","table"]');

-- Step 5: Create provider config for localhost brand
INSERT INTO "BrandProviderConfigs" ("BrandId", "ProviderCode", "Secret", "AllowNegativeOnRollback", "CreatedAt", "UpdatedAt")
VALUES 
    ('11111111-1111-1111-1111-111111111111', 'DEV_PROVIDER', 'localhost_dev_secret_key_12345', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Step 6: Create a development player for testing
INSERT INTO "Players" ("Id", "BrandId", "ExternalId", "Username", "Email", "Status", "CreatedAt")
VALUES 
    ('55555555-5555-5555-5555-555555555555', '11111111-1111-1111-1111-111111111111', 'dev_001', 'dev_player', 'dev@localhost.com', 'ACTIVE', CURRENT_TIMESTAMP);

-- Step 7: Create wallet for development player
INSERT INTO "Wallets" ("PlayerId", "BalanceBigint")
VALUES 
    ('55555555-5555-5555-5555-555555555555', 1000000); -- $10,000.00 for testing

-- Step 8: Create initial deposit in ledger
INSERT INTO "Ledger" ("BrandId", "PlayerId", "DeltaBigint", "Reason", "CreatedAt")
VALUES 
    ('11111111-1111-1111-1111-111111111111', '55555555-5555-5555-5555-555555555555', 1000000, 'INITIAL_DEPOSIT', CURRENT_TIMESTAMP);

-- Verification
SELECT 'LOCALHOST DEVELOPMENT SEED COMPLETED' as status;

SELECT 'Brand Created' as info, 
       "Code" as brand_code, 
       "Name" as brand_name, 
       "Domain" as domain,
       "AdminDomain" as admin_domain
FROM "Brands" 
WHERE "Code" = 'LOCALHOST_DEV';

SELECT 'SuperAdmin Created' as info,
       "Username" as username,
       "Role" as role,
       CASE WHEN "BrandId" IS NULL THEN 'ALL BRANDS' ELSE "BrandId"::text END as brand_access
FROM "BackofficeUsers" 
WHERE "Username" = 'superadmin';

SELECT 'CORS Origins for Localhost Brand' as info;
SELECT "CorsOrigins" as cors_config 
FROM "Brands" 
WHERE "Code" = 'LOCALHOST_DEV';

SELECT 'Ready for Development!' as message;
SELECT 'Use these credentials:' as info;
SELECT 'Username: superadmin' as credential1;
SELECT 'Password: hola1234' as credential2;
SELECT 'Brand Domain: localhost:3000' as domain;
SELECT 'Admin Domain: localhost:3001' as admin_domain;
-- Seed data for Brand-Only Casino Platform

-- Step 1: Create sample brands
INSERT INTO "Brands" ("Id", "Code", "Name", "Locale", "Domain", "AdminDomain", "CorsOrigins", "Status", "CreatedAt", "UpdatedAt")
VALUES 
    ('22222222-2222-2222-2222-222222222222', 'DEMO_BRAND', 'Demo Casino', 'en-US', 'demo.casino.com', 'admin-demo.casino.com', '["http://localhost:3000","https://demo.casino.com"]', 'ACTIVE', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('33333333-3333-3333-3333-333333333333', 'VIP_BRAND', 'VIP Casino', 'en-US', 'vip.casino.com', 'admin-vip.casino.com', '["http://localhost:3001","https://vip.casino.com"]', 'ACTIVE', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('44444444-4444-4444-4444-444444444444', 'EURO_BRAND', 'Euro Casino', 'es-ES', 'euro.casino.com', 'admin-euro.casino.com', '["http://localhost:3002","https://euro.casino.com"]', 'ACTIVE', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Step 2: Create backoffice users with new brand-based hierarchy
INSERT INTO "BackofficeUsers" ("Id", "BrandId", "Username", "PasswordHash", "Role", "Status", "CreatedAt", "ParentCashierId", "CommissionRate")
VALUES 
    -- SUPER_ADMIN (can access all brands)
    ('55555555-5555-5555-5555-555555555555', NULL, 'superadmin', '$2a$11$rOzJqjftrfEQ.aGYUyP6gOKqA8RKhOEqGq8p4cZEJ5x5n5n5n5n5n', 'SUPER_ADMIN', 'ACTIVE', CURRENT_TIMESTAMP, NULL, 0.00),
    
    -- BRAND_ADMIN for Demo Brand
    ('66666666-6666-6666-6666-666666666666', '22222222-2222-2222-2222-222222222222', 'demo_admin', '$2a$11$rOzJqjftrfEQ.aGYUyP6gOKqA8RKhOEqGq8p4cZEJ5x5n5n5n5n5n', 'BRAND_ADMIN', 'ACTIVE', CURRENT_TIMESTAMP, NULL, 0.00),
    
    -- BRAND_ADMIN for VIP Brand  
    ('77777777-7777-7777-7777-777777777777', '33333333-3333-3333-3333-333333333333', 'vip_admin', '$2a$11$rOzJqjftrfEQ.aGYUyP6gOKqA8RKhOEqGq8p4cZEJ5x5n5n5n5n5n', 'BRAND_ADMIN', 'ACTIVE', CURRENT_TIMESTAMP, NULL, 0.00),
    
    -- BRAND_ADMIN for Euro Brand
    ('88888888-8888-8888-8888-888888888888', '44444444-4444-4444-4444-444444444444', 'euro_admin', '$2a$11$rOzJqjftrfEQ.aGYUyP6gOKqA8RKhOEqGq8p4cZEJ5x5n5n5n5n5n', 'BRAND_ADMIN', 'ACTIVE', CURRENT_TIMESTAMP, NULL, 0.00),
    
    -- CASHIERs for Demo Brand
    ('99999999-9999-9999-9999-999999999999', '22222222-2222-2222-2222-222222222222', 'demo_cashier1', '$2a$11$rOzJqjftrfEQ.aGYUyP6gOKqA8RKhOEqGq8p4cZEJ5x5n5n5n5n5n', 'CASHIER', 'ACTIVE', CURRENT_TIMESTAMP, NULL, 5.00),
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '22222222-2222-2222-2222-222222222222', 'demo_cashier2', '$2a$11$rOzJqjftrfEQ.aGYUyP6gOKqA8RKhOEqGq8p4cZEJ5x5n5n5n5n5n', 'CASHIER', 'ACTIVE', CURRENT_TIMESTAMP, '99999999-9999-9999-9999-999999999999', 3.00),
    
    -- CASHIERs for VIP Brand
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '33333333-3333-3333-3333-333333333333', 'vip_cashier1', '$2a$11$rOzJqjftrfEQ.aGYUyP6gOKqA8RKhOEqGq8p4cZEJ5x5n5n5n5n5n', 'CASHIER', 'ACTIVE', CURRENT_TIMESTAMP, NULL, 7.50);

-- Step 3: Create some sample games
INSERT INTO "Games" ("Id", "Code", "Provider", "Name", "Enabled", "CreatedAt")
VALUES 
    ('11111111-1111-1111-1111-111111111111', 'SLOT_001', 'PRAGMATIC', 'Sweet Bonanza', true, CURRENT_TIMESTAMP),
    ('11111111-1111-1111-1111-111111111112', 'SLOT_002', 'PRAGMATIC', 'Gates of Olympus', true, CURRENT_TIMESTAMP),
    ('11111111-1111-1111-1111-111111111113', 'BLACKJACK_001', 'EVOLUTION', 'Classic Blackjack', true, CURRENT_TIMESTAMP),
    ('11111111-1111-1111-1111-111111111114', 'ROULETTE_001', 'EVOLUTION', 'European Roulette', true, CURRENT_TIMESTAMP);

-- Step 4: Assign games to brands
INSERT INTO "BrandGames" ("BrandId", "GameId", "Enabled", "DisplayOrder", "Tags")
VALUES 
    -- Demo Brand games
    ('22222222-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111111', true, 1, '["slots","popular"]'),
    ('22222222-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111112', true, 2, '["slots","featured"]'),
    ('22222222-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111113', true, 3, '["table","classic"]'),
    
    -- VIP Brand games (all games)
    ('33333333-3333-3333-3333-333333333333', '11111111-1111-1111-1111-111111111111', true, 1, '["slots","vip"]'),
    ('33333333-3333-3333-3333-333333333333', '11111111-1111-1111-1111-111111111112', true, 2, '["slots","vip"]'),
    ('33333333-3333-3333-3333-333333333333', '11111111-1111-1111-1111-111111111113', true, 3, '["table","vip"]'),
    ('33333333-3333-3333-3333-333333333333', '11111111-1111-1111-1111-111111111114', true, 4, '["table","vip"]'),
    
    -- Euro Brand games (limited selection)
    ('44444444-4444-4444-4444-444444444444', '11111111-1111-1111-1111-111111111114', true, 1, '["table","european"]');

-- Step 5: Create brand provider configurations
INSERT INTO "BrandProviderConfigs" ("BrandId", "ProviderCode", "Secret", "AllowNegativeOnRollback", "CreatedAt", "UpdatedAt")
VALUES 
    ('22222222-2222-2222-2222-222222222222', 'PRAGMATIC', 'demo_pragmatic_secret_key_123456789', false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('22222222-2222-2222-2222-222222222222', 'EVOLUTION', 'demo_evolution_secret_key_123456789', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    
    ('33333333-3333-3333-3333-333333333333', 'PRAGMATIC', 'vip_pragmatic_secret_key_987654321', false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('33333333-3333-3333-3333-333333333333', 'EVOLUTION', 'vip_evolution_secret_key_987654321', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    
    ('44444444-4444-4444-4444-444444444444', 'EVOLUTION', 'euro_evolution_secret_key_456789123', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Step 6: Create some sample players for testing
INSERT INTO "Players" ("Id", "BrandId", "ExternalId", "Username", "Email", "Status", "CreatedAt")
VALUES 
    -- Demo Brand players
    ('player01-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222', 'demo_ext_001', 'demo_player1', 'player1@demo.com', 'ACTIVE', CURRENT_TIMESTAMP),
    ('player02-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', 'demo_ext_002', 'demo_player2', 'player2@demo.com', 'ACTIVE', CURRENT_TIMESTAMP),
    
    -- VIP Brand players
    ('player03-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', 'vip_ext_001', 'vip_player1', 'vip1@vip.com', 'ACTIVE', CURRENT_TIMESTAMP),
    
    -- Euro Brand players
    ('player04-4444-4444-4444-444444444444', '44444444-4444-4444-4444-444444444444', 'euro_ext_001', 'euro_player1', 'player1@euro.com', 'ACTIVE', CURRENT_TIMESTAMP);

-- Step 7: Create wallets for players
INSERT INTO "Wallets" ("PlayerId", "BalanceBigint")
VALUES 
    ('player01-1111-1111-1111-111111111111', 100000), -- $1000.00 (assuming 2 decimal places)
    ('player02-2222-2222-2222-222222222222', 250000), -- $2500.00
    ('player03-3333-3333-3333-333333333333', 500000), -- $5000.00 (VIP player)
    ('player04-4444-4444-4444-444444444444', 150000); -- €1500.00

-- Step 8: Assign some players to cashiers
INSERT INTO "CashierPlayers" ("CashierId", "PlayerId", "AssignedAt")
VALUES 
    ('99999999-9999-9999-9999-999999999999', 'player01-1111-1111-1111-111111111111', CURRENT_TIMESTAMP),
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'player02-2222-2222-2222-222222222222', CURRENT_TIMESTAMP),
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'player03-3333-3333-3333-333333333333', CURRENT_TIMESTAMP);

-- Step 9: Create some sample ledger entries
INSERT INTO "Ledger" ("BrandId", "PlayerId", "DeltaBigint", "Reason", "CreatedAt")
VALUES 
    ('22222222-2222-2222-2222-222222222222', 'player01-1111-1111-1111-111111111111', 100000, 'DEPOSIT', CURRENT_TIMESTAMP),
    ('22222222-2222-2222-2222-222222222222', 'player02-2222-2222-2222-222222222222', 250000, 'DEPOSIT', CURRENT_TIMESTAMP),
    ('33333333-3333-3333-3333-333333333333', 'player03-3333-3333-3333-333333333333', 500000, 'DEPOSIT', CURRENT_TIMESTAMP),
    ('44444444-4444-4444-4444-444444444444', 'player04-4444-4444-4444-444444444444', 150000, 'DEPOSIT', CURRENT_TIMESTAMP);

-- Verification queries
SELECT 'SEED DATA SUMMARY' as status;

SELECT 'Brands' as entity, COUNT(*) as count FROM "Brands";
SELECT 'BackofficeUsers' as entity, COUNT(*) as count FROM "BackofficeUsers";
SELECT 'Games' as entity, COUNT(*) as count FROM "Games";
SELECT 'BrandGames' as entity, COUNT(*) as count FROM "BrandGames";
SELECT 'Players' as entity, COUNT(*) as count FROM "Players";
SELECT 'Wallets' as entity, COUNT(*) as count FROM "Wallets";

SELECT 'Users by Role and Brand' as category;
SELECT 
    bu.Role,
    COALESCE(b.Name, 'ALL BRANDS (SUPER_ADMIN)') as BrandName,
    COUNT(*) as UserCount
FROM "BackofficeUsers" bu
LEFT JOIN "Brands" b ON bu.BrandId = b.Id
GROUP BY bu.Role, b.Name
ORDER BY bu.Role, b.Name;

SELECT 'SEED DATA COMPLETED SUCCESSFULLY' as result;
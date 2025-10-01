-- Script de ejemplo para configurar brands con soporte multi-site
-- Ejecutar después de tener la base de datos migrada

-- Insertar un operador de ejemplo
INSERT INTO "Operators" ("Id", "Name", "Status", "CreatedAt") 
VALUES (
    '00000000-0000-0000-0000-000000000001'::uuid, 
    'Test Casino Group', 
    'ACTIVE', 
    NOW()
) ON CONFLICT ("Id") DO NOTHING;

-- Insertar brands con dominios configurados
INSERT INTO "Brands" ("Id", "OperatorId", "Code", "Name", "Locale", "Domain", "AdminDomain", "CorsOrigins", "Status", "CreatedAt")
VALUES 
    -- Brand 1: Casino Local
    (
        '11111111-1111-1111-1111-111111111111'::uuid,
        '00000000-0000-0000-0000-000000000001'::uuid,
        'LOCAL',
        'Local Casino',
        'es-ES',
        'localhost:3000',
        'admin.localhost:3000',
        'http://localhost:3000,http://localhost:3001,http://127.0.0.1:3000',
        'ACTIVE',
        NOW()
    ),
    -- Brand 2: Casino de Prueba
    (
        '22222222-2222-2222-2222-222222222222'::uuid,
        '00000000-0000-0000-0000-000000000001'::uuid,
        'TEST',
        'Test Casino',
        'en-US',
        'test.casino.com',
        'admin.test.casino.com',
        'https://test.casino.com,https://www.test.casino.com',
        'ACTIVE',
        NOW()
    ),
    -- Brand 3: Netlify Deploy
    (
        '33333333-3333-3333-3333-333333333333'::uuid,
        '00000000-0000-0000-0000-000000000001'::uuid,
        'BET30',
        'Bet30 Casino',
        'es-ES',
        'bet30test.netlify.app',
        'admin.bet30test.netlify.app',
        'https://bet30test.netlify.app,https://admin.bet30test.netlify.app',
        'ACTIVE',
        NOW()
    )
ON CONFLICT ("Id") DO NOTHING;

-- Crear algunos juegos de ejemplo
INSERT INTO "Games" ("Id", "Code", "Provider", "Name", "Enabled", "CreatedAt")
VALUES 
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'::uuid, 'SLOTS_001', 'demo', 'Magic Slots', true, NOW()),
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'::uuid, 'POKER_001', 'demo', 'Texas Poker', true, NOW()),
    ('cccccccc-cccc-cccc-cccc-cccccccccccc'::uuid, 'ROULETTE_001', 'pragmatic', 'European Roulette', true, NOW())
ON CONFLICT ("Id") DO NOTHING;

-- Asignar juegos a brands
INSERT INTO "BrandGames" ("BrandId", "GameId", "Enabled", "DisplayOrder", "Tags")
VALUES 
    -- Local Casino gets all games
    ('11111111-1111-1111-1111-111111111111'::uuid, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'::uuid, true, 1, 'slots,popular'),
    ('11111111-1111-1111-1111-111111111111'::uuid, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'::uuid, true, 2, 'poker,table'),
    ('11111111-1111-1111-1111-111111111111'::uuid, 'cccccccc-cccc-cccc-cccc-cccccccccccc'::uuid, true, 3, 'roulette,table'),
    
    -- Test Casino gets only slots and roulette
    ('22222222-2222-2222-2222-222222222222'::uuid, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'::uuid, true, 1, 'slots,featured'),
    ('22222222-2222-2222-2222-222222222222'::uuid, 'cccccccc-cccc-cccc-cccc-cccccccccccc'::uuid, true, 2, 'roulette,premium'),
    
    -- Bet30 Casino gets only slots
    ('33333333-3333-3333-3333-333333333333'::uuid, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'::uuid, true, 1, 'slots,exclusive')
ON CONFLICT ("BrandId", "GameId") DO NOTHING;

-- Crear jugadores de ejemplo para cada brand
INSERT INTO "Players" ("Id", "BrandId", "Username", "Email", "Status", "CreatedAt")
VALUES 
    -- Local Casino players
    ('d1111111-1111-1111-1111-111111111111'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 'player1_local', 'player1@local.test', 'ACTIVE', NOW()),
    ('d2222222-2222-2222-2222-222222222222'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 'player2_local', 'player2@local.test', 'ACTIVE', NOW()),
    
    -- Test Casino players
    ('d3333333-3333-3333-3333-333333333333'::uuid, '22222222-2222-2222-2222-222222222222'::uuid, 'player1_test', 'player1@test.casino.com', 'ACTIVE', NOW()),
    
    -- Bet30 Casino players
    ('d4444444-4444-4444-4444-444444444444'::uuid, '33333333-3333-3333-3333-333333333333'::uuid, 'player1_bet30', 'player1@bet30.com', 'ACTIVE', NOW())
ON CONFLICT ("Id") DO NOTHING;

-- Crear wallets para los jugadores con saldo inicial
INSERT INTO "Wallets" ("PlayerId", "BalanceBigint")
VALUES 
    ('d1111111-1111-1111-1111-111111111111'::uuid, 100000), -- 1000.00 chips
    ('d2222222-2222-2222-2222-222222222222'::uuid, 50000),  -- 500.00 chips
    ('d3333333-3333-3333-3333-333333333333'::uuid, 75000),  -- 750.00 chips
    ('d4444444-4444-4444-4444-444444444444'::uuid, 200000)  -- 2000.00 chips
ON CONFLICT ("PlayerId") DO NOTHING;

-- Crear entradas de ledger inicial para los saldos
INSERT INTO "Ledger" ("OperatorId", "BrandId", "PlayerId", "DeltaBigint", "Reason", "ExternalRef", "CreatedAt")
VALUES 
    ('00000000-0000-0000-0000-000000000001'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 'd1111111-1111-1111-1111-111111111111'::uuid, 100000, 'ADMIN_GRANT', 'INITIAL_BALANCE_1', NOW()),
    ('00000000-0000-0000-0000-000000000001'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 'd2222222-2222-2222-2222-222222222222'::uuid, 50000, 'ADMIN_GRANT', 'INITIAL_BALANCE_2', NOW()),
    ('00000000-0000-0000-0000-000000000001'::uuid, '22222222-2222-2222-2222-222222222222'::uuid, 'd3333333-3333-3333-3333-333333333333'::uuid, 75000, 'ADMIN_GRANT', 'INITIAL_BALANCE_3', NOW()),
    ('00000000-0000-0000-0000-000000000001'::uuid, '33333333-3333-3333-3333-333333333333'::uuid, 'd4444444-4444-4444-4444-444444444444'::uuid, 200000, 'ADMIN_GRANT', 'INITIAL_BALANCE_4', NOW())
ON CONFLICT ("ExternalRef") DO NOTHING;

SELECT 'Multi-site setup completed successfully!' as message;
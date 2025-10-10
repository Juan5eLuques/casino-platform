-- Script para configurar brands con soporte localhost para desarrollo
-- Ejecutar después de tener la base de datos migrada

-- Limpiar datos existentes si es necesario (opcional)
-- DELETE FROM "BrandGames";
-- DELETE FROM "Wallets";
-- DELETE FROM "Players";
-- DELETE FROM "Brands";
-- DELETE FROM "Operators";

-- Insertar un operador de ejemplo
INSERT INTO "Operators" ("Id", "Name", "Status", "CreatedAt") 
VALUES (
    '00000000-0000-0000-0000-000000000001'::uuid, 
    'Local Development Casino Group', 
    'ACTIVE', 
    NOW()
) ON CONFLICT ("Id") DO NOTHING;

-- Insertar brands con dominios localhost para desarrollo
INSERT INTO "Brands" ("Id", "OperatorId", "Code", "Name", "Locale", "Domain", "AdminDomain", "CorsOrigins", "Status", "CreatedAt")
VALUES 
    -- Brand para desarrollo local - Admin
    (
        '11111111-1111-1111-1111-111111111111'::uuid,
        '00000000-0000-0000-0000-000000000001'::uuid,
        'LOCALHOST',
        'Local Development Casino',
        'es-ES',
        'localhost:3000',
        'localhost:5173',
        ARRAY['http://localhost:5173', 'https://localhost:5173', 'http://localhost:3000', 'https://localhost:3000', 'http://127.0.0.1:5173', 'https://127.0.0.1:5173']::text[],
        'ACTIVE',
        NOW()
    ),
    -- Brand para testing con dominios personalizados (mantener para referencia)
    (
        '22222222-2222-2222-2222-222222222222'::uuid,
        '00000000-0000-0000-0000-000000000001'::uuid,
        'BET30',
        'Bet30 Casino',
        'es-ES',
        'bet30.local',
        'admin.bet30.local',
        ARRAY['http://admin.bet30.local:5173', 'https://admin.bet30.local:5173', 'http://bet30.local:3000', 'https://bet30.local:3000']::text[],
        'ACTIVE',
        NOW()
    )
ON CONFLICT ("Id") DO UPDATE SET
    "Domain" = EXCLUDED."Domain",
    "AdminDomain" = EXCLUDED."AdminDomain",
    "CorsOrigins" = EXCLUDED."CorsOrigins";

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
    -- Local Development Casino gets all games
    ('11111111-1111-1111-1111-111111111111'::uuid, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'::uuid, true, 1, ARRAY['slots', 'popular']::text[]),
    ('11111111-1111-1111-1111-111111111111'::uuid, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'::uuid, true, 2, ARRAY['poker', 'table']::text[]),
    ('11111111-1111-1111-1111-111111111111'::uuid, 'cccccccc-cccc-cccc-cccc-cccccccccccc'::uuid, true, 3, ARRAY['roulette', 'table']::text[]),
    
    -- Bet30 Casino gets all games too
    ('22222222-2222-2222-2222-222222222222'::uuid, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'::uuid, true, 1, ARRAY['slots', 'featured']::text[]),
    ('22222222-2222-2222-2222-222222222222'::uuid, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'::uuid, true, 2, ARRAY['poker', 'premium']::text[]),
    ('22222222-2222-2222-2222-222222222222'::uuid, 'cccccccc-cccc-cccc-cccc-cccccccccccc'::uuid, true, 3, ARRAY['roulette', 'premium']::text[])
ON CONFLICT ("BrandId", "GameId") DO NOTHING;

-- Crear jugadores de ejemplo para cada brand
INSERT INTO "Players" ("Id", "BrandId", "Username", "Email", "Status", "CreatedAt")
VALUES 
    -- Local Development Casino players
    ('d1111111-1111-1111-1111-111111111111'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 'player1_local', 'player1@localhost.test', 'ACTIVE', NOW()),
    ('d2222222-2222-2222-2222-222222222222'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 'player2_local', 'player2@localhost.test', 'ACTIVE', NOW()),
    
    -- Bet30 Casino players
    ('d3333333-3333-3333-3333-333333333333'::uuid, '22222222-2222-2222-2222-222222222222'::uuid, 'player1_bet30', 'player1@bet30.local', 'ACTIVE', NOW())
ON CONFLICT ("Id") DO NOTHING;

-- Crear wallets para los jugadores con saldo inicial
INSERT INTO "Wallets" ("PlayerId", "BalanceBigint")
VALUES 
    ('d1111111-1111-1111-1111-111111111111'::uuid, 100000), -- 1000.00 chips
    ('d2222222-2222-2222-2222-222222222222'::uuid, 50000),  -- 500.00 chips
    ('d3333333-3333-3333-3333-333333333333'::uuid, 75000)   -- 750.00 chips
ON CONFLICT ("PlayerId") DO NOTHING;

-- Verificar la configuración
SELECT 'Operators' as table_name, COUNT(*) as count FROM "Operators"
UNION ALL
SELECT 'Brands', COUNT(*) FROM "Brands"
UNION ALL
SELECT 'Games', COUNT(*) FROM "Games"
UNION ALL
SELECT 'BrandGames', COUNT(*) FROM "BrandGames"
UNION ALL
SELECT 'Players', COUNT(*) FROM "Players"
UNION ALL
SELECT 'Wallets', COUNT(*) FROM "Wallets";

-- Mostrar brands configurados
SELECT "Code", "Name", "Domain", "AdminDomain", "CorsOrigins", "Status"
FROM "Brands"
ORDER BY "Code";
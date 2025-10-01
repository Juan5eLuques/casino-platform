-- Script para poblar datos de ejemplo para Brand Provider Config
-- Ejecutar después de aplicar las migraciones

-- Actualizar configuraciones de brand existentes con settings y CORS
UPDATE "Brands" 
SET 
    "Settings" = '{"maxBetLimit": 10000, "currency": "USD", "theme": "dark", "language": "en"}',
    "CorsOrigins" = 'http://localhost:3000,https://localhost:3001,https://bet30test.netlify.app',
    "UpdatedAt" = NOW()
WHERE "Code" = 'LOCAL';

UPDATE "Brands" 
SET 
    "Settings" = '{"maxBetLimit": 5000, "currency": "EUR", "theme": "light", "language": "es"}',
    "CorsOrigins" = 'https://test.casino.com,https://www.test.casino.com',
    "UpdatedAt" = NOW()
WHERE "Code" = 'TEST';

UPDATE "Brands" 
SET 
    "Settings" = '{"maxBetLimit": 20000, "currency": "USD", "theme": "premium", "language": "en"}',
    "CorsOrigins" = 'https://bet30test.netlify.app,https://admin.bet30test.netlify.app',
    "UpdatedAt" = NOW()
WHERE "Code" = 'BET30';

-- Insertar configuraciones de proveedores para cada brand
INSERT INTO "BrandProviderConfigs" ("BrandId", "ProviderCode", "Secret", "AllowNegativeOnRollback", "Meta", "CreatedAt", "UpdatedAt")
VALUES 
    -- LOCAL brand providers
    (
        (SELECT "Id" FROM "Brands" WHERE "Code" = 'LOCAL' LIMIT 1),
        'demo',
        'demo-secret-key-local-brand-12345',
        false,
        '{"apiUrl": "http://localhost:8080", "maxRetries": 3}',
        NOW(),
        NOW()
    ),
    (
        (SELECT "Id" FROM "Brands" WHERE "Code" = 'LOCAL' LIMIT 1),
        'pragmatic',
        'pragmatic-secret-key-local-abcd1234',
        true,
        '{"apiUrl": "https://api.pragmatic.local", "timeout": 30}',
        NOW(),
        NOW()
    ),
    
    -- TEST brand providers
    (
        (SELECT "Id" FROM "Brands" WHERE "Code" = 'TEST' LIMIT 1),
        'demo',
        'demo-secret-key-test-brand-67890',
        false,
        '{"apiUrl": "https://demo-api.test.com", "maxRetries": 5}',
        NOW(),
        NOW()
    ),
    (
        (SELECT "Id" FROM "Brands" WHERE "Code" = 'TEST' LIMIT 1),
        'evolution',
        'evolution-secret-key-test-efgh5678',
        false,
        '{"apiUrl": "https://api.evolution.test", "liveDealer": true}',
        NOW(),
        NOW()
    ),
    
    -- BET30 brand providers
    (
        (SELECT "Id" FROM "Brands" WHERE "Code" = 'BET30' LIMIT 1),
        'pragmatic',
        'pragmatic-secret-key-bet30-production',
        true,
        '{"apiUrl": "https://api.pragmaticplay.net", "jurisdiction": "MGA"}',
        NOW(),
        NOW()
    ),
    (
        (SELECT "Id" FROM "Brands" WHERE "Code" = 'BET30' LIMIT 1),
        'evolution',
        'evolution-secret-key-bet30-live-games',
        false,
        '{"apiUrl": "https://api.evolution.com", "liveDealer": true, "tables": ["blackjack", "roulette", "baccarat"]}',
        NOW(),
        NOW()
    )
ON CONFLICT ("BrandId", "ProviderCode") DO UPDATE SET
    "Secret" = EXCLUDED."Secret",
    "AllowNegativeOnRollback" = EXCLUDED."AllowNegativeOnRollback",
    "Meta" = EXCLUDED."Meta",
    "UpdatedAt" = NOW();

-- Verificar la configuración
SELECT 
    b."Code" as "BrandCode",
    b."Name" as "BrandName",
    b."Domain",
    b."Settings",
    bpc."ProviderCode",
    bpc."AllowNegativeOnRollback",
    bpc."Meta",
    bpc."CreatedAt" as "ProviderConfigCreated"
FROM "Brands" b
LEFT JOIN "BrandProviderConfigs" bpc ON b."Id" = bpc."BrandId"
ORDER BY b."Code", bpc."ProviderCode";

SELECT 'Brand Provider Configuration setup completed successfully!' as message;
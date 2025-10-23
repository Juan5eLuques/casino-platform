-- ================================================================================================
-- MIGRACIÓN: Sistema de Catálogo y Launch de Juegos
-- Versión: 1.0
-- Fecha: 2025-01-23
-- Descripción: Agrega tablas GameProviders, GameLaunchLogs y campos extendidos en Games
-- ================================================================================================

-- ================================================================================================
-- 1. CREAR TABLA GameProviders
-- ================================================================================================

CREATE TABLE IF NOT EXISTS "GameProviders" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "Code" varchar(50) UNIQUE NOT NULL,
    "Name" varchar(200) NOT NULL,
    "LaunchEndpointTemplate" text NOT NULL,
    "RequiresSessionToken" boolean DEFAULT true NOT NULL,
    "SupportsRealMode" boolean DEFAULT true NOT NULL,
    "SupportsDemoMode" boolean DEFAULT false NOT NULL,
    "DefaultMeta" jsonb,
  "Enabled" boolean DEFAULT true NOT NULL,
    "CreatedAt" timestamptz DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "UpdatedAt" timestamptz DEFAULT CURRENT_TIMESTAMP NOT NULL
);

-- Índices para GameProviders
CREATE INDEX IF NOT EXISTS "IX_GameProviders_Code" ON "GameProviders"("Code");
CREATE INDEX IF NOT EXISTS "IX_GameProviders_Enabled" ON "GameProviders"("Enabled") WHERE "Enabled" = true;

COMMENT ON TABLE "GameProviders" IS 'Proveedores de juegos externos (Pragmatic, Evolution, etc.)';
COMMENT ON COLUMN "GameProviders"."Code" IS 'Código único del proveedor (ej: pragmatic, evolution, mock)';
COMMENT ON COLUMN "GameProviders"."LaunchEndpointTemplate" IS 'Template del endpoint de launch con placeholders: {token}, {gameSymbol}, etc.';

-- ================================================================================================
-- 2. INSERTAR PROVEEDORES INICIALES
-- ================================================================================================

INSERT INTO "GameProviders" ("Code", "Name", "LaunchEndpointTemplate", "RequiresSessionToken", "SupportsRealMode", "SupportsDemoMode")
VALUES 
    ('mock', 'Mock Provider (Local)', 'https://demo.local/games/{gameCode}?session={session}&player={playerId}', true, true, true),
    ('pragmatic', 'Pragmatic Play', 'https://api.pragmaticplay.net/gs2c/openGame.do?gameSymbol={gameSymbol}&token={token}', true, true, true),
    ('evolution', 'Evolution Gaming', 'https://api.evolution.com/launch?game={game}&token={token}', true, true, false)
ON CONFLICT ("Code") DO NOTHING;

-- ================================================================================================
-- 3. EXTENDER TABLA Games CON CAMPOS NUEVOS
-- ================================================================================================

-- Agregar columna ProviderId (FK a GameProviders)
ALTER TABLE "Games" ADD COLUMN IF NOT EXISTS "ProviderId" uuid REFERENCES "GameProviders"("Id") ON DELETE SET NULL;

-- Agregar campos de catálogo
ALTER TABLE "Games" ADD COLUMN IF NOT EXISTS "LaunchId" varchar(200);
ALTER TABLE "Games" ADD COLUMN IF NOT EXISTS "RTP" decimal(5,2);
ALTER TABLE "Games" ADD COLUMN IF NOT EXISTS "Volatility" varchar(20);
ALTER TABLE "Games" ADD COLUMN IF NOT EXISTS "Category" varchar(50);
ALTER TABLE "Games" ADD COLUMN IF NOT EXISTS "ImageUrl" varchar(500);
ALTER TABLE "Games" ADD COLUMN IF NOT EXISTS "MinBet" decimal(18,2);
ALTER TABLE "Games" ADD COLUMN IF NOT EXISTS "MaxBet" decimal(18,2);
ALTER TABLE "Games" ADD COLUMN IF NOT EXISTS "IsFeatured" boolean DEFAULT false NOT NULL;
ALTER TABLE "Games" ADD COLUMN IF NOT EXISTS "IsNew" boolean DEFAULT false NOT NULL;
ALTER TABLE "Games" ADD COLUMN IF NOT EXISTS "AdditionalTags" text[];
ALTER TABLE "Games" ADD COLUMN IF NOT EXISTS "UpdatedAt" timestamptz DEFAULT CURRENT_TIMESTAMP;

-- Índices adicionales para Games
CREATE INDEX IF NOT EXISTS "IX_Games_ProviderId" ON "Games"("ProviderId");
CREATE INDEX IF NOT EXISTS "IX_Games_Category" ON "Games"("Category") WHERE "Category" IS NOT NULL;
CREATE INDEX IF NOT EXISTS "IX_Games_IsFeatured" ON "Games"("IsFeatured") WHERE "IsFeatured" = true;
CREATE INDEX IF NOT EXISTS "IX_Games_IsNew" ON "Games"("IsNew") WHERE "IsNew" = true;

COMMENT ON COLUMN "Games"."LaunchId" IS 'ID del juego en el sistema del proveedor (si es null, se usa Code)';
COMMENT ON COLUMN "Games"."RTP" IS 'Return to Player percentage (ej: 96.51)';
COMMENT ON COLUMN "Games"."Volatility" IS 'Volatilidad: LOW, MEDIUM, HIGH';
COMMENT ON COLUMN "Games"."Category" IS 'Categoría: slots, table, live, crash, etc.';

-- ================================================================================================
-- 4. ACTUALIZAR JUEGOS EXISTENTES CON ProviderId
-- ================================================================================================

-- Actualizar juegos existentes para vincularlos con su proveedor
UPDATE "Games" g
SET "ProviderId" = (
    SELECT p."Id" 
    FROM "GameProviders" p 
    WHERE LOWER(p."Code") = LOWER(g."Provider")
)
WHERE "ProviderId" IS NULL;

-- Ejemplo: actualizar un juego mock con metadata completa
UPDATE "Games"
SET 
    "LaunchId" = "Code",
    "RTP" = 96.50,
    "Volatility" = 'MEDIUM',
    "Category" = 'slots',
    "ImageUrl" = 'https://via.placeholder.com/300x200?text=' || "Name",
    "MinBet" = 0.10,
    "MaxBet" = 100.00,
    "IsFeatured" = false,
    "IsNew" = true,
    "AdditionalTags" = ARRAY['popular', 'featured'],
    "UpdatedAt" = CURRENT_TIMESTAMP
WHERE "LaunchId" IS NULL AND "Enabled" = true;

-- ================================================================================================
-- 5. CREAR TABLA GameLaunchLogs
-- ================================================================================================

CREATE TABLE IF NOT EXISTS "GameLaunchLogs" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
"SessionId" uuid NOT NULL REFERENCES "GameSessions"("Id") ON DELETE RESTRICT,
    "PlayerId" uuid NOT NULL REFERENCES "Players"("Id") ON DELETE RESTRICT,
    "GameId" uuid NOT NULL REFERENCES "Games"("Id") ON DELETE RESTRICT,
    "BrandId" uuid NOT NULL REFERENCES "Brands"("Id") ON DELETE RESTRICT,
    "Provider" varchar(50) NOT NULL,
    "LaunchUrl" text NOT NULL,
    "SessionToken" varchar(500) NOT NULL,
    "Success" boolean NOT NULL,
    "ErrorMessage" varchar(1000),
    "IpAddress" varchar(45),
    "UserAgent" varchar(500),
    "CreatedAt" timestamptz DEFAULT CURRENT_TIMESTAMP NOT NULL
);

-- Índices para GameLaunchLogs
CREATE INDEX IF NOT EXISTS "IX_GameLaunchLogs_SessionId" ON "GameLaunchLogs"("SessionId");
CREATE INDEX IF NOT EXISTS "IX_GameLaunchLogs_PlayerId" ON "GameLaunchLogs"("PlayerId");
CREATE INDEX IF NOT EXISTS "IX_GameLaunchLogs_GameId" ON "GameLaunchLogs"("GameId");
CREATE INDEX IF NOT EXISTS "IX_GameLaunchLogs_BrandId" ON "GameLaunchLogs"("BrandId");
CREATE INDEX IF NOT EXISTS "IX_GameLaunchLogs_CreatedAt" ON "GameLaunchLogs"("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_GameLaunchLogs_Provider_CreatedAt" ON "GameLaunchLogs"("Provider", "CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_GameLaunchLogs_Success" ON "GameLaunchLogs"("Success") WHERE "Success" = false;

COMMENT ON TABLE "GameLaunchLogs" IS 'Logs de auditoría de cada lanzamiento de juego';
COMMENT ON COLUMN "GameLaunchLogs"."SessionToken" IS 'Token de sesión generado para el proveedor (puede estar encriptado)';
COMMENT ON COLUMN "GameLaunchLogs"."LaunchUrl" IS 'URL completa generada para el iframe del juego';

-- ================================================================================================
-- 6. VERIFICACIONES Y VALIDACIONES
-- ================================================================================================

-- Verificar que la tabla GameProviders existe y tiene datos
DO $$
BEGIN
    RAISE NOTICE 'GameProviders count: %', (SELECT COUNT(*) FROM "GameProviders");
    RAISE NOTICE 'Games with ProviderId: %', (SELECT COUNT(*) FROM "Games" WHERE "ProviderId" IS NOT NULL);
    RAISE NOTICE 'GameLaunchLogs table created: %', (SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'GameLaunchLogs'));
END $$;

-- ================================================================================================
-- 7. DATOS DE EJEMPLO (OPCIONAL - SOLO PARA DESARROLLO)
-- ================================================================================================

-- Insertar un juego de ejemplo completo
INSERT INTO "Games" (
    "Code", 
    "Provider", 
    "Name", 
    "LaunchId", 
    "RTP", 
    "Volatility", 
 "Category", 
    "ImageUrl", 
    "MinBet", 
    "MaxBet", 
    "IsFeatured", 
    "IsNew", 
    "AdditionalTags", 
    "Enabled", 
    "ProviderId",
    "CreatedAt",
    "UpdatedAt"
)
VALUES (
    'sweet-bonanza-mock',
    'mock',
    'Sweet Bonanza (Mock)',
    'vs20sbxmas',
  96.51,
    'HIGH',
    'slots',
    'https://via.placeholder.com/300x200?text=Sweet+Bonanza',
    0.20,
    100.00,
 true,
  true,
 ARRAY['slots', 'featured', 'popular'],
    true,
    (SELECT "Id" FROM "GameProviders" WHERE "Code" = 'mock'),
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
)
ON CONFLICT ("Code") DO NOTHING;

-- ================================================================================================
-- RESUMEN DE MIGRACIÓN
-- ================================================================================================

DO $$
DECLARE
    providers_count int;
    games_count int;
    games_with_provider int;
    games_with_rtp int;
    launch_logs_exists boolean;
BEGIN
    SELECT COUNT(*) INTO providers_count FROM "GameProviders";
    SELECT COUNT(*) INTO games_count FROM "Games";
    SELECT COUNT(*) INTO games_with_provider FROM "Games" WHERE "ProviderId" IS NOT NULL;
    SELECT COUNT(*) INTO games_with_rtp FROM "Games" WHERE "RTP" IS NOT NULL;
    SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'GameLaunchLogs') INTO launch_logs_exists;
    
    RAISE NOTICE '========================================';
    RAISE NOTICE 'MIGRACIÓN COMPLETADA EXITOSAMENTE';
    RAISE NOTICE '========================================';
    RAISE NOTICE 'GameProviders creados: %', providers_count;
    RAISE NOTICE 'Juegos totales: %', games_count;
    RAISE NOTICE 'Juegos con ProviderId: %', games_with_provider;
    RAISE NOTICE 'Juegos con RTP configurado: %', games_with_rtp;
    RAISE NOTICE 'GameLaunchLogs creada: %', launch_logs_exists;
    RAISE NOTICE '========================================';
    
    IF providers_count = 0 THEN
        RAISE WARNING 'No hay proveedores registrados. Ejecutar inserts de proveedores.';
    END IF;
    
    IF games_with_provider < games_count THEN
        RAISE WARNING 'Algunos juegos no tienen ProviderId asignado. Ejecutar UPDATE de ProviderId.';
END IF;
END $$;

-- ================================================================================================
-- FIN DE MIGRACIÓN
-- ================================================================================================

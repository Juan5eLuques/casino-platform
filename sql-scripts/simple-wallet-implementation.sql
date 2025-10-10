-- =============================================================================
-- SCRIPT PARA IMPLEMENTAR SISTEMA SIMPLE DE WALLETS CON DECIMAL
-- Fecha: $(Get-Date)
-- Descripción: Agrega soporte de wallets decimales para todos los usuarios
-- =============================================================================

-- 1. AGREGAR CAMPOS DE WALLET DECIMAL A ENTIDADES EXISTENTES
-- ============================================================================

-- Agregar balance decimal a usuarios de backoffice
ALTER TABLE "BackofficeUsers" ADD COLUMN "WalletBalance" numeric(18,2) DEFAULT 0.00;

-- Agregar balance decimal a players (paralelo al sistema bigint existente)
ALTER TABLE "Players" ADD COLUMN "WalletBalance" numeric(18,2) DEFAULT 0.00;

-- 2. MIGRAR BALANCES EXISTENTES DE BIGINT A DECIMAL
-- ============================================================================

-- Migrar balances existentes de players (convertir de centavos a pesos)
UPDATE "Players" 
SET "WalletBalance" = COALESCE((
  SELECT w."BalanceBigint" / 100.0 
  FROM "Wallets" w 
  WHERE w."PlayerId" = "Players"."Id"
), 0.00);

-- 3. CREAR TABLA SIMPLE DE TRANSACCIONES
-- ============================================================================

CREATE TABLE "WalletTransactions" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "FromUserId" uuid, -- NULL para MINT
    "FromUserType" text CHECK ("FromUserType" IS NULL OR "FromUserType" IN ('BACKOFFICE', 'PLAYER')),
    "ToUserId" uuid NOT NULL,
    "ToUserType" text NOT NULL CHECK ("ToUserType" IN ('BACKOFFICE', 'PLAYER')),
    "Amount" numeric(18,2) NOT NULL CHECK ("Amount" > 0),
    "Description" text,
    "CreatedByUserId" uuid NOT NULL,
    "CreatedAt" timestamp DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT "FK_WalletTransactions_CreatedByUserId" 
        FOREIGN KEY ("CreatedByUserId") REFERENCES "BackofficeUsers"("Id") ON DELETE RESTRICT
);

-- 4. CREAR ÍNDICES PARA RENDIMIENTO
-- ============================================================================

CREATE INDEX "IX_WalletTransactions_CreatedAt" ON "WalletTransactions"("CreatedAt");
CREATE INDEX "IX_WalletTransactions_FromUserId" ON "WalletTransactions"("FromUserId");
CREATE INDEX "IX_WalletTransactions_ToUserId" ON "WalletTransactions"("ToUserId");
CREATE INDEX "IX_WalletTransactions_CreatedByUserId" ON "WalletTransactions"("CreatedByUserId");
CREATE INDEX "IX_WalletTransactions_FromUserType" ON "WalletTransactions"("FromUserType");
CREATE INDEX "IX_WalletTransactions_ToUserType" ON "WalletTransactions"("ToUserType");

-- 5. VERIFICAR RESULTADOS
-- ============================================================================

-- Verificar que los campos se agregaron correctamente
SELECT 
    'BackofficeUsers' as table_name,
    COUNT(*) as total_records,
    COUNT("WalletBalance") as records_with_wallet,
    AVG("WalletBalance") as avg_balance
FROM "BackofficeUsers"
UNION ALL
SELECT 
    'Players' as table_name,
    COUNT(*) as total_records,
    COUNT("WalletBalance") as records_with_wallet,
    AVG("WalletBalance") as avg_balance
FROM "Players";

-- Verificar migración de balances
SELECT 
    COUNT(*) as total_players,
    COUNT(CASE WHEN "WalletBalance" > 0 THEN 1 END) as players_with_balance,
    SUM("WalletBalance") as total_balance_migrated
FROM "Players";

-- Verificar estructura de tabla de transacciones
SELECT 
    column_name, 
    data_type, 
    is_nullable, 
    column_default
FROM information_schema.columns 
WHERE table_name = 'WalletTransactions' 
ORDER BY ordinal_position;

-- =============================================================================
-- EJEMPLOS DE USO DEL NUEVO SISTEMA
-- =============================================================================

-- Ejemplo 1: MINT - Crear $1000 para un SUPER_ADMIN
-- INSERT INTO "WalletTransactions" ("FromUserId", "FromUserType", "ToUserId", "ToUserType", "Amount", "Description", "CreatedByUserId")
-- VALUES (NULL, NULL, 'uuid-super-admin', 'BACKOFFICE', 1000.00, 'MINT inicial', 'uuid-super-admin');
-- UPDATE "BackofficeUsers" SET "WalletBalance" = "WalletBalance" + 1000.00 WHERE "Id" = 'uuid-super-admin';

-- Ejemplo 2: TRANSFER - Transferir $500 de BRAND_ADMIN a CASHIER
-- INSERT INTO "WalletTransactions" ("FromUserId", "FromUserType", "ToUserId", "ToUserType", "Amount", "Description", "CreatedByUserId")
-- VALUES ('uuid-brand-admin', 'BACKOFFICE', 'uuid-cashier', 'BACKOFFICE', 500.00, 'Asignación mensual', 'uuid-brand-admin');
-- UPDATE "BackofficeUsers" SET "WalletBalance" = "WalletBalance" - 500.00 WHERE "Id" = 'uuid-brand-admin';
-- UPDATE "BackofficeUsers" SET "WalletBalance" = "WalletBalance" + 500.00 WHERE "Id" = 'uuid-cashier';

-- Ejemplo 3: TRANSFER - Transferir $100 de CASHIER a PLAYER
-- INSERT INTO "WalletTransactions" ("FromUserId", "FromUserType", "ToUserId", "ToUserType", "Amount", "Description", "CreatedByUserId")
-- VALUES ('uuid-cashier', 'BACKOFFICE', 'uuid-player', 'PLAYER', 100.00, 'Crédito inicial', 'uuid-cashier');
-- UPDATE "BackofficeUsers" SET "WalletBalance" = "WalletBalance" - 100.00 WHERE "Id" = 'uuid-cashier';
-- UPDATE "Players" SET "WalletBalance" = "WalletBalance" + 100.00 WHERE "Id" = 'uuid-player';

COMMIT;
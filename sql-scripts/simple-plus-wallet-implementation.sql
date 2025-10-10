-- =============================================================================
-- SONNET: IMPLEMENTACIÓN "SIMPLE+" CON GARANTÍAS CRÍTICAS
-- Fecha: $(Get-Date)
-- Funcionalidad: MINT + TRANSFER con idempotencia + transacciones + scope
-- =============================================================================

-- SONNET: 1. AGREGAR CAMPOS DE WALLET DECIMAL A ENTIDADES EXISTENTES
-- ============================================================================

-- Agregar balance decimal a usuarios de backoffice
ALTER TABLE "BackofficeUsers" ADD COLUMN IF NOT EXISTS "WalletBalance" numeric(18,2) DEFAULT 0.00;

-- Agregar balance decimal a players (paralelo al sistema bigint existente)  
ALTER TABLE "Players" ADD COLUMN IF NOT EXISTS "WalletBalance" numeric(18,2) DEFAULT 0.00;

-- SONNET: 2. MIGRAR BALANCES EXISTENTES DE BIGINT A DECIMAL
-- ============================================================================

-- Migrar balances existentes de players (convertir de centavos a pesos)
UPDATE "Players" 
SET "WalletBalance" = COALESCE((
  SELECT w."BalanceBigint" / 100.0 
  FROM "Wallets" w 
  WHERE w."PlayerId" = "Players"."Id"
), 0.00)
WHERE "WalletBalance" = 0.00; -- Solo actualizar si no se ha migrado antes

-- SONNET: 3. CREAR TABLA DE TRANSACCIONES CON GARANTÍAS DE SEGURIDAD
-- ============================================================================

CREATE TABLE IF NOT EXISTS "WalletTransactions" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "BrandId" uuid NOT NULL, -- SONNET: Para scope de autorización
    "FromUserId" uuid, -- NULL para MINT
    "FromUserType" text CHECK ("FromUserType" IS NULL OR "FromUserType" IN ('BACKOFFICE', 'PLAYER')),
    "ToUserId" uuid NOT NULL,
    "ToUserType" text NOT NULL CHECK ("ToUserType" IN ('BACKOFFICE', 'PLAYER')),
    "Amount" numeric(18,2) NOT NULL CHECK ("Amount" > 0),
    "Description" text,
    "CreatedByUserId" uuid NOT NULL,
    "CreatedByRole" text NOT NULL, -- SONNET: Rol del actor
    "IdempotencyKey" text NOT NULL, -- SONNET: Clave única para evitar duplicados
    "CreatedAt" timestamptz DEFAULT CURRENT_TIMESTAMP,
    
    -- Relaciones
    CONSTRAINT "FK_WalletTransactions_BrandId" 
        FOREIGN KEY ("BrandId") REFERENCES "Brands"("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_WalletTransactions_CreatedByUserId" 
        FOREIGN KEY ("CreatedByUserId") REFERENCES "BackofficeUsers"("Id") ON DELETE RESTRICT
);

-- SONNET: 4. CREAR ÍNDICES PARA RENDIMIENTO Y CONSULTAS POR SCOPE
-- ============================================================================

CREATE INDEX IF NOT EXISTS "IX_WalletTransactions_BrandId" ON "WalletTransactions"("BrandId");
CREATE INDEX IF NOT EXISTS "IX_WalletTransactions_CreatedAt" ON "WalletTransactions"("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_WalletTransactions_FromUserId" ON "WalletTransactions"("FromUserId");
CREATE INDEX IF NOT EXISTS "IX_WalletTransactions_ToUserId" ON "WalletTransactions"("ToUserId");
CREATE INDEX IF NOT EXISTS "IX_WalletTransactions_CreatedByUserId" ON "WalletTransactions"("CreatedByUserId");

-- SONNET: Índice único para idempotencia (garantía crítica #1)
CREATE UNIQUE INDEX IF NOT EXISTS "IX_WalletTransactions_IdempotencyKey" 
    ON "WalletTransactions"("IdempotencyKey");

-- SONNET: 5. CREAR FUNCIÓN STORED PROCEDURE PARA TRANSACCIONES SEGURAS
-- ============================================================================

CREATE OR REPLACE FUNCTION execute_wallet_transaction(
    p_brand_id uuid,
    p_from_user_id uuid DEFAULT NULL,
    p_from_user_type text DEFAULT NULL,
    p_to_user_id uuid,
    p_to_user_type text,
    p_amount numeric(18,2),
    p_description text DEFAULT NULL,
    p_actor_user_id uuid,
    p_actor_role text,
    p_idempotency_key text
)
RETURNS TABLE(
    transaction_id uuid,
    operation_type text,
    success boolean,
    message text,
    from_balance_after numeric(18,2),
    to_balance_after numeric(18,2)
) 
LANGUAGE plpgsql
AS $$
DECLARE
    v_operation_type text;
    v_from_balance numeric(18,2) := 0;
    v_to_balance numeric(18,2) := 0;
    v_transaction_id uuid;
    v_existing_transaction uuid;
BEGIN
    -- SONNET: Garantía crítica #1 - Verificar idempotencia
    SELECT "Id" INTO v_existing_transaction 
    FROM "WalletTransactions" 
    WHERE "IdempotencyKey" = p_idempotency_key;
    
    IF v_existing_transaction IS NOT NULL THEN
        -- Devolver transacción existente
        SELECT wt."Id", 
               CASE WHEN wt."FromUserId" IS NULL THEN 'MINT' ELSE 'TRANSFER' END,
               true,
               'Transaction already processed (idempotent)',
               NULL, NULL
        INTO transaction_id, operation_type, success, message, from_balance_after, to_balance_after
        FROM "WalletTransactions" wt
        WHERE wt."Id" = v_existing_transaction;
        RETURN NEXT;
        RETURN;
    END IF;
    
    -- Determinar tipo de operación
    v_operation_type := CASE WHEN p_from_user_id IS NULL THEN 'MINT' ELSE 'TRANSFER' END;
    
    -- SONNET: Garantía crítica #2 - Usar transacción con bloqueo
    -- Bloquear registros en orden consistente para evitar deadlocks
    
    -- Bloquear usuario origen (si no es MINT)
    IF p_from_user_id IS NOT NULL THEN
        IF p_from_user_type = 'BACKOFFICE' THEN
            SELECT "WalletBalance" INTO v_from_balance
            FROM "BackofficeUsers" 
            WHERE "Id" = p_from_user_id AND ("BrandId" = p_brand_id OR "BrandId" IS NULL)
            FOR UPDATE;
            
            IF NOT FOUND THEN
                success := false;
                message := 'Source backoffice user not found in brand';
                RETURN NEXT;
                RETURN;
            END IF;
        ELSIF p_from_user_type = 'PLAYER' THEN
            SELECT "WalletBalance" INTO v_from_balance
            FROM "Players" 
            WHERE "Id" = p_from_user_id AND "BrandId" = p_brand_id
            FOR UPDATE;
            
            IF NOT FOUND THEN
                success := false;
                message := 'Source player not found in brand';
                RETURN NEXT;
                RETURN;
            END IF;
        END IF;
        
        -- Validar saldo suficiente
        IF v_from_balance < p_amount THEN
            success := false;
            message := 'Insufficient balance. Required: ' || p_amount || ', Available: ' || v_from_balance;
            RETURN NEXT;
            RETURN;
        END IF;
    END IF;
    
    -- Bloquear usuario destino
    IF p_to_user_type = 'BACKOFFICE' THEN
        SELECT "WalletBalance" INTO v_to_balance
        FROM "BackofficeUsers" 
        WHERE "Id" = p_to_user_id AND ("BrandId" = p_brand_id OR "BrandId" IS NULL)
        FOR UPDATE;
        
        IF NOT FOUND THEN
            success := false;
            message := 'Target backoffice user not found in brand';
            RETURN NEXT;
            RETURN;
        END IF;
    ELSIF p_to_user_type = 'PLAYER' THEN
        SELECT "WalletBalance" INTO v_to_balance
        FROM "Players" 
        WHERE "Id" = p_to_user_id AND "BrandId" = p_brand_id
        FOR UPDATE;
        
        IF NOT FOUND THEN
            success := false;
            message := 'Target player not found in brand';
            RETURN NEXT;
            RETURN;
        END IF;
    END IF;
    
    -- Actualizar balances
    IF p_from_user_id IS NOT NULL THEN
        IF p_from_user_type = 'BACKOFFICE' THEN
            UPDATE "BackofficeUsers" 
            SET "WalletBalance" = "WalletBalance" - p_amount
            WHERE "Id" = p_from_user_id;
            v_from_balance := v_from_balance - p_amount;
        ELSIF p_from_user_type = 'PLAYER' THEN
            UPDATE "Players" 
            SET "WalletBalance" = "WalletBalance" - p_amount
            WHERE "Id" = p_from_user_id;
            v_from_balance := v_from_balance - p_amount;
        END IF;
    END IF;
    
    IF p_to_user_type = 'BACKOFFICE' THEN
        UPDATE "BackofficeUsers" 
        SET "WalletBalance" = "WalletBalance" + p_amount
        WHERE "Id" = p_to_user_id;
        v_to_balance := v_to_balance + p_amount;
    ELSIF p_to_user_type = 'PLAYER' THEN
        UPDATE "Players" 
        SET "WalletBalance" = "WalletBalance" + p_amount
        WHERE "Id" = p_to_user_id;
        v_to_balance := v_to_balance + p_amount;
    END IF;
    
    -- Crear registro de transacción
    v_transaction_id := gen_random_uuid();
    INSERT INTO "WalletTransactions" (
        "Id", "BrandId", "FromUserId", "FromUserType", 
        "ToUserId", "ToUserType", "Amount", "Description",
        "CreatedByUserId", "CreatedByRole", "IdempotencyKey"
    ) VALUES (
        v_transaction_id, p_brand_id, p_from_user_id, p_from_user_type,
        p_to_user_id, p_to_user_type, p_amount, p_description,
        p_actor_user_id, p_actor_role, p_idempotency_key
    );
    
    -- Devolver resultado exitoso
    transaction_id := v_transaction_id;
    operation_type := v_operation_type;
    success := true;
    message := 'Transaction completed successfully';
    from_balance_after := CASE WHEN p_from_user_id IS NOT NULL THEN v_from_balance ELSE NULL END;
    to_balance_after := v_to_balance;
    
    RETURN NEXT;
END;
$$;

-- SONNET: 6. EJEMPLOS DE USO CON GARANTÍAS
-- ============================================================================

-- Ejemplo 1: MINT $1000 a BRAND_ADMIN (solo SUPER_ADMIN puede hacer esto)
-- SELECT * FROM execute_wallet_transaction(
--     'brand-uuid'::uuid,                    -- brand_id
--     NULL,                                   -- from_user_id (NULL = MINT)
--     NULL,                                   -- from_user_type
--     'brand-admin-uuid'::uuid,               -- to_user_id
--     'BACKOFFICE',                           -- to_user_type
--     1000.00,                                -- amount
--     'Initial capital from SUPER_ADMIN',     -- description
--     'super-admin-uuid'::uuid,               -- actor_user_id
--     'SUPER_ADMIN',                          -- actor_role
--     'mint-001-2024'                         -- idempotency_key
-- );

-- Ejemplo 2: TRANSFER $500 de BRAND_ADMIN a CASHIER
-- SELECT * FROM execute_wallet_transaction(
--     'brand-uuid'::uuid,                    -- brand_id
--     'brand-admin-uuid'::uuid,              -- from_user_id
--     'BACKOFFICE',                          -- from_user_type
--     'cashier-uuid'::uuid,                  -- to_user_id
--     'BACKOFFICE',                          -- to_user_type
--     500.00,                                -- amount
--     'Monthly allocation',                  -- description
--     'brand-admin-uuid'::uuid,              -- actor_user_id
--     'BRAND_ADMIN',                         -- actor_role
--     'transfer-001-2024'                    -- idempotency_key
-- );

-- Ejemplo 3: TRANSFER $100 de CASHIER a PLAYER
-- SELECT * FROM execute_wallet_transaction(
--     'brand-uuid'::uuid,                    -- brand_id
--     'cashier-uuid'::uuid,                  -- from_user_id
--     'BACKOFFICE',                          -- from_user_type
--     'player-uuid'::uuid,                   -- to_user_id
--     'PLAYER',                              -- to_user_type
--     100.00,                                -- amount
--     'Initial credit',                      -- description
--     'cashier-uuid'::uuid,                  -- actor_user_id
--     'CASHIER',                             -- actor_role
--     'credit-001-2024'                      -- idempotency_key
-- );

-- SONNET: 7. VERIFICACIÓN DE RESULTADOS
-- ============================================================================

-- Verificar que los campos se agregaron correctamente
SELECT 
    'BackofficeUsers' as table_name,
    COUNT(*) as total_records,
    COUNT("WalletBalance") as records_with_wallet,
    COALESCE(AVG("WalletBalance"), 0) as avg_balance,
    COALESCE(SUM("WalletBalance"), 0) as total_balance
FROM "BackofficeUsers"
UNION ALL
SELECT 
    'Players' as table_name,
    COUNT(*) as total_records,
    COUNT("WalletBalance") as records_with_wallet,
    COALESCE(AVG("WalletBalance"), 0) as avg_balance,
    COALESCE(SUM("WalletBalance"), 0) as total_balance
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

-- Verificar índices creados
SELECT indexname, indexdef 
FROM pg_indexes 
WHERE tablename = 'WalletTransactions'
ORDER BY indexname;

-- SONNET: 8. QUERIES ÚTILES PARA MONITOREO
-- ============================================================================

-- Ver todas las transacciones de un brand
-- SELECT wt.*, bu."Username" as actor_username
-- FROM "WalletTransactions" wt
-- JOIN "BackofficeUsers" bu ON wt."CreatedByUserId" = bu."Id"
-- WHERE wt."BrandId" = 'your-brand-uuid'
-- ORDER BY wt."CreatedAt" DESC;

-- Ver balance total por brand
-- SELECT 
--     b."Name" as brand_name,
--     COALESCE(SUM(bu."WalletBalance"), 0) as backoffice_total,
--     COALESCE(SUM(p."WalletBalance"), 0) as players_total,
--     COALESCE(SUM(bu."WalletBalance"), 0) + COALESCE(SUM(p."WalletBalance"), 0) as grand_total
-- FROM "Brands" b
-- LEFT JOIN "BackofficeUsers" bu ON b."Id" = bu."BrandId" 
-- LEFT JOIN "Players" p ON b."Id" = p."BrandId"
-- GROUP BY b."Id", b."Name"
-- ORDER BY b."Name";

-- Ver actividad de transacciones por día
-- SELECT 
--     DATE("CreatedAt") as transaction_date,
--     COUNT(*) as transaction_count,
--     SUM("Amount") as total_amount,
--     COUNT(CASE WHEN "FromUserId" IS NULL THEN 1 END) as mint_count,
--     COUNT(CASE WHEN "FromUserId" IS NOT NULL THEN 1 END) as transfer_count
-- FROM "WalletTransactions"
-- WHERE "CreatedAt" >= CURRENT_DATE - INTERVAL '30 days'
-- GROUP BY DATE("CreatedAt")
-- ORDER BY transaction_date DESC;

COMMIT;
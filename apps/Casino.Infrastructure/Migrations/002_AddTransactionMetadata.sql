-- Migration: 002_AddTransactionMetadata.sql
-- Purpose: Add metadata fields to WalletTransactions for audit and flexibility
-- Date: 2025-01-22
-- Author: System Migration

BEGIN;

-- 1. Add metadata columns
ALTER TABLE "WalletTransactions"
    ADD COLUMN notes TEXT,
    ADD COLUMN metadata JSONB,
    ADD COLUMN actor_ip VARCHAR(45),
    ADD COLUMN approved_by_user_id UUID REFERENCES "BackofficeUsers"("Id"),
    ADD COLUMN approved_at TIMESTAMP;

-- 2. Create GIN index for JSONB metadata (for efficient querying)
CREATE INDEX idx_wallet_txn_metadata_gin 
    ON "WalletTransactions" USING GIN (metadata)
    WHERE metadata IS NOT NULL;

-- 3. Create index for approved_by queries
CREATE INDEX idx_wallet_txn_approved_by 
    ON "WalletTransactions"(approved_by_user_id)
    WHERE approved_by_user_id IS NOT NULL;

-- 4. Add comment to document metadata usage
COMMENT ON COLUMN "WalletTransactions".metadata IS 
    'JSONB field for flexible metadata. Examples: {"subtype":"PLAYER_TOPUP_INTERNAL","source":"CASHIER_WALLET"}';

-- 5. Validation: Show table structure
SELECT 
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_name = 'WalletTransactions'
  AND column_name IN ('notes', 'metadata', 'actor_ip', 'approved_by_user_id', 'approved_at')
ORDER BY ordinal_position;

COMMIT;

-- Success message
DO $$
BEGIN
    RAISE NOTICE '? Migration 002_AddTransactionMetadata completed successfully';
    RAISE NOTICE 'Added columns: notes, metadata, actor_ip, approved_by_user_id, approved_at';
    RAISE NOTICE 'Created GIN index for metadata field';
    RAISE NOTICE 'Transactions can now store flexible metadata for subtypes and audit';
END $$;

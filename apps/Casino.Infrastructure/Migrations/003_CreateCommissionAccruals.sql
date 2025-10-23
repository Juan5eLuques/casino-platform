-- Migration: 003_CreateCommissionAccruals.sql
-- Purpose: Create table for multilevel commission accrual and monthly settlement
-- Date: 2025-01-22
-- Author: System Migration

BEGIN;

-- 1. Create commission_accruals table
CREATE TABLE commission_accruals (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    brand_id UUID NOT NULL REFERENCES "Brands"("Id") ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES "BackofficeUsers"("Id") ON DELETE CASCADE,
    parent_user_id UUID REFERENCES "BackofficeUsers"("Id") ON DELETE SET NULL,
    
    -- Period
    period_month INTEGER NOT NULL CHECK (period_month BETWEEN 1 AND 12),
    period_year INTEGER NOT NULL CHECK (period_year >= 2024 AND period_year <= 2100),
    
    -- Commission calculation
    base_amount BIGINT NOT NULL CHECK (base_amount >= 0),
    commission_rate DECIMAL(5,4) NOT NULL CHECK (commission_rate BETWEEN 0 AND 1),
    commission_amount BIGINT NOT NULL CHECK (commission_amount >= 0),
    
    -- Settlement
    settled BOOLEAN NOT NULL DEFAULT FALSE,
    settled_at TIMESTAMP,
    settled_transaction_id UUID REFERENCES "WalletTransactions"("Id") ON DELETE SET NULL,
    
    -- Source tracking
    source_type VARCHAR(50),  -- 'NETWIN', 'TRANSFER_FEE', etc.
    source_transaction_id UUID REFERENCES "WalletTransactions"("Id") ON DELETE SET NULL,
    source_round_id UUID REFERENCES "Rounds"("Id") ON DELETE SET NULL,
    source_player_id UUID REFERENCES "Players"("Id") ON DELETE SET NULL,
    
    -- Audit
    notes TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    
    -- Unique constraint to prevent duplicates
    CONSTRAINT uq_commission_accrual 
        UNIQUE NULLS NOT DISTINCT (brand_id, user_id, period_year, period_month, source_transaction_id, source_round_id)
);

-- 2. Create indexes for common queries
CREATE INDEX idx_commission_user_period 
    ON commission_accruals(user_id, period_year, period_month);

CREATE INDEX idx_commission_settled 
    ON commission_accruals(settled) 
    WHERE NOT settled;

CREATE INDEX idx_commission_brand_period 
    ON commission_accruals(brand_id, period_year, period_month);

CREATE INDEX idx_commission_source_txn 
    ON commission_accruals(source_transaction_id)
    WHERE source_transaction_id IS NOT NULL;

CREATE INDEX idx_commission_source_round 
    ON commission_accruals(source_round_id)
    WHERE source_round_id IS NOT NULL;

-- 3. Create trigger to auto-update updated_at
CREATE OR REPLACE FUNCTION update_commission_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at := NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_commission_updated_at
    BEFORE UPDATE ON commission_accruals
    FOR EACH ROW
    EXECUTE FUNCTION update_commission_updated_at();

-- 4. Add constraint: settled_at must be set if settled=true
ALTER TABLE commission_accruals
    ADD CONSTRAINT chk_settled_at_consistency
    CHECK (
        (settled = FALSE AND settled_at IS NULL) OR
        (settled = TRUE AND settled_at IS NOT NULL)
    );

-- 5. Validation: Show table structure
SELECT 
    table_name,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_name = 'commission_accruals'
ORDER BY ordinal_position;

COMMIT;

-- Success message
DO $$
DECLARE
    index_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO index_count
    FROM pg_indexes
    WHERE tablename = 'commission_accruals';
    
    RAISE NOTICE '? Migration 003_CreateCommissionAccruals completed successfully';
    RAISE NOTICE 'Created table: commission_accruals';
    RAISE NOTICE 'Created % indexes for performance', index_count;
    RAISE NOTICE 'Added triggers and constraints';
    RAISE NOTICE 'Ready to start accumulating commissions per period';
END $$;

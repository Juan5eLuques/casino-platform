-- Migration: 004_CreateMonthlyClosures.sql
-- Purpose: Create table for monthly closures and KPI snapshots
-- Date: 2025-01-22
-- Author: System Migration

BEGIN;

-- 1. Create monthly_closures table
CREATE TABLE monthly_closures (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    brand_id UUID NOT NULL REFERENCES "Brands"("Id") ON DELETE CASCADE,
    user_id UUID REFERENCES "BackofficeUsers"("Id") ON DELETE CASCADE,  -- NULL = brand-wide closure
    
    -- Period
    period_month INTEGER NOT NULL CHECK (period_month BETWEEN 1 AND 12),
    period_year INTEGER NOT NULL CHECK (period_year >= 2024 AND period_year <= 2100),
    
    -- Gaming KPIs
    total_handle BIGINT NOT NULL DEFAULT 0 CHECK (total_handle >= 0),
    total_payouts BIGINT NOT NULL DEFAULT 0 CHECK (total_payouts >= 0),
    gross_gaming_revenue BIGINT NOT NULL DEFAULT 0,
    
    -- Commission tracking
    total_commissions_paid BIGINT NOT NULL DEFAULT 0 CHECK (total_commissions_paid >= 0),
    
    -- Closure control
    closure_status VARCHAR(50) NOT NULL DEFAULT 'PENDING',
    closed_at TIMESTAMP,
    closed_by_user_id UUID REFERENCES "BackofficeUsers"("Id") ON DELETE SET NULL,
    
    -- Audit
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    
    -- Unique constraint: one closure per brand/user/period
    CONSTRAINT uq_monthly_closure 
        UNIQUE NULLS NOT DISTINCT (brand_id, user_id, period_year, period_month),
    
    -- Constraint: closure_status must be valid
    CONSTRAINT chk_closure_status
        CHECK (closure_status IN ('PENDING', 'PROCESSING', 'COMPLETED', 'FAILED'))
);

-- 2. Create indexes
CREATE INDEX idx_monthly_closure_brand_period 
    ON monthly_closures(brand_id, period_year, period_month);

CREATE INDEX idx_monthly_closure_status 
    ON monthly_closures(closure_status) 
    WHERE closure_status != 'COMPLETED';

CREATE INDEX idx_monthly_closure_user_period 
    ON monthly_closures(user_id, period_year, period_month)
    WHERE user_id IS NOT NULL;

-- 3. Create trigger to auto-update updated_at
CREATE TRIGGER trg_monthly_closure_updated_at
    BEFORE UPDATE ON monthly_closures
    FOR EACH ROW
    EXECUTE FUNCTION update_commission_updated_at();  -- Reuse function from 003

-- 4. Add constraint: closed_at must be set if status=COMPLETED
ALTER TABLE monthly_closures
    ADD CONSTRAINT chk_closure_completed_at
    CHECK (
        (closure_status != 'COMPLETED') OR
        (closure_status = 'COMPLETED' AND closed_at IS NOT NULL)
    );

-- 5. Create helper view for active closures
CREATE OR REPLACE VIEW v_active_closures AS
SELECT 
    mc.id,
    mc.brand_id,
    b."Code" AS brand_code,
    mc.user_id,
    u."Username" AS user_username,
    mc.period_year,
    mc.period_month,
    mc.closure_status,
    mc.total_handle,
    mc.total_payouts,
    mc.gross_gaming_revenue,
    mc.total_commissions_paid,
    mc.created_at,
    mc.updated_at
FROM monthly_closures mc
INNER JOIN "Brands" b ON mc.brand_id = b."Id"
LEFT JOIN "BackofficeUsers" u ON mc.user_id = u."Id"
WHERE mc.closure_status != 'COMPLETED'
ORDER BY mc.period_year DESC, mc.period_month DESC;

-- 6. Validation
SELECT 
    table_name,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_name = 'monthly_closures'
ORDER BY ordinal_position;

COMMIT;

-- Success message
DO $$
DECLARE
    index_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO index_count
    FROM pg_indexes
    WHERE tablename = 'monthly_closures';
    
    RAISE NOTICE '? Migration 004_CreateMonthlyClosures completed successfully';
    RAISE NOTICE 'Created table: monthly_closures';
    RAISE NOTICE 'Created % indexes', index_count;
    RAISE NOTICE 'Created view: v_active_closures';
    RAISE NOTICE 'Ready to start monthly closure process';
END $$;

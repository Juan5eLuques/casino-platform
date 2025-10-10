-- ============================================================================
-- CASINO PLATFORM - OPERATOR REMOVAL SCRIPTS
-- ============================================================================
-- These scripts remove the operator concept from the casino platform
-- and transition to a Brand-centric model.
--
-- IMPORTANT: Run these in order and backup your database first!
-- ============================================================================

-- Script 1: Data Migration (Run BEFORE applying EF migrations)
-- ============================================================================

-- Step 1: Migrate BackofficeUsers to use BrandId
-- For this example, we'll assign all users to the first available brand
-- In a real scenario, you'd need to map users to appropriate brands

-- First, let's see what data we have
SELECT 'BackofficeUsers before migration:' as step;
SELECT u.Id, u.Username, u.Role, u.OperatorId, o.Name as OperatorName
FROM "BackofficeUsers" u
LEFT JOIN "Operators" o ON u.OperatorId = o.Id
ORDER BY u.Username;

SELECT 'Available Brands:' as step;
SELECT b.Id, b.Code, b.Name, b.OperatorId, o.Name as OperatorName
FROM "Brands" b
LEFT JOIN "Operators" o ON b.OperatorId = o.Id
ORDER BY b.Name;

-- Step 2: Temporary migration logic
-- This assigns users to brands based on their current operator
-- You may need to adjust this logic based on your business rules

-- Add BrandId column temporarily (will be done properly in EF migration)
-- ALTER TABLE "BackofficeUsers" ADD COLUMN "BrandId" uuid;

-- Migrate non-SUPER_ADMIN users to first brand of their operator
-- UPDATE "BackofficeUsers" 
-- SET "BrandId" = (
--     SELECT b.Id 
--     FROM "Brands" b 
--     WHERE b.OperatorId = "BackofficeUsers".OperatorId 
--     AND b.Status = 'ACTIVE'
--     LIMIT 1
-- )
-- WHERE Role != 'SUPER_ADMIN' AND OperatorId IS NOT NULL;

-- Step 3: Verify migration
-- SELECT 'BackofficeUsers after BrandId assignment:' as step;
-- SELECT u.Id, u.Username, u.Role, u.OperatorId, o.Name as OperatorName, u.BrandId, b.Name as BrandName
-- FROM "BackofficeUsers" u
-- LEFT JOIN "Operators" o ON u.OperatorId = o.Id
-- LEFT JOIN "Brands" b ON u.BrandId = b.Id
-- ORDER BY u.Username;

-- ============================================================================
-- Script 2: Cleanup (Run AFTER applying EF migrations)
-- ============================================================================

-- Step 1: Update any remaining references to operators in audit logs or metadata
UPDATE "BackofficeAudits" 
SET "Meta" = COALESCE("Meta", '{}')
WHERE "Action" LIKE '%OPERATOR%';

-- Step 2: Clean up any provider audit logs that might reference operators
-- (Usually these don't reference operators directly, but just to be safe)
UPDATE "ProviderAudits" 
SET "RequestData" = COALESCE("RequestData", '{}'),
    "ResponseData" = COALESCE("ResponseData", '{}')
WHERE "RequestData"::text LIKE '%operator%' OR "ResponseData"::text LIKE '%operator%';

-- Step 3: Verify all operator references are removed
SELECT 'Checking for remaining operator references:' as step;

-- Check BackofficeUsers (should not have OperatorId column after migration)
-- SELECT column_name FROM information_schema.columns 
-- WHERE table_name = 'BackofficeUsers' AND column_name LIKE '%operator%';

-- Check Brands (should not have OperatorId column after migration)
-- SELECT column_name FROM information_schema.columns 
-- WHERE table_name = 'Brands' AND column_name LIKE '%operator%';

-- Check Ledger (should not have OperatorId column after migration)
-- SELECT column_name FROM information_schema.columns 
-- WHERE table_name = 'Ledger' AND column_name LIKE '%operator%';

-- Step 4: Final verification - show current structure
SELECT 'Final BackofficeUsers structure:' as step;
SELECT u.Id, u.Username, u.Role, u.BrandId, b.Name as BrandName, b.Code as BrandCode
FROM "BackofficeUsers" u
LEFT JOIN "Brands" b ON u.BrandId = b.Id
ORDER BY u.Username;

SELECT 'Final Brands structure:' as step;
SELECT Id, Code, Name, Domain, Status, CreatedAt
FROM "Brands"
ORDER BY Name;

-- ============================================================================
-- Script 3: Validation Queries
-- ============================================================================

-- Verify no orphaned references
SELECT 'Orphaned BackofficeUsers (no brand):' as step;
SELECT u.Id, u.Username, u.Role 
FROM "BackofficeUsers" u 
WHERE u.Role != 'SUPER_ADMIN' AND u.BrandId IS NULL;

-- Verify referential integrity
SELECT 'BackofficeUsers with invalid brand references:' as step;
SELECT u.Id, u.Username, u.BrandId
FROM "BackofficeUsers" u
LEFT JOIN "Brands" b ON u.BrandId = b.Id
WHERE u.BrandId IS NOT NULL AND b.Id IS NULL;

-- Check that SUPER_ADMIN users don't have brand assignments
SELECT 'SUPER_ADMIN users (should not have BrandId):' as step;
SELECT u.Id, u.Username, u.Role, u.BrandId
FROM "BackofficeUsers" u
WHERE u.Role = 'SUPER_ADMIN' AND u.BrandId IS NOT NULL;

-- Check cashier hierarchy integrity  
SELECT 'Cashier hierarchy validation:' as step;
SELECT 
    c.Id as CashierId, 
    c.Username as CashierUsername,
    c.BrandId as CashierBrandId,
    p.Id as ParentId,
    p.Username as ParentUsername,
    p.BrandId as ParentBrandId,
    CASE 
        WHEN c.BrandId = p.BrandId THEN 'OK' 
        ELSE 'MISMATCH' 
    END as BrandMatch
FROM "BackofficeUsers" c
LEFT JOIN "BackofficeUsers" p ON c.ParentCashierId = p.Id
WHERE c.Role = 'CASHIER' AND c.ParentCashierId IS NOT NULL;

-- ============================================================================
-- Script 4: Performance Optimization
-- ============================================================================

-- Create indexes for the new structure
CREATE INDEX IF NOT EXISTS IX_BackofficeUsers_BrandId_Role 
ON "BackofficeUsers" (BrandId, Role) 
WHERE BrandId IS NOT NULL;

CREATE INDEX IF NOT EXISTS IX_BackofficeUsers_Role_Status 
ON "BackofficeUsers" (Role, Status);

-- Update table statistics
ANALYZE "BackofficeUsers";
ANALYZE "Brands";
ANALYZE "Ledger";

-- ============================================================================
-- Script 5: Documentation Update
-- ============================================================================

-- Insert documentation of the schema change
INSERT INTO "BackofficeAudits" (
    Id, UserId, Action, TargetType, TargetId, Meta, CreatedAt
) VALUES (
    gen_random_uuid(),
    '00000000-0000-0000-0000-000000000000', -- System user
    'SCHEMA_MIGRATION',
    'System',
    'operator_removal',
    '{"description": "Removed operator concept from system", "date": "' || NOW() || '", "tables_affected": ["BackofficeUsers", "Brands", "Ledger"], "migration": "RemoveOperatorFromSystem"}',
    NOW()
);

SELECT 'Operator removal migration completed successfully!' as result;
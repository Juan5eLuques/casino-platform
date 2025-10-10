-- Final Migration Script: Remove Operator System
-- This script safely migrates from operator-based to brand-only system

-- Step 1: Backup current BackofficeUsers assignments before the migration
CREATE TEMP TABLE temp_user_brand_mapping AS
SELECT 
    bu.Id as UserId,
    bu.Username,
    bu.Role,
    bu.OperatorId as CurrentOperatorId,
    -- For SUPER_ADMIN, keep BrandId as NULL
    CASE 
        WHEN bu.Role = 'SUPER_ADMIN' THEN NULL
        ELSE (
            -- For other roles, assign to first brand of their operator
            SELECT b.Id 
            FROM "Brands" b 
            WHERE b.OperatorId = bu.OperatorId 
            LIMIT 1
        )
    END as NewBrandId
FROM "BackofficeUsers" bu;

-- Step 2: Show what will happen
SELECT 
    'MIGRATION PREVIEW' as action,
    Username,
    Role,
    COALESCE(NewBrandId::text, 'NULL (SUPER_ADMIN)') as "Will be assigned to BrandId"
FROM temp_user_brand_mapping;

-- Step 3: Apply the migration changes manually

-- Remove foreign key constraints
ALTER TABLE "BackofficeUsers" DROP CONSTRAINT IF EXISTS "FK_BackofficeUsers_Operators_OperatorId";
ALTER TABLE "Brands" DROP CONSTRAINT IF EXISTS "FK_Brands_Operators_OperatorId"; 
ALTER TABLE "Ledger" DROP CONSTRAINT IF EXISTS "FK_Ledger_Operators_OperatorId";

-- Drop indexes
DROP INDEX IF EXISTS "IX_Ledger_OperatorId";
DROP INDEX IF EXISTS "IX_Brands_OperatorId";
DROP INDEX IF EXISTS "IX_BackofficeUsers_OperatorId";

-- Rename column in BackofficeUsers from OperatorId to BrandId
ALTER TABLE "BackofficeUsers" RENAME COLUMN "OperatorId" TO "BrandId";

-- Update the BrandId values based on our mapping
UPDATE "BackofficeUsers" bu
SET "BrandId" = tm.NewBrandId
FROM temp_user_brand_mapping tm
WHERE bu.Id = tm.UserId;

-- Remove OperatorId columns from other tables
ALTER TABLE "Ledger" DROP COLUMN IF EXISTS "OperatorId";
ALTER TABLE "Brands" DROP COLUMN IF EXISTS "OperatorId";

-- Create new index for BackofficeUsers.BrandId
CREATE INDEX "IX_BackofficeUsers_BrandId" ON "BackofficeUsers" ("BrandId");

-- Add foreign key constraint from BackofficeUsers to Brands
ALTER TABLE "BackofficeUsers" 
ADD CONSTRAINT "FK_BackofficeUsers_Brands_BrandId" 
FOREIGN KEY ("BrandId") REFERENCES "Brands" ("Id");

-- Drop the Operators table
DROP TABLE IF EXISTS "Operators";

-- Step 4: Update the migration history to mark this as applied
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251007004455_AddCashierHierarchyFields', '9.0.9')
ON CONFLICT ("MigrationId") DO NOTHING;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") 
VALUES ('20251007235311_RemoveOperatorSystemFinal', '9.0.9')
ON CONFLICT ("MigrationId") DO NOTHING;

-- Step 5: Verification queries
SELECT 'FINAL VERIFICATION' as status;

SELECT 
    'BackofficeUsers by Role' as category,
    Role,
    COUNT(*) as count,
    COUNT("BrandId") as "with_brand",
    COUNT(*) - COUNT("BrandId") as "without_brand (should be SUPER_ADMIN only)"
FROM "BackofficeUsers"
GROUP BY Role;

SELECT 
    'Brands count' as category,
    COUNT(*) as total_brands
FROM "Brands";

-- Clean up temp table
DROP TABLE temp_user_brand_mapping;

SELECT 'MIGRATION COMPLETED SUCCESSFULLY' as result;
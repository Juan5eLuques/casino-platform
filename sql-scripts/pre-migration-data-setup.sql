-- ============================================================================
-- CASINO PLATFORM - PRE-MIGRATION DATA SETUP
-- ============================================================================
-- This script prepares existing data for the operator removal migration
-- Run this BEFORE applying EF Core migrations
-- ============================================================================

-- Step 1: Backup existing data (optional but recommended)
-- CREATE TABLE BackofficeUsers_Backup AS SELECT * FROM "BackofficeUsers";
-- CREATE TABLE Brands_Backup AS SELECT * FROM "Brands";
-- CREATE TABLE Ledger_Backup AS SELECT * FROM "Ledger";

-- Step 2: Show current data state
SELECT 'Current Operators:' as info;
SELECT "Id", "Name", "Status", "CreatedAt" FROM "Operators" ORDER BY "Name";

SELECT 'Current BackofficeUsers with OperatorId:' as info;
SELECT 
    u."Id", 
    u."Username", 
    u."Role", 
    u."OperatorId", 
    o."Name" as "OperatorName"
FROM "BackofficeUsers" u
LEFT JOIN "Operators" o ON u."OperatorId" = o."Id"
ORDER BY u."Username";

SELECT 'Current Brands with OperatorId:' as info;
SELECT 
    b."Id", 
    b."Code", 
    b."Name", 
    b."OperatorId", 
    o."Name" as "OperatorName"
FROM "Brands" b
LEFT JOIN "Operators" o ON b."OperatorId" = o."Id"
ORDER BY b."Name";

-- Step 3: Create a temporary mapping table to help with migration
-- This will store the mapping between operators and brands for reference
CREATE TEMP TABLE operator_brand_mapping AS
SELECT 
    o."Id" as operator_id,
    o."Name" as operator_name,
    b."Id" as brand_id,
    b."Code" as brand_code,
    b."Name" as brand_name,
    ROW_NUMBER() OVER (PARTITION BY o."Id" ORDER BY b."CreatedAt") as brand_rank
FROM "Operators" o
LEFT JOIN "Brands" b ON b."OperatorId" = o."Id"
WHERE b."Status" = 'ACTIVE';

-- Step 4: Show the mapping that will be used for migration
SELECT 'Operator to Brand Mapping:' as info;
SELECT * FROM operator_brand_mapping ORDER BY operator_name, brand_rank;

-- Step 5: Validate that all non-SUPER_ADMIN users can be mapped to a brand
SELECT 'Users that will need brand assignment:' as info;
SELECT 
    u."Id",
    u."Username", 
    u."Role",
    u."OperatorId",
    o."Name" as "OperatorName",
    CASE 
        WHEN u."Role" = 'SUPER_ADMIN' THEN 'No brand needed'
        WHEN EXISTS (SELECT 1 FROM operator_brand_mapping m WHERE m.operator_id = u."OperatorId") 
        THEN 'Can be mapped'
        ELSE 'NO BRAND AVAILABLE - NEEDS MANUAL INTERVENTION'
    END as mapping_status
FROM "BackofficeUsers" u
LEFT JOIN "Operators" o ON u."OperatorId" = o."Id"
ORDER BY u."Role", mapping_status, u."Username";

-- Step 6: Check for potential issues
SELECT 'Potential Migration Issues:' as info;

-- Users with no operator assigned (except SUPER_ADMIN)
SELECT 'Users with no operator (non-SUPER_ADMIN):' as issue_type, COUNT(*) as count
FROM "BackofficeUsers" 
WHERE "OperatorId" IS NULL AND "Role" != 'SUPER_ADMIN'
UNION ALL
-- Operators with no active brands
SELECT 'Operators with no active brands:' as issue_type, COUNT(*) as count
FROM "Operators" o
WHERE NOT EXISTS (
    SELECT 1 FROM "Brands" b 
    WHERE b."OperatorId" = o."Id" AND b."Status" = 'ACTIVE'
)
UNION ALL
-- Cashier hierarchy that spans operators
SELECT 'Cashiers with parent in different operator:' as issue_type, COUNT(*) as count
FROM "BackofficeUsers" c
INNER JOIN "BackofficeUsers" p ON c."ParentCashierId" = p."Id"
WHERE c."OperatorId" != p."OperatorId";

-- Step 7: Generate the actual migration commands (commented out for safety)
-- Uncomment and run these after reviewing the above analysis

/*
-- Add BrandId column to BackofficeUsers
ALTER TABLE "BackofficeUsers" ADD COLUMN "BrandId" uuid;

-- Migrate BRAND_ADMIN and CASHIER users to first brand of their operator
UPDATE "BackofficeUsers" 
SET "BrandId" = (
    SELECT m.brand_id 
    FROM operator_brand_mapping m 
    WHERE m.operator_id = "BackofficeUsers"."OperatorId" 
    AND m.brand_rank = 1
)
WHERE "Role" IN ('BRAND_ADMIN', 'CASHIER') 
AND "OperatorId" IS NOT NULL;

-- Verify the migration
SELECT 'Migration Results:' as info;
SELECT 
    u."Id",
    u."Username", 
    u."Role",
    u."OperatorId",
    o."Name" as "OperatorName",
    u."BrandId",
    b."Name" as "BrandName"
FROM "BackofficeUsers" u
LEFT JOIN "Operators" o ON u."OperatorId" = o."Id"
LEFT JOIN "Brands" b ON u."BrandId" = b."Id"
ORDER BY u."Role", u."Username";

-- Add foreign key constraint
ALTER TABLE "BackofficeUsers" 
ADD CONSTRAINT "FK_BackofficeUsers_Brands_BrandId" 
FOREIGN KEY ("BrandId") REFERENCES "Brands"("Id");

-- Create index
CREATE INDEX "IX_BackofficeUsers_BrandId" ON "BackofficeUsers" ("BrandId");
*/

SELECT 'Pre-migration analysis complete. Review the output above before proceeding with EF migrations.' as result;
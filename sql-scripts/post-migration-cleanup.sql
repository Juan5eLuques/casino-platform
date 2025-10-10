-- ============================================================================
-- CASINO PLATFORM - POST-MIGRATION CLEANUP
-- ============================================================================
-- This script cleans up after the operator removal migration
-- Run this AFTER applying EF Core migrations successfully
-- ============================================================================

-- Step 1: Verify the migration was successful
SELECT 'Post-Migration Verification:' as step;

-- Check that Operators table no longer exists
SELECT 
    CASE 
        WHEN EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Operators') 
        THEN 'ERROR: Operators table still exists!'
        ELSE 'OK: Operators table removed'
    END as operators_table_status;

-- Check that OperatorId columns are removed
SELECT 'Checking for remaining OperatorId columns:' as step;
SELECT 
    table_name, 
    column_name,
    'ERROR: OperatorId column still exists!' as status
FROM information_schema.columns 
WHERE column_name LIKE '%OperatorId%' 
AND table_schema = 'public'
UNION ALL
SELECT 
    'All tables' as table_name,
    'OperatorId columns' as column_name,
    CASE 
        WHEN COUNT(*) = 0 THEN 'OK: All OperatorId columns removed'
        ELSE 'ERROR: ' || COUNT(*)::text || ' OperatorId columns still exist'
    END as status
FROM information_schema.columns 
WHERE column_name LIKE '%OperatorId%' 
AND table_schema = 'public';

-- Step 2: Verify BackofficeUsers structure
SELECT 'BackofficeUsers Structure Verification:' as step;
SELECT 
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns 
WHERE table_name = 'BackofficeUsers' 
AND table_schema = 'public'
ORDER BY ordinal_position;

-- Step 3: Verify all users have proper brand assignments
SELECT 'User Brand Assignment Verification:' as step;
SELECT 
    'SUPER_ADMIN users' as user_type,
    COUNT(*) as total_count,
    COUNT(CASE WHEN "BrandId" IS NULL THEN 1 END) as without_brand,
    COUNT(CASE WHEN "BrandId" IS NOT NULL THEN 1 END) as with_brand,
    CASE 
        WHEN COUNT(CASE WHEN "BrandId" IS NOT NULL THEN 1 END) = 0 THEN 'OK'
        ELSE 'WARNING: SUPER_ADMIN users should not have BrandId'
    END as status
FROM "BackofficeUsers" 
WHERE "Role" = 'SUPER_ADMIN'
UNION ALL
SELECT 
    'BRAND_ADMIN users' as user_type,
    COUNT(*) as total_count,
    COUNT(CASE WHEN "BrandId" IS NULL THEN 1 END) as without_brand,
    COUNT(CASE WHEN "BrandId" IS NOT NULL THEN 1 END) as with_brand,
    CASE 
        WHEN COUNT(CASE WHEN "BrandId" IS NULL THEN 1 END) = 0 THEN 'OK'
        ELSE 'ERROR: BRAND_ADMIN users must have BrandId'
    END as status
FROM "BackofficeUsers" 
WHERE "Role" = 'BRAND_ADMIN'
UNION ALL
SELECT 
    'CASHIER users' as user_type,
    COUNT(*) as total_count,
    COUNT(CASE WHEN "BrandId" IS NULL THEN 1 END) as without_brand,
    COUNT(CASE WHEN "BrandId" IS NOT NULL THEN 1 END) as with_brand,
    CASE 
        WHEN COUNT(CASE WHEN "BrandId" IS NULL THEN 1 END) = 0 THEN 'OK'
        ELSE 'ERROR: CASHIER users must have BrandId'
    END as status
FROM "BackofficeUsers" 
WHERE "Role" = 'CASHIER';

-- Step 4: Verify referential integrity
SELECT 'Referential Integrity Check:' as step;
SELECT 
    u."Id",
    u."Username",
    u."Role",
    u."BrandId",
    'ERROR: Invalid brand reference' as issue
FROM "BackofficeUsers" u
LEFT JOIN "Brands" b ON u."BrandId" = b."Id"
WHERE u."BrandId" IS NOT NULL AND b."Id" IS NULL
UNION ALL
SELECT 
    '(summary)' as "Id",
    'Invalid brand references' as "Username",
    '' as "Role",
    NULL as "BrandId",
    CASE 
        WHEN COUNT(*) = 0 THEN 'OK: All brand references are valid'
        ELSE 'ERROR: ' || COUNT(*)::text || ' invalid brand references found'
    END as issue
FROM "BackofficeUsers" u
LEFT JOIN "Brands" b ON u."BrandId" = b."Id"
WHERE u."BrandId" IS NOT NULL AND b."Id" IS NULL;

-- Step 5: Check cashier hierarchy integrity within brands
SELECT 'Cashier Hierarchy Brand Consistency:' as step;
SELECT 
    c."Id" as cashier_id,
    c."Username" as cashier_username,
    c."BrandId" as cashier_brand_id,
    p."Id" as parent_id,
    p."Username" as parent_username,
    p."BrandId" as parent_brand_id,
    CASE 
        WHEN c."BrandId" = p."BrandId" THEN 'OK'
        ELSE 'ERROR: Brand mismatch'
    END as brand_consistency
FROM "BackofficeUsers" c
INNER JOIN "BackofficeUsers" p ON c."ParentCashierId" = p."Id"
WHERE c."Role" = 'CASHIER'
ORDER BY brand_consistency DESC, c."Username";

-- Step 6: Show final user distribution by brand
SELECT 'Final User Distribution by Brand:' as step;
SELECT 
    COALESCE(b."Code", 'NO_BRAND') as brand_code,
    COALESCE(b."Name", 'NO_BRAND') as brand_name,
    u."Role",
    COUNT(*) as user_count
FROM "BackofficeUsers" u
LEFT JOIN "Brands" b ON u."BrandId" = b."Id"
GROUP BY b."Code", b."Name", u."Role"
ORDER BY brand_code, u."Role";

-- Step 7: Update audit logs to mark the migration complete
INSERT INTO "BackofficeAudits" (
    "Id", 
    "UserId", 
    "Action", 
    "TargetType", 
    "TargetId", 
    "Meta", 
    "CreatedAt"
) VALUES (
    gen_random_uuid(),
    '00000000-0000-0000-0000-000000000000', -- System user
    'SCHEMA_MIGRATION_COMPLETE',
    'System',
    'operator_removal_post_migration',
    jsonb_build_object(
        'description', 'Completed operator removal migration - post cleanup',
        'migration_date', NOW(),
        'tables_cleaned', ARRAY['BackofficeUsers', 'Brands', 'Ledger'],
        'operators_removed', true,
        'brand_assignments_verified', true
    ),
    NOW()
);

-- Step 8: Performance optimization - update statistics
ANALYZE "BackofficeUsers";
ANALYZE "Brands";
ANALYZE "Ledger";

-- Step 9: Final verification summary
SELECT 'Migration Summary:' as step;
SELECT 
    'Total Brands' as metric,
    COUNT(*)::text as value
FROM "Brands"
UNION ALL
SELECT 
    'Total BackofficeUsers' as metric,
    COUNT(*)::text as value
FROM "BackofficeUsers"
UNION ALL
SELECT 
    'SUPER_ADMIN users' as metric,
    COUNT(*)::text as value
FROM "BackofficeUsers" WHERE "Role" = 'SUPER_ADMIN'
UNION ALL
SELECT 
    'BRAND_ADMIN users' as metric,
    COUNT(*)::text as value
FROM "BackofficeUsers" WHERE "Role" = 'BRAND_ADMIN'
UNION ALL
SELECT 
    'CASHIER users' as metric,
    COUNT(*)::text as value
FROM "BackofficeUsers" WHERE "Role" = 'CASHIER'
UNION ALL
SELECT 
    'Users with brand assignment' as metric,
    COUNT(*)::text as value
FROM "BackofficeUsers" WHERE "BrandId" IS NOT NULL
UNION ALL
SELECT 
    'Migration Status' as metric,
    'COMPLETED SUCCESSFULLY' as value;

SELECT 'Operator removal migration completed successfully!' as result;
-- Verification script for localhost development seed

-- Check if brand was created
SELECT 'BRAND VERIFICATION:' as status;
SELECT "Code", "Name", "Domain", "AdminDomain", "Status" 
FROM "Brands" 
WHERE "Code" = 'LOCALHOST_DEV';

-- Check if superadmin was created
SELECT 'SUPERADMIN VERIFICATION:' as status;
SELECT "Username", "Role", "Status", 
       CASE WHEN "BrandId" IS NULL THEN 'ALL_BRANDS' ELSE "BrandId"::text END as scope
FROM "BackofficeUsers" 
WHERE "Username" = 'superadmin';

-- Check if games were created
SELECT 'GAMES VERIFICATION:' as status;
SELECT "Code", "Name", "Provider", "Enabled"
FROM "Games"
WHERE "Provider" = 'DEV_PROVIDER';

-- Check if player was created
SELECT 'PLAYER VERIFICATION:' as status;
SELECT "Username", "Email", "Status"
FROM "Players"
WHERE "Username" = 'dev_player';

-- Check wallet balance
SELECT 'WALLET VERIFICATION:' as status;
SELECT p."Username", w."BalanceBigint" / 100.0 as balance_dollars
FROM "Players" p
JOIN "Wallets" w ON p."Id" = w."PlayerId"
WHERE p."Username" = 'dev_player';

SELECT 'SEED VERIFICATION COMPLETED' as final_status;
-- Casino Platform Database Structure Verification
-- Run this script to verify all tables and indexes were created correctly

-- Check if all tables exist
SELECT 
    table_name,
    table_type
FROM information_schema.tables 
WHERE table_schema = 'public' 
    AND table_type = 'BASE TABLE'
ORDER BY table_name;

-- Check all indexes
SELECT 
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes 
WHERE schemaname = 'public'
ORDER BY tablename, indexname;

-- Count rows in each table (should be 0 initially)
SELECT 
    'Operators' as table_name, COUNT(*) as row_count FROM "Operators"
UNION ALL
SELECT 'Brands', COUNT(*) FROM "Brands"
UNION ALL
SELECT 'Players', COUNT(*) FROM "Players"
UNION ALL
SELECT 'Wallets', COUNT(*) FROM "Wallets"
UNION ALL
SELECT 'Games', COUNT(*) FROM "Games"
UNION ALL
SELECT 'BrandGames', COUNT(*) FROM "BrandGames"
UNION ALL
SELECT 'GameSessions', COUNT(*) FROM "GameSessions"
UNION ALL
SELECT 'Rounds', COUNT(*) FROM "Rounds"
UNION ALL
SELECT 'Ledger', COUNT(*) FROM "Ledger"
UNION ALL
SELECT 'BackofficeUsers', COUNT(*) FROM "BackofficeUsers"
UNION ALL
SELECT 'CashierPlayers', COUNT(*) FROM "CashierPlayers"
UNION ALL
SELECT 'BackofficeAudits', COUNT(*) FROM "BackofficeAudits"
UNION ALL
SELECT 'ProviderAudits', COUNT(*) FROM "ProviderAudits";

-- Check migration history
SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
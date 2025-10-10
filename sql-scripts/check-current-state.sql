-- Check current database state before migration
SELECT 'CURRENT DATABASE STATE' as status;

-- Check Operators
SELECT 'Operators' as table_name, COUNT(*) as count FROM "Operators";

-- Check Brands with their operators
SELECT 'Brands by Operator' as category, o.Name as operator_name, COUNT(b.Id) as brand_count
FROM "Operators" o
LEFT JOIN "Brands" b ON b.OperatorId = o.Id
GROUP BY o.Id, o.Name;

-- Check BackofficeUsers by role and operator
SELECT 'BackofficeUsers by Role' as category, 
       bu.Role, 
       o.Name as operator_name,
       COUNT(*) as user_count
FROM "BackofficeUsers" bu
LEFT JOIN "Operators" o ON bu.OperatorId = o.Id
GROUP BY bu.Role, o.Name;

-- Check tables that will be affected
SELECT 'Tables with OperatorId' as info;
SELECT 'Brands' as table_name, COUNT(*) as rows_with_operatorid FROM "Brands" WHERE OperatorId IS NOT NULL;
SELECT 'BackofficeUsers' as table_name, COUNT(*) as rows_with_operatorid FROM "BackofficeUsers" WHERE OperatorId IS NOT NULL;
SELECT 'Ledger' as table_name, COUNT(*) as rows_with_operatorid FROM "Ledger" WHERE OperatorId IS NOT NULL;
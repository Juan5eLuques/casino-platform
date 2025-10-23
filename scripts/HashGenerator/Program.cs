using Microsoft.AspNetCore.Identity;

Console.WriteLine("==============================================");
Console.WriteLine("Password Hash Generator - Casino Platform");
Console.WriteLine("==============================================");
Console.WriteLine();

var hasher = new PasswordHasher<object>();

// Obtener password desde argumentos o usar default
string password = args.Length > 0 ? args[0] : "password";
string hash = hasher.HashPassword(new object(), password);

Console.WriteLine($"Password: {password}");
Console.WriteLine($"Hash Generated: {hash}");
Console.WriteLine();

// Verificar que el hash funciona
var verificationResult = hasher.VerifyHashedPassword(new object(), hash, password);
Console.WriteLine($"Verification: {verificationResult}");
Console.WriteLine();

Console.WriteLine("==============================================");
Console.WriteLine("SQL SCRIPT COMPLETO:");
Console.WriteLine("==============================================");
Console.WriteLine();

var sql = $@"-- Auto-generated script
-- Password: {password}
-- Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

BEGIN;

DELETE FROM ""BackofficeUsers"" WHERE ""Username"" = 'superadmin';

INSERT INTO ""BackofficeUsers"" (
    ""Id"",
    ""BrandId"",
    ""Username"",
    ""PasswordHash"",
    ""Role"",
""Status"",
    ""CreatedAt"",
    ""LastLoginAt"",
    ""ParentCashierId"",
    ""ParentAdminId"",
    ""HierarchyLevel"",
    ""HierarchyPath"",
    ""CommissionPercent"",
  ""CreatedByUserId"",
    ""CreatedByRole"",
    ""WalletBalance""
)
VALUES (
    gen_random_uuid(),
    NULL,
    'superadmin',
    '{hash}',
    'SUPER_ADMIN',
    'ACTIVE',
    CURRENT_TIMESTAMP,
    NULL,
    NULL,
    NULL,
    0,
    NULL,
    0,
    NULL,
    NULL,
    0.00
);

SELECT 
    ""Id"",
    ""Username"",
    ""Role"",
    ""Status"",
    ""CreatedAt""
FROM ""BackofficeUsers""
WHERE ""Username"" = 'superadmin';

COMMIT;";

Console.WriteLine(sql);
Console.WriteLine();
Console.WriteLine("==============================================");
Console.WriteLine("COPIA EL SQL DE ARRIBA Y EJECUTALO EN TU BD");
Console.WriteLine("==============================================");

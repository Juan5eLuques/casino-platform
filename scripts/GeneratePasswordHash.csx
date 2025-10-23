using Microsoft.AspNetCore.Identity;

// Herramienta simple para generar hash de contraseña
// Compatible con PasswordService.cs de Casino Platform

Console.WriteLine("==============================================");
Console.WriteLine("Password Hash Generator - Casino Platform");
Console.WriteLine("==============================================");
Console.WriteLine();

var hasher = new PasswordHasher<object>();

// Generar hash para la contraseña por defecto
string password = "password";
string hash = hasher.HashPassword(new object(), password);

Console.WriteLine($"Password: {password}");
Console.WriteLine($"Hash: {hash}");
Console.WriteLine();
Console.WriteLine("Copia este hash en el archivo SQL create-superadmin.sql");
Console.WriteLine();

// Verificar que el hash funciona
var verificationResult = hasher.VerifyHashedPassword(new object(), hash, password);
Console.WriteLine($"Verificación: {verificationResult}");
Console.WriteLine();

// Generar SQL completo
Console.WriteLine("==============================================");
Console.WriteLine("SQL COMPLETO PARA COPIAR:");
Console.WriteLine("==============================================");
Console.WriteLine();

var sql = $@"-- Script generado automáticamente
-- Password: {password}
-- Generado: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

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

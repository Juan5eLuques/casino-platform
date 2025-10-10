using Microsoft.AspNetCore.Identity;

// Programa simple para generar hash de password compatible con el sistema
var passwordHasher = new PasswordHasher<object>();

Console.WriteLine("?? Generador de Hash para Passwords");
Console.WriteLine("=====================================");

// Generar hashes para passwords comunes
var passwords = new[] { "admin123", "hola1234", "password123", "admin", "test123" };

foreach (var password in passwords)
{
    var hash = passwordHasher.HashPassword(new object(), password);
    Console.WriteLine($"Password: {password}");
    Console.WriteLine($"Hash:     {hash}");
    Console.WriteLine($"Length:   {hash.Length} characters");
    Console.WriteLine();
    
    // Verificar que el hash funciona
    var result = passwordHasher.VerifyHashedPassword(new object(), hash, password);
    Console.WriteLine($"Verification: {result}");
    Console.WriteLine("".PadRight(50, '-'));
}

Console.WriteLine("\n? Usa cualquiera de estos hashes en tu base de datos");
Console.WriteLine("?? Recuerda: el orden en VerifyPassword es (password, hash)");
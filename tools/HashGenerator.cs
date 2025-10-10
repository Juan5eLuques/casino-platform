using BCrypt.Net;

Console.WriteLine("Generating BCrypt hash for 'hola1234'...");
string password = "hola1234";
string hash = BCrypt.Net.BCrypt.HashPassword(password, 11);
Console.WriteLine($"Password: {password}");
Console.WriteLine($"BCrypt Hash: {hash}");
Console.WriteLine();
Console.WriteLine("Copy this hash to your SQL script:");
Console.WriteLine($"'{hash}'");
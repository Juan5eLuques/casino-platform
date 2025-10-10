using System;
using BCrypt.Net;

// Simple utility to generate BCrypt hash for password
// Usage: dotnet run -- "password"

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: dotnet run -- \"password\"");
            Console.WriteLine("Example: dotnet run -- \"hola1234\"");
            return;
        }

        string password = args[0];
        string hash = BCrypt.Net.BCrypt.HashPassword(password, 11);
        
        Console.WriteLine($"Password: {password}");
        Console.WriteLine($"BCrypt Hash: {hash}");
        Console.WriteLine();
        Console.WriteLine("SQL Insert example:");
        Console.WriteLine($"INSERT INTO \"BackofficeUsers\" (\"PasswordHash\") VALUES ('{hash}');");
    }
}
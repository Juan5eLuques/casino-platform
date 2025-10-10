using Microsoft.AspNetCore.Identity;
using Casino.Infrastructure.Data;
using Casino.Domain.Entities;
using Casino.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Casino.Scripts;

/// <summary>
/// Script para crear usuarios administradores con password hasheado correctamente
/// </summary>
public class CreateAdminUserScript
{
    public static async Task RunAsync(CasinoDbContext context)
    {
        var passwordHasher = new PasswordHasher<object>();
        
        // Password por defecto para todos los usuarios de prueba
        const string defaultPassword = "admin123";
        var hashedPassword = passwordHasher.HashPassword(new object(), defaultPassword);
        
        Console.WriteLine($"Generated password hash: {hashedPassword}");
        Console.WriteLine($"Default password for all users: {defaultPassword}");
        
        // 1. Crear operador de prueba si no existe
        var operatorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var testOperator = await context.Operators.FirstOrDefaultAsync(o => o.Id == operatorId);
        
        if (testOperator == null)
        {
            testOperator = new Operator
            {
                Id = operatorId,
                Name = "Test Operator",
                Status = OperatorStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow
            };
            context.Operators.Add(testOperator);
            Console.WriteLine("? Created test operator");
        }
        else
        {
            Console.WriteLine("? Test operator already exists");
        }
        
        // 2. Lista de usuarios a crear
        var usersToCreate = new[]
        {
            new { Username = "superadmin", Role = BackofficeUserRole.SUPER_ADMIN },
            new { Username = "operator_admin", Role = BackofficeUserRole.OPERATOR_ADMIN },
            new { Username = "cashier_user", Role = BackofficeUserRole.CASHIER }
        };
        
        foreach (var userInfo in usersToCreate)
        {
            var existingUser = await context.BackofficeUsers
                .FirstOrDefaultAsync(u => u.Username == userInfo.Username);
                
            if (existingUser == null)
            {
                var newUser = new BackofficeUser
                {
                    Id = Guid.NewGuid(),
                    OperatorId = operatorId,
                    Username = userInfo.Username,
                    PasswordHash = hashedPassword,
                    Role = userInfo.Role,
                    Status = BackofficeUserStatus.ACTIVE,
                    CreatedAt = DateTime.UtcNow
                };
                
                context.BackofficeUsers.Add(newUser);
                Console.WriteLine($"? Created user: {userInfo.Username} ({userInfo.Role})");
            }
            else
            {
                // Actualizar password si el usuario ya existe
                existingUser.PasswordHash = hashedPassword;
                Console.WriteLine($"? Updated password for existing user: {userInfo.Username}");
            }
        }
        
        await context.SaveChangesAsync();
        Console.WriteLine("\n? All users created/updated successfully!");
        Console.WriteLine($"\nLogin credentials for all users:");
        Console.WriteLine($"Password: {defaultPassword}");
        Console.WriteLine("\nUsernames:");
        foreach (var user in usersToCreate)
        {
            Console.WriteLine($"- {user.Username} ({user.Role})");
        }
    }
}

/// <summary>
/// Programa de consola para ejecutar el script
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("?? Creating admin users...");
        
        // Configurar connection string (usar la misma de appsettings.json)
        var connectionString = "Host=shortline.proxy.rlwy.net;Port=47433;Database=railway;Username=postgres;Password=dzPvAkviRrmLjpinAeNakUymDpWaHVuq;SSL Mode=Require;Trust Server Certificate=true;";
        
        var options = new DbContextOptionsBuilder<CasinoDbContext>()
            .UseNpgsql(connectionString)
            .Options;
            
        using var context = new CasinoDbContext(options);
        
        try
        {
            await CreateAdminUserScript.RunAsync(context);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
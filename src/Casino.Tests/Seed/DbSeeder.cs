using System;
using System.Threading.Tasks;
using Casino.Domain.Users;
using Casino.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Casino.Tests.Seed;

public static class DbSeeder
{
    public static async Task SeedTestUsersAsync(AppDbContext db)
    {
        if (!await db.Users.AnyAsync(u => u.Email == "prueba@gmail.com"))
        {
            db.Users.Add(new User
            {
                Email = "prueba@gmail.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("hola1234"),
                Role = Role.ADMIN,
                Balance = 10000,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        if (!await db.Users.AnyAsync(u => u.Email == "player@gmail.com"))
        {
            db.Users.Add(new User
            {
                Email = "jugador@gmail.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("jugador123"),
                Role = Role.PLAYER,
                Balance = 1000,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();
    }
}

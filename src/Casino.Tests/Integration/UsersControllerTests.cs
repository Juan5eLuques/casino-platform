using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Casino.Application.Features.Users;
using Casino.Application.Features.Users.Commands;
using Casino.Application.Features.Users.Queries;
using Casino.Domain.Users;
using Casino.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Casino.Tests.Seed;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Casino.Tests.Integration;

public class UsersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public UsersControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Seed DB before each test run
                using var scope = services.BuildServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
                DbSeeder.SeedTestUsersAsync(db).GetAwaiter().GetResult();
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateUser_AsAdmin_ShouldCreateCashier()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Crear un admin
        var admin = new User
        {
            Username = "admintest",
            Email = "admin@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = Role.ADMIN
        };
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        // Simular autenticación
        var token = GenerateJwtToken(admin.Id, admin.Email, admin.Role.ToString());
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateUserRequest(
            Username: "cashiertest",
            Email: "cashier@test.com",
            Password: "password",
            Role: "CASHIER",
            CommissionRate: 10.5m
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/users", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<UserResponse>();
        
        Assert.NotNull(result);
        Assert.Equal("cashiertest", result.Username);
        Assert.Equal("cashier@test.com", result.Email);
        Assert.Equal("CASHIER", result.Role);
        Assert.Equal(10.5m, result.CommissionRate);
        Assert.Equal(admin.Id, result.ParentUserId);
    }

    [Fact]
    public async Task CreateUser_AsCashier_ShouldNotCreateAdmin()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Crear un cajero
        var cashier = new User
        {
            Username = "cashiertest",
            Email = "cashier@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = Role.CASHIER
        };
        db.Users.Add(cashier);
        await db.SaveChangesAsync();

        // Simular autenticación
        var token = GenerateJwtToken(cashier.Id, cashier.Email, cashier.Role.ToString());
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateUserRequest(
            Username: "admintest",
            Email: "admin@test.com",
            Password: "password",
            Role: "ADMIN"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/users", request);

        // Assert
        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task GetMyUsers_ShouldReturnDirectSubordinates()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Crear jerarquía: Admin -> Cashier -> Player
        var admin = new User
        {
            Username = "admintest",
            Email = "admin@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = Role.ADMIN
        };
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        var cashier = new User
        {
            Username = "cashiertest",
            Email = "cashier@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = Role.CASHIER,
            ParentUserId = admin.Id
        };
        db.Users.Add(cashier);
        await db.SaveChangesAsync();

        var player = new User
        {
            Username = "playertest",
            Email = "player@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = Role.PLAYER,
            ParentUserId = cashier.Id
        };
        db.Users.Add(player);
        await db.SaveChangesAsync();

        // Simular autenticación del admin
        var token = GenerateJwtToken(admin.Id, admin.Email, admin.Role.ToString());
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/users/my-users");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<UserResponse>>();
        
        Assert.NotNull(result);
        Assert.Single(result); // Solo debería ver al cajero, no al jugador
        Assert.Equal("cashiertest", result[0].Username);
    }

    private string GenerateJwtToken(int id, string email, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var jwtService = scope.ServiceProvider.GetRequiredService<Casino.Application.Abstractions.IJwtService>();
        return jwtService.CreateToken(id, email, role);
    }
}
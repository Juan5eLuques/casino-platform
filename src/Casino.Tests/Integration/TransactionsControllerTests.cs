using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Casino.Api;
using Casino.Application.Features.Auth;
using Casino.Application.Features.Transactions;
using Casino.Infrastructure.Persistence;
using Casino.Tests.Seed;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

namespace Casino.Tests.Integration;

public class TransactionsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TransactionsControllerTests(WebApplicationFactory<Program> factory)
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
    }

    [Fact]
    public async Task Admin_Can_Login_And_See_History()
    {
        var client = _factory.CreateClient();
        var token = await GetJwtTokenAsync(client, "prueba@gmail.com", "hola1234");
        token.Should().NotBeNullOrWhiteSpace();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var historyResp = await client.GetAsync("/api/transactions/history");
        historyResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Admin_Can_Load_And_Unload_Chips()
    {
        var client = _factory.CreateClient();
        var token = await GetJwtTokenAsync(client, "prueba@gmail.com", "hola1234");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Cargar fichas a jugador
        var loadResp = await client.PostAsJsonAsync("/api/transactions/load", new LoadChipsCommand(ToUserId: 2, Amount: 500));
        loadResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var loadResult = await loadResp.Content.ReadFromJsonAsync<TransferResult>();
        loadResult.Success.Should().BeTrue();

        // Descargar fichas del jugador
        var unloadResp = await client.PostAsJsonAsync("/api/transactions/unload", new UnloadChipsCommand(FromUserId: 2, Amount: 200));
        unloadResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var unloadResult = await unloadResp.Content.ReadFromJsonAsync<TransferResult>();
        unloadResult.Success.Should().BeTrue();
    }

    private async Task<string> GetJwtTokenAsync(HttpClient client, string email, string password)
    {
        var resp = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<AuthResponse>();
        return body.Token;
    }
}

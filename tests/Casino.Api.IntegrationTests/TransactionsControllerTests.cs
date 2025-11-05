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

namespace Casino.Api.IntegrationTests;

public class TransactionsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TransactionsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task History_ReturnsUnauthorized_WhenNoToken()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/transactions/history");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Transfer_ReturnsBadRequest_WhenAmountZero()
    {
        var client = _factory.CreateClient();
        // arrange: create test user and get token - assumes test DB seeded or adjust accordingly
        var token = await GetJwtTokenAsync(client, "test1@example.com", "password");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.PostAsJsonAsync("/api/transactions/transfer", new TransferRequest(2, 0));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await resp.Content.ReadFromJsonAsync<TransferResult>();
        result.Success.Should().BeFalse();
    }

    private async Task<string> GetJwtTokenAsync(HttpClient client, string email, string password)
    {
        var resp = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<AuthResponse>();
        return body.Token;
    }
}

using Casino.Api.Endpoints;
using Casino.Application.Services;
using Casino.Application.Services.Implementations;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON options for enum handling
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

// Add services to the container
builder.Services.AddDbContext<CasinoDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Register essential services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map health check endpoint
app.MapGet("/health", () => "OK");

// Map authentication login endpoint (unprotected)
app.MapPost("/api/v1/admin/auth/login", AuthEndpoints.AdminLogin)
    .WithName("AdminLogin")
    .WithSummary("Admin login");

// Map the /me endpoint temporarily without authorization to test
app.MapGet("/api/v1/admin/auth/me", AuthEndpoints.GetAdminProfile)
    .WithName("GetAdminProfile")
    .WithSummary("Get current admin user profile");

// Basic hello world endpoint
app.MapGet("/", () => new { 
    Message = "Casino Platform API", 
    Version = "1.0.0",
    Environment = app.Environment.EnvironmentName,
    Timestamp = DateTime.UtcNow
});

app.Run();
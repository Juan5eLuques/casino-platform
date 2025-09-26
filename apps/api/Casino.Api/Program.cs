using Casino.Api.Endpoints;
using Casino.Api.Filters;
using Casino.Application.Services;
using Casino.Application.Services.Implementations;
using Casino.Application.Validators;
using Casino.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddDbContext<CasinoDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Register application services
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IRoundService, RoundService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IGameService, GameService>();

// Register validators
builder.Services.AddValidatorsFromAssemblyContaining<DebitRequestValidator>();

// Register filters
builder.Services.AddScoped<HmacEndpointFilter>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Default")!);

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "Casino Platform API", 
        Version = "v1",
        Description = "B2B Casino Platform with virtual chips"
    });
    
    c.AddSecurityDefinition("HMAC", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "X-Signature",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "HMAC-SHA256 signature for gateway endpoints"
    });
});

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Casino Platform API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();
app.UseCors();

// Add structured logging middleware
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var correlationId = Guid.NewGuid().ToString();
    
    context.Items["CorrelationId"] = correlationId;
    
    using (logger.BeginScope(new Dictionary<string, object>
    {
        ["CorrelationId"] = correlationId,
        ["RequestPath"] = context.Request.Path,
        ["RequestMethod"] = context.Request.Method
    }))
    {
        logger.LogInformation("Request started: {Method} {Path}", 
            context.Request.Method, context.Request.Path);
        
        await next();
        
        logger.LogInformation("Request completed: {Method} {Path} - {StatusCode}", 
            context.Request.Method, context.Request.Path, context.Response.StatusCode);
    }
});

// Map health check endpoint
app.MapHealthChecks("/health")
    .WithTags("Health")
    .WithName("HealthCheck");

// Map API endpoints
app.MapWalletEndpoints();
app.MapGatewayEndpoints();
app.MapSessionEndpoints();
app.MapGameEndpoints();
app.MapAdminEndpoints();

// Basic hello world endpoint
app.MapGet("/", () => new { 
    Message = "Casino Platform API", 
    Version = "1.0.0",
    Environment = app.Environment.EnvironmentName,
    Timestamp = DateTime.UtcNow
})
.WithTags("Info")
.WithName("GetApiInfo");

// Ensure database is created (for development)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<CasinoDbContext>();
    try
    {
        await context.Database.EnsureCreatedAsync();
        app.Logger.LogInformation("Database ensured created successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error ensuring database is created");
    }
}

app.Run();

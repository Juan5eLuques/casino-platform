using Casino.Api.Endpoints;
using Casino.Api.Filters;
using Casino.Api.Middleware;
using Casino.Application.Services;
using Casino.Application.Services.Implementations;
using Casino.Application.Validators;
using Casino.Infrastructure.Data;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddDbContext<CasinoDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Register brand context as scoped service
builder.Services.AddScoped<BrandContext>();

// Register application services
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IRoundService, RoundService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();

// Register new services for missing endpoints
builder.Services.AddScoped<IOperatorService, OperatorService>();
builder.Services.AddScoped<IBackofficeUserService, BackofficeUserService>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IBrandGameService, BrandGameService>();

// Register validators
builder.Services.AddValidatorsFromAssemblyContaining<DebitRequestValidator>();

// Register filters
builder.Services.AddScoped<HmacEndpointFilter>();

// Configure JWT Authentication
var jwtKey = builder.Configuration["Auth:JwtKey"] ?? throw new InvalidOperationException("Auth:JwtKey is required");
var issuer = builder.Configuration["Auth:Issuer"] ?? "casino";
var clockSkew = TimeSpan.FromMinutes(2);
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication()
    .AddJwtBearer("BackofficeJwt", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = "backoffice",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = true,
            ClockSkew = clockSkew,
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Check Authorization header first
                var auth = context.Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(auth) && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    return Task.CompletedTask;

                // Fallback to cookie
                if (context.Request.Cookies.TryGetValue("bk.token", out var token))
                    context.Token = token;

                return Task.CompletedTask;
            }
        };
    })
    .AddJwtBearer("PlayerJwt", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = "player",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = true,
            ClockSkew = clockSkew,
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Check Authorization header first
                var auth = context.Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(auth) && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    return Task.CompletedTask;

                // Fallback to cookie
                if (context.Request.Cookies.TryGetValue("pl.token", out var token))
                    context.Token = token;

                return Task.CompletedTask;
            }
        };
    });

// Configure Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("BackofficePolicy", policy =>
        policy.RequireAuthenticatedUser()
              .AddAuthenticationSchemes("BackofficeJwt")
              .RequireClaim(ClaimTypes.Role, "SUPER_ADMIN", "OPERATOR_ADMIN", "CASHIER"));

    options.AddPolicy("PlayerPolicy", policy =>
        policy.RequireAuthenticatedUser()
              .AddAuthenticationSchemes("PlayerJwt")
              .RequireClaim(ClaimTypes.Role, "PLAYER"));
});

// Remove default CORS since we'll use dynamic CORS
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
        Description = "B2B Casino Platform with virtual chips, multi-site support and JWT authentication"
    });
    
    // Resolver colisiones de nombres en Swagger
    c.CustomSchemaIds(t => (t.FullName ?? t.Name).Replace('+', '.'));
    
    // Resolver conflictos de rutas duplicadas (temporal mientras se limpia)
    c.ResolveConflictingActions(apiDescriptions =>
        apiDescriptions.OrderByDescending(d => d.SupportedResponseTypes.Count).First());
    
    // Add JWT Bearer security schemes
    c.AddSecurityDefinition("BackofficeBearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header for backoffice users (audience: backoffice)"
    });
    
    c.AddSecurityDefinition("PlayerBearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header for players (audience: player)"
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

// Add authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

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
        ["RequestMethod"] = context.Request.Method,
        ["Host"] = context.Request.Host.Host
    }))
    {
        logger.LogInformation("Request started: {Method} {Path} from {Host}", 
            context.Request.Method, context.Request.Path, context.Request.Host.Host);
        
        await next();
        
        logger.LogInformation("Request completed: {Method} {Path} - {StatusCode}", 
            context.Request.Method, context.Request.Path, context.Response.StatusCode);
    }
});

// Add brand resolver middleware (before endpoints)
app.UseMiddleware<BrandResolverMiddleware>();

// Add dynamic CORS middleware (after brand resolver)
app.UseMiddleware<DynamicCorsMiddleware>();

// Map health check endpoint
app.MapHealthChecks("/health")
    .WithTags("Health")
    .WithName("HealthCheck");

// Map authentication endpoints (unprotected)
app.MapAuthEndpoints();

// Map protected API endpoints with authorization
var adminGroup = app.MapGroup("/api/v1/admin")
    .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = "BackofficeJwt", Policy = "BackofficePolicy" });

var playerGroup = app.MapGroup("/api/v1/player")
    .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = "PlayerJwt", Policy = "PlayerPolicy" });

// Map existing endpoints to protected groups
adminGroup.MapWalletEndpoints();
adminGroup.MapSessionEndpoints();
adminGroup.MapGameEndpoints();
adminGroup.MapBrandAdminEndpoints();
adminGroup.MapAdminEndpoints();

// Map new endpoints for complete site creation
adminGroup.MapOperatorEndpoints();
adminGroup.MapBackofficeUserEndpoints();
adminGroup.MapPlayerManagementEndpoints();
adminGroup.MapBrandGameEndpoints();

// Map public endpoints (no auth required)
app.MapGatewayEndpoints(); // HMAC protected, no JWT required
app.MapCatalogEndpoints(); // Public catalog, brand-scoped

// Basic hello world endpoint
app.MapGet("/", (BrandContext brandContext) => new { 
    Message = "Casino Platform API", 
    Version = "1.0.0",
    Environment = app.Environment.EnvironmentName,
    Timestamp = DateTime.UtcNow,
    Brand = brandContext.IsResolved ? new 
    { 
        brandContext.BrandId, 
        brandContext.BrandCode, 
        brandContext.Domain 
    } : null
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

using Casino.Api.Endpoints;
using Casino.Api.Filters;
using Casino.Api.Middleware;
using Casino.Application.Services;
using Casino.Application.Services.Implementations;
using Casino.Application.Validators;
using Casino.Application.DTOs.Wallet;
using Casino.Infrastructure.Data;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Reflection;

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

// Register brand context as scoped service
builder.Services.AddScoped<BrandContext>();

// Register application services - CLEAN VERSION
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IRoundService, RoundService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();

// Register new services for missing endpoints
builder.Services.AddScoped<IBackofficeUserService, BackofficeUserService>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IBrandGameService, BrandGameService>();
builder.Services.AddScoped<ICashierPlayerService, CashierPlayerService>();

// SONNET: Unified user service - RESTAURA funcionalidad original de /users
builder.Services.AddScoped<IUnifiedUserService, UnifiedUserService>();

// SONNET: Wallet services - UNIFIED SYSTEM
// Unified wallet service for gateway/games (uses Player.WalletBalance + WalletTransactions)
builder.Services.AddScoped<IWalletService, UnifiedWalletService>();
// Admin transaction service (uses UnifiedWalletService internally for complete unification)
builder.Services.AddScoped<IAdminTransactionService, AdminTransactionService>();
// Simple wallet service DEPRECATED (commented out due to compilation issues) 
// builder.Services.AddScoped<ISimpleWalletService, SimpleWalletService>();
// Legacy wallet service DEPRECATED (kept for rollback if needed)
builder.Services.AddScoped<ILegacyWalletService, LegacyWalletService>();

// SONNET: FluentValidation - REGISTER ALL VALIDATORS BY ASSEMBLY
// This replaces individual AddScoped<IValidator<...>> registrations
builder.Services.AddValidatorsFromAssembly(typeof(CreateTransactionRequestValidator).Assembly); // Application assembly
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly); // API assembly (if any validators here)

// SONNET: Enable FluentValidation auto-validation for Minimal APIs
builder.Services.AddFluentValidationAutoValidation();

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
                // 1) Leer desde Authorization: Bearer header primero
                var auth = context.Request.Headers.Authorization.ToString();
                if (!string.IsNullOrWhiteSpace(auth) && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    context.Token = auth.Substring("Bearer ".Length).Trim();
                    return Task.CompletedTask;
                }
                
                // 2) Fallback: leer desde cookie bk.token
                if (context.Request.Cookies.TryGetValue("bk.token", out var cookieToken))
                {
                    context.Token = cookieToken;
                }

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError("🚨 JWT Authentication FAILED: {Error}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var user = context.Principal?.Identity?.Name;
                var role = context.Principal?.FindFirst(ClaimTypes.Role)?.Value;
                logger.LogInformation("✅ JWT Token VALIDATED - User: {User}, Role: {Role}", user, role);
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
    // Política básica para backoffice (mantener compatibilidad)
    options.AddPolicy("BackofficePolicy", policy =>
        policy.RequireAuthenticatedUser()
              .AddAuthenticationSchemes("BackofficeJwt")
              .RequireClaim(ClaimTypes.Role, "SUPER_ADMIN", "BRAND_ADMIN", "CASHIER"));

    // Política básica para players (mantener compatibilidad)
    options.AddPolicy("PlayerPolicy", policy =>
        policy.RequireAuthenticatedUser()
              .AddAuthenticationSchemes("PlayerJwt")
              .RequireClaim(ClaimTypes.Role, "PLAYER"));

    // === NUEVAS POLÍTICAS PARA BRAND-ONLY SYSTEM ===

    // Solo SUPER_ADMIN (acceso global)
    options.AddPolicy("SuperAdminOnly", policy =>
        policy.RequireAuthenticatedUser()
              .AddAuthenticationSchemes("BackofficeJwt")
              .RequireClaim(ClaimTypes.Role, "SUPER_ADMIN"));

    // BRAND_ADMIN con scope a su brand (requiere brand context válido)
    options.AddPolicy("BrandScopedAdmin", policy =>
        policy.RequireAuthenticatedUser()
              .AddAuthenticationSchemes("BackofficeJwt")
              .RequireClaim(ClaimTypes.Role, "BRAND_ADMIN"));

    // BRAND_ADMIN o CASHIER con scope a su brand
    options.AddPolicy("BrandScopedCashierOrAdmin", policy =>
        policy.RequireAuthenticatedUser()
              .AddAuthenticationSchemes("BackofficeJwt")
              .RequireClaim(ClaimTypes.Role, "BRAND_ADMIN", "CASHIER"));

    // SUPER_ADMIN o BRAND_ADMIN (con diferentes niveles de acceso)
    options.AddPolicy("AdminOrSuperAdmin", policy =>
        policy.RequireAuthenticatedUser()
              .AddAuthenticationSchemes("BackofficeJwt")
              .RequireClaim(ClaimTypes.Role, "SUPER_ADMIN", "BRAND_ADMIN"));

    // NUEVA: Política inclusiva para cualquier usuario autenticado de backoffice
    options.AddPolicy("AnyBackofficeUser", policy =>
        policy.RequireAuthenticatedUser()
              .AddAuthenticationSchemes("BackofficeJwt")
              .RequireClaim(ClaimTypes.Role, "SUPER_ADMIN", "BRAND_ADMIN", "CASHIER"));
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
        Description = "B2B Casino Platform with virtual chips, multi-site support and JWT authentication"
    });
    
    // SONNET: Resolver colisiones de nombres en Swagger
    c.CustomSchemaIds(t => (t.FullName ?? t.Name).Replace('+', '.'));
    
    // SONNET: Resolver conflictos de rutas duplicadas
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

// SONNET: ORDEN CORRECTO DE MIDDLEWARES (verificado)
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

// SONNET: 1) UseForwardedHeaders
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                      Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto |
                      Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost
});

// SONNET: 2) BrandResolverMiddleware
app.UseMiddleware<BrandResolverMiddleware>();

// SONNET: 3) DynamicCorsMiddleware
app.UseMiddleware<DynamicCorsMiddleware>();

// SONNET: 4) Authentication y Authorization
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

// SONNET: Map health check endpoint
app.MapHealthChecks("/health")
    .WithTags("Health")
    .WithName("HealthCheck");

// SONNET: Map authentication endpoints (unprotected)
app.MapAuthEndpoints();

// === GATEWAY ENDPOINTS (UNPROTECTED) ===
// SONNET: Mantener compatibilidad con providers externos usando LegacyWalletService
app.MapGatewayEndpoints();

// === INTERNAL WALLET ENDPOINTS (UNPROTECTED) ===
// SONNET: Endpoints internos para compatibilidad con gateway usando LegacyWalletService
app.MapInternalWalletEndpoints();

// === UNIFIED ADMIN TRANSACTION SYSTEM ===
// SONNET: Sistema administrativo unificado que usa UnifiedWalletService
app.MapAdminTransactionEndpoints();

// === SIMPLE WALLET SYSTEM (DEPRECATED) ===
// SONNET: Sistema simple de transacciones DEPRECATED - usar AdminTransactionEndpoints
// app.MapSimpleWalletEndpoints();

// === WORKING CORE ENDPOINTS ===
// SONNET: Map protected API endpoints with authorization - UN SOLO MAPGROUP por /api/v1/admin
var adminGroup = app.MapGroup("/api/v1/admin")
    .RequireAuthorization("BackofficePolicy");

adminGroup.MapSessionEndpoints();
adminGroup.MapGameEndpoints();
adminGroup.MapAuditEndpoints();
adminGroup.MapBrandGameEndpoints();

// SONNET: UNIFIED USER ENDPOINTS - Restaura funcionalidad original de /users
adminGroup.MapUnifiedUserEndpoints();

// SONNET: DEPRECATED - Endpoints específicos por tipo de usuario (comentados para evitar duplicación)
// adminGroup.MapBrandOnlyBackofficeUserEndpoints();
// adminGroup.MapBrandOnlyPlayerEndpoints();

adminGroup.MapPasswordEndpoints();

app.Run();

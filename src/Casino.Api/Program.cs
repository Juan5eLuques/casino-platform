using Casino.Application.Features.Auth;
using Casino.Application.Features.Auth.Commands;
using Casino.Application.Features.Auth.Queries;
using Casino.Infrastructure;
using Casino.Infrastructure.Features.Auth; // para obtener la Assembly con los handlers
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Swagger con configuración mejorada para JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Casino API", Version = "v1" });
    
    // Configuración mejorada de JWT para Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. \r\n\r\n" +
                      "Ingresa 'Bearer' [espacio] y luego tu token JWT.\r\n\r\n" +
                      "Ejemplo: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });

    // Incluir comentarios XML si los tienes
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// MediatR: registra Application (requests) + Infrastructure (handlers)
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(
        typeof(RegisterCommand).Assembly,   // Application
        typeof(RegisterHandler).Assembly,   // Infrastructure (handlers)
        typeof(Casino.Infrastructure.Features.Transactions.TransferHandler).Assembly,
        typeof(Casino.Infrastructure.Features.Transactions.TransferHistoryHandler).Assembly,
        typeof(Casino.Infrastructure.Features.Transactions.LoadChipsHandler).Assembly,
        typeof(Casino.Infrastructure.Features.Transactions.UnloadChipsHandler).Assembly,
        typeof(Casino.Infrastructure.Features.Users.CreateUserHandler).Assembly
    );
});

// Autorización (políticas/roles)
builder.Services.AddAuthorization();

// Infra (DbContext, JwtService, CurrentUser, etc. + JWT ya configurado)
builder.Services.AddInfrastructure(builder.Configuration);

// Habilita controladores
builder.Services.AddControllers();

// Build
var app = builder.Build();

// (Opcional) Auto-migrar en dev
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<Casino.Infrastructure.Persistence.AppDbContext>();
    try
    {
        await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
        logger.LogWarning(ex, "Could not run migrations");
    }
}

// Middlewares - ORDEN IMPORTANTE
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Casino API v1");
        c.DisplayRequestDuration();
        c.EnableTryItOutByDefault();
        // Mejorar la experiencia de autorización en Swagger
        c.DefaultModelsExpandDepth(-1);
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    });
}

app.UseCors(); // CORS antes de autenticación

app.UseAuthentication();  // Autenticación antes de autorización
app.UseAuthorization();   // Autorización después de autenticación

// Mapear controladores
app.MapControllers();

// Run
await app.RunAsync();

// Expose Program class for integration tests
public partial class Program { }

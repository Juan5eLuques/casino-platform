# ?? Análisis Completo: Sistema de Catálogo y Launch de Juegos

## ?? **Diagnóstico del Estado Actual**

### **Nivel de Madurez: 2.5/4** ???

#### ? **Lo que YA existe (Parcialmente Funcional)**

1. **Entidades Base** (? **Completo**)
   - `Game`: Juego global con `Id`, `Code`, `Provider`, `Name`, `Enabled`
   - `BrandGame`: Relación Brand-Game con `Enabled`, `DisplayOrder`, `Tags`
   - `GameSession`: Sesión de juego con `PlayerId`, `GameCode`, `Provider`, `Status`, `ExpiresAt`
   - `BrandProviderConfig`: Configuración de provider por brand con `Secret`, `Meta`

2. **Servicios Existentes** (? **Funcional**)
 - `IGameService`: CRUD completo de juegos y asignación a brands
   - `IBrandService`: Gestión de brands con `GetBrandCatalogAsync()`
   - `ISessionService`: Creación y gestión de sesiones de juego

3. **Endpoints de Administración** (? **Funcional**)
   - `POST /api/v1/admin/games` - Crear juego
   - `GET /api/v1/admin/games` - Listar juegos
   - `POST /api/v1/admin/catalog/brands/{brandId}/games/{gameId}` - Asignar juego
   - `GET /api/v1/admin/catalog/brands/{brandId}/games` - Catálogo del brand

4. **Endpoints de Catálogo Público** (? **Funcional**)
   - `GET /api/v1/catalog/games` - Obtener juegos del brand (desde `BrandContext`)

5. **Launch Básico** (?? **Parcial**)
   - `POST /api/v1/catalog/games/{gameCode}/launch` - Lanza juego y crea sesión
   - **Respuesta actual**: `{ sessionId, gameCode, gameUrl: "/games/{gameCode}?session={sessionId}", expiresAt }`
   - **Problema**: ? La URL es **interna**, no redirige a iframe del proveedor

---

#### ? **Lo que FALTA para Catálogo + Launch Multi-Proveedor**

### 1. **Campos Faltantes en `Game`**

La entidad actual es muy básica:

```csharp
public class Game
{
    public Guid Id { get; set; }
 public string Code { get; set; }     // ? Existe
    public string Provider { get; set; }  // ? Existe
    public string Name { get; set; }// ? Existe
    public bool Enabled { get; set; }     // ? Existe
    // ? FALTAN:
    // - string? LaunchId (ID del proveedor para launch)
    // - decimal? RTP
    // - string? Volatility (LOW, MEDIUM, HIGH)
    // - string? Category (slots, table, live, etc.)
    // - string? ImageUrl
    // - decimal? MinBet
  // - decimal? MaxBet
    // - bool? IsFeatured
 // - bool? IsNew
    // - string[]? AdditionalTags
}
```

---

### 2. **Proveedor No Modelado Correctamente**

Actualmente `Provider` es un **string suelto** en `Game`, no una entidad.

**Problemas**:
- ? No hay tabla `GameProviders`
- ? No hay metadata de proveedores (API endpoints, configuración)
- ? `BrandProviderConfig` solo guarda `Secret` y `AllowNegativeOnRollback`
- ? **No existe abstracción `IProviderAdapter`** para lanzar juegos

**Lo que se necesita**:
```csharp
public class GameProvider
{
    public Guid Id { get; set; }
public string Code { get; set; } // "pragmatic", "evolution", etc.
    public string Name { get; set; } // "Pragmatic Play"
    public string LaunchEndpointTemplate { get; set; } // "https://api.pragmatic.com/launch?token={token}"
    public bool RequiresSessionToken { get; set; }
    public bool SupportsRealMode { get; set; }
    public bool SupportsDemoMode { get; set; }
    public JsonDocument? DefaultMeta { get; set; }
    public bool Enabled { get; set; }
public DateTime CreatedAt { get; set; }
}
```

---

### 3. **Launch Handler Incompleto**

**Endpoint actual**:
```csharp
POST /api/v1/catalog/games/{gameCode}/launch
{
  "playerId": "guid",
  "expirationMinutes": 60
}
```

**Respuesta actual**:
```json
{
  "sessionId": "guid",
  "gameCode": "sweet-bonanza",
  "gameUrl": "/games/sweet-bonanza?session=guid", // ? URL INTERNA
  "expiresAt": "2025-01-23T12:00:00Z"
}
```

**Problemas**:
- ? No construye URL del proveedor real
- ? No genera `token` para el proveedor
- ? No consulta `BrandProviderConfig` para secret
- ? No hay interfaz `ILaunchHandler` o `IProviderAdapter`

**Lo que se necesita**:
```csharp
public interface IProviderAdapter
{
    string ProviderCode { get; }
    Task<GameLaunchResponse> LaunchGameAsync(LaunchGameRequest request);
}

public record LaunchGameRequest(
  string GameCode,
    string LaunchId,      // ID del proveedor para este juego
    Guid PlayerId,
    string PlayerUsername,
    decimal Balance,
    string Secret,        // Secret del BrandProviderConfig
    bool IsDemo,
    JsonDocument? ProviderMeta
);

public record GameLaunchResponse(
    string LaunchUrl,     // URL completa del iframe
    string SessionToken,  // Token para el proveedor
    DateTime ExpiresAt
);
```

---

### 4. **Logs de Launch No Existen**

Actualmente no hay tabla para guardar:
- ? `GameSession` ? Sesión interna con `PlayerId`, `GameCode`, `Provider`, `Status`
- ? **Falta**: `GameLaunchLog` ? Log de cada launch al proveedor con URL, token, response

```csharp
public class GameLaunchLog
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid PlayerId { get; set; }
    public string GameCode { get; set; }
    public string Provider { get; set; }
    public string LaunchUrl { get; set; }
    public string SessionToken { get; set; } // Encriptado
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

---

### 5. **Falta Endpoint `/casino/games/url/{provider}/{game}`**

El frontend espera:
```
GET /casino/games/url/pragmatic/sweet-bonanza
```

**Comportamiento esperado**:
1. Resolver el juego desde el catálogo
2. Obtener configuración del proveedor (secret, meta)
3. Llamar al adapter del proveedor para generar URL
4. Retornar **redirección 302** o HTML con iframe

**Actualmente**: ? No existe este endpoint

---

## ?? **Propuesta de Arquitectura Completa**

### **Objetivo Final**

Backend listo como **plataforma agregadora de juegos** con:
- ? Catálogo multi-proveedor con metadata rica
- ? Launch dinámico con adapters por proveedor
- ? URLs firmadas y sesiones seguras
- ? Logs de auditoría completos
- ? Estructura lista para agregar proveedores reales

---

## ?? **1. Modelo de Datos Completo**

### **1.1. Tabla `GameProviders` (NUEVA)**

```sql
CREATE TABLE "GameProviders" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "Code" varchar(50) UNIQUE NOT NULL,
    "Name" varchar(200) NOT NULL,
    "LaunchEndpointTemplate" text NOT NULL,
    "RequiresSessionToken" boolean DEFAULT true,
    "SupportsRealMode" boolean DEFAULT true,
    "SupportsDemoMode" boolean DEFAULT false,
    "DefaultMeta" jsonb,
    "Enabled" boolean DEFAULT true,
    "CreatedAt" timestamptz DEFAULT NOW(),
    "UpdatedAt" timestamptz DEFAULT NOW()
);

INSERT INTO "GameProviders" ("Code", "Name", "LaunchEndpointTemplate") VALUES
('pragmatic', 'Pragmatic Play', 'https://api.pragmaticplay.net/gs2c/openGame.do?gameSymbol={gameSymbol}&token={token}'),
('evolution', 'Evolution Gaming', 'https://api.evolution.com/launch?game={game}&token={token}'),
('mock', 'Mock Provider (Local)', 'https://demo.local/games/{gameCode}?session={session}');
```

---

### **1.2. Tabla `Games` (EXTENDIDA)**

```sql
ALTER TABLE "Games" ADD COLUMN "ProviderId" uuid REFERENCES "GameProviders"("Id");
ALTER TABLE "Games" ADD COLUMN "LaunchId" varchar(200); -- ID del proveedor para launch
ALTER TABLE "Games" ADD COLUMN "RTP" decimal(5,2);
ALTER TABLE "Games" ADD COLUMN "Volatility" varchar(20); -- LOW, MEDIUM, HIGH
ALTER TABLE "Games" ADD COLUMN "Category" varchar(50); -- slots, table, live
ALTER TABLE "Games" ADD COLUMN "ImageUrl" text;
ALTER TABLE "Games" ADD COLUMN "MinBet" decimal(18,2);
ALTER TABLE "Games" ADD COLUMN "MaxBet" decimal(18,2);
ALTER TABLE "Games" ADD COLUMN "IsFeatured" boolean DEFAULT false;
ALTER TABLE "Games" ADD COLUMN "IsNew" boolean DEFAULT false;
ALTER TABLE "Games" ADD COLUMN "AdditionalTags" text[];
ALTER TABLE "Games" ADD COLUMN "UpdatedAt" timestamptz DEFAULT NOW();

-- Ejemplo de juego actualizado:
UPDATE "Games" SET
    "ProviderId" = (SELECT "Id" FROM "GameProviders" WHERE "Code" = 'pragmatic'),
    "LaunchId" = 'vs20sbxmas', -- ID interno de Pragmatic
    "RTP" = 96.51,
 "Volatility" = 'HIGH',
    "Category" = 'slots',
  "ImageUrl" = 'https://cdn.pragmaticplay.net/games/sweet-bonanza.jpg',
    "MinBet" = 0.20,
    "MaxBet" = 100.00,
    "IsFeatured" = true,
    "IsNew" = false
WHERE "Code" = 'sweet-bonanza';
```

---

### **1.3. Tabla `GameLaunchLogs` (NUEVA)**

```sql
CREATE TABLE "GameLaunchLogs" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "SessionId" uuid NOT NULL REFERENCES "GameSessions"("Id"),
    "PlayerId" uuid NOT NULL REFERENCES "Players"("Id"),
    "GameId" uuid NOT NULL REFERENCES "Games"("Id"),
    "BrandId" uuid NOT NULL REFERENCES "Brands"("Id"),
    "Provider" varchar(50) NOT NULL,
 "LaunchUrl" text NOT NULL,
    "SessionToken" text NOT NULL, -- Encrypted
    "Success" boolean NOT NULL,
    "ErrorMessage" text,
    "IpAddress" varchar(45),
    "UserAgent" text,
    "CreatedAt" timestamptz DEFAULT NOW()
);

CREATE INDEX "idx_game_launch_logs_session_id" ON "GameLaunchLogs"("SessionId");
CREATE INDEX "idx_game_launch_logs_player_id" ON "GameLaunchLogs"("PlayerId");
CREATE INDEX "idx_game_launch_logs_created_at" ON "GameLaunchLogs"("CreatedAt");
```

---

## ??? **2. Servicios y Adapters**

### **2.1. Interfaz `IProviderAdapter`**

```csharp
namespace Casino.Application.Providers;

public interface IProviderAdapter
{
    string ProviderCode { get; }
    
    Task<GameLaunchResponse> LaunchGameAsync(LaunchGameRequest request, CancellationToken cancellationToken = default);
    
    Task<bool> ValidateSessionAsync(string sessionToken, CancellationToken cancellationToken = default);
}

public record LaunchGameRequest(
    string GameCode,
    string LaunchId,
    Guid PlayerId,
    string PlayerUsername,
    decimal PlayerBalance,
    string BrandSecret,
    bool IsDemo,
    string? ReturnUrl,
    JsonDocument? ProviderMeta
);

public record GameLaunchResponse(
    bool Success,
    string? LaunchUrl,
    string? SessionToken,
    DateTime? ExpiresAt,
    string? ErrorMessage
);
```

---

### **2.2. Adapter Mock (Para Pruebas)**

```csharp
namespace Casino.Application.Providers.Implementations;

public class MockProviderAdapter : IProviderAdapter
{
    public string ProviderCode => "mock";
    
    public Task<GameLaunchResponse> LaunchGameAsync(LaunchGameRequest request, CancellationToken cancellationToken = default)
    {
  // Mock: construir URL local con token
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var launchUrl = $"https://demo.local/games/{request.GameCode}?session={token}&player={request.PlayerId}";
     
    return Task.FromResult(new GameLaunchResponse(
      Success: true,
            LaunchUrl: launchUrl,
            SessionToken: token,
            ExpiresAt: DateTime.UtcNow.AddMinutes(60),
     ErrorMessage: null
        ));
    }
    
    public Task<bool> ValidateSessionAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        // Mock: siempre válido
        return Task.FromResult(true);
    }
}
```

---

### **2.3. Factory de Adapters**

```csharp
namespace Casino.Application.Providers;

public interface IProviderAdapterFactory
{
    IProviderAdapter? GetAdapter(string providerCode);
}

public class ProviderAdapterFactory : IProviderAdapterFactory
{
    private readonly IEnumerable<IProviderAdapter> _adapters;
    
    public ProviderAdapterFactory(IEnumerable<IProviderAdapter> adapters)
  {
        _adapters = adapters;
    }
    
    public IProviderAdapter? GetAdapter(string providerCode)
    {
  return _adapters.FirstOrDefault(a => a.ProviderCode.Equals(providerCode, StringComparison.OrdinalIgnoreCase));
    }
}
```

---

### **2.4. Servicio `IGameLaunchService`**

```csharp
namespace Casino.Application.Services;

public interface IGameLaunchService
{
    Task<GameLaunchResponse> LaunchGameAsync(
        string gameCode, 
        Guid playerId, 
        Guid brandId, 
        bool isDemo = false, 
    CancellationToken cancellationToken = default);
        
  Task<GameLaunchLog?> GetLaunchLogAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

public class GameLaunchService : IGameLaunchService
{
    private readonly CasinoDbContext _context;
    private readonly ISessionService _sessionService;
    private readonly IProviderAdapterFactory _adapterFactory;
    private readonly ILogger<GameLaunchService> _logger;
    
    public GameLaunchService(
      CasinoDbContext context,
        ISessionService sessionService,
        IProviderAdapterFactory adapterFactory,
        ILogger<GameLaunchService> logger)
    {
        _context = context;
        _sessionService = sessionService;
  _adapterFactory = adapterFactory;
        _logger = logger;
    }
    
    public async Task<GameLaunchResponse> LaunchGameAsync(
        string gameCode, 
  Guid playerId, 
        Guid brandId, 
        bool isDemo = false, 
        CancellationToken cancellationToken = default)
    {
        // 1. Obtener juego con proveedor
     var game = await _context.Games
   .Include(g => g.Provider)
     .FirstOrDefaultAsync(g => g.Code == gameCode && g.Enabled, cancellationToken);
      
        if (game == null)
return new GameLaunchResponse(false, null, null, null, "Game not found or disabled");
        
        // 2. Verificar que el juego esté asignado al brand
        var brandGame = await _context.BrandGames
      .FirstOrDefaultAsync(bg => bg.BrandId == brandId && bg.GameId == game.Id && bg.Enabled, cancellationToken);
    
        if (brandGame == null)
            return new GameLaunchResponse(false, null, null, null, "Game not available for this brand");
        
        // 3. Obtener configuración del proveedor
        var providerConfig = await _context.BrandProviderConfigs
            .FirstOrDefaultAsync(c => c.BrandId == brandId && c.ProviderCode == game.Provider, cancellationToken);
    
        if (providerConfig == null)
       return new GameLaunchResponse(false, null, null, null, "Provider not configured for this brand");
        
 // 4. Obtener player
        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.Id == playerId && p.BrandId == brandId, cancellationToken);
    
        if (player == null)
     return new GameLaunchResponse(false, null, null, null, "Player not found");
        
      // 5. Crear sesión de juego
        var sessionRequest = new CreateSessionRequest(playerId, gameCode, game.Provider, 60);
        var session = await _sessionService.CreateSessionAsync(sessionRequest, cancellationToken);
        
 // 6. Obtener adapter del proveedor
        var adapter = _adapterFactory.GetAdapter(game.Provider);
  if (adapter == null)
        {
            _logger.LogError("Provider adapter not found: {Provider}", game.Provider);
    return new GameLaunchResponse(false, null, null, null, $"Provider '{game.Provider}' not supported");
        }
        
      // 7. Llamar al adapter para generar launch URL
        var launchRequest = new LaunchGameRequest(
    game.Code,
     game.LaunchId ?? game.Code,
    playerId,
       player.Username,
            player.WalletBalance,
          providerConfig.Secret,
  isDemo,
  null,
    providerConfig.Meta
        );
        
        var launchResponse = await adapter.LaunchGameAsync(launchRequest, cancellationToken);
        
        // 8. Guardar log de launch
        var launchLog = new GameLaunchLog
      {
          Id = Guid.NewGuid(),
      SessionId = session.SessionId,
       PlayerId = playerId,
GameId = game.Id,
   BrandId = brandId,
            Provider = game.Provider,
            LaunchUrl = launchResponse.LaunchUrl ?? "",
      SessionToken = launchResponse.SessionToken ?? "",
   Success = launchResponse.Success,
            ErrorMessage = launchResponse.ErrorMessage,
   CreatedAt = DateTime.UtcNow
        };
        
    _context.GameLaunchLogs.Add(launchLog);
        await _context.SaveChangesAsync(cancellationToken);
  
        _logger.LogInformation("Game launch {Status}: {GameCode} for player {PlayerId}", 
 launchResponse.Success ? "SUCCESS" : "FAILED", gameCode, playerId);
        
    return launchResponse;
    }
    
    public async Task<GameLaunchLog?> GetLaunchLogAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.GameLaunchLogs
        .FirstOrDefaultAsync(l => l.SessionId == sessionId, cancellationToken);
    }
}
```

---

## ?? **3. Endpoints REST**

### **3.1. Endpoint de Launch** `/api/v1/casino/games/url/{provider}/{game}`

```csharp
namespace Casino.Api.Endpoints;

public static class CasinoEndpoints
{
    public static void MapCasinoEndpoints(this IEndpointRouteBuilder app)
    {
  var group = app.MapGroup("/api/v1/casino")
            .WithTags("Casino");
   
        // Endpoint de launch
     group.MapGet("/games/url/{provider}/{gameCode}", LaunchGameUrl)
 .WithName("LaunchGameUrl")
      .WithSummary("Launch game and redirect to provider iframe")
         .Produces(302)
  .Produces<LaunchGameUrlResponse>()
          .Produces(404)
        .Produces(400);
    }
    
    private static async Task<IResult> LaunchGameUrl(
        string provider,
        string gameCode,
        [FromQuery] string playerId,
        [FromQuery] bool demo = false,
        BrandContext brandContext,
        IGameLaunchService launchService,
        ILogger<Program> logger)
    {
        if (!brandContext.IsResolved)
        {
 return Results.Problem("Brand not resolved", statusCode: 400);
        }
  
        if (!Guid.TryParse(playerId, out var playerGuid))
   {
  return Results.Problem("Invalid player ID", statusCode: 400);
  }
     
    logger.LogInformation("Launching game: {GameCode} for player {PlayerId}, brand {BrandCode}", 
            gameCode, playerId, brandContext.BrandCode);
        
  var response = await launchService.LaunchGameAsync(
            gameCode, 
playerGuid, 
       brandContext.BrandId, 
            demo);
     
        if (!response.Success)
    {
  logger.LogWarning("Game launch failed: {ErrorMessage}", response.ErrorMessage);
      return Results.Problem(response.ErrorMessage, statusCode: 404);
}
        
        // Opción 1: Redirección 302 (recomendado)
        return Results.Redirect(response.LaunchUrl!);
        
        // Opción 2: JSON con URL (para iframe manual)
     // return TypedResults.Ok(new LaunchGameUrlResponse(response.LaunchUrl!, response.ExpiresAt!.Value));
    }
    
    public record LaunchGameUrlResponse(string LaunchUrl, DateTime ExpiresAt);
}
```

---

### **3.2. Endpoint de Catálogo Extendido** `/api/v1/catalog/games`

```csharp
// Modificar CatalogEndpoints.cs existente

private static async Task<IResult> GetCatalogGames(
    BrandContext brandContext,
    CasinoDbContext context,
    [FromQuery] string? category = null,
    [FromQuery] string? provider = null,
    [FromQuery] bool? featured = null,
 [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    ILogger<Program>? logger = null!)
{
    if (!brandContext.IsResolved)
    {
    return Results.Problem("Brand not resolved", statusCode: 400);
    }
    
 var query = context.BrandGames
        .Include(bg => bg.Game)
            .ThenInclude(g => g.Provider)
    .Where(bg => bg.BrandId == brandContext.BrandId && bg.Enabled && bg.Game.Enabled);
    
    if (!string.IsNullOrEmpty(category))
        query = query.Where(bg => bg.Game.Category == category);
    
    if (!string.IsNullOrEmpty(provider))
        query = query.Where(bg => bg.Game.Provider == provider);
    
    if (featured.HasValue)
   query = query.Where(bg => bg.Game.IsFeatured == featured.Value);
    
    var totalCount = await query.CountAsync();
    
    var games = await query
        .OrderBy(bg => bg.DisplayOrder)
  .ThenBy(bg => bg.Game.Name)
        .Skip((page - 1) * pageSize)
     .Take(pageSize)
        .Select(bg => new CatalogGameDetailResponse(
            bg.GameId,
            bg.Game.Code,
            bg.Game.Name,
    bg.Game.Provider,
         bg.Game.ProviderName, // Nuevo
            bg.Game.Category,     // Nuevo
   bg.Game.ImageUrl,     // Nuevo
bg.Game.RTP,          // Nuevo
  bg.Game.Volatility,   // Nuevo
    bg.Game.MinBet,       // Nuevo
       bg.Game.MaxBet,    // Nuevo
  bg.Game.IsFeatured,   // Nuevo
     bg.Game.IsNew,    // Nuevo
            bg.Enabled,
   bg.DisplayOrder,
   bg.Tags))
      .ToListAsync();
    
    var response = new CatalogGamesResponse(games, page, pageSize, totalCount);
    
    return TypedResults.Ok(response);
}

public record CatalogGameDetailResponse(
    Guid GameId,
    string Code,
    string Name,
    string Provider,
    string ProviderName,
    string? Category,
    string? ImageUrl,
    decimal? RTP,
    string? Volatility,
    decimal? MinBet,
    decimal? MaxBet,
    bool IsFeatured,
    bool IsNew,
    bool Enabled,
    int DisplayOrder,
    string[] Tags
);
```

---

## ?? **4. DTOs Completos**

```csharp
namespace Casino.Application.DTOs.Game;

// Request para crear juego (extendido)
public record CreateGameRequest(
 string Code,
    string Provider,
    string Name,
    string? LaunchId,
    decimal? RTP,
    string? Volatility,
    string? Category,
    string? ImageUrl,
    decimal? MinBet,
    decimal? MaxBet,
    bool IsFeatured = false,
    bool IsNew = false,
    bool Enabled = true
);

// Response de catálogo (extendido)
public record GetGameResponse(
    Guid Id,
    string Code,
    string Provider,
    string Name,
    string? LaunchId,
    decimal? RTP,
    string? Volatility,
    string? Category,
    string? ImageUrl,
    decimal? MinBet,
    decimal? MaxBet,
    bool IsFeatured,
    bool IsNew,
    bool Enabled,
    DateTime CreatedAt
);

// Response de launch
public record LaunchGameUrlResponse(
    string LaunchUrl,
    DateTime ExpiresAt
);
```

---

## ?? **5. Registro en `Program.cs`**

```csharp
// Register provider adapters
builder.Services.AddScoped<IProviderAdapter, MockProviderAdapter>();
// Agregar más adapters aquí en el futuro:
// builder.Services.AddScoped<IProviderAdapter, PragmaticProviderAdapter>();
// builder.Services.AddScoped<IProviderAdapter, EvolutionProviderAdapter>();

// Register factory
builder.Services.AddScoped<IProviderAdapterFactory, ProviderAdapterFactory>();

// Register launch service
builder.Services.AddScoped<IGameLaunchService, GameLaunchService>();

// Map endpoints
app.MapCasinoEndpoints();
```

---

## ?? **6. Migración SQL Completa**

```sql
-- 1. Crear tabla GameProviders
CREATE TABLE "GameProviders" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "Code" varchar(50) UNIQUE NOT NULL,
    "Name" varchar(200) NOT NULL,
    "LaunchEndpointTemplate" text NOT NULL,
    "RequiresSessionToken" boolean DEFAULT true,
    "SupportsRealMode" boolean DEFAULT true,
    "SupportsDemoMode" boolean DEFAULT false,
    "DefaultMeta" jsonb,
    "Enabled" boolean DEFAULT true,
 "CreatedAt" timestamptz DEFAULT NOW(),
    "UpdatedAt" timestamptz DEFAULT NOW()
);

-- 2. Insertar proveedores iniciales
INSERT INTO "GameProviders" ("Code", "Name", "LaunchEndpointTemplate") VALUES
('mock', 'Mock Provider', 'https://demo.local/games/{gameCode}?session={session}'),
('pragmatic', 'Pragmatic Play', 'https://api.pragmaticplay.net/gs2c/openGame.do?gameSymbol={gameSymbol}&token={token}'),
('evolution', 'Evolution Gaming', 'https://api.evolution.com/launch?game={game}&token={token}');

-- 3. Extender tabla Games
ALTER TABLE "Games" ADD COLUMN "ProviderId" uuid REFERENCES "GameProviders"("Id");
ALTER TABLE "Games" ADD COLUMN "LaunchId" varchar(200);
ALTER TABLE "Games" ADD COLUMN "RTP" decimal(5,2);
ALTER TABLE "Games" ADD COLUMN "Volatility" varchar(20);
ALTER TABLE "Games" ADD COLUMN "Category" varchar(50);
ALTER TABLE "Games" ADD COLUMN "ImageUrl" text;
ALTER TABLE "Games" ADD COLUMN "MinBet" decimal(18,2);
ALTER TABLE "Games" ADD COLUMN "MaxBet" decimal(18,2);
ALTER TABLE "Games" ADD COLUMN "IsFeatured" boolean DEFAULT false;
ALTER TABLE "Games" ADD COLUMN "IsNew" boolean DEFAULT false;
ALTER TABLE "Games" ADD COLUMN "AdditionalTags" text[];
ALTER TABLE "Games" ADD COLUMN "UpdatedAt" timestamptz DEFAULT NOW();

-- 4. Crear tabla GameLaunchLogs
CREATE TABLE "GameLaunchLogs" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "SessionId" uuid NOT NULL REFERENCES "GameSessions"("Id"),
    "PlayerId" uuid NOT NULL REFERENCES "Players"("Id"),
    "GameId" uuid NOT NULL REFERENCES "Games"("Id"),
    "BrandId" uuid NOT NULL REFERENCES "Brands"("Id"),
    "Provider" varchar(50) NOT NULL,
  "LaunchUrl" text NOT NULL,
    "SessionToken" text NOT NULL,
    "Success" boolean NOT NULL,
    "ErrorMessage" text,
    "IpAddress" varchar(45),
    "UserAgent" text,
    "CreatedAt" timestamptz DEFAULT NOW()
);

CREATE INDEX "idx_game_launch_logs_session_id" ON "GameLaunchLogs"("SessionId");
CREATE INDEX "idx_game_launch_logs_player_id" ON "GameLaunchLogs"("PlayerId");
CREATE INDEX "idx_game_launch_logs_created_at" ON "GameLaunchLogs"("CreatedAt");

-- 5. Actualizar juegos existentes con ProviderId
UPDATE "Games" g SET "ProviderId" = (
    SELECT p."Id" FROM "GameProviders" p WHERE p."Code" = g."Provider"
);
```

---

## ?? **Resultado Final**

### **Backend Preparado Como:**

? **Plataforma de Lanzamiento de Juegos** con:

1. **Catálogo Rico**: RTP, volatilidad, categorías, imágenes, featured, min/max bet
2. **Launch Multi-Proveedor**: Adapters genéricos listos para extender
3. **Sesiones Seguras**: Tokens firmados, expiración, logs completos
4. **Endpoints REST Completos**:
   - `GET /api/v1/catalog/games` ? Catálogo filtrable
   - `GET /api/v1/casino/games/url/{provider}/{game}` ? Launch con redirección
5. **Auditoría Completa**: Logs de launch con URLs y tokens
6. **Arquitectura Extensible**: Agregar nuevos proveedores = implementar `IProviderAdapter`

### **Frontend Puede**:

```typescript
// 1. Listar juegos con filtros
fetch('/api/v1/catalog/games?category=slots&featured=true')

// 2. Launch game en iframe
<iframe src="/api/v1/casino/games/url/pragmatic/sweet-bonanza?playerId={id}" />
```

---

## ?? **Archivos a Crear/Modificar**

### **Crear**:
1. `apps/Casino.Domain/Entities/GameProvider.cs`
2. `apps/Casino.Domain/Entities/GameLaunchLog.cs`
3. `apps/Casino.Application/Providers/IProviderAdapter.cs`
4. `apps/Casino.Application/Providers/IProviderAdapterFactory.cs`
5. `apps/Casino.Application/Providers/Implementations/MockProviderAdapter.cs`
6. `apps/Casino.Application/Providers/Implementations/ProviderAdapterFactory.cs`
7. `apps/Casino.Application/Services/IGameLaunchService.cs`
8. `apps/Casino.Application/Services/Implementations/GameLaunchService.cs`
9. `apps/api/Casino.Api/Endpoints/CasinoEndpoints.cs`
10. `scripts/add-game-catalog-extensions.sql`

### **Modificar**:
1. `apps/Casino.Domain/Entities/Game.cs` ? Agregar campos nuevos
2. `apps/Casino.Infrastructure/Data/CasinoDbContext.cs` ? DbSet<GameProvider>, DbSet<GameLaunchLog>
3. `apps/api/Casino.Api/Endpoints/CatalogEndpoints.cs` ? Extender response
4. `apps/api/Casino.Api/Program.cs` ? Registrar servicios
5. `apps/Casino.Application/DTOs/Game/*.cs` ? Extender DTOs

---

## ? **Checklist de Implementación**

### **Fase 1: Modelo de Datos** (1-2 días)
- [ ] Crear entidad `GameProvider`
- [ ] Crear entidad `GameLaunchLog`
- [ ] Extender entidad `Game` con campos nuevos
- [ ] Migración SQL completa
- [ ] Actualizar `DbContext`

### **Fase 2: Adapters** (2-3 días)
- [ ] Crear interfaz `IProviderAdapter`
- [ ] Implementar `MockProviderAdapter`
- [ ] Crear factory `IProviderAdapterFactory`
- [ ] Crear servicio `IGameLaunchService`

### **Fase 3: Endpoints** (1-2 días)
- [ ] Crear `CasinoEndpoints.cs`
- [ ] Endpoint `GET /casino/games/url/{provider}/{game}`
- [ ] Extender `CatalogEndpoints` con campos nuevos
- [ ] Registrar en `Program.cs`

### **Fase 4: Testing** (1 día)
- [ ] Probar catálogo extendido
- [ ] Probar launch con mock provider
- [ ] Verificar logs de launch
- [ ] Probar redirección de iframe

---

**Total estimado**: 5-8 días de desarrollo

**Prioridad**: ? ALTA (Frontend depende de esto)

**Riesgo**: ?? BAJO (No rompe funcionalidad existente)

---

**Fecha**: 2025-01-23  
**Versión**: 1.0  
**Autor**: Backend Team

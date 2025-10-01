# Auth Design — JWT + Cookies (Backoffice & Players)

## Objetivo
- Dos “mundos” separados en el mismo dominio:
  - **Backoffice** (`/admin/*`) para `SUPER_ADMIN | OPERATOR_ADMIN | CASHIER`
  - **Players** (`/`) para `PLAYER`
- **JWT** para autenticación y autorización, con **audiences** distintas por realm.
- **Cookies HttpOnly** que transportan el JWT automáticamente (sin exponerlo a JS), con **paths** diferentes para aislar sesiones.
- Compatible con **Authorization: Bearer** (alternativa a cookie).
- Resuelve **brand por host** (BrandResolver) y **scoping** por rol/operador.

---

## Esquema general
- **Schemes JWT**:
  - `BackofficeJwt` → `aud = "backoffice"`
  - `PlayerJwt` → `aud = "player"`
- **Cookies** (solo transporte del token):
  - `bk.token` con `Path=/admin` (solo se envía bajo `/admin/*`)
  - `pl.token` con `Path=/` (sitio público)
  - `HttpOnly`, `Secure`, `SameSite=Lax`
- **Policies**:
  - `BackofficePolicy` → requiere `BackofficeJwt` + claim `role ∈ {SUPER_ADMIN, OPERATOR_ADMIN, CASHIER}`
  - `PlayerPolicy` → requiere `PlayerJwt` + claim `role = PLAYER`

> No usamos `CookieAuthentication`. Las **cookies solo almacenan el JWT**. Los handlers **JwtBearer** lo leen desde el header o desde la cookie (event `OnMessageReceived`).

---

## Configuración en `Program.cs`
```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

var jwtKey = builder.Configuration["Auth:JwtKey"]!;          // mínimo 256 bits
var issuer = builder.Configuration["Auth:Issuer"] ?? "casino";
var clockSkew = TimeSpan.FromMinutes(2);
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication()
    .AddJwtBearer("BackofficeJwt", o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
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
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var auth = ctx.Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(auth) && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    return Task.CompletedTask;
                if (ctx.Request.Cookies.TryGetValue("bk.token", out var token))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    })
    .AddJwtBearer("PlayerJwt", o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
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
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var auth = ctx.Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(auth) && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    return Task.CompletedTask;
                if (ctx.Request.Cookies.TryGetValue("pl.token", out var token))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("BackofficePolicy", p =>
        p.RequireAuthenticatedUser()
         .AddAuthenticationSchemes("BackofficeJwt")
         .RequireClaim(ClaimTypes.Role, "SUPER_ADMIN", "OPERATOR_ADMIN", "CASHIER"));

    options.AddPolicy("PlayerPolicy", p =>
        p.RequireAuthenticatedUser()
         .AddAuthenticationSchemes("PlayerJwt")
         .RequireClaim(ClaimTypes.Role, "PLAYER"));
});
```

---

## Emisión de tokens y cookies (endpoints)

### DTOs
```csharp
public record AdminLoginRequest(string Username, string Password);
public record PlayerLoginRequest(Guid? PlayerId, string? Username, string Password);
public record TokenResponse(string accessToken, DateTime expiresAt);
```

### Emisor de JWT (helper)
```csharp
using System.IdentityModel.Tokens.Jwt;

static TokenResponse IssueJwt(string audience, IEnumerable<Claim> claims, string issuer, SymmetricSecurityKey key, TimeSpan ttl)
{
    var now = DateTimeOffset.UtcNow;
    var jwt = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims: claims,
        notBefore: now.UtcDateTime,
        expires: now.Add(ttl).UtcDateTime,
        signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
    );
    var token = new JwtSecurityTokenHandler().WriteToken(jwt);
    return new TokenResponse(token, now.Add(ttl).UtcDateTime);
}
```

### Backoffice `/api/v1/admin/auth/login`
```csharp
admin.MapPost("/login", async (AdminLoginRequest req, CasinoDbContext db, HttpContext http, IConfiguration cfg) =>
{
    var user = await db.BackofficeUsers.FirstOrDefaultAsync(u => u.Username == req.Username && u.Status == "ACTIVE");
    if (user is null) return Results.Unauthorized();

    var claims = new List<Claim> {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name, user.Username),
        new(ClaimTypes.Role, user.Role),
        new("operator_id", user.OperatorId?.ToString() ?? string.Empty)
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Auth:JwtKey"]!));
    var issuer = cfg["Auth:Issuer"] ?? "casino";

    var resp = IssueJwt("backoffice", claims, issuer, key, TimeSpan.FromHours(8));

    http.Response.Cookies.Append(
        "bk.token",
        resp.accessToken,
        new CookieOptions {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/admin",
            Expires = resp.expiresAt
        });

    return Results.Ok(new { ok = true, user = new { user.Username, user.Role }, resp.expiresAt });
})
.WithName("BackofficeLogin");
```

### Players `/api/v1/auth/login`
```csharp
players.MapPost("/login", async (PlayerLoginRequest req, CasinoDbContext db, BrandContext brand, HttpContext http, IConfiguration cfg) =>
{
    if (!brand.IsResolved) return Results.BadRequest(new { error = "brand_not_resolved" });

    var q = db.Players.Where(p => p.BrandId == brand.BrandId && p.Status == "ACTIVE");
    var player = req.PlayerId.HasValue
        ? await q.FirstOrDefaultAsync(p => p.Id == req.PlayerId)
        : await q.FirstOrDefaultAsync(p => p.Username == req.Username);

    if (player is null) return Results.Unauthorized();

    var claims = new List<Claim> {
        new(ClaimTypes.NameIdentifier, player.Id.ToString()),
        new(ClaimTypes.Role, "PLAYER"),
        new("brand_id", brand.BrandId!.Value.ToString())
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Auth:JwtKey"]!));
    var issuer = cfg["Auth:Issuer"] ?? "casino";

    var resp = IssueJwt("player", claims, issuer, key, TimeSpan.FromHours(8));

    http.Response.Cookies.Append(
        "pl.token",
        resp.accessToken,
        new CookieOptions {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = resp.expiresAt
        });

    return Results.Ok(new { ok = true, player = new { player.Id, player.Username }, resp.expiresAt });
})
.WithName("PlayerLogin");
```

### Logout
```csharp
admin.MapPost("/logout", (HttpContext http) =>
{
    http.Response.Cookies.Delete("bk.token", new CookieOptions { Path = "/admin" });
    return Results.Ok(new { ok = true });
}).RequireAuthorization("BackofficePolicy");

players.MapPost("/logout", (HttpContext http) =>
{
    http.Response.Cookies.Delete("pl.token", new CookieOptions { Path = "/" });
    return Results.Ok(new { ok = true });
}).RequireAuthorization("PlayerPolicy");
```

### Grupos protegidos
```csharp
app.MapGroup("/api/v1/admin")
   .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = "BackofficeJwt", Policy = "BackofficePolicy" });

app.MapGroup("/api/v1/player")
   .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = "PlayerJwt", Policy = "PlayerPolicy" });
```

---

## CSRF y CORS
- Como usamos **cookies HttpOnly** para transportar tokens, para llamadas **state-changing** desde SPA usa **CSRF token** (double-submit o header).
- Si consumís “same-origin” con proxy `/api`, casi no necesitás CORS. Si es cross-origin, habilitar `AllowCredentials()` y `WithOrigins(...)` por brand.

---

## Configuración (`appsettings.<env>.json`)
```json
{
  "Auth": {
    "JwtKey": "REEMPLAZAR_POR_CLAVE_DE_32+_CARACTERES",
    "Issuer": "casino"
  }
}
```

---

## Pruebas rápidas (curl)
```bash
# Login backoffice
curl -i -X POST -H "Host: admin.bet30.local" -H "Content-Type: application/json"   -d '{"username":"admin","password":"TuPass123!"}' http://localhost:5000/api/v1/admin/auth/login

# Usar cookie en admin
curl -i -H "Host: admin.bet30.local" --cookie "bk.token=<TOKEN>"   http://localhost:5000/api/v1/admin/brands

# Login player
curl -i -X POST -H "Host: bet30.local" -H "Content-Type: application/json"   -d '{"username":"demo","password":"1234"}' http://localhost:5000/api/v1/auth/login

# Usar cookie en player
curl -i -H "Host: bet30.local" --cookie "pl.token=<TOKEN>"   http://localhost:5000/api/v1/player/me
```

---

## Notas finales
- Cookies con nombres y **paths distintos** → aislamiento total de sesiones.
- **BrandContext** sigue siendo obligatorio en endpoints de players.
- Se puede agregar refresh tokens y rotación.
- En producción, `Secure=true` y HTTPS obligatorio.

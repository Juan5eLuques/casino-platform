# Fix: Logout Brand Code from JWT Token

## ?? **Problema**

Al hacer logout, se producía una excepción:

```
System.ArgumentException: Brand code cannot be null or empty (Parameter 'brandCode')
at Casino.Infrastructure.Helpers.CookieHelper.GetBackofficeCookieName(String brandCode)
at Casino.Api.Endpoints.AuthEndpoints.AdminLogout(...)
```

### **Causa Raíz:**

El `BrandResolverMiddleware` **salta la resolución de brand** para los endpoints de logout:

```csharp
// BrandResolverMiddleware.cs línea ~27
if (path.StartsWith("/api/v1/admin/auth/logout") || // ? SKIP
    path.StartsWith("/api/v1/auth/logout"))
{
    _logger.LogInformation("Skipping brand resolution for path: {Path}", path);
    await _next(context);
    return; // ? BrandContext NO se popula
}
```

**Resultado:**
- `BrandContext.BrandCode` = `null`
- `CookieHelper.GetBackofficeCookieName(null)` ? **Exception** ?

---

## ? **Solución Implementada**

### **Extraer `brand_code` del JWT Token**

En lugar de depender del `BrandContext`, extraemos el `brand_code` directamente del **JWT token** que viene en la cookie.

**Por qué funciona:**
- El JWT token **siempre** incluye el claim `brand_code` (se agrega en el login)
- El middleware de autenticación JWT valida el token **antes** del logout
- El claim está disponible en `httpContext.User.FindFirst("brand_code")`

---

## ?? **Código Modificado**

### **AdminLogout - ANTES:**

```csharp
public static IResult AdminLogout(HttpContext httpContext, BrandContext brandContext, ILoggerFactory loggerFactory)
{
    var logger = loggerFactory.CreateLogger("AuthEndpoints");
    
    // ? PROBLEMA: brandContext.BrandCode es NULL
    var cookieName = CookieHelper.GetBackofficeCookieName(brandContext.BrandCode);
    
    CookieHelper.DeleteAuthCookie(httpContext, cookieName, logger);
    
    return Results.Ok(new { ok = true, message = "Logged out successfully" });
}
```

### **AdminLogout - AHORA:**

```csharp
public static IResult AdminLogout(HttpContext httpContext, BrandContext brandContext, ILoggerFactory loggerFactory)
{
    var logger = loggerFactory.CreateLogger("AuthEndpoints");
    
    // ? SOLUCIÓN: Extraer brand_code del JWT token
    var brandCodeClaim = httpContext.User.FindFirst("brand_code")?.Value;
    
    if (string.IsNullOrWhiteSpace(brandCodeClaim))
    {
        logger.LogError("Logout failed: brand_code claim not found in JWT token");
        return Results.Problem(
            title: "Invalid Token",
            detail: "Brand code not found in authentication token",
            statusCode: 401);
    }
    
    logger.LogInformation("Logout attempt for brand {BrandCode}", brandCodeClaim);
    
    // Usar brand_code del token (no del BrandContext)
    var cookieName = CookieHelper.GetBackofficeCookieName(brandCodeClaim);
    
    CookieHelper.DeleteAuthCookie(httpContext, cookieName, logger);
    
    var cookieWasPresent = CookieHelper.IsCookiePresent(httpContext, cookieName);
    
    logger.LogInformation(
        "Logout completed for brand {BrandCode}, cookie {CookieName} was present: {Present}",
        brandCodeClaim, cookieName, cookieWasPresent);
    
    return Results.Ok(new 
    { 
        ok = true, 
        message = "Logged out successfully",
        cookieName = cookieName,
        cookieWasPresent = cookieWasPresent
    });
}
```

---

## ?? **Claims en el JWT Token**

### **AdminLogin - Claims Incluidos:**

```csharp
var claims = new List<Claim>
{
    new(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new(ClaimTypes.Name, user.Username),
    new(ClaimTypes.Role, user.Role.ToString()),
    new("brand_id", brandContext.BrandId.ToString()),
    new("brand_code", brandContext.BrandCode)  // ? CRÍTICO para logout
};

var tokenResponse = jwtService.IssueToken("backoffice", claims, TimeSpan.FromHours(8));
```

### **JWT Token Decodificado (ejemplo):**

```json
{
  "nameid": "f08c3a35-cd43-a4b3-f05f479db28e",
  "name": "Cajero",
  "role": "CASHIER",
  "brand_id": "df2e8648-7d55-4717-adeb-8265b83c04c3",
  "brand_code": "NETLIFY_PROD",  // ? ESTO se usa en logout
  "nbf": 1761084883,
  "exp": 1761113683,
  "iss": "casino",
  "aud": "backoffice"
}
```

---

## ?? **Flujo Completo**

### **Login:**
```
1. BrandResolverMiddleware ejecuta ? BrandContext poblado ?
2. AdminLogin valida usuario
3. AdminLogin crea JWT con brand_code claim
4. Cookie creada: bk.token.netlify_prod
```

### **Logout:**
```
1. BrandResolverMiddleware SKIP ? BrandContext = null ?
2. JWT Middleware valida token ? httpContext.User poblado ?
3. AdminLogout extrae brand_code del token ?
4. Cookie eliminada: bk.token.netlify_prod ?
```

---

## ?? **Testing**

### **Test 1: Logout Normal**

```bash
# 1. Login
curl -X POST https://api.railway.app/api/v1/admin/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"pass"}' \
  -c cookies.txt

# Cookie creada: bk.token.netlify_prod

# 2. Logout (con cookie)
curl -X POST https://api.railway.app/api/v1/admin/auth/logout \
  -b cookies.txt \
  -v

# Response:
{
  "ok": true,
  "message": "Logged out successfully",
  "cookieName": "bk.token.netlify_prod",
  "cookieWasPresent": true
}

# ? No exception
# ? Cookie eliminada
```

### **Test 2: Logout sin Token (Error Esperado)**

```bash
curl -X POST https://api.railway.app/api/v1/admin/auth/logout

# Response: 401 Unauthorized
# (No hay JWT token ? AuthenticationMiddleware bloquea)
```

### **Test 3: Logout con Token Inválido**

```bash
curl -X POST https://api.railway.app/api/v1/admin/auth/logout \
  -H "Cookie: bk.token.netlify_prod=invalid_token"

# Response: 401 Unauthorized
# (Token inválido ? JWT middleware bloquea)
```

---

## ?? **Logs Mejorados**

### **ANTES (Con Exception):**

```
[ERROR] An unhandled exception has occurred
System.ArgumentException: Brand code cannot be null or empty
```

### **AHORA (Exitoso):**

```
[INFO] Logout attempt for brand NETLIFY_PROD
[INFO] Deleting auth cookie: bk.token.netlify_prod, Present in request: True
[INFO] Auth cookie deleted using dual strategy: bk.token.netlify_prod
[INFO] Logout completed for brand NETLIFY_PROD, cookie bk.token.netlify_prod was present: True
```

### **AHORA (Sin brand_code claim):**

```
[ERROR] Logout failed: brand_code claim not found in JWT token
```

---

## ?? **Seguridad**

### **Validaciones Agregadas:**

1. **Token Requerido:**
   - `RequireAuthorization("BackofficePolicy")` en el endpoint
   - JWT debe ser válido

2. **Brand Code Requerido:**
   - Valida que el claim `brand_code` exista
   - Retorna 401 si no está presente

3. **No Depende de BrandContext:**
   - Funciona incluso si BrandResolver se salta
   - Usa solo información del token autenticado

---

## ?? **Consideraciones**

### **¿Por Qué No Quitar el Skip de BrandResolver?**

**Opción A (Actual):** Skip brand resolution, usar token
- ? Logout más rápido (no query a BD)
- ? Funciona aunque brand esté inactivo
- ? Menos dependencias

**Opción B (Alternativa):** Resolver brand en logout
- ? Logout más lento (query extra a BD)
- ? Falla si brand fue desactivado
- ? Mayor acoplamiento

**Decisión:** Mantener skip, usar token (más robusto)

---

## ?? **Referencias**

- **JWT Claims Standard:** https://www.iana.org/assignments/jwt/jwt.xhtml
- **ASP.NET Core Claims:** https://learn.microsoft.com/en-us/aspnet/core/security/authorization/claims
- **CookieHelper:** `apps/Casino.Infrastructure/Helpers/CookieHelper.cs`

---

## ? **Resumen**

**Problema:** `BrandContext.BrandCode` null en logout causaba exception

**Solución:** Extraer `brand_code` del JWT token en lugar de BrandContext

**Beneficios:**
- ? No exceptions en logout
- ? Logout más rápido (no query a BD)
- ? Más robusto (independiente de BrandResolver)
- ? Logging mejorado
- ? Validación de token

**Cambios:**
- `AdminLogout`: Usa `httpContext.User.FindFirst("brand_code")`
- `PlayerLogout`: Sin cambios (usa cookie fija `pl.token`)

**Testing:** ? Logout funciona correctamente

**Deploy:** Reiniciar backend para aplicar cambios

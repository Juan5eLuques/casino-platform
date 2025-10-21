# ?? Contexto Completo: Sistema Multi-Brand Session Isolation

## ?? Objetivo del Sistema

Permitir sesiones **completamente independientes** para múltiples brands en el mismo navegador:
- Frontend A (`sitea.netlify.app`) ? Backend (`api.railway.app`) ? Brand A
- Frontend B (`siteb.netlify.app`) ? Backend (`api.railway.app`) ? Brand B  
- **Ambos** deben mantener sesiones **independientes** aunque usen el mismo backend

---

## ?? Flujo Completo (Request ? Response)

### **1. Request llega al Backend**

```
Cliente: https://backoffice-casino.netlify.app
?
POST https://api.railway.app/api/v1/admin/auth/login
Headers:
  Origin: https://backoffice-casino.netlify.app
  Content-Type: application/json
Body:
  { username, password }
```

---

### **2. UseForwardedHeaders** (Program.cs línea ~307)

```csharp
app.UseForwardedHeaders(new ForwardedHeadersOptions {
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | 
                      ForwardedHeaders.XForwardedProto |
                      ForwardedHeaders.XForwardedHost
});
```

**Qué hace:** Procesa headers de proxy (`X-Forwarded-*`) para obtener el origen real.

---

### **3. BrandResolverMiddleware** (línea ~310)

#### **Prioridad de Resolución:**
```
1. Origin Header ? "backoffice-casino.netlify.app"
2. Referer Header ? (fallback)
3. Host Header (backend) ? "api.railway.app"
```

#### **Código Actual:**
```csharp
var originHeader = context.Request.Headers["Origin"].FirstOrDefault();
string? resolvedHost = null;

if (!string.IsNullOrEmpty(originHeader))
{
    resolvedHost = new Uri(originHeader).Host.ToLower();
    // resolvedHost = "backoffice-casino.netlify.app"
}
```

#### **? PREGUNTA CRÍTICA #1:**
**¿Qué hostname tienes configurado en la base de datos para cada brand?**

```sql
SELECT "Code", "Domain", "AdminDomain" FROM "Brands";
```

**Opción A (Hostname del Frontend):**
```sql
Code: NETLIFY_PROD
Domain: backoffice-casino.netlify.app
AdminDomain: backoffice-casino.netlify.app
```

**Opción B (Hostname del Backend):**
```sql
Code: NETLIFY_PROD
Domain: casino-platform-production.up.railway.app
AdminDomain: casino-platform-production.up.railway.app
```

**El middleware busca:**
```csharp
var brand = await dbContext.Brands
    .FirstOrDefaultAsync(b => 
        (b.Domain != null && b.Domain.ToLower() == resolvedHost) ||
        (b.AdminDomain != null && b.AdminDomain.ToLower() == resolvedHost));
```

**Si usas Opción B**, el `resolvedHost` (`backoffice-casino.netlify.app`) **nunca** coincidirá con `Domain` (`casino-platform-production.up.railway.app`).

#### **? SOLUCIÓN:**
**Los brands deben tener el hostname del FRONTEND:**
```sql
UPDATE "Brands" 
SET 
  "Domain" = 'backoffice-casino.netlify.app',
  "AdminDomain" = 'backoffice-casino.netlify.app'
WHERE "Code" = 'NETLIFY_PROD';
```

---

### **4. DynamicCorsMiddleware** (línea ~313)

```csharp
var origin = context.Request.Headers.Origin.FirstOrDefault();
// origin = "https://backoffice-casino.netlify.app"

// Para auth endpoints que no requieren brand ya resuelto
if (path.StartsWith("/api/v1/admin/auth"))
{
    // Verifica si origin está en development list
    var isDevOriginAllowed = IsOriginAllowedForAuth(origin);
    
    if (!isDevOriginAllowed)
    {
        // Busca si origin está en ALGÚN brand en BD
        var isOriginInDatabase = brands.Any(b => 
            b.CorsOrigins.Contains(origin));
    }
    
    SetCorsHeaders(context, origin);  // Siempre origen específico, nunca "*"
}
```

#### **? PREGUNTA CRÍTICA #2:**
**¿Tu brand tiene el `Origin` correcto en `CorsOrigins`?**

```sql
SELECT "Code", "CorsOrigins" FROM "Brands" WHERE "Code" = 'NETLIFY_PROD';
```

**Debe contener:**
```json
{
  "https://backoffice-casino.netlify.app"
}
```

---

### **5. Login Endpoint** (AuthEndpoints.cs)

#### **5.1 Validar Brand Context**
```csharp
if (!brandContext.IsResolved)
{
    return Results.Problem("Brand Not Resolved");
}
```

**Si brandContext NO está resuelto aquí, significa que el middleware BrandResolver NO encontró el brand.**

#### **5.2 Crear Cookie con Nombre Único**
```csharp
var cookieName = $"bk.token.{brandContext.BrandCode.ToLower()}";
// Ejemplo: "bk.token.netlify_prod"
```

#### **5.3 Detectar Cross-Site**
```csharp
var host = httpContext.Request.Host.Host;  
// "casino-platform-production.up.railway.app"

var origin = httpContext.Request.Headers["Origin"].FirstOrDefault();
// "https://backoffice-casino.netlify.app"

string? originHost = null;
if (!string.IsNullOrEmpty(origin))
{
    var originUri = new Uri(origin);
    originHost = originUri.Host;  
    // "backoffice-casino.netlify.app"
}

bool isCrossSite = !string.IsNullOrEmpty(originHost) && 
                  originHost != host &&  
                  // "backoffice-casino.netlify.app" != "casino-platform-production.up.railway.app"
                  !host.Contains("localhost");

// isCrossSite = TRUE ?
```

#### **5.4 Configurar Cookie**
```csharp
if (isCrossSite)
{
    cookieOptions.SameSite = SameSiteMode.None;
    // NO SE SETEA Domain (host-only cookie)
    logger.LogInformation("Cross-site: SameSite=None, NO Domain");
}
```

#### **? PREGUNTA CRÍTICA #3:**
**¿Qué ves en los logs del backend cuando haces login?**

Deberías ver:
```
[INFO] Cross-site detected: OriginHost=backoffice-casino.netlify.app, BackendHost=casino-platform-production.up.railway.app ? Using SameSite=None, NO Domain
[INFO] Cookie set: bk.token.netlify_prod for brand NETLIFY_PROD
```

Si ves:
```
[INFO] Same-site: Using SameSite=Lax, Domain=casino-platform-production.up.railway.app
```

Significa que `isCrossSite = FALSE` por alguna razón.

---

## ?? Diagnóstico: ¿Por Qué Falla?

### **Síntoma Reportado:**
```
Domain: casino-platform-production.up.railway.app ?
SameSite: None
```

Esto indica que `isCrossSite = FALSE` cuando debería ser `TRUE`.

### **Posibles Causas:**

#### **Causa #1: Origin Header No Llega**

Si CORS está mal configurado, el navegador puede no enviar el `Origin` header.

**Verificar en Network Tab:**
```
Request Headers:
  Origin: https://backoffice-casino.netlify.app  ? ¿Está presente?
```

Si no está, `originHost = null` y `isCrossSite = false`.

---

#### **Causa #2: Brand No Resuelve Correctamente**

Si el brand `Domain` está configurado con el hostname del **backend** en lugar del **frontend**:

```sql
-- ? INCORRECTO:
Domain: casino-platform-production.up.railway.app

-- ? CORRECTO:
Domain: backoffice-casino.netlify.app
```

El middleware no encontrará el brand y `brandContext.IsResolved = false`.

---

#### **Causa #3: CORS Origins No Incluye el Frontend**

```sql
SELECT "CorsOrigins" FROM "Brands" WHERE "Code" = 'NETLIFY_PROD';
```

Debe contener:
```json
["https://backoffice-casino.netlify.app"]
```

Si no está, el middleware CORS bloqueará el request ANTES del login.

---

## ? Checklist de Solución

### **1. Verificar Configuración de Brands en BD**

```sql
SELECT 
  "Code",
  "Domain",
  "AdminDomain",
  "CorsOrigins"
FROM "Brands" 
WHERE "Status" = 'ACTIVE';
```

**Debe mostrar:**
```
Code         | Domain                              | AdminDomain                         | CorsOrigins
-------------|-------------------------------------|-------------------------------------|------------------
NETLIFY_PROD | backoffice-casino.netlify.app       | backoffice-casino.netlify.app       | ["https://backoffice-casino.netlify.app"]
BET30_PROD   | another-backoffice.netlify.app      | another-backoffice.netlify.app      | ["https://another-backoffice.netlify.app"]
```

**? NO debe ser:**
```
Domain: casino-platform-production.up.railway.app
```

---

### **2. Agregar Logs Temporales en Login**

En `AuthEndpoints.cs` después de línea ~145:

```csharp
// TEMPORAL: Debug logging
logger.LogWarning("?? DEBUG CROSS-SITE DETECTION:");
logger.LogWarning("  ? Origin Header: {Origin}", origin);
logger.LogWarning("  ? Origin Host: {OriginHost}", originHost);
logger.LogWarning("  ? Backend Host: {BackendHost}", host);
logger.LogWarning("  ? Is Cross-Site: {IsCrossSite}", isCrossSite);
logger.LogWarning("  ? Cookie Name: {CookieName}", cookieName);
logger.LogWarning("  ? Cookie Domain: {Domain}", cookieOptions.Domain ?? "(not set)");
logger.LogWarning("  ? Cookie SameSite: {SameSite}", cookieOptions.SameSite);
```

---

### **3. Verificar Request en Network Tab**

**Chrome DevTools ? Network ? Login request:**

**Request Headers:**
```
Origin: https://backoffice-casino.netlify.app  ? DEBE ESTAR PRESENTE
Content-Type: application/json
```

**Response Headers:**
```
Access-Control-Allow-Origin: https://backoffice-casino.netlify.app
Access-Control-Allow-Credentials: true
Set-Cookie: bk.token.netlify_prod=xxx; SameSite=None; Secure; HttpOnly; Path=/
```

**? NO debe tener:**
```
Set-Cookie: bk.token.netlify_prod=xxx; Domain=casino-platform-production.up.railway.app
```

---

### **4. Test de Verificación**

```bash
# Test 1: Verificar brand resolution
curl -X POST https://casino-platform-production.up.railway.app/api/v1/admin/auth/login \
  -H "Origin: https://backoffice-casino.netlify.app" \
  -H "Content-Type: application/json" \
  -d '{"username":"test","password":"test"}' \
  -v

# Buscar en output:
# - BrandResolver logs: "Brand resolved: NETLIFY_PROD"
# - CORS logs: "CORS allowed for origin"
# - Login logs: "Cross-site detected: OriginHost=backoffice-casino.netlify.app"
```

---

## ?? Solución Paso a Paso

### **Paso 1: Corregir Brands en Base de Datos**

```sql
-- Para cada brand, setear Domain/AdminDomain con el hostname del FRONTEND
UPDATE "Brands" 
SET 
  "Domain" = 'backoffice-casino.netlify.app',
  "AdminDomain" = 'backoffice-casino.netlify.app',
  "CorsOrigins" = ARRAY['https://backoffice-casino.netlify.app']::TEXT[]
WHERE "Code" = 'NETLIFY_PROD';

-- Repetir para cada brand que tengas
```

### **Paso 2: Reiniciar Backend**

```bash
# Railway redeploy o restart local
```

### **Paso 3: Limpiar Cookies del Navegador**

```
Chrome DevTools ? Application ? Cookies ? casino-platform-production.up.railway.app
Eliminar TODAS las cookies
```

### **Paso 4: Hacer Login Nuevamente**

1. Ir a `https://backoffice-casino.netlify.app`
2. Login con usuario válido
3. Verificar en DevTools ? Application ? Cookies:

```
Name: bk.token.netlify_prod
Value: eyJ...
Domain: (vacío o no aparece)  ? CRÍTICO
SameSite: None
Secure: ?
HttpOnly: ?
```

### **Paso 5: Verificar Logs del Backend**

```
[INFO] Brand resolved: NETLIFY_PROD for host: backoffice-casino.netlify.app
[INFO] Cross-site detected: OriginHost=backoffice-casino.netlify.app, BackendHost=casino-platform-production.up.railway.app
[INFO] Cookie set: bk.token.netlify_prod for brand NETLIFY_PROD
```

---

## ?? Resumen del Problema Más Probable

**Hipótesis Principal:**

Los brands en tu base de datos tienen configurado:
```sql
Domain: casino-platform-production.up.railway.app  ? Backend
```

Cuando debería ser:
```sql
Domain: backoffice-casino.netlify.app  ? Frontend
```

Esto causa que:
1. BrandResolver no encuentre el brand (busca por `backoffice-casino.netlify.app` pero BD tiene `casino-platform-production.up.railway.app`)
2. Si el brand no resuelve, el login falla con "Brand Not Resolved"
3. Si de alguna forma resuelve (por fallback a localhost), la detección cross-site falla

**Verificación Rápida:**
```sql
SELECT "Code", "Domain" FROM "Brands" WHERE "Status" = 'ACTIVE';
```

Si ves `casino-platform-production.up.railway.app`, **ese es el problema**.

---

## ?? Acción Inmediata

**Ejecuta esto en tu base de datos PostgreSQL:**

```sql
-- Listar brands actuales
SELECT "Code", "Domain", "AdminDomain", "CorsOrigins" FROM "Brands";

-- Si ves que Domain es el backend, corregir:
UPDATE "Brands"
SET 
  "Domain" = '<HOSTNAME_DEL_FRONTEND>',  -- ej: backoffice-casino.netlify.app
  "AdminDomain" = '<HOSTNAME_DEL_FRONTEND>',
  "CorsOrigins" = ARRAY['https://<HOSTNAME_DEL_FRONTEND>']::TEXT[]
WHERE "Code" = '<TU_BRAND_CODE>';
```

Luego reinicia el backend y prueba nuevamente.

---

**¿Qué ves cuando ejecutas ese SELECT? Eso me dirá exactamente cuál es el problema.**

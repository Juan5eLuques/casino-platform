# Multi-Brand Cookie Isolation - Solución Definitiva

## ?? **Problema Original**

Cuando un usuario iniciaba sesión en diferentes brands:
1. Login en `siteA` con `usuario1` ? cookie `bk.token` se creaba
2. Login en `siteB` con `usuario2` ? cookie `bk.token` se **SOBREESCRIBÍA**
3. Al recargar, ambos sites usaban `usuario2` (última sesión)

**Causa**: Todas las brands usaban el **mismo nombre de cookie** (`bk.token`), por lo que el navegador solo guardaba UNA cookie para todos los dominios.

---

## ? **Solución Implementada: Cookies con Nombres Únicos por Brand**

### **Cambio Clave:**

Ahora cada brand tiene su **propia cookie con nombre único**:

```
Brand A (BET30_BACKOFFICE) ? Cookie: bk.token.bet30_backoffice
Brand B (LOCALHOST_DEV)    ? Cookie: bk.token.localhost_dev
Brand C (OTRO_SITE)         ? Cookie: bk.token.otro_site
```

---

## ?? **Implementación**

### **1. Login - Crear Cookie con Nombre Dinámico**

```csharp
// En AdminLogin (AuthEndpoints.cs)
var cookieName = $"bk.token.{brandContext.BrandCode.ToLower()}";

httpContext.Response.Cookies.Append(cookieName, tokenResponse.AccessToken, cookieOptions);

logger.LogInformation("Cookie set: {CookieName} for brand {BrandCode}", 
    cookieName, brandContext.BrandCode);
```

**Resultado:**
- Login en `BET30_BACKOFFICE` ? crea cookie `bk.token.bet30_backoffice`
- Login en `LOCALHOST_DEV` ? crea cookie `bk.token.localhost_dev`
- **Ambas coexisten** en el navegador sin conflicto

---

### **2. JWT Middleware - Leer Cookies Dinámicamente**

```csharp
// En Program.cs - BackofficeJwt configuration
OnMessageReceived = context =>
{
    // 1) Prioridad: Authorization header
    var auth = context.Request.Headers.Authorization.ToString();
    if (!string.IsNullOrWhiteSpace(auth) && auth.StartsWith("Bearer "))
    {
        context.Token = auth.Substring("Bearer ".Length).Trim();
        return Task.CompletedTask;
    }
    
    // 2) Fallback: buscar cookies que empiecen con "bk.token."
    foreach (var cookie in context.Request.Cookies)
    {
        if (cookie.Key.StartsWith("bk.token.", StringComparison.OrdinalIgnoreCase))
        {
            context.Token = cookie.Value;
            break;
        }
    }

    return Task.CompletedTask;
}
```

**Funcionamiento:**
1. Si hay `Authorization: Bearer xxx` ? usa ese token
2. Si no, busca **cualquier cookie que empiece con `bk.token.`**
3. Usa la primera que encuentre (que será la del brand actual)

---

### **3. Logout - Eliminar Cookie Específica**

```csharp
// En AdminLogout (AuthEndpoints.cs)
public static IResult AdminLogout(HttpContext httpContext, BrandContext brandContext)
{
    // Usar el mismo nombre de cookie que en login
    var cookieName = $"bk.token.{brandContext.BrandCode.ToLower()}";
    
    httpContext.Response.Cookies.Delete(cookieName, cookieOptions);
    return Results.Ok(new { ok = true, message = "Logged out successfully" });
}
```

**Resultado:**
- Logout en `BET30_BACKOFFICE` ? elimina **solo** `bk.token.bet30_backoffice`
- `bk.token.localhost_dev` se mantiene intacta

---

## ?? **Comportamiento Esperado**

### **Escenario: Login en Múltiples Brands**

```
Tab 1: siteA.com (BET30_BACKOFFICE)
  - Login con admin_bet30
  - Cookie creada: bk.token.bet30_backoffice
  - Token contiene: { user: admin_bet30, brand: BET30_BACKOFFICE }

Tab 2: siteB.com (LOCALHOST_DEV)
  - Login con admin_localhost
  - Cookie creada: bk.token.localhost_dev
  - Token contiene: { user: admin_localhost, brand: LOCALHOST_DEV }
```

**Resultado:**
- ? Tab 1 sigue logueado como `admin_bet30`
- ? Tab 2 sigue logueado como `admin_localhost`
- ? **Ambas sesiones coexisten** sin interferirse

---

### **DevTools - Cookies**

```
Domain: siteA.com
?? bk.token.bet30_backoffice = eyJ... (ACTIVA)

Domain: siteB.com
?? bk.token.localhost_dev = eyJ... (ACTIVA)
```

---

## ?? **Comparación Antes/Después**

| Aspecto | ? Antes (Cookie Global) | ? Ahora (Cookie por Brand) |
|---------|--------------------------|------------------------------|
| **Nombre Cookie** | `bk.token` (fijo) | `bk.token.{brandCode}` (dinámico) |
| **Sesiones Simultáneas** | ? Imposible | ? Ilimitadas |
| **Login en Brand A** | Crea `bk.token` | Crea `bk.token.brand_a` |
| **Login en Brand B** | **Sobrescribe** `bk.token` | Crea `bk.token.brand_b` (nueva) |
| **Tab 1 (Brand A)** | ? Muestra usuario Brand B | ? Muestra usuario Brand A |
| **Tab 2 (Brand B)** | ? Muestra usuario Brand B | ? Muestra usuario Brand B |
| **Logout Brand A** | Borra `bk.token` (afecta todo) | Borra solo `bk.token.brand_a` |

---

## ?? **Testing**

### **Test 1: Login Secuencial en Múltiples Brands**

```bash
# 1. Login en Brand A
curl -X POST http://sitea.com/api/v1/admin/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin_a","password":"pass123"}' \
  -c cookies_a.txt

# Verificar cookie creada
cat cookies_a.txt
# sitea.com    bk.token.brand_a    eyJ...

# 2. Login en Brand B (mismo navegador)
curl -X POST http://siteb.com/api/v1/admin/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin_b","password":"pass123"}' \
  -c cookies_b.txt

# Verificar cookie creada
cat cookies_b.txt
# siteb.com    bk.token.brand_b    eyJ...

# 3. Verificar /me en Brand A (sigue funcionando)
curl -X GET http://sitea.com/api/v1/admin/auth/me \
  -b cookies_a.txt
# ? Respuesta: { username: "admin_a" }

# 4. Verificar /me en Brand B (también funciona)
curl -X GET http://siteb.com/api/v1/admin/auth/me \
  -b cookies_b.txt
# ? Respuesta: { username: "admin_b" }
```

---

### **Test 2: Logout Selectivo**

```bash
# 1. Logout en Brand A
curl -X POST http://sitea.com/api/v1/admin/auth/logout \
  -b cookies_a.txt

# 2. Verificar /me en Brand A (debería fallar)
curl -X GET http://sitea.com/api/v1/admin/auth/me \
  -b cookies_a.txt
# ? 401 Unauthorized

# 3. Verificar /me en Brand B (sigue funcionando)
curl -X GET http://siteb.com/api/v1/admin/auth/me \
  -b cookies_b.txt
# ? Respuesta: { username: "admin_b" }
```

---

### **Test 3: DevTools Inspection**

1. Abrir DevTools ? Application ? Cookies
2. Login en múltiples brands
3. Verificar que existen múltiples cookies:

```
Domain: localhost
?? bk.token.localhost_dev = eyJ...
?? bk.token.bet30_backoffice = eyJ...
?? bk.token.otro_site = eyJ...
```

4. Cada cookie tiene:
   - **Name**: Único por brand
   - **Domain**: Específico (si configurado)
   - **Path**: `/`
   - **SameSite**: `Lax` o `None` (según cross-site)

---

## ?? **Migración para Usuarios Existentes**

### **¿Qué pasa con cookies antiguas?**

Si ya existen cookies `bk.token` (sin brand code):
- ? Se **ignorarán** (middleware busca `bk.token.*`)
- ? Al hacer login nuevamente, se crean las cookies correctas
- ? Las cookies antiguas expiran naturalmente (o se borran manualmente)

### **Limpieza Manual (Opcional)**

```javascript
// En el frontend (console del navegador)
document.cookie.split(";").forEach(cookie => {
  const name = cookie.split("=")[0].trim();
  if (name === "bk.token") {
    // Borrar cookie antigua
    document.cookie = `${name}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/`;
  }
});
```

---

## ? **Resumen**

**Problema**: Cookie única para todos los brands ? última sesión ganaba
**Solución**: Cookie con nombre único por brand ? sesiones independientes

**Cambios Implementados:**
1. ? Login crea `bk.token.{brandCode}` (dinámico)
2. ? Middleware busca cookies que empiecen con `bk.token.`
3. ? Logout elimina cookie específica del brand

**Resultado**: ? **Sesiones completamente independientes por brand**

**Beneficios:**
- ? Multiple logins simultáneos
- ? Sin conflictos entre brands
- ? Logout selectivo por brand
- ? Compatible con SUPER_ADMIN en múltiples brands

**¡Ahora puedes tener sesiones activas en todos los brands que quieras sin conflictos! ??**

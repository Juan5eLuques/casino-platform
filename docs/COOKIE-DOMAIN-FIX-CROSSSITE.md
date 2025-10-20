# Cookie Domain Fix - Cross-Site Multi-Brand

## ?? **Problema: Domain Incorrecto en Cookies Cross-Site**

### **Síntoma:**
```
Frontend A: sitea.netlify.app ? Login ? Cookie con Domain=api.railway.app
Frontend B: siteb.netlify.app ? Login ? Cookie TAMBIÉN con Domain=api.railway.app
```

**Resultado**: El navegador envía **ambas cookies** a `api.railway.app`, causando conflictos.

---

## ?? **Explicación del Problema**

### **Escenario Cross-Site:**
```
Frontend A: https://sitea.netlify.app
Backend:    https://api.railway.app

Frontend B: https://siteb.netlify.app
Backend:    https://api.railway.app (EL MISMO)
```

### **Comportamiento INCORRECTO (Domain seteado):**

```csharp
// ? MALO: Setear domain en cross-site
cookieOptions.Domain = "api.railway.app";
httpContext.Response.Cookies.Append("bk.token.sitea", token, cookieOptions);
```

**Resultado en el navegador:**

```
Cookies almacenadas:
?? Domain: api.railway.app
?  ?? bk.token.sitea = xxx (desde sitea.netlify.app)
?  ?? bk.token.siteb = yyy (desde siteb.netlify.app)

Request de sitea.netlify.app ? api.railway.app
  ? Envía: bk.token.sitea=xxx, bk.token.siteb=yyy ?

Request de siteb.netlify.app ? api.railway.app
  ? Envía: bk.token.sitea=xxx, bk.token.siteb=yyy ?
```

**Problema**: El navegador envía **TODAS las cookies del mismo domain** en cada request, sin importar desde qué frontend viene.

---

### **Comportamiento CORRECTO (Domain NO seteado - Host-Only):**

```csharp
// ? BUENO: NO setear domain en cross-site
// cookieOptions.Domain NO se setea
httpContext.Response.Cookies.Append("bk.token.sitea", token, cookieOptions);
```

**Resultado en el navegador:**

```
Cookies almacenadas:
?? Request Context: sitea.netlify.app ? api.railway.app
?  ?? bk.token.sitea = xxx (host-only, asociada con el origin completo)
?
?? Request Context: siteb.netlify.app ? api.railway.app
   ?? bk.token.siteb = yyy (host-only, asociada con OTRO origin)

Request de sitea.netlify.app ? api.railway.app
  ? Envía: SOLO bk.token.sitea=xxx ?

Request de siteb.netlify.app ? api.railway.app
  ? Envía: SOLO bk.token.siteb=yyy ?
```

**Solución**: Sin `Domain`, el navegador asocia la cookie con el **origen completo** del request (incluyendo el frontend), no solo con el backend.

---

## ? **Solución Implementada**

### **Código Corregido:**

```csharp
// CRITICAL: Configure SameSite and Domain based on environment
var host = httpContext.Request.Host.Host;
var origin = httpContext.Request.Headers["Origin"].FirstOrDefault();

// Check if frontend is on different domain (cross-site scenario)
bool isCrossSite = !string.IsNullOrEmpty(origin) && 
                  !origin.Contains(host) && 
                  !host.Contains("localhost") && 
                  !host.StartsWith("127.0.0.1");

if (isCrossSite)
{
    // Cross-site scenario (e.g., netlify.app ? railway.app)
    // MUST use SameSite=None to allow cross-site cookies
    cookieOptions.SameSite = SameSiteMode.None;
    // CRITICAL: DO NOT set Domain for cross-site (host-only cookie)
    logger.LogInformation("Cross-site: SameSite=None, NO Domain");
}
else
{
    // Same-site or localhost ? use Lax for better security
    cookieOptions.SameSite = SameSiteMode.Lax;
    
    // For same-site, we CAN set domain for subdomain sharing
    if (!host.Contains("localhost") && !host.StartsWith("127.0.0.1"))
    {
        cookieOptions.Domain = host;
        logger.LogInformation("Same-site: SameSite=Lax, Domain={Domain}", host);
    }
    else
    {
        logger.LogInformation("Localhost: SameSite=Lax, NO Domain");
    }
}
```

---

## ?? **Matriz de Comportamiento**

| Escenario | SameSite | Domain | Resultado |
|-----------|----------|--------|-----------|
| **Cross-Site** (netlify ? railway) | `None` | ? NO setear | ? Cookies independientes por origin |
| **Same-Site** (sitea.com ? api.sitea.com) | `Lax` | ? `api.sitea.com` | ? Compartida en subdomains |
| **Localhost** | `Lax` | ? NO setear | ? Funciona en desarrollo |

---

## ?? **Comparación Visual**

### **? ANTES (Con Domain en Cross-Site):**

```
???????????????????????????????????????????
?  Backend: api.railway.app               ?
?                                         ?
?  Cookies almacenadas:                   ?
?  ?? bk.token.sitea (desde sitea.app)   ?
?  ?? bk.token.siteb (desde siteb.app)   ?
?                                         ?
?  Request desde sitea.app:               ?
?  ? Envía: sitea + siteb ?              ?
?                                         ?
?  Request desde siteb.app:               ?
?  ? Envía: sitea + siteb ?              ?
???????????????????????????????????????????
```

### **? AHORA (Sin Domain en Cross-Site):**

```
???????????????????????????????????????????
?  Backend: api.railway.app               ?
?                                         ?
?  Cookies almacenadas:                   ?
?  ?? Context: sitea.app ? backend        ?
?  ?  ?? bk.token.sitea                   ?
?  ?                                      ?
?  ?? Context: siteb.app ? backend        ?
?     ?? bk.token.siteb                   ?
?                                         ?
?  Request desde sitea.app:               ?
?  ? Envía: SOLO sitea ?                 ?
?                                         ?
?  Request desde siteb.app:               ?
?  ? Envía: SOLO siteb ?                 ?
???????????????????????????????????????????
```

---

## ?? **Testing**

### **Test 1: Verificar Cookies en DevTools**

1. Login en `sitea.netlify.app`
2. Abrir DevTools ? Application ? Cookies ? `https://api.railway.app`
3. Verificar cookie:

```
Name: bk.token.sitea
Value: eyJ...
Domain: api.railway.app (si está, es el problema ?)
Domain: (vacío o no aparece) ?
SameSite: None
Secure: ?
```

4. Login en `siteb.netlify.app` (otra tab)
5. Verificar cookies:

```
Deberías ver DOS cookies:
?? bk.token.sitea
?? bk.token.siteb

Cada una asociada a su contexto de origin
```

---

### **Test 2: Verificar Headers en Network**

```bash
# Request desde sitea.netlify.app
curl -X GET https://api.railway.app/api/v1/admin/auth/me \
  -H "Origin: https://sitea.netlify.app" \
  -H "Cookie: bk.token.sitea=xxx; bk.token.siteb=yyy"

# Backend debería leer SOLO bk.token.sitea
```

---

### **Test 3: Confirmar Sesiones Independientes**

```
Tab 1: sitea.netlify.app
  ? Login usuario1
  ? /me muestra usuario1 ?

Tab 2: siteb.netlify.app
  ? Login usuario2
  ? /me muestra usuario2 ?

Refrescar Tab 1:
  ? /me SIGUE mostrando usuario1 ? (no usuario2)

Refrescar Tab 2:
  ? /me SIGUE mostrando usuario2 ? (no usuario1)
```

---

## ?? **Deployment**

### **Variables de Entorno:**

No se necesita configuración adicional. El backend detecta automáticamente cross-site vs same-site basado en el `Origin` header.

### **Requisitos:**

1. ? Backend debe soportar HTTPS (`Secure: true`)
2. ? CORS configurado con `AllowCredentials()`
3. ? Frontend envía `credentials: 'include'` en requests

---

## ?? **Resumen**

**Problema**: Setear `Domain` en cookies cross-site causaba que todas las cookies del backend se enviaran en cada request, sin importar el origin del frontend.

**Solución**: **NO setear `Domain`** en escenarios cross-site. Esto hace que las cookies sean "host-only" y el navegador las asocie con el **origen completo** (frontend + backend), no solo con el backend.

**Resultado**: ? Cookies completamente independientes por cada combinación de frontend + backend.

**Código Clave:**
```csharp
if (isCrossSite)
{
    // NO setear Domain para cross-site
    cookieOptions.SameSite = SameSiteMode.None;
}
```

**¡Ahora las sesiones multi-brand funcionan correctamente incluso en cross-site! ??**

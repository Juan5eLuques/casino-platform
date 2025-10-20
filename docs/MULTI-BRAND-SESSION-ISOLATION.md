# Multi-Brand Session Isolation - Problema y Solución

## ?? **Problema Identificado**

### **Descripción del Issue:**
Al tener múltiples brands (ej: `siteA.com` y `siteB.com`), las sesiones de login se compartían entre brands:
1. Login en `siteA` con `usuario1`
2. Login en `siteB` con `usuario2`
3. Al recargar ambas páginas, **ambos sites usaban el mismo token** (el último login)

### **Causa Raíz:**
```csharp
// ? PROBLEMA: Cookie compartida entre todos los dominios
httpContext.Response.Cookies.Append("bk.token", token, new CookieOptions {
    Path = "/",              // Se envía a TODAS las rutas
    SameSite = SameSiteMode.None,  // Permite compartir entre dominios
    // Domain NO especificado ? se comparte entre subdominios
});
```

### **Impacto de Seguridad:**
- ? **Token JWT válido**: El token en sí tiene el `brand_id` correcto
- ? **Cookie compartida**: El navegador envía la MISMA cookie a todos los dominios
- ? **Sin validación de brand en login**: Cualquier usuario podía loguearse en cualquier brand
- ? **Última sesión gana**: El último login sobrescribe la cookie para TODOS los dominios

---

## ? **Solución Implementada**

### **1. Validación de Brand en Login**

```csharp
// ? NUEVO: Validar que el usuario pertenece al brand actual
if (user.Role != BackofficeUserRole.SUPER_ADMIN)
{
    if (!user.BrandId.HasValue || user.BrandId.Value != brandContext.BrandId)
    {
        logger.LogWarning(
            "Admin login failed: user {Username} (BrandId: {UserBrandId}) attempted login on different brand {CurrentBrandId}",
            request.Username, user.BrandId, brandContext.BrandId);
        return Results.Problem(
            title: "Brand Mismatch",
            detail: "This user account is not authorized for this brand/site.",
            statusCode: 403);
    }
}
```

**Comportamiento:**
- **SUPER_ADMIN**: Puede loguearse en cualquier brand
- **BRAND_ADMIN/CASHIER**: Solo puede loguearse en SU brand asignado
- **Error 403**: Si intenta loguearse en brand incorrecto

---

### **2. Brand Context Requerido en Login**

```csharp
// ? NUEVO: Requiere brand context resuelto
if (!brandContext.IsResolved)
{
    logger.LogWarning("Admin login attempted without resolved brand context on host: {Host}", 
        httpContext.Request.Host.Host);
    return Results.Problem(
        title: "Brand Not Resolved",
        detail: "Cannot login without a valid brand context. Ensure you're accessing from a configured domain.",
        statusCode: 400);
}
```

**Beneficios:**
- Asegura que el login siempre ocurre con un brand válido
- Previene logins desde dominios no configurados
- El `brand_id` en el token es siempre correcto

---

### **3. Cookies con Domain Específico (Producción)**

```csharp
// ? NUEVO: Set Domain para aislamiento en producción
var host = httpContext.Request.Host.Host;
if (!host.Contains("localhost") && !host.StartsWith("127.0.0.1"))
{
    // Cookie solo para este dominio específico
    cookieOptions.Domain = host;
    logger.LogInformation("Setting cookie domain to: {Domain}", host);
}
```

**Comportamiento:**
- **Desarrollo (localhost)**: Cookie sin domain ? funciona para desarrollo local
- **Producción**: Cookie con `Domain = "siteA.com"` ? aislada por dominio
- **Resultado**: Cada brand tiene su propia cookie independiente

---

### **4. SameSite=Lax para Mejor Aislamiento**

```csharp
// ? CAMBIADO: SameSite.Lax en lugar de None
SameSite = SameSiteMode.Lax  // Mejor aislamiento entre sitios
```

**Beneficios:**
- Mayor seguridad contra CSRF
- Cookies no se comparten entre dominios diferentes
- Compatible con navegación normal (GET requests)

---

### **5. Brand Claims en JWT**

```csharp
// ? NUEVO: Siempre incluir brand en el token
var claims = new List<Claim>
{
    new(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new(ClaimTypes.Name, user.Username),
    new(ClaimTypes.Role, user.Role.ToString()),
    // CRITICAL: Brand actual del login
    new("brand_id", brandContext.BrandId.ToString()),
    new("brand_code", brandContext.BrandCode)
};
```

**Verificación en Endpoints:**
Los endpoints protegidos pueden validar:
```csharp
var tokenBrandId = Guid.Parse(httpContext.User.FindFirst("brand_id")!.Value);
if (tokenBrandId != brandContext.BrandId)
{
    return Results.Unauthorized(); // Token de otro brand
}
```

---

## ?? **Casos de Uso Soportados**

### **Caso 1: Usuario Normal (BRAND_ADMIN/CASHIER)**
```
???????????????????????????????????????????????
?  Usuario: admin1 (BrandId: AAA)             ?
?  ? Login en siteA.com ? SUCCESS            ?
?  ? Login en siteB.com ? 403 Brand Mismatch ?
???????????????????????????????????????????????
```

### **Caso 2: SUPER_ADMIN**
```
???????????????????????????????????????????????
?  Usuario: superadmin (BrandId: NULL)        ?
?  ? Login en siteA.com ? SUCCESS            ?
?  ? Login en siteB.com ? SUCCESS            ?
?  ? Login en siteC.com ? SUCCESS            ?
?  Token diferente por cada brand             ?
???????????????????????????????????????????????
```

### **Caso 3: Sesiones Simultáneas (Producción)**
```
Tab 1: siteA.com ? usuario1 (token A en cookie domain=siteA.com)
Tab 2: siteB.com ? usuario2 (token B en cookie domain=siteB.com)

? Cookies AISLADAS por dominio
? Cada tab mantiene su sesión independiente
```

---

## ?? **Testing**

### **Test 1: Login con Brand Incorrecto**
```bash
# Usuario admin1 pertenece a Brand A
curl -X POST https://siteb.com/api/v1/admin/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin1","password":"pass123"}'

# Respuesta esperada: 403
{
  "title": "Brand Mismatch",
  "detail": "This user account is not authorized for this brand/site.",
  "status": 403
}
```

### **Test 2: SUPER_ADMIN en Múltiples Brands**
```bash
# Login en Brand A
curl -X POST https://sitea.com/api/v1/admin/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"superadmin","password":"pass123"}' \
  -c cookies_a.txt

# Login en Brand B
curl -X POST https://siteb.com/api/v1/admin/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"superadmin","password":"pass123"}' \
  -c cookies_b.txt

# ? Cada archivo tiene su propia cookie con domain diferente
```

### **Test 3: Verificar Cookie Domain**
```bash
# Inspeccionar cookies en el navegador
# Chrome DevTools ? Application ? Cookies

# siteA.com:
bk.token | Domain: siteA.com | Path: / | SameSite: Lax

# siteB.com:
bk.token | Domain: siteB.com | Path: / | SameSite: Lax
```

---

## ?? **Deployment Checklist**

### **Desarrollo (localhost)**
- [ ] BrandResolver funciona para múltiples hosts en `/etc/hosts`
- [ ] Cookies sin `Domain` (funciona para localhost)
- [ ] SUPER_ADMIN puede probar múltiples brands

### **Producción**
- [ ] DNS configurado para cada brand (siteA.com, siteB.com)
- [ ] Certificados SSL para HTTPS (Secure cookie)
- [ ] Cookies con `Domain` específico por brand
- [ ] Usuarios asignados al brand correcto en BD

---

## ?? **Comparación Antes/Después**

| Aspecto | ? Antes | ? Después |
|---------|----------|------------|
| **Cookie Domain** | Compartida | Aislada por dominio |
| **Validación Brand** | No | Sí (403 si incorrecto) |
| **SameSite** | None | Lax |
| **Brand en Token** | Opcional | Siempre |
| **Sesiones Simultáneas** | ? Conflicto | ? Independientes |
| **SUPER_ADMIN** | Funciona | ? Funciona mejor |
| **Seguridad** | ?? Media | ? Alta |

---

## ?? **Mejoras de Seguridad**

1. ? **Brand-Based Access Control**: Usuarios solo acceden a su brand
2. ? **Domain Isolation**: Cookies aisladas por dominio en producción
3. ? **Token Verification**: Brand ID en claims para validación
4. ? **CSRF Protection**: SameSite=Lax mejora protección
5. ? **Audit Trail**: Logs con brand_id para auditoría
6. ? **No Default Brand in Dev**: Localhost NO usa brand por defecto (evita bypass)
7. ? **Login Requires Brand**: BrandResolver se ejecuta ANTES del login

---

## ?? **Notas Adicionales**

### **Desarrollo Multi-Brand Local**

?? **IMPORTANTE**: Localhost (`localhost` o `127.0.0.1`) **NO resolverá ningún brand** automáticamente.

Para probar múltiples brands en desarrollo, **DEBES** configurar `/etc/hosts` (Linux/Mac) o `C:\Windows\System32\drivers\etc\hosts` (Windows):

```
# Desarrollo Multi-Brand
127.0.0.1  sitea.local
127.0.0.1  siteb.local
127.0.0.1  sitec.local
```

Luego crear brands en la base de datos con estos dominios:
```sql
INSERT INTO "Brands" ("Id", "Code", "Name", "Locale", "Domain", "AdminDomain", "CorsOrigins", "Status", "CreatedAt", "UpdatedAt")
VALUES 
  (gen_random_uuid(), 'SITEA_LOCAL', 'Site A Local', 'en-US', 'sitea.local', 'sitea.local', 
   'http://sitea.local:5173,http://sitea.local:5000', 'ACTIVE', NOW(), NOW()),
  (gen_random_uuid(), 'SITEB_LOCAL', 'Site B Local', 'en-US', 'siteb.local', 'siteb.local', 
   'http://siteb.local:5173,http://siteb.local:5000', 'ACTIVE', NOW(), NOW());
```

Acceder:
- `http://sitea.local:5173` (frontend)
- `http://sitea.local:5000/api/...` (backend)

### **¿Por qué NO hay brand por defecto en localhost?**

**Antes (inseguro):**
```
? localhost ? usa CUALQUIER brand activo
? admin1 (brand A) puede loguearse en localhost con brand B activo
? Bypasea validación de brand
```

**Ahora (seguro):**
```
? localhost ? ERROR: brand_not_resolved
? FUERZA configurar /etc/hosts con dominios correctos
? Validación de brand siempre se ejecuta
```

### **Error en localhost:**
```json
{
  "error": "brand_not_resolved",
  "host": "localhost:5000",
  "message": "No brand found for this host. Please configure the brand domain in the database or use a configured domain.",
  "hint_localhost": "For localhost development, configure /etc/hosts with brand domains like '127.0.0.1 sitea.local'"
}
```

### **Frontend Considerations**
El frontend debe:
1. Detectar brand desde `window.location.host`
2. Hacer login contra el host correcto
3. No compartir tokens entre tabs de diferentes brands

### **Refresh Tokens (Futuro)**
Si se implementan refresh tokens:
- También deben tener `brand_id` claim
- Cookies de refresh con mismo `Domain` que access token
- Validar brand en refresh endpoint

---

## ? **Resumen**

**Problema**: Sesiones compartidas entre brands diferentes
**Causa**: Cookies sin aislamiento + sin validación de brand
**Solución**: 
1. Validar brand en login
2. Cookies con domain específico
3. SameSite=Lax
4. Brand claims en JWT

**Resultado**: ? Sesiones completamente aisladas por brand con seguridad mejorada

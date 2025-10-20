# Cookie Troubleshooting - Cross-Site Authentication

## ?? **Problema: Cookie No Se Envía en Requests**

### **Síntomas:**
- Login exitoso (`200 OK`)
- Cookie aparece en `Set-Cookie` header del response
- Endpoint `/me` retorna `401 Unauthorized`
- Cookie **NO aparece** en DevTools ? Application ? Cookies

### **Causa Raíz:**
Frontend y Backend en **dominios diferentes** con configuración incorrecta de `SameSite`.

---

## ?? **Diagnóstico**

### **Paso 1: Verificar Dominios**

**Frontend:**
```
https://backoffice-casino.netlify.app
```

**Backend:**
```
https://casino-platform-production.up.railway.app
```

**Resultado**: **Cross-Site** ? (dominios diferentes)

---

### **Paso 2: Inspeccionar Cookie en Response**

Abrir DevTools ? Network ? Login request ? Response Headers:

```http
Set-Cookie: bk.token=eyJ...; 
    expires=Mon, 20 Oct 2025 13:21:42 GMT; 
    domain=casino-platform-production.up.railway.app; 
    path=/; 
    secure; 
    samesite=lax;  ? ? PROBLEMA AQUÍ
    httponly
```

**Problema**: `samesite=lax` **NO permite** cookies cross-site.

---

### **Paso 3: Verificar Request a /me**

DevTools ? Network ? `/me` request ? Request Headers:

```http
GET /api/v1/admin/auth/me
Origin: https://backoffice-casino.netlify.app
```

**Cookie header**: ? **VACÍO** (no se envía)

---

## ? **Solución**

### **Opción 1: SameSite=None (Recomendada para Cross-Site)**

El backend ahora **detecta automáticamente** cross-site y usa `SameSite=None`:

```csharp
// Backend auto-detecta cross-site
var origin = httpContext.Request.Headers["Origin"].FirstOrDefault();
bool isCrossSite = !origin.Contains(host);

if (isCrossSite)
{
    cookieOptions.SameSite = SameSiteMode.None;  // ? Permite cross-site
}
```

**Resultado esperado:**
```http
Set-Cookie: bk.token=eyJ...; 
    samesite=none;  ? ? CORRECTO
    secure; 
    httponly
```

---

### **Opción 2: Same-Origin (Frontend y Backend en Mismo Dominio)**

Si quieres **máxima seguridad**, usa el mismo dominio:

**Configuración:**
```
Frontend:  https://admin.mysite.com
Backend:   https://api.mysite.com
```

**Resultado**: Same-Site ? `SameSite=Lax` funciona ?

---

### **Opción 3: Proxy (Desarrollo Local)**

En desarrollo, usa proxy en Vite para hacer que frontend y backend parezcan same-site:

```typescript
// vite.config.ts
export default defineConfig({
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      }
    }
  }
})
```

**Resultado**: 
```
Frontend:  http://localhost:5173
Request:   http://localhost:5173/api/v1/admin/auth/login
Backend:   http://localhost:5000/api/v1/admin/auth/login (proxy)
```

Same-origin ? `SameSite=Lax` funciona ?

---

## ?? **Testing**

### **Test 1: Verificar SameSite en Response**

```bash
curl -i -X POST https://casino-platform-production.up.railway.app/api/v1/admin/auth/login \
  -H "Origin: https://backoffice-casino.netlify.app" \
  -H "Content-Type: application/json" \
  -d '{"username":"superadmin","password":"pass123"}'

# Buscar en response:
# ? Esperado: samesite=none
# ? Incorrecto: samesite=lax
```

### **Test 2: Verificar Cookie Se Envía**

En DevTools ? Application ? Cookies:

**URL**: `https://casino-platform-production.up.railway.app`

**Cookie esperada:**
```
Name: bk.token
Value: eyJ...
Domain: casino-platform-production.up.railway.app
Path: /
SameSite: None  ? ? CRÍTICO
Secure: ?
HttpOnly: ?
```

### **Test 3: Request a /me con Credentials**

En tu frontend, asegurar `credentials: 'include'`:

```typescript
// ? CORRECTO
fetch('https://casino-platform-production.up.railway.app/api/v1/admin/auth/me', {
  credentials: 'include',  // ? CRÍTICO para enviar cookies cross-site
  headers: {
    'Content-Type': 'application/json'
  }
})

// ? INCORRECTO (no envía cookies)
fetch('https://casino-platform-production.up.railway.app/api/v1/admin/auth/me', {
  headers: {
    'Content-Type': 'application/json'
  }
})
```

---

## ?? **Problemas Comunes**

### **Problema 1: Cookie No Aparece en DevTools**

**Causa**: `SameSite=Lax` en escenario cross-site

**Solución**: Verificar que backend usa `SameSite=None` (auto-detectado)

**Test**:
```bash
# Ver Set-Cookie header
curl -i [...] | grep -i "set-cookie"
```

---

### **Problema 2: Cookie Se Crea pero No Se Envía**

**Causa**: Frontend no usa `credentials: 'include'`

**Solución**:
```typescript
// Axios
axios.defaults.withCredentials = true;

// Fetch
fetch(url, { credentials: 'include' })

// React Query
const queryClient = new QueryClient({
  defaultOptions: {
    queries: { 
      credentials: 'include' 
    }
  }
})
```

---

### **Problema 3: CORS Preflight Falla**

**Causa**: Backend no permite credenciales en CORS

**Verificar en backend** (`Program.cs` o middleware):
```csharp
// ? DEBE TENER
app.UseCors(policy => policy
    .WithOrigins("https://backoffice-casino.netlify.app")
    .AllowCredentials()  // ? CRÍTICO
    .AllowAnyHeader()
    .AllowAnyMethod());
```

**Test**:
```bash
curl -i -X OPTIONS https://casino-platform-production.up.railway.app/api/v1/admin/auth/me \
  -H "Origin: https://backoffice-casino.netlify.app" \
  -H "Access-Control-Request-Method: GET" \
  -H "Access-Control-Request-Headers: content-type"

# Buscar:
# ? Access-Control-Allow-Credentials: true
```

---

### **Problema 4: Cookie Con Domain Incorrecto**

**Síntoma**:
```
Domain: backoffice-casino.netlify.app  ? ? Frontend domain
```

**Debe ser**:
```
Domain: casino-platform-production.up.railway.app  ? ? Backend domain
```

**Solución**: Cookie se setea en el **dominio del backend**, no del frontend.

---

## ?? **Matriz de Compatibilidad**

| Frontend Domain | Backend Domain | Same-Site? | SameSite Required |
|----------------|----------------|------------|-------------------|
| localhost:5173 | localhost:5000 | ? Yes | `Lax` |
| admin.mysite.com | api.mysite.com | ? Yes | `Lax` |
| netlify.app | railway.app | ? No | `None` + `Secure` |
| vercel.app | render.com | ? No | `None` + `Secure` |

---

## ?? **Checklist de Seguridad**

Para producción con `SameSite=None`:

- [ ] ? `Secure=true` (solo HTTPS)
- [ ] ? `HttpOnly=true` (no accesible desde JS)
- [ ] ? CORS configurado con `AllowCredentials()`
- [ ] ? Frontend usa `credentials: 'include'`
- [ ] ? Backend valida `Origin` header
- [ ] ? CSRF protection implementado (futuro)

---

## ?? **Configuración Recomendada por Escenario**

### **Desarrollo Local**
```typescript
// Frontend: http://localhost:5173
// Backend: http://localhost:5000
// SameSite: Lax ?
// Domain: no se setea
```

### **Staging (Same Domain)**
```typescript
// Frontend: https://admin-staging.mysite.com
// Backend: https://api-staging.mysite.com
// SameSite: Lax ?
// Domain: no se setea o .mysite.com
```

### **Producción (Cross-Domain)**
```typescript
// Frontend: https://backoffice.netlify.app
// Backend: https://api.railway.app
// SameSite: None ? (auto-detectado)
// Secure: true ?
// Domain: api.railway.app
```

---

## ? **Verificación Final**

Después de aplicar los cambios:

1. **Login**:
   - [ ] Response tiene `Set-Cookie` con `samesite=none`
   - [ ] Cookie aparece en DevTools bajo dominio del backend
   
2. **Request /me**:
   - [ ] Request Headers incluye `Cookie: bk.token=...`
   - [ ] Response es `200 OK` con datos del usuario

3. **Otras Requests**:
   - [ ] Todas las requests protegidas incluyen la cookie
   - [ ] No hay errores 401 después del login

**Si todo está ?, el sistema funciona correctamente en cross-site! ??**

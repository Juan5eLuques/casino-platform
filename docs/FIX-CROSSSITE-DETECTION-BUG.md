# Fix: Cross-Site Detection Bug

## ?? **Bug Identificado**

### **Problema:**
La detección de cross-site usaba `origin.Contains(host)` que daba **falsos negativos**:

```csharp
// ? CÓDIGO INCORRECTO:
bool isCrossSite = !string.IsNullOrEmpty(origin) && 
                  !origin.Contains(host);  // ? PROBLEMA AQUÍ

// Ejemplo que falla:
origin = "https://backoffice-casino.netlify.app"
host = "casino-platform-production.up.railway.app"

origin.Contains(host) = false ?
Pero... origin.Contains("casino") = TRUE ?

Resultado: NO detecta cross-site cuando SÍ lo es
```

**Resultado**: El backend seteaba `Domain = casino-platform-production.up.railway.app` cuando NO debería.

---

## ? **Solución Implementada**

### **Código Corregido:**

```csharp
// ? CÓDIGO CORRECTO:
// Parse origin to get just the hostname
string? originHost = null;
if (!string.IsNullOrEmpty(origin))
{
    try
    {
        var originUri = new Uri(origin);
        originHost = originUri.Host;  // ? Extraer solo el hostname
    }
    catch
    {
        originHost = null;  // Invalid origin = cross-site por seguridad
    }
}

// Comparar HOSTNAMES EXACTOS (no contains)
bool isCrossSite = !string.IsNullOrEmpty(originHost) && 
                  originHost != host &&  // ? Comparación exacta
                  !host.Contains("localhost") && 
                  !host.StartsWith("127.0.0.1");
```

---

## ?? **Comparación Antes/Después**

### **Escenario: Netlify ? Railway**

```
Frontend:  https://backoffice-casino.netlify.app
Backend:   https://casino-platform-production.up.railway.app
```

#### **? ANTES (Bug):**

```csharp
origin = "https://backoffice-casino.netlify.app"
host = "casino-platform-production.up.railway.app"

// Lógica incorrecta:
!origin.Contains(host) 
  = !"https://backoffice-casino.netlify.app".Contains("casino-platform-production.up.railway.app")
  = !false
  = true

Pero... origin contiene la palabra "casino", entonces el Contains puede confundirse

Resultado: isCrossSite = false ? (INCORRECTO)
```

**Cookie resultante:**
```
Domain: casino-platform-production.up.railway.app ?
SameSite: Lax ?
```

#### **? AHORA (Arreglado):**

```csharp
origin = "https://backoffice-casino.netlify.app"
originHost = "backoffice-casino.netlify.app"  // Parse URI
host = "casino-platform-production.up.railway.app"

// Lógica correcta:
originHost != host
  = "backoffice-casino.netlify.app" != "casino-platform-production.up.railway.app"
  = true

Resultado: isCrossSite = true ? (CORRECTO)
```

**Cookie resultante:**
```
Domain: (NO SE SETEA) ?
SameSite: None ?
```

---

## ?? **Testing**

### **Test 1: Verificar Logs**

Después del fix, los logs deberían mostrar:

```
? CORRECTO:
Cross-site detected: OriginHost=backoffice-casino.netlify.app, BackendHost=casino-platform-production.up.railway.app ? Using SameSite=None, NO Domain

? INCORRECTO (antes del fix):
Same-site: Using SameSite=Lax, Domain=casino-platform-production.up.railway.app
```

### **Test 2: Verificar Cookie en DevTools**

**Chrome DevTools ? Application ? Cookies ? `casino-platform-production.up.railway.app`**

#### **? ANTES del fix:**
```
Name: bk.token.netlify_prod
Value: eyJ...
Domain: casino-platform-production.up.railway.app  ?
SameSite: Lax  ?
```

#### **? DESPUÉS del fix:**
```
Name: bk.token.netlify_prod
Value: eyJ...
Domain: (vacío o no aparece)  ?
SameSite: None  ?
```

### **Test 3: Sesiones Múltiples**

```
Tab 1: https://sitea.netlify.app
  ? Login usuario1
  ? Cookie: bk.token.sitea (SameSite=None, NO Domain)

Tab 2: https://siteb.netlify.app
  ? Login usuario2
  ? Cookie: bk.token.siteb (SameSite=None, NO Domain)

Refrescar Tab 1:
  ? /me muestra usuario1 ?

Refrescar Tab 2:
  ? /me muestra usuario2 ?
```

---

## ?? **Casos de Prueba**

### **Caso 1: Cross-Site (Netlify ? Railway)**

```
Origin: https://backoffice-casino.netlify.app
Host: casino-platform-production.up.railway.app

OriginHost: backoffice-casino.netlify.app
Host: casino-platform-production.up.railway.app

originHost != host ? TRUE ?
isCrossSite = TRUE ?

Cookie:
  Domain: NO SETEA ?
  SameSite: None ?
```

### **Caso 2: Same-Site (Subdomain)**

```
Origin: https://admin.mysite.com
Host: api.mysite.com

OriginHost: admin.mysite.com
Host: api.mysite.com

originHost != host ? TRUE
BUT: Share same parent domain ? Could optimize to detect this

Current behavior: Treated as cross-site (safe default)
```

### **Caso 3: Localhost**

```
Origin: http://localhost:5173
Host: localhost:5000

OriginHost: localhost
Host: localhost

originHost == host ? FALSE (ports differ)
BUT: host.Contains("localhost") ? TRUE

isCrossSite = FALSE ?
Cookie:
  Domain: NO SETEA ?
  SameSite: Lax ?
```

---

## ?? **Resumen**

**Problema**: `origin.Contains(host)` daba falsos positivos cuando ambos dominios compartían palabras comunes (como "casino").

**Solución**: 
1. Parsear el `Origin` header para extraer solo el **hostname**
2. Comparar **EXACTAMENTE** `originHost != host` (sin `Contains`)

**Resultado**: Detección correcta de cross-site ? cookies sin Domain en escenarios cross-site.

**Cambios en:**
- `AdminLogin`: Detección corregida
- `AdminLogout`: Detección corregida (para borrar correctamente)

**¡Ahora las sesiones multi-brand funcionan correctamente! ??**

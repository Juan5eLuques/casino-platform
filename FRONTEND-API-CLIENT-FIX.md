# ?? Frontend API Client - Configuración y Troubleshooting

## ?? Problema Identificado

Estás experimentando errores CORS y 401 Unauthorized al intentar consumir endpoints del backend desde el frontend. Esto se debe a:

1. **CORS mal configurado** entre dominios HTTP/HTTPS mixtos
2. **Cookies no se están enviando** correctamente
3. **Headers incorrectos** en el cliente Axios

---

## ? Solución Completa

### **1. Backend: CORS Middleware Corregido** ?

El middleware `DynamicCorsMiddleware.cs` ya está corregido para:
- ? Permitir requests a `/api/v1/admin/*` sin requerir brand resolution
- ? Soportar origins de admin específicos
- ? Enviar headers CORS correctos con `Access-Control-Allow-Credentials: true`

**Origins permitidos para Admin:**
```csharp
"http://localhost:5173"
"http://admin.bet30.local:5173"
"https://admin.bet30.local:5173"
```

### **2. Frontend: Cliente API Correcto**

```typescript
// src/api/client.ts
import axios, { AxiosError } from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  withCredentials: true, // ? CRÍTICO: Envía cookies automáticamente
  headers: {
    'Content-Type': 'application/json',
    // ? NO incluir 'Host' header - Lo maneja el browser
  },
});

// Interceptor para logging de requests (debugging)
apiClient.interceptors.request.use(
  (config) => {
    console.log('?? API Request:', config.method?.toUpperCase(), config.url, {
      baseURL: config.baseURL,
      withCredentials: config.withCredentials
    });
    return config;
  },
  (error) => {
    console.error('? Request Error:', error);
    return Promise.reject(error);
  }
);

// Interceptor para manejar respuestas y errores
apiClient.interceptors.response.use(
  (response) => {
    console.log('? API Response:', response.status, response.config.url);
    return response;
  },
  (error: AxiosError<ApiError>) => {
    console.error('? Response Error:', {
      status: error.response?.status,
      message: error.message,
      url: error.config?.url,
      data: error.response?.data
    });

    // Si es 401, redirigir al login (excepto si ya estamos en login)
    if (error.response?.status === 401 && !error.config?.url?.includes('/login')) {
      console.warn('?? Unauthorized - Redirecting to login');
      window.location.href = '/login';
    }
    
    return Promise.reject({
      title: error.response?.data?.title || 'Error',
      detail: error.response?.data?.detail || error.message || 'An error occurred',
      status: error.response?.status || 500,
    });
  }
);

export interface ApiError {
  title: string;
  detail: string;
  status: number;
}
```

### **3. Variables de Entorno Correctas**

**Opción A: Desarrollo Local Simple (Recomendado) ?**

```env
# .env.development
VITE_API_URL=http://localhost:5000/api/v1
```

**Backend:** Debe correr en `http://localhost:5000`

**Frontend:** Debe correr en `http://localhost:5173`

**Opción B: Dominios Locales Custom**

```env
# .env.development
VITE_API_URL=http://admin.bet30.local:5000/api/v1
```

**Configurar** `/etc/hosts` (Linux/Mac) o `C:\Windows\System32\drivers\etc\hosts` (Windows):
```
127.0.0.1 admin.bet30.local
127.0.0.1 bet30.local
```

**Backend:** Configurar en `launchSettings.json`:
```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://admin.bet30.local:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

**Frontend:** Configurar en `vite.config.ts`:
```typescript
export default defineConfig({
  server: {
    port: 5173,
    host: 'admin.bet30.local'
  }
})
```

---

## ?? Troubleshooting por Error

### **Error 1: CORS policy: No 'Access-Control-Allow-Origin'**

```
Access to XMLHttpRequest at 'https://admin.bet30.local:7182/api/v1/admin/operators'
from origin 'http://admin.bet30.local:5173' has been blocked by CORS policy
```

**Causas:**
1. Backend no envía headers CORS
2. Origin del frontend no está en la whitelist
3. Mixed content (HTTP frontend ? HTTPS backend)

**Soluciones:**
```bash
# 1. Verifica que el backend esté corriendo
dotnet run --project apps/api/Casino.Api

# 2. Verifica los logs del backend para ver qué origin recibe
# Busca en logs: "CORS Request: GET /admin/operators from Origin: ..."

# 3. Usa el MISMO protocolo (HTTP o HTTPS) en ambos lados
# Recomendado para desarrollo: TODO HTTP
VITE_API_URL=http://localhost:5000/api/v1
```

### **Error 2: 401 Unauthorized en endpoints protegidos**

```
GET /api/v1/admin/operators 401 (Unauthorized)
```

**Causas:**
1. Cookie `bk.token` no existe o expiró
2. Cookie no se está enviando (`withCredentials: false`)
3. No has hecho login

**Soluciones:**
```typescript
// 1. Verifica que withCredentials esté en true
axios.create({
  withCredentials: true // ? DEBE estar
});

// 2. Verifica en DevTools > Application > Cookies
// Debe existir: bk.token con el JWT

// 3. Si no existe la cookie, haz login primero
await authApi.login('superadmin', 'password123');

// 4. La cookie debe tener el path correcto
// bk.token: Path=/admin (solo se envía a /admin/*)
```

### **Error 3: Network Error / ERR_FAILED**

```
? Network Error: Network Error
GET https://admin.bet30.local:7182/... net::ERR_FAILED
```

**Causas:**
1. Backend no está corriendo
2. Puerto incorrecto
3. Certificado SSL inválido (HTTPS)
4. Firewall bloqueando

**Soluciones:**
```bash
# 1. Verifica que el backend esté corriendo
dotnet run --project apps/api/Casino.Api
# Debe mostrar: "Now listening on: http://localhost:5000"

# 2. Verifica el puerto en VITE_API_URL
echo $VITE_API_URL
# Debe coincidir con el puerto del backend

# 3. Si usas HTTPS, acepta el certificado
# Navega directamente a: https://localhost:7182
# Click en "Advanced" > "Proceed to localhost (unsafe)"

# 4. O usa HTTP en desarrollo (más simple)
VITE_API_URL=http://localhost:5000/api/v1
```

### **Error 4: Cookie not sent / Third-party cookie blocked**

```
Warning: Cookie "bk.token" blocked by browser
```

**Causas:**
1. `SameSite` restrictivo
2. Dominios diferentes (third-party cookie)
3. `withCredentials: false`

**Soluciones:**
```typescript
// Backend: Cookie config en AuthEndpoints.cs
httpContext.Response.Cookies.Append("bk.token", jwt, new CookieOptions {
    HttpOnly = true,
    Secure = httpContext.Request.IsHttps, // false en dev HTTP
    SameSite = SameSiteMode.Lax, // ? Permite same-site
    Path = "/admin",
    Expires = tokenResponse.ExpiresAt
});

// Frontend: Axios config
axios.create({
  withCredentials: true, // ? CRÍTICO
  baseURL: 'http://localhost:5000/api/v1' // Mismo dominio
});
```

---

## ?? Checklist de Verificación

### **Backend (API)**
- [ ] Backend corriendo: `dotnet run --project apps/api/Casino.Api`
- [ ] Puerto correcto: `http://localhost:5000` (o el que uses)
- [ ] Logs muestran: "CORS Request: ... from Origin: http://localhost:5173"
- [ ] Logs muestran: "CORS allowed for origin"
- [ ] No errores de excepción en logs

### **Frontend**
- [ ] `withCredentials: true` en axios config
- [ ] `VITE_API_URL` apunta al backend correcto
- [ ] NO incluir header `Host` manualmente
- [ ] Mismo protocolo (HTTP/HTTP o HTTPS/HTTPS)
- [ ] Cookie `bk.token` existe después del login (DevTools > Application > Cookies)

### **Network**
- [ ] Backend y frontend en mismo protocolo (HTTP o HTTPS)
- [ ] Hosts configurados si usas dominios custom (`/etc/hosts`)
- [ ] Firewall no bloquea puertos 5000 o 5173
- [ ] Browser no bloquea cookies de terceros (en Settings)

---

## ?? Testing Paso a Paso

### **1. Verificar Backend**
```bash
# Terminal 1: Correr backend
cd apps/api/Casino.Api
dotnet run

# Debe mostrar:
# Now listening on: http://localhost:5000
```

### **2. Verificar CORS con curl**
```bash
# Hacer OPTIONS request (preflight)
curl -X OPTIONS http://localhost:5000/api/v1/admin/operators \
  -H "Origin: http://localhost:5173" \
  -H "Access-Control-Request-Method: GET" \
  -v

# Debe responder con:
# Access-Control-Allow-Origin: http://localhost:5173
# Access-Control-Allow-Credentials: true
```

### **3. Login desde Frontend**
```typescript
// En React DevTools Console
await authApi.login('superadmin', 'password123');

// Verificar en Application > Cookies:
// Name: bk.token
// Value: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
// Path: /admin
// HttpOnly: ?
```

### **4. Request Autenticado**
```typescript
// En React DevTools Console
await operatorsApi.getOperators();

// Debe retornar:
// { data: [...], totalCount: X, ... }
```

---

## ?? Configuración Recomendada Final

### **Backend: `appsettings.Development.json`**
```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=casino;Username=postgres;Password=postgres"
  },
  "Auth": {
    "Issuer": "casino",
    "JwtKey": "supersecretdevkey-please-change-32chars!!"
  }
}
```

### **Backend: `launchSettings.json`**
```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### **Frontend: `.env.development`**
```env
VITE_API_URL=http://localhost:5000/api/v1
```

### **Frontend: `vite.config.ts`**
```typescript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    host: 'localhost'
  }
})
```

---

## ?? Recursos Adicionales

### **Documentación de CORS**
- [MDN CORS](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS)
- [ASP.NET Core CORS](https://learn.microsoft.com/en-us/aspnet/core/security/cors)

### **Cookies con Credentials**
- [Axios withCredentials](https://axios-http.com/docs/req_config)
- [Fetch credentials](https://developer.mozilla.org/en-US/docs/Web/API/Fetch_API/Using_Fetch#sending_a_request_with_credentials_included)

---

## ? Resumen de Cambios Aplicados

### **Backend:**
1. ? `DynamicCorsMiddleware.cs` actualizado para permitir `/api/v1/admin/*` sin brand resolution
2. ? Origins de admin agregados a whitelist
3. ? Headers CORS correctos con `Access-Control-Allow-Credentials: true`

### **Frontend (Tu tarea):**
1. ?? Remover header `Host` del cliente Axios
2. ?? Verificar `withCredentials: true` en axios config
3. ?? Usar `VITE_API_URL=http://localhost:5000/api/v1`
4. ?? Asegurar mismo protocolo (HTTP/HTTP)

---

**¡Con estos cambios, el frontend debería poder consumir la API correctamente! ??**

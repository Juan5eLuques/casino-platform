# ?? SOLUCIÓN DEFINITIVA PARA ERROR DE CORS

## ? Problema Identificado

El error que tienes:
```
Access to XMLHttpRequest at 'https://admin.bet30.local:7182/api/v1/admin/auth/login' 
from origin 'http://admin.bet30.local:5173' has been blocked by CORS policy: 
Response to preflight request doesn't pass access control check: The value of the 
'Access-Control-Allow-Origin' header in the response must not be the wildcard '*' 
when the request's credentials mode is 'include'.
```

**Causa**: El middleware de CORS estaba enviando `Access-Control-Allow-Origin: *` cuando tu frontend usa `credentials: 'include'`. Esto no está permitido por las especificaciones de CORS.

## ? Solución Implementada

### 1. **Middleware Corregido** (`DynamicCorsMiddleware.cs`)
- ? **ANTES**: Usaba wildcard `*` en modo permisivo
- ? **AHORA**: Siempre usa el origin específico cuando credentials están habilitadas
- ? **AHORA**: Nunca combina `*` con `Access-Control-Allow-Credentials: true`

### 2. **Verificación de Base de Datos**
Tu campo `cors_origins` en la base de datos parece estar bien: `{http://bet30.local:5173,http://admin.bet30.local:5173}`

Pero vamos a asegurar que esté perfecto:

```sql
-- Ejecutar para verificar
SELECT code, cors_origins FROM brands WHERE code = 'bet30';

-- Si necesitas corregir, ejecutar:
UPDATE brands 
SET cors_origins = ARRAY[
    'http://admin.bet30.local:5173',
    'https://admin.bet30.local:5173',
    'http://bet30.local:5173',
    'https://bet30.local:5173',
    'http://localhost:5173',
    'http://127.0.0.1:5173'
]
WHERE code = 'bet30';
```

## ?? Pasos para Aplicar la Solución

### Paso 1: Reiniciar la API
```bash
# Detener la API si está corriendo
# Luego iniciar de nuevo:
dotnet watch --project apps/api/Casino.Api
```

### Paso 2: Verificar los Headers
Usa las herramientas de desarrollador del navegador (F12) y revisa:

**En Network tab, para el request OPTIONS:**
```
Access-Control-Allow-Origin: http://admin.bet30.local:5173  ? Debe ser tu origin específico
Access-Control-Allow-Credentials: true                     ? Debe estar presente
Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS, PATCH
```

**? NO debe aparecer:**
```
Access-Control-Allow-Origin: *  ? Esto causaba el error
```

### Paso 3: Probar desde el Frontend
Tu frontend debería poder hacer esta llamada sin error:

```javascript
fetch('https://admin.bet30.local:7182/api/v1/admin/auth/login', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  credentials: 'include',  // Esto requiere origin específico, no wildcard
  body: JSON.stringify({
    username: 'superadmin',
    password: 'admin123'
  })
})
```

## ?? Scripts de Diagnóstico

Si el problema persiste, usa estos scripts:

### 1. Verificar Base de Datos
```bash
psql [tu_connection_string] -f scripts/verify-cors-config.sql
```

### 2. Probar CORS con PowerShell
```bash
.\scripts\test-cors.ps1
```

## ?? Checklist de Verificación

- [ ] API reiniciada con los cambios del middleware
- [ ] Base de datos tiene el origin correcto en `cors_origins`
- [ ] Headers del navegador muestran origin específico (no `*`)
- [ ] Frontend puede hacer login sin error CORS
- [ ] Cookie `bk.token` se establece correctamente

## ?? Resultado Esperado

Después de aplicar estos cambios:

1. ? El request OPTIONS (preflight) devuelve `Access-Control-Allow-Origin: http://admin.bet30.local:5173`
2. ? El request POST de login funciona sin error CORS
3. ? La cookie `bk.token` se establece en el navegador
4. ? El frontend puede autenticarse correctamente

## ?? Si el Problema Persiste

1. **Revisa los logs de la API** - deberían mostrar:
   ```
   CORS Request: POST /api/v1/admin/auth/login from Origin: http://admin.bet30.local:5173
   CORS allowed for auth endpoint without brand resolution: /api/v1/admin/auth/login
   CORS headers set for origin: http://admin.bet30.local:5173
   ```

2. **Verifica que tu frontend esté usando el host correcto**:
   - URL de la API: `https://admin.bet30.local:7182`
   - URL del frontend: `http://admin.bet30.local:5173`

3. **Asegúrate de que el archivo hosts tenga**:
   ```
   127.0.0.1 admin.bet30.local
   127.0.0.1 bet30.local
   ```

La corrección del middleware debería resolver completamente el problema de CORS que estás experimentando.
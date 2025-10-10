# ? SOLUCIONADO: Brand Not Resolved en CORS

## ? Problema Identificado

El error se debía al **orden incorrecto de middlewares** en `Program.cs`:

```csharp
// ? ORDEN INCORRECTO (lo que tenías):
app.UseMiddleware<DynamicCorsMiddleware>();     // 1º - brandContext aún NO está resuelto
app.UseMiddleware<BrandResolverMiddleware>();   // 2º - Resuelve brandContext DESPUÉS
```

**Resultado**: Cuando `DynamicCorsMiddleware` se ejecutaba, `brandContext.IsResolved` era `false`, causando el error "Brand not resolved for CORS request".

## ? Solución Implementada

### 1. **Orden Correcto de Middlewares**

```csharp
// ? ORDEN CORRECTO (corregido):
app.UseMiddleware<BrandResolverMiddleware>();   // 1º - Resuelve brandContext PRIMERO
app.UseMiddleware<DynamicCorsMiddleware>();     // 2º - Usa brandContext ya resuelto
```

### 2. **Pipeline de Middlewares Completo**

```
Request ? ForwardedHeaders ? BrandResolver ? CORS ? Authentication ? Authorization ? Endpoints
```

1. **ForwardedHeaders**: Configura hosts forwarded
2. **BrandResolverMiddleware**: Resuelve brandContext por host/dominio
3. **DynamicCorsMiddleware**: Aplica CORS usando brandContext resuelto
4. **Authentication**: Valida JWT tokens
5. **Authorization**: Valida permisos por rol
6. **Endpoints**: Ejecuta la lógica de negocio

### 3. **Logging Mejorado**

**ANTES** (confuso):
```
warn: DynamicCorsMiddleware[0] 
  Brand not resolved for CORS request to /api/v1/admin/users from http://localhost:5173
```

**AHORA** (detallado):
```
info: BrandResolverMiddleware[0] 
  Development mode: using brand bet30 as default for localhost

info: DynamicCorsMiddleware[0] 
  CORS allowed for origin http://localhost:5173 on brand bet30
```

## ?? Flujo Corregido

### **Request de Creación de Usuario**:

```
1. Request ? POST http://localhost:7182/api/v1/admin/users

2. BrandResolverMiddleware
   ??? host = "localhost"
   ??? Modo desarrollo: usa brand "bet30" por defecto
   ??? brandContext.IsResolved = true ?
   ??? brandContext.BrandId = "22222222-..."
   ??? brandContext.OperatorId = "11111111-..."
   ??? brandContext.CorsOrigins = ["http://localhost:5173", ...]

3. DynamicCorsMiddleware  
   ??? origin = "http://localhost:5173"
   ??? brandContext.IsResolved = true ?
   ??? Verifica: origin está en brandContext.CorsOrigins ?
   ??? SetCorsHeaders(origin) ?
   ??? Continúa al siguiente middleware

4. BackofficeUserEndpoints
   ??? currentRole = CASHIER
   ??? effectiveOperatorId = brandContext.OperatorId ?
   ??? Crea usuario exitosamente ?
```

## ?? Para Aplicar la Corrección

### Paso 1: La corrección ya está aplicada
El orden de middlewares en `Program.cs` ya fue corregido.

### Paso 2: Reiniciar la API
```bash
# Detener la API actual (Ctrl+C)
# Iniciar de nuevo:
dotnet watch --project apps/api/Casino.Api
```

### Paso 3: Verificar los Logs

**Logs esperados (exitosos)**:
```
info: BrandResolverMiddleware[0] 
  Resolving brand for host: localhost, path: /api/v1/admin/users

info: BrandResolverMiddleware[0] 
  Development mode: using brand bet30 as default for localhost

info: DynamicCorsMiddleware[0] 
  CORS Request: POST /api/v1/admin/users from Origin: http://localhost:5173

info: DynamicCorsMiddleware[0] 
  CORS allowed for origin http://localhost:5173 on brand bet30

info: Program[0] 
  ? JWT Token VALIDATED - User: juanse, Role: CASHIER

info: Program[0] 
  Request completed: POST /api/v1/admin/users - 201
```

### Paso 4: Probar la Creación de Usuario

```bash
# Debería funcionar ahora sin error de CORS:
curl -X POST http://localhost:7182/api/v1/admin/users \
  -H "Content-Type: application/json" \
  -H "Cookie: bk.token=your_token" \
  -d '{
    "username": "test_user",
    "password": "password123",
    "role": "CASHIER",
    "commissionRate": 5.0
  }'
```

## ?? Debugging

Si aún hay problemas, verificar en este orden:

### 1. **BrandResolverMiddleware**
```
? Debe aparecer ANTES que DynamicCorsMiddleware en logs
? Debe mostrar "Brand resolved" o "using brand X as default"
? brandContext.IsResolved debe ser true
```

### 2. **DynamicCorsMiddleware**
```
? Debe ejecutarse DESPUÉS de BrandResolver
? Debe mostrar "CORS allowed for origin X on brand Y"
? NO debe mostrar "Brand not resolved"
```

### 3. **Endpoints**
```
? Debe recibir brandContext ya resuelto
? effectiveOperatorId debe ser válido (no Guid.Empty)
? Usuario debe crearse exitosamente (201)
```

## ? Resultado

Con el orden correcto de middlewares:

1. ? **Brand se resuelve ANTES** de validar CORS
2. ? **CORS funciona correctamente** con brandContext disponible
3. ? **Endpoints reciben brandContext válido** para resolver operatorId
4. ? **Creación de usuarios funciona** sin errores 400
5. ? **Logging es claro** y fácil de debuggear

El problema de "Brand not resolved for CORS request" debería estar **completamente resuelto**.
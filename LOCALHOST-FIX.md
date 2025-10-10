# ?? SOLUCIONADO: Error 400 con Localhost en Desarrollo

## ? Problema Identificado

Cuando usas `localhost` como host, el sistema:

1. **BrandResolverMiddleware** no encontraba un brand exacto para `localhost`
2. En modo desarrollo permitía el request pero **NO configuraba el brandContext**
3. Para roles **no-SUPER_ADMIN**, `brandContext.OperatorId` era `Guid.Empty`
4. **BackofficeUserService** fallaba porque requiere un `operatorId` válido

## ? Solución Implementada

### 1. **BrandResolverMiddleware Mejorado**

**ANTES**:
```csharp
// Permitía el request pero NO configuraba brandContext
if (_env.IsDevelopment() && host.Contains("localhost")) {
    await _next(context); // Sin configurar brandContext
    return;
}
```

**AHORA**:
```csharp
// Busca y configura un brand por defecto para localhost
if (_env.IsDevelopment() && host.Contains("localhost")) {
    brand = await dbContext.Brands.FirstOrDefaultAsync(b => b.Code == "bet30");
    
    if (brand != null) {
        // Configura brandContext con brand por defecto
        brandContext.BrandId = brand.Id;
        brandContext.OperatorId = brand.OperatorId; // ? Esto es lo que faltaba
        // ...
    }
}
```

### 2. **CORS Mejorado para Desarrollo**

Agregué origins adicionales para desarrollo:
```csharp
private bool IsOriginAllowedForDevelopment(string origin) {
    var devOrigins = new[] {
        "http://localhost:7182",
        "https://localhost:7182",
        "http://127.0.0.1:7182",
        // ...
    };
}
```

### 3. **Script de Configuración para Localhost**

Creé `scripts/configure-localhost-dev.sql` para configurar correctamente:
- Operador de desarrollo
- Brand para localhost con CORS
- Usuarios de prueba (dev_admin, dev_cashier)

## ?? Pasos para Aplicar la Solución

### Paso 1: Ejecutar el Script SQL

```bash
# Ejecutar el script de configuración de localhost
psql [tu_connection_string] -f scripts/configure-localhost-dev.sql
```

Esto creará:
- **Operador**: `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa` (Development Operator)
- **Brand**: `localhost-dev` con dominio `localhost`
- **Usuarios**: `dev_admin`, `dev_cashier` (password: admin123)

### Paso 2: Reiniciar la API

```bash
dotnet watch --project apps/api/Casino.Api
```

### Paso 3: Verificar que Funciona

**Request desde localhost debería funcionar**:
```bash
curl -X POST http://localhost:7182/api/v1/admin/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"dev_cashier","password":"admin123"}'
```

**Crear usuario debería funcionar**:
```bash
curl -X POST http://localhost:7182/api/v1/admin/users \
  -H "Content-Type: application/json" \
  -H "Cookie: bk.token=your_token" \
  -d '{
    "username": "test_cashier",
    "password": "password123",
    "role": "CASHIER",
    "commissionRate": 5.0
  }'
```

## ?? Cómo Funciona Ahora

### **Flujo para Localhost**:

```
1. Request ? http://localhost:7182/api/v1/admin/users

2. BrandResolverMiddleware
   ??? host = "localhost"
   ??? No encuentra brand exacto para localhost
   ??? Modo desarrollo: busca brand "bet30" como default
   ??? Configura brandContext:
   ?   ??? brandContext.BrandId = bet30.Id
   ?   ??? brandContext.OperatorId = bet30.OperatorId ?
   ?   ??? brandContext.BrandCode = "bet30"

3. BackofficeUserEndpoints
   ??? currentRole = CASHIER
   ??? effectiveOperatorId = brandContext.OperatorId ? (Ya no es Guid.Empty)
   ??? request.OperatorId = effectiveOperatorId

4. BackofficeUserService
   ??? Valida operatorId != null ?
   ??? Crea usuario exitosamente ?
```

## ?? Alternativas para Desarrollo

### **Opción 1: Usar Localhost (Recomendado)**
- ? Funciona con la solución implementada
- ? Usa brand por defecto automáticamente
- ? No requiere configurar hosts

### **Opción 2: Configurar Hosts File**
```
# Agregar al archivo hosts:
127.0.0.1 admin.bet30.local
127.0.0.1 bet30.local
```

Luego usar: `http://admin.bet30.local:7182`

### **Opción 3: Frontend Proxy**
Si tu frontend está en localhost:5173, configurar proxy:
```json
// vite.config.js
export default {
  server: {
    proxy: {
      '/api': 'http://localhost:7182'
    }
  }
}
```

## ?? Logs de Verificación

**Exitoso**:
```
info: BrandResolverMiddleware[0] 
  Development mode: using brand bet30 as default for localhost

info: BrandResolverMiddleware[0] 
  Brand resolved: bet30 (22222222-...) for host: localhost

info: Program[0] 
  Backoffice user created: guid - test_cashier - CASHIER
```

**Fallido (antes del fix)**:
```
warn: BrandResolverMiddleware[0] 
  Brand not resolved for host: localhost

info: BrandResolverMiddleware[0] 
  Development mode: allowing request to localhost without brand resolution
  
# ? brandContext.OperatorId = Guid.Empty
# ? Error 400 en CreateUserAsync
```

## ? Resultado

Ahora puedes:

1. ? **Usar localhost directamente** sin configurar hosts
2. ? **Crear usuarios desde cualquier rol** no-SUPER_ADMIN
3. ? **El sistema resuelve automáticamente** brand/operator para desarrollo
4. ? **CORS funciona correctamente** para localhost origins
5. ? **Logging detallado** para debugging

El error 400 debería estar completamente resuelto para localhost en desarrollo.
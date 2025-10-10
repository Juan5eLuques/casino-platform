# ?? SOLUCIONADO: Error 400 al Crear Cajero con Comisión

## ? Problema Identificado

Al intentar crear un cajero con comisión desde el rol `CASHIER`, se obtenía un error 400 porque:

1. **BrandResolverMiddleware** estaba saltando la resolución de brand para `/api/v1/admin/users`
2. **BackofficeUserEndpoints** estaba usando la lógica antigua de `GetCurrentOperatorId()` del token
3. Para roles **no-SUPER_ADMIN**, el `operatorId` debe resolverse automáticamente del `BrandContext`

## ? Solución Implementada

### 1. **Corregido BrandResolverMiddleware**

**ANTES**:
```csharp
// Saltaba resolución para /api/v1/admin/users
if (path.StartsWith("/api/v1/admin/users") || ...) {
    await _next(context);
    return;
}
```

**AHORA**:
```csharp
// NO salta resolución para /api/v1/admin/users
// Solo salta para auth, health, swagger, gateway
if (path.StartsWith("/api/v1/admin/auth") || 
    path.StartsWith("/health") || ...) {
    await _next(context);
    return;
}
```

### 2. **Actualizado BackofficeUserEndpoints**

**ANTES**:
```csharp
var currentOperatorId = GetCurrentOperatorId(httpContext); // Del token
```

**AHORA**:
```csharp
// Usar AuthorizationHelper para resolución transparente
var effectiveOperatorId = currentRole == BackofficeUserRole.SUPER_ADMIN 
    ? request.OperatorId // SUPER_ADMIN puede especificar cualquier operador
    : brandContext.OperatorId; // Otros roles usan el del contexto automáticamente
```

### 3. **Patrón Consistente con Players**

Ahora tanto `PlayerManagementEndpoints` como `BackofficeUserEndpoints` usan el mismo patrón:
- **SUPER_ADMIN**: Puede especificar `operatorId`/`brandId` explícitamente
- **OPERATOR_ADMIN/CASHIER**: Usan automáticamente el contexto del brand

## ?? Cómo Funciona Ahora

### Para Crear Cajero con Comisión

**Request del Frontend**:
```json
POST http://admin.bet30.local:7182/api/v1/admin/users
{
  "username": "cajero_subordinado",
  "password": "password123",
  "role": "CASHIER",
  "parentCashierId": "70bc0342-4301-45c0-91cc-9dbfc26d3f87",
  "commissionRate": 5.5
}
```

**Flujo Interno**:
```
1. BrandResolverMiddleware ? Resuelve brand "bet30" por dominio
   brandContext.OperatorId = "11111111-1111-1111-1111-111111111111"

2. BackofficeUserEndpoints ? AuthorizationHelper
   currentRole = CASHIER
   effectiveOperatorId = brandContext.OperatorId (automático)

3. BackofficeUserService ? Crea usuario con operatorId resuelto
   new BackofficeUser {
     OperatorId = "11111111-1111-1111-1111-111111111111", // Automático
     ParentCashierId = "70bc0342-4301-45c0-91cc-9dbfc26d3f87",
     CommissionRate = 5.5
   }
```

## ?? Validaciones de Autorización

### Para CASHIER que crea otro CASHIER:

1. ? **Rol permitido**: Solo puede crear otros `CASHIER`
2. ? **Parent obligatorio**: Debe especificar `parentCashierId = currentUserId`
3. ? **Operator scope**: Automáticamente usa su `operatorId` del brand context
4. ? **Comisión**: Puede especificar `commissionRate` entre 0-100

### Para OPERATOR_ADMIN:

1. ? **Roles permitidos**: Puede crear `OPERATOR_ADMIN`, `CASHIER`
2. ? **Operator scope**: Automáticamente usa su `operatorId` del brand context
3. ? **Sin parent**: No necesita especificar `parentCashierId` para crear `OPERATOR_ADMIN`

### Para SUPER_ADMIN:

1. ? **Sin restricciones**: Puede crear cualquier rol
2. ? **Operator explícito**: Debe especificar `operatorId` en el request
3. ? **Cross-operator**: Puede crear usuarios en cualquier operator

## ?? Body del Request Correcto

### Para CASHIER creando subordinado:
```json
{
  "username": "cajero_hijo",
  "password": "password123",
  "role": "CASHIER",
  "parentCashierId": "id-del-cashier-padre",
  "commissionRate": 5.5
}
```

### Para OPERATOR_ADMIN creando cajero:
```json
{
  "username": "nuevo_cajero",
  "password": "password123", 
  "role": "CASHIER",
  "commissionRate": 10.0
}
```

### Para SUPER_ADMIN:
```json
{
  "username": "admin_externo",
  "password": "password123",
  "role": "OPERATOR_ADMIN",
  "operatorId": "22222222-2222-2222-2222-222222222222"
}
```

## ?? Debugging Mejorado

Los logs ahora muestran:
```
info: BrandResolverMiddleware[0] 
  Resolving brand for host: admin.bet30.local, path: /api/v1/admin/users

info: BrandResolverMiddleware[0] 
  Brand resolved: bet30 (22222222-...) for host: admin.bet30.local

info: Program[0] 
  Backoffice user created: guid - cajero_hijo - CASHIER by 
  { UserId: 70bc0342-..., Role: CASHIER, BrandId: 22222222-..., OperatorId: 11111111-... }
```

## ? Error 400 Resuelto

El error 400 se debía a que:
1. No se resolvía el brand context
2. `brandContext.OperatorId` era `Guid.Empty`
3. Las validaciones fallaban por falta de `operatorId`

**Ahora**:
1. ? Brand context se resuelve correctamente
2. ? `operatorId` se obtiene automáticamente del `brandContext.OperatorId`
3. ? Validaciones pasan y el usuario se crea exitosamente

## ?? Resultado

Ahora puedes crear cajeros con comisión sin especificar `operatorId` en el request. El sistema:

1. **Resuelve automáticamente** el `operatorId` del brand context
2. **Valida permisos** según el rol del usuario actual
3. **Crea la jerarquía** de cajeros con comisiones correctamente
4. **Mantiene la seguridad** por operator/brand automáticamente

¡El sistema de creación de usuarios ahora es **completamente transparente** para el `operatorId` igual que los players!
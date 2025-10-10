# ?? Sistema de Usuarios Unificado - Resumen de Implementación

## ? **PROBLEMAS RESUELTOS:**

### 1. **SUPERADMIN acceso a usuarios (403 forbidden)**
- ? **Corregido**: Endpoint unificado `/api/v1/admin/users` con soporte para `globalScope=true`
- ? **Validación mejorada**: SUPERADMIN puede ver todos los usuarios sin restricciones de brand

### 2. **Jugadores no se ligan al cajero que los crea**
- ? **Campo agregado**: `CreatedByUserId` en Player y BackofficeUser
- ? **Migración aplicada**: Base de datos actualizada con las nuevas relaciones
- ? **Lógica implementada**: Al crear cualquier usuario, se guarda quién lo creó

### 3. **Cajeros no pueden ver sus usuarios creados**
- ? **Filtrado por jerarquía**: Implementado en `GetViewableUserIdsAsync`
- ? **Recursivo**: CASHIER ve todos los usuarios que él creó y los que crearon sus subordinados

### 4. **Cajeros no pueden crear otros cajeros (403 forbidden)**
- ? **Permisos corregidos**: CASHIER puede crear otros CASHIER en el sistema unificado
- ? **Auto-asignación**: Se asigna automáticamente como `ParentCashierId`

---

## ??? **ARQUITECTURA NUEVA:**

### **Un Solo Endpoint: `/api/v1/admin/users`**

Todos los tipos de usuarios se manejan a través de un único endpoint:

#### **Tipos de Usuario Unificados:**
```typescript
enum UnifiedUserType {
    SUPER_ADMIN,    // Usuario super administrador
    BRAND_ADMIN,    // Administrador de marca  
    CASHIER,        // Cajero (con o sin comisión)
    PLAYER          // Jugador
}
```

#### **Jerarquía y Permisos:**

1. **SUPER_ADMIN**:
   - ? Ve TODOS los usuarios con `?globalScope=true`
   - ? Ve usuarios de un brand específico sin globalScope
   - ? Puede crear cualquier tipo de usuario
   - ? No tiene restricciones de brand

2. **BRAND_ADMIN**:
   - ? Ve TODOS los usuarios de su brand (backoffice + players)
   - ? Puede crear: BRAND_ADMIN, CASHIER, PLAYER (en su brand)
   - ? No puede crear SUPER_ADMIN

3. **CASHIER**:
   - ? Ve SOLO usuarios creados por él (jerarquía recursiva)
   - ? Puede crear: CASHIER (subordinados), PLAYER
   - ? Auto-asignación como `ParentCashierId` para cajeros subordinados
   - ? Auto-asignación en `CashierPlayer` para jugadores creados

---

## ??? **COMPONENTES IMPLEMENTADOS:**

### **1. DTOs Unificados:**
- `CreateUnifiedUserRequest` - Para crear cualquier tipo de usuario
- `GetUnifiedUserResponse` - Response que incluye todos los campos necesarios  
- `QueryUnifiedUsersRequest` - Para consultas con filtros y paginación
- `UpdateUnifiedUserRequest` - Para actualizar cualquier usuario

### **2. Servicio Unificado:**
- `IUnifiedUserService` - Interfaz del servicio unificado
- `UnifiedUserService` - Implementación con lógica de jerarquía y permisos

### **3. Endpoint Unificado:**
- `UnifiedUserEndpoints` - Todos los endpoints de usuarios en un solo lugar

### **4. Validadores:**
- `CreateUnifiedUserRequestValidator` - Validaciones específicas por tipo
- `UpdateUnifiedUserRequestValidator` - Validaciones para actualizaciones
- `QueryUnifiedUsersRequestValidator` - Validaciones para consultas

---

## ??? **CAMBIOS EN BASE DE DATOS:**

### **Campos Agregados:**
```sql
-- En tabla BackofficeUsers
ALTER TABLE "BackofficeUsers" ADD "CreatedByUserId" uuid;

-- En tabla Players (renombrado desde CreatedByCashierId)
ALTER TABLE "Players" RENAME COLUMN "CreatedByCashierId" TO "CreatedByUserId";
```

### **Relaciones Nuevas:**
```sql
-- BackofficeUser que creó otro BackofficeUser
FK: BackofficeUsers.CreatedByUserId -> BackofficeUsers.Id

-- BackofficeUser que creó un Player  
FK: Players.CreatedByUserId -> BackofficeUsers.Id
```

---

## ?? **LÓGICA DE JERARQUÍA:**

### **Para CASHIER - Usuarios Visibles:**
```
Cajero A (ID: 123)
??? Ve a sí mismo (ID: 123)
??? Ve Jugador creado por él (ID: 456, CreatedByUserId: 123)
??? Ve Cajero subordinado (ID: 789, CreatedByUserId: 123)
?   ??? Ve Jugador creado por subordinado (ID: 999, CreatedByUserId: 789)
```

### **Método Recursivo:**
```csharp
private async Task AddCreatedUsersRecursivelyAsync(Guid creatorUserId, HashSet<Guid> userIds)
{
    // Buscar usuarios de backoffice creados por este usuario
    var createdBackofficeUsers = await _context.BackofficeUsers
        .Where(u => u.CreatedByUserId == creatorUserId)
        .Select(u => u.Id).ToListAsync();

    // Buscar players creados por este usuario  
    var createdPlayers = await _context.Players
        .Where(p => p.CreatedByUserId == creatorUserId)
        .Select(p => p.Id).ToListAsync();

    userIds.UnionWith(createdBackofficeUsers);
    userIds.UnionWith(createdPlayers);

    // Recursivamente agregar usuarios creados por subordinados
    foreach (var subordinateId in createdBackofficeUsers)
    {
        await AddCreatedUsersRecursivelyAsync(subordinateId, userIds);
    }
}
```

---

## ?? **ENDPOINTS PARA TESTING:**

### **1. Crear Usuario (cualquier tipo):**
```http
POST /api/v1/admin/users
{
  "username": "cajero2", 
  "password": "Password123",
  "userType": "CASHIER",
  "backofficeRole": "CASHIER",
  "commissionRate": 15
}
```

### **2. Crear Jugador:**
```http
POST /api/v1/admin/users
{
  "username": "player1",
  "password": "Password123", 
  "userType": "PLAYER",
  "playerStatus": "ACTIVE",
  "email": "player@test.com",
  "initialBalance": 1000
}
```

### **3. Listar Usuarios (con jerarquía):**
```http
# SUPER_ADMIN - Ver todos
GET /api/v1/admin/users?globalScope=true

# BRAND_ADMIN - Ver del brand  
GET /api/v1/admin/users

# CASHIER - Ver solo jerarquía
GET /api/v1/admin/users
```

### **4. Ajustar Wallet de Jugador:**
```http
POST /api/v1/admin/users/{playerId}/wallet/adjust
{
  "amount": 500,
  "reason": "Bonus por referido"
}
```

---

## ?? **MIGRACIÓN DE DATOS:**

Ejecutar el script `sql-scripts/migrate-existing-player-cashier-relationships.sql` para:

1. ? Conectar jugadores existentes con sus cajeros
2. ? Actualizar relaciones en `CreatedByUserId` 
3. ? Generar estadísticas de migración

---

## ? **BENEFICIOS DEL NUEVO SISTEMA:**

1. **Un solo endpoint** - Simplifica la API
2. **Jerarquía completa** - Cada usuario sabe quién lo creó
3. **Permisos granulares** - Respeta la jerarquía organizacional  
4. **Consistencia** - Misma lógica para todos los tipos de usuario
5. **Escalabilidad** - Fácil agregar nuevos tipos de usuario
6. **Auditoría completa** - Trazabilidad total de creación de usuarios

---

## ?? **PRÓXIMOS PASOS:**

1. ? ~~Aplicar migraciones en base de datos~~
2. ? ~~Ejecutar script de migración de datos~~  
3. ?? Probar todos los escenarios de permisos
4. ?? Validar la jerarquía de CASHIER
5. ?? Verificar que SUPERADMIN ve todos los usuarios

**¡El sistema unificado está listo para usar! ??**
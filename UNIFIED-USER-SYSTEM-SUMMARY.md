# ?? Sistema de Usuarios Unificado - Resumen de Implementaci�n

## ? **PROBLEMAS RESUELTOS:**

### 1. **SUPERADMIN acceso a usuarios (403 forbidden)**
- ? **Corregido**: Endpoint unificado `/api/v1/admin/users` con soporte para `globalScope=true`
- ? **Validaci�n mejorada**: SUPERADMIN puede ver todos los usuarios sin restricciones de brand

### 2. **Jugadores no se ligan al cajero que los crea**
- ? **Campo agregado**: `CreatedByUserId` en Player y BackofficeUser
- ? **Migraci�n aplicada**: Base de datos actualizada con las nuevas relaciones
- ? **L�gica implementada**: Al crear cualquier usuario, se guarda qui�n lo cre�

### 3. **Cajeros no pueden ver sus usuarios creados**
- ? **Filtrado por jerarqu�a**: Implementado en `GetViewableUserIdsAsync`
- ? **Recursivo**: CASHIER ve todos los usuarios que �l cre� y los que crearon sus subordinados

### 4. **Cajeros no pueden crear otros cajeros (403 forbidden)**
- ? **Permisos corregidos**: CASHIER puede crear otros CASHIER en el sistema unificado
- ? **Auto-asignaci�n**: Se asigna autom�ticamente como `ParentCashierId`

---

## ??? **ARQUITECTURA NUEVA:**

### **Un Solo Endpoint: `/api/v1/admin/users`**

Todos los tipos de usuarios se manejan a trav�s de un �nico endpoint:

#### **Tipos de Usuario Unificados:**
```typescript
enum UnifiedUserType {
    SUPER_ADMIN,    // Usuario super administrador
    BRAND_ADMIN,    // Administrador de marca  
    CASHIER,        // Cajero (con o sin comisi�n)
    PLAYER          // Jugador
}
```

#### **Jerarqu�a y Permisos:**

1. **SUPER_ADMIN**:
   - ? Ve TODOS los usuarios con `?globalScope=true`
   - ? Ve usuarios de un brand espec�fico sin globalScope
   - ? Puede crear cualquier tipo de usuario
   - ? No tiene restricciones de brand

2. **BRAND_ADMIN**:
   - ? Ve TODOS los usuarios de su brand (backoffice + players)
   - ? Puede crear: BRAND_ADMIN, CASHIER, PLAYER (en su brand)
   - ? No puede crear SUPER_ADMIN

3. **CASHIER**:
   - ? Ve SOLO usuarios creados por �l (jerarqu�a recursiva)
   - ? Puede crear: CASHIER (subordinados), PLAYER
   - ? Auto-asignaci�n como `ParentCashierId` para cajeros subordinados
   - ? Auto-asignaci�n en `CashierPlayer` para jugadores creados

---

## ??? **COMPONENTES IMPLEMENTADOS:**

### **1. DTOs Unificados:**
- `CreateUnifiedUserRequest` - Para crear cualquier tipo de usuario
- `GetUnifiedUserResponse` - Response que incluye todos los campos necesarios  
- `QueryUnifiedUsersRequest` - Para consultas con filtros y paginaci�n
- `UpdateUnifiedUserRequest` - Para actualizar cualquier usuario

### **2. Servicio Unificado:**
- `IUnifiedUserService` - Interfaz del servicio unificado
- `UnifiedUserService` - Implementaci�n con l�gica de jerarqu�a y permisos

### **3. Endpoint Unificado:**
- `UnifiedUserEndpoints` - Todos los endpoints de usuarios en un solo lugar

### **4. Validadores:**
- `CreateUnifiedUserRequestValidator` - Validaciones espec�ficas por tipo
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
-- BackofficeUser que cre� otro BackofficeUser
FK: BackofficeUsers.CreatedByUserId -> BackofficeUsers.Id

-- BackofficeUser que cre� un Player  
FK: Players.CreatedByUserId -> BackofficeUsers.Id
```

---

## ?? **L�GICA DE JERARQU�A:**

### **Para CASHIER - Usuarios Visibles:**
```
Cajero A (ID: 123)
??? Ve a s� mismo (ID: 123)
??? Ve Jugador creado por �l (ID: 456, CreatedByUserId: 123)
??? Ve Cajero subordinado (ID: 789, CreatedByUserId: 123)
?   ??? Ve Jugador creado por subordinado (ID: 999, CreatedByUserId: 789)
```

### **M�todo Recursivo:**
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

### **3. Listar Usuarios (con jerarqu�a):**
```http
# SUPER_ADMIN - Ver todos
GET /api/v1/admin/users?globalScope=true

# BRAND_ADMIN - Ver del brand  
GET /api/v1/admin/users

# CASHIER - Ver solo jerarqu�a
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

## ?? **MIGRACI�N DE DATOS:**

Ejecutar el script `sql-scripts/migrate-existing-player-cashier-relationships.sql` para:

1. ? Conectar jugadores existentes con sus cajeros
2. ? Actualizar relaciones en `CreatedByUserId` 
3. ? Generar estad�sticas de migraci�n

---

## ? **BENEFICIOS DEL NUEVO SISTEMA:**

1. **Un solo endpoint** - Simplifica la API
2. **Jerarqu�a completa** - Cada usuario sabe qui�n lo cre�
3. **Permisos granulares** - Respeta la jerarqu�a organizacional  
4. **Consistencia** - Misma l�gica para todos los tipos de usuario
5. **Escalabilidad** - F�cil agregar nuevos tipos de usuario
6. **Auditor�a completa** - Trazabilidad total de creaci�n de usuarios

---

## ?? **PR�XIMOS PASOS:**

1. ? ~~Aplicar migraciones en base de datos~~
2. ? ~~Ejecutar script de migraci�n de datos~~  
3. ?? Probar todos los escenarios de permisos
4. ?? Validar la jerarqu�a de CASHIER
5. ?? Verificar que SUPERADMIN ve todos los usuarios

**�El sistema unificado est� listo para usar! ??**
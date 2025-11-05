# ?? Fix: BRAND_ADMIN ya no puede ver SUPER_ADMIN en la lista de usuarios

## ?? **Problema Corregido**

Anteriormente, cuando un `BRAND_ADMIN` listaba usuarios en `/api/v1/admin/users`, podía ver a los `SUPER_ADMIN` en la lista. Esto era un problema de seguridad y jerarquía.

---

## ? **Solución Implementada**

Se modificó `UnifiedUserService.cs` para **filtrar explícitamente a los SUPER_ADMIN** cuando un `BRAND_ADMIN` consulta usuarios.

### **Cambios en 3 métodos**:

#### **1. `GetBackofficeUsersAsync` (Lista de usuarios)**
```csharp
// ? ANTES (incorrecto)
else if (currentRole == BackofficeUserRole.BRAND_ADMIN)
{
    if (brandScope.HasValue)
    {
        query = query.Where(u => u.BrandId == brandScope.Value || u.BrandId == null); // ? Incluía SUPER_ADMINs
    }
}

// ? DESPUÉS (correcto)
else if (currentRole == BackofficeUserRole.BRAND_ADMIN)
{
    if (brandScope.HasValue)
    {
        query = query.Where(u => u.BrandId == brandScope.Value && u.Role != BackofficeUserRole.SUPER_ADMIN); // ? Excluye SUPER_ADMINs
  }
}
```

#### **2. `GetBackofficeUserByIdAsync` (Obtener usuario por ID)**
```csharp
// ? NUEVO: Filtro agregado
else if (currentRole == BackofficeUserRole.BRAND_ADMIN && brandScope.HasValue)
{
    query = query.Where(u => u.BrandId == brandScope.Value && u.Role != BackofficeUserRole.SUPER_ADMIN);
}
```

#### **3. `FindBackofficeUserByUsernameAsync` (Buscar por username)**
```csharp
// ? NUEVO: Filtro agregado
else if (currentRole == BackofficeUserRole.BRAND_ADMIN && brandScope.HasValue)
{
    query = query.Where(u => u.BrandId == brandScope.Value && u.Role != BackofficeUserRole.SUPER_ADMIN);
}
```

---

## ?? **Matriz de Visibilidad Actualizada**

| Rol | Puede Ver | NO Puede Ver |
|-----|-----------|--------------|
| **SUPER_ADMIN** | Todos los usuarios (incluido otros SUPER_ADMINs) | - |
| **BRAND_ADMIN** | BRAND_ADMINs, CASHIERs y PLAYERs de su brand | ? **SUPER_ADMINs** |
| **CASHIER** | Solo usuarios creados por él + él mismo | ? SUPER_ADMINs, ? Otros BRAND_ADMINs, ? Otros CASHIERs |

---

## ?? **Comportamiento por Endpoint**

### **GET /api/v1/admin/users**
```http
GET /api/v1/admin/users
Authorization: Bearer <BRAND_ADMIN_TOKEN>
```

**ANTES** (?):
```json
{
  "data": [
    { "id": "...", "username": "superadmin", "role": "SUPER_ADMIN" }, // ? VISIBLE
    { "id": "...", "username": "brand_admin_1", "role": "BRAND_ADMIN" },
    { "id": "...", "username": "cashier_1", "role": "CASHIER" }
  ]
}
```

**DESPUÉS** (?):
```json
{
  "data": [
    { "id": "...", "username": "brand_admin_1", "role": "BRAND_ADMIN" },
    { "id": "...", "username": "cashier_1", "role": "CASHIER" }
  ]
  // ? SUPER_ADMIN no aparece en la lista
}
```

---

### **GET /api/v1/admin/users/{userId}**
```http
GET /api/v1/admin/users/11111111-1111-1111-1111-111111111111
Authorization: Bearer <BRAND_ADMIN_TOKEN>
```

**ANTES** (?):
```json
{
  "id": "11111111-1111-1111-1111-111111111111",
  "username": "superadmin",
  "role": "SUPER_ADMIN"
}
```

**DESPUÉS** (?):
```json
{
  "error": "user_not_found",
  "userId": "11111111-1111-1111-1111-111111111111"
}
// ? 404 Not Found - BRAND_ADMIN no puede acceder por ID
```

---

### **GET /api/v1/admin/users/search?username=superadmin**
```http
GET /api/v1/admin/users/search?username=superadmin
Authorization: Bearer <BRAND_ADMIN_TOKEN>
```

**ANTES** (?):
```json
{
  "id": "11111111-1111-1111-1111-111111111111",
  "username": "superadmin",
  "role": "SUPER_ADMIN"
}
```

**DESPUÉS** (?):
```json
{
  "error": "user_not_found",
  "username": "superadmin"
}
// ? 404 Not Found - BRAND_ADMIN no puede buscar por username
```

---

## ?? **Testing**

### **1. Como SUPER_ADMIN (debe ver todos)**
```bash
curl -X GET "http://localhost:5000/api/v1/admin/users" \
  -H "Authorization: Bearer <SUPER_ADMIN_TOKEN>"
  
# ? Debe incluir todos los roles: SUPER_ADMIN, BRAND_ADMIN, CASHIER, PLAYER
```

### **2. Como BRAND_ADMIN (NO debe ver SUPER_ADMINs)**
```bash
curl -X GET "http://localhost:5000/api/v1/admin/users" \
  -H "Authorization: Bearer <BRAND_ADMIN_TOKEN>"
  
# ? Solo debe incluir: BRAND_ADMIN, CASHIER, PLAYER (de su brand)
# ? NO debe incluir: SUPER_ADMIN
```

### **3. Intentar acceder a un SUPER_ADMIN por ID (debe fallar)**
```bash
curl -X GET "http://localhost:5000/api/v1/admin/users/{superadmin-id}" \
  -H "Authorization: Bearer <BRAND_ADMIN_TOKEN>"
  
# ? Debe retornar 404 Not Found
```

---

## ?? **Notas Adicionales**

1. **Consistencia**: El filtro se aplica en los 3 métodos principales de consulta
2. **Seguridad**: Un BRAND_ADMIN nunca puede ver, editar o eliminar un SUPER_ADMIN
3. **SUPER_ADMIN sin scope**: Puede ver todos los usuarios de su brand (sin otros SUPER_ADMINs)
4. **SUPER_ADMIN con globalScope**: Puede ver absolutamente todos los usuarios

---

## ? **Estado**

- ? Código actualizado
- ? Compilación exitosa
- ? **Reiniciar backend para aplicar cambios**

---

**Archivo modificado**: `apps/Casino.Application/Services/Implementations/UnifiedUserService.cs`  
**Fecha**: 2025-01-24  
**Versión**: 1.1

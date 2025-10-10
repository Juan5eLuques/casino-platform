# Resumen de Cambios - Sistema de Jerarquía de Cashiers

## ? Funcionalidad Implementada

Se ha implementado un **sistema completo de jerarquía de cashiers** con estructura de árbol N-ario, donde:

- Los cashiers pueden crear cashiers subordinados
- Los cashiers pueden crear jugadores (players)
- Cada cashier subordinado puede tener una comisión configurable (0-100%)
- La jerarquía es de múltiples niveles (sin límite de profundidad)
- Sistema de permisos y validaciones completo

## ?? Archivos Modificados

### 1. **apps/Casino.Domain/Entities/BackofficeUser.cs**
- ? Agregado campo `ParentCashierId` (Guid? nullable)
- ? Agregado campo `CommissionRate` (decimal, 0-100)
- ? Agregada navegación `ParentCashier` (BackofficeUser?)
- ? Agregada colección `SubordinateCashiers` (ICollection<BackofficeUser>)

### 2. **apps/Casino.Infrastructure/Data/CasinoDbContext.cs**
- ? Configurada relación auto-referencial (self-join) para jerarquía
- ? Agregada configuración de `CommissionRate` con precisión decimal(5,2)
- ? Configurado `DeleteBehavior.Restrict` para evitar eliminación en cascada

### 3. **apps/Casino.Application/DTOs/Admin/BackofficeUserDTOs.cs**
- ? **CreateBackofficeUserRequest**: Agregados `ParentCashierId` y `CommissionRate`
- ? **UpdateBackofficeUserRequest**: Agregado `CommissionRate` opcional
- ? **QueryBackofficeUsersRequest**: Agregados `ParentCashierId` e `IncludeSubordinates`
- ? **GetBackofficeUserResponse**: Agregados campos de jerarquía y `SubordinatesCount`
- ? **GetBackofficeUserHierarchyResponse**: NUEVO DTO para árbol jerárquico recursivo

### 4. **apps/Casino.Application/Services/IBackofficeUserService.cs**
- ? Agregado método `GetUserHierarchyAsync()` para obtener árbol completo

### 5. **apps/Casino.Application/Services/Implementations/BackofficeUserService.cs**
- ? **CreateUserAsync**: Validaciones de jerarquía y parent cashier
- ? **GetUsersAsync**: Soporte para filtrar por `ParentCashierId`
- ? **GetUserAsync**: Incluye conteo de subordinados
- ? **GetUserHierarchyAsync**: NUEVO - Construye árbol recursivo de subordinados
- ? **BuildUserHierarchyAsync**: NUEVO método privado recursivo
- ? **UpdateUserAsync**: Validaciones al cambiar rol con subordinados
- ? **DeleteUserAsync**: Validación para no eliminar cashier con subordinados

### 6. **apps/api/Casino.Api/Endpoints/BackofficeUserEndpoints.cs**
- ? Actualizado endpoint POST `/users` con validaciones de jerarquía
- ? Actualizado endpoint GET `/users` con soporte para CASHIER (solo ve subordinados)
- ? NUEVO endpoint GET `/users/{userId}/hierarchy` para obtener árbol
- ? Validaciones de autorización por rol:
  - SUPER_ADMIN: acceso completo
  - OPERATOR_ADMIN: crea cashiers raíz
  - CASHIER: crea subordinados bajo sí mismo

### 7. **apps/Casino.Application/Validators/Admin/BackofficeUserValidators.cs**
- ? Validación de `CommissionRate` (0-100)
- ? Validación de que solo CASHIER puede tener `ParentCashierId` y `CommissionRate`

### 8. **apps/api/Casino.Api/Program.cs**
- ? Configuración de JSON para manejar enums como strings
- ? `JsonStringEnumConverter` agregado
- ? `PropertyNameCaseInsensitive = true`

## ?? Nuevos Endpoints

### `POST /api/v1/admin/users`
Crear usuario con soporte de jerarquía
```json
{
  "username": "cashier_sub",
  "password": "pass123",
  "role": "CASHIER",
  "operatorId": "uuid",
  "parentCashierId": "parent-uuid",  // NUEVO
  "commissionRate": 15.5              // NUEVO
}
```

### `GET /api/v1/admin/users?parentCashierId={uuid}`
Filtrar por cashier padre (subordinados directos)

### `GET /api/v1/admin/users/{userId}/hierarchy`
Obtener árbol jerárquico completo (recursivo) - **NUEVO ENDPOINT**

## ?? Reglas de Autorización

### SUPER_ADMIN
- ? Puede crear cualquier tipo de usuario
- ? Ve todos los usuarios del sistema
- ? Puede ver cualquier jerarquía

### OPERATOR_ADMIN
- ? Puede crear OPERATOR_ADMIN y CASHIER
- ? Puede crear cashiers raíz (sin parent)
- ? Solo ve usuarios de su operador
- ? Puede ver jerarquías de su operador
- ? No puede crear SUPER_ADMIN

### CASHIER
- ? Puede crear CASHIER subordinados bajo sí mismo
- ? Debe especificar su propio ID como `parentCashierId`
- ? Solo ve sus propios subordinados directos
- ? Solo puede ver su propia jerarquía
- ? No puede actualizar usuarios
- ? No puede eliminar usuarios
- ? No puede crear OPERATOR_ADMIN ni SUPER_ADMIN

## ? Validaciones Implementadas

### Al Crear Usuario
1. ? Username único
2. ? SUPER_ADMIN no puede tener operador
3. ? Otros roles deben tener operador
4. ? Parent cashier debe existir y ser CASHIER
5. ? Parent cashier debe estar ACTIVE
6. ? Parent cashier debe pertenecer al mismo operador
7. ? CommissionRate entre 0-100
8. ? Solo CASHIER puede tener parent y comisión

### Al Actualizar Usuario
1. ? Username único (si se cambia)
2. ? No cambiar a SUPER_ADMIN con operador asignado
3. ? No cambiar de CASHIER si tiene subordinados
4. ? CommissionRate válido (0-100)
5. ? Limpia parent y comisión al cambiar de CASHIER a otro rol

### Al Eliminar Usuario
1. ? No eliminar propio usuario
2. ? No eliminar SUPER_ADMIN si eres OPERATOR_ADMIN
3. ? No eliminar CASHIER con subordinados

## ??? Estructura de Base de Datos

### Columnas Agregadas a `BackofficeUsers`
```sql
ALTER TABLE "BackofficeUsers" 
ADD COLUMN "ParentCashierId" UUID NULL,
ADD COLUMN "CommissionRate" DECIMAL(5,2) NOT NULL DEFAULT 0;

ALTER TABLE "BackofficeUsers"
ADD CONSTRAINT "FK_BackofficeUsers_BackofficeUsers_ParentCashierId"
FOREIGN KEY ("ParentCashierId") 
REFERENCES "BackofficeUsers"("Id")
ON DELETE RESTRICT;
```

## ?? Componentes Frontend Sugeridos

Ver archivo `CASHIER-HIERARCHY-FRONTEND-GUIDE.md` para:
- ? Componente de creación de subordinados
- ? Visualización de árbol jerárquico
- ? Lista de subordinados directos
- ? Ejemplos de código React/TypeScript
- ? Manejo de errores
- ? Casos de uso

## ?? Cómo Usar

### 1. Como OPERATOR_ADMIN - Crear cashier raíz
```bash
POST /api/v1/admin/users
{
  "username": "root_cashier",
  "password": "secure123",
  "role": "CASHIER",
  "operatorId": "operator-id",
  "parentCashierId": null,
  "commissionRate": 0
}
```

### 2. Como CASHIER - Crear subordinado
```bash
POST /api/v1/admin/users
{
  "username": "sub_cashier",
  "password": "secure123",
  "role": "CASHIER",
  "operatorId": "operator-id",
  "parentCashierId": "my-cashier-id",  # Mi propio ID
  "commissionRate": 15.5
}
```

### 3. Ver mi jerarquía completa
```bash
GET /api/v1/admin/users/my-cashier-id/hierarchy
```

### 4. Ver mis subordinados directos
```bash
GET /api/v1/admin/users?parentCashierId=my-cashier-id
```

## ?? Ejemplo de Estructura de Árbol

```
OPERATOR_ADMIN
    |
    ??? ROOT_CASHIER_1 (comisión: 0%)
    |    |
    |    ??? SUB_CASHIER_1_1 (comisión: 10%)
    |    |    |
    |    |    ??? SUB_SUB_CASHIER_1_1_1 (comisión: 5%)
    |    |
    |    ??? SUB_CASHIER_1_2 (comisión: 15%)
    |
    ??? ROOT_CASHIER_2 (comisión: 0%)
         |
         ??? SUB_CASHIER_2_1 (comisión: 20%)
              |
              ??? SUB_CASHIER_2_1_1 (comisión: 8%)
```

## ?? Próximos Pasos

Para usar esta funcionalidad:

1. **Migrar la base de datos**:
   ```bash
   cd apps/api/Casino.Api
   dotnet ef migrations add AddCashierHierarchy
   dotnet ef database update
   ```

2. **Probar endpoints** con Swagger UI o Postman

3. **Implementar frontend** usando la guía en `CASHIER-HIERARCHY-FRONTEND-GUIDE.md`

4. **(Opcional) Crear Players**: Los cashiers también pueden crear players asignados a ellos mediante los endpoints existentes en `/api/v1/admin/players`

## ?? Notas Adicionales

- La jerarquía es **recursiva** sin límite de profundidad
- Los cashiers de nivel N pueden crear cashiers de nivel N+1
- La comisión es un porcentaje (0-100) que representa lo que el padre recibe del subordinado
- El sistema de auditoría registra todas las operaciones de creación/modificación/eliminación
- Los enums se serializan como strings en UPPER_CASE (ej: "CASHIER", "ACTIVE")

## ? Testing

Se recomienda probar:
1. ? Crear cashier raíz como OPERATOR_ADMIN
2. ? Crear subordinados como CASHIER
3. ? Intentar crear subordinado con parent de otro operador (debe fallar)
4. ? Intentar eliminar cashier con subordinados (debe fallar)
5. ? Obtener jerarquía completa
6. ? Filtrar por parentCashierId
7. ? CASHIER intentando crear OPERATOR_ADMIN (debe fallar)
8. ? CASHIER intentando ver usuarios fuera de su jerarquía (debe fallar)

---

**Estado: ? Completamente Implementado y Compilado**

# Resumen de Cambios - Sistema de Jerarqu�a de Cashiers

## ? Funcionalidad Implementada

Se ha implementado un **sistema completo de jerarqu�a de cashiers** con estructura de �rbol N-ario, donde:

- Los cashiers pueden crear cashiers subordinados
- Los cashiers pueden crear jugadores (players)
- Cada cashier subordinado puede tener una comisi�n configurable (0-100%)
- La jerarqu�a es de m�ltiples niveles (sin l�mite de profundidad)
- Sistema de permisos y validaciones completo

## ?? Archivos Modificados

### 1. **apps/Casino.Domain/Entities/BackofficeUser.cs**
- ? Agregado campo `ParentCashierId` (Guid? nullable)
- ? Agregado campo `CommissionRate` (decimal, 0-100)
- ? Agregada navegaci�n `ParentCashier` (BackofficeUser?)
- ? Agregada colecci�n `SubordinateCashiers` (ICollection<BackofficeUser>)

### 2. **apps/Casino.Infrastructure/Data/CasinoDbContext.cs**
- ? Configurada relaci�n auto-referencial (self-join) para jerarqu�a
- ? Agregada configuraci�n de `CommissionRate` con precisi�n decimal(5,2)
- ? Configurado `DeleteBehavior.Restrict` para evitar eliminaci�n en cascada

### 3. **apps/Casino.Application/DTOs/Admin/BackofficeUserDTOs.cs**
- ? **CreateBackofficeUserRequest**: Agregados `ParentCashierId` y `CommissionRate`
- ? **UpdateBackofficeUserRequest**: Agregado `CommissionRate` opcional
- ? **QueryBackofficeUsersRequest**: Agregados `ParentCashierId` e `IncludeSubordinates`
- ? **GetBackofficeUserResponse**: Agregados campos de jerarqu�a y `SubordinatesCount`
- ? **GetBackofficeUserHierarchyResponse**: NUEVO DTO para �rbol jer�rquico recursivo

### 4. **apps/Casino.Application/Services/IBackofficeUserService.cs**
- ? Agregado m�todo `GetUserHierarchyAsync()` para obtener �rbol completo

### 5. **apps/Casino.Application/Services/Implementations/BackofficeUserService.cs**
- ? **CreateUserAsync**: Validaciones de jerarqu�a y parent cashier
- ? **GetUsersAsync**: Soporte para filtrar por `ParentCashierId`
- ? **GetUserAsync**: Incluye conteo de subordinados
- ? **GetUserHierarchyAsync**: NUEVO - Construye �rbol recursivo de subordinados
- ? **BuildUserHierarchyAsync**: NUEVO m�todo privado recursivo
- ? **UpdateUserAsync**: Validaciones al cambiar rol con subordinados
- ? **DeleteUserAsync**: Validaci�n para no eliminar cashier con subordinados

### 6. **apps/api/Casino.Api/Endpoints/BackofficeUserEndpoints.cs**
- ? Actualizado endpoint POST `/users` con validaciones de jerarqu�a
- ? Actualizado endpoint GET `/users` con soporte para CASHIER (solo ve subordinados)
- ? NUEVO endpoint GET `/users/{userId}/hierarchy` para obtener �rbol
- ? Validaciones de autorizaci�n por rol:
  - SUPER_ADMIN: acceso completo
  - OPERATOR_ADMIN: crea cashiers ra�z
  - CASHIER: crea subordinados bajo s� mismo

### 7. **apps/Casino.Application/Validators/Admin/BackofficeUserValidators.cs**
- ? Validaci�n de `CommissionRate` (0-100)
- ? Validaci�n de que solo CASHIER puede tener `ParentCashierId` y `CommissionRate`

### 8. **apps/api/Casino.Api/Program.cs**
- ? Configuraci�n de JSON para manejar enums como strings
- ? `JsonStringEnumConverter` agregado
- ? `PropertyNameCaseInsensitive = true`

## ?? Nuevos Endpoints

### `POST /api/v1/admin/users`
Crear usuario con soporte de jerarqu�a
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
Obtener �rbol jer�rquico completo (recursivo) - **NUEVO ENDPOINT**

## ?? Reglas de Autorizaci�n

### SUPER_ADMIN
- ? Puede crear cualquier tipo de usuario
- ? Ve todos los usuarios del sistema
- ? Puede ver cualquier jerarqu�a

### OPERATOR_ADMIN
- ? Puede crear OPERATOR_ADMIN y CASHIER
- ? Puede crear cashiers ra�z (sin parent)
- ? Solo ve usuarios de su operador
- ? Puede ver jerarqu�as de su operador
- ? No puede crear SUPER_ADMIN

### CASHIER
- ? Puede crear CASHIER subordinados bajo s� mismo
- ? Debe especificar su propio ID como `parentCashierId`
- ? Solo ve sus propios subordinados directos
- ? Solo puede ver su propia jerarqu�a
- ? No puede actualizar usuarios
- ? No puede eliminar usuarios
- ? No puede crear OPERATOR_ADMIN ni SUPER_ADMIN

## ? Validaciones Implementadas

### Al Crear Usuario
1. ? Username �nico
2. ? SUPER_ADMIN no puede tener operador
3. ? Otros roles deben tener operador
4. ? Parent cashier debe existir y ser CASHIER
5. ? Parent cashier debe estar ACTIVE
6. ? Parent cashier debe pertenecer al mismo operador
7. ? CommissionRate entre 0-100
8. ? Solo CASHIER puede tener parent y comisi�n

### Al Actualizar Usuario
1. ? Username �nico (si se cambia)
2. ? No cambiar a SUPER_ADMIN con operador asignado
3. ? No cambiar de CASHIER si tiene subordinados
4. ? CommissionRate v�lido (0-100)
5. ? Limpia parent y comisi�n al cambiar de CASHIER a otro rol

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
- ? Componente de creaci�n de subordinados
- ? Visualizaci�n de �rbol jer�rquico
- ? Lista de subordinados directos
- ? Ejemplos de c�digo React/TypeScript
- ? Manejo de errores
- ? Casos de uso

## ?? C�mo Usar

### 1. Como OPERATOR_ADMIN - Crear cashier ra�z
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

### 3. Ver mi jerarqu�a completa
```bash
GET /api/v1/admin/users/my-cashier-id/hierarchy
```

### 4. Ver mis subordinados directos
```bash
GET /api/v1/admin/users?parentCashierId=my-cashier-id
```

## ?? Ejemplo de Estructura de �rbol

```
OPERATOR_ADMIN
    |
    ??? ROOT_CASHIER_1 (comisi�n: 0%)
    |    |
    |    ??? SUB_CASHIER_1_1 (comisi�n: 10%)
    |    |    |
    |    |    ??? SUB_SUB_CASHIER_1_1_1 (comisi�n: 5%)
    |    |
    |    ??? SUB_CASHIER_1_2 (comisi�n: 15%)
    |
    ??? ROOT_CASHIER_2 (comisi�n: 0%)
         |
         ??? SUB_CASHIER_2_1 (comisi�n: 20%)
              |
              ??? SUB_CASHIER_2_1_1 (comisi�n: 8%)
```

## ?? Pr�ximos Pasos

Para usar esta funcionalidad:

1. **Migrar la base de datos**:
   ```bash
   cd apps/api/Casino.Api
   dotnet ef migrations add AddCashierHierarchy
   dotnet ef database update
   ```

2. **Probar endpoints** con Swagger UI o Postman

3. **Implementar frontend** usando la gu�a en `CASHIER-HIERARCHY-FRONTEND-GUIDE.md`

4. **(Opcional) Crear Players**: Los cashiers tambi�n pueden crear players asignados a ellos mediante los endpoints existentes en `/api/v1/admin/players`

## ?? Notas Adicionales

- La jerarqu�a es **recursiva** sin l�mite de profundidad
- Los cashiers de nivel N pueden crear cashiers de nivel N+1
- La comisi�n es un porcentaje (0-100) que representa lo que el padre recibe del subordinado
- El sistema de auditor�a registra todas las operaciones de creaci�n/modificaci�n/eliminaci�n
- Los enums se serializan como strings en UPPER_CASE (ej: "CASHIER", "ACTIVE")

## ? Testing

Se recomienda probar:
1. ? Crear cashier ra�z como OPERATOR_ADMIN
2. ? Crear subordinados como CASHIER
3. ? Intentar crear subordinado con parent de otro operador (debe fallar)
4. ? Intentar eliminar cashier con subordinados (debe fallar)
5. ? Obtener jerarqu�a completa
6. ? Filtrar por parentCashierId
7. ? CASHIER intentando crear OPERATOR_ADMIN (debe fallar)
8. ? CASHIER intentando ver usuarios fuera de su jerarqu�a (debe fallar)

---

**Estado: ? Completamente Implementado y Compilado**

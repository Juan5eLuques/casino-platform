# Gu�a de Migraci�n - Eliminaci�n del Sistema de Operadores

Esta gu�a te ayudar� a aplicar las migraciones necesarias para eliminar completamente el concepto de "Operador" del sistema casino y transitionar a un modelo basado �nicamente en Brands.

## ?? Pasos de Migraci�n

### 1. Pre-requisitos
- ? Backup de la base de datos actual
- ? Verificar que todos los cambios de c�digo est�n aplicados
- ? Confirmar que no hay sesiones activas cr�ticas

### 2. An�lisis de Datos Actual (ANTES DE MIGRAR)
```bash
# Ejecutar script de an�lisis previo
psql -h localhost -U casino_user -d casino_db -f sql-scripts/pre-migration-data-setup.sql
```

Este script mostrar�:
- Operadores existentes
- Usuarios backoffice y su mapping a operadores
- Brands existentes y su relaci�n con operadores
- Posibles conflictos o datos hu�rfanos

### 3. Aplicar Migraciones EF Core

**Paso 3.1: Preparar el entorno**
```bash
cd apps/api/Casino.Api
dotnet build
```

**Paso 3.2: Aplicar primera migraci�n (Estructura)**
```bash
dotnet ef database update 20251215000000_RemoveOperatorFromSystem --project ../../Casino.Infrastructure
```

Esta migraci�n:
- ? Agrega columna `BrandId` a `BackofficeUsers`
- ? Crea relaciones Brand ? BackofficeUser
- ? Elimina tabla `Operators`
- ? Mantiene columnas `OperatorId` temporalmente para migraci�n de datos

**Paso 3.3: Migraci�n de datos manual (CR�TICO)**

En este punto, necesitas ejecutar scripts SQL para migrar los datos existentes:

```sql
-- Asignar usuarios a brands basado en su operador actual
UPDATE "BackofficeUsers" 
SET "BrandId" = (
    SELECT b."Id" 
    FROM "Brands" b 
    WHERE b."OperatorId" = "BackofficeUsers"."OperatorId" 
    AND b."Status" = 'ACTIVE'
    ORDER BY b."CreatedAt" ASC
    LIMIT 1
)
WHERE "Role" IN ('BRAND_ADMIN', 'CASHIER') 
AND "OperatorId" IS NOT NULL;

-- Verificar que todos los usuarios no-SUPER_ADMIN tienen BrandId
SELECT COUNT(*) as users_without_brand 
FROM "BackofficeUsers" 
WHERE "Role" != 'SUPER_ADMIN' AND "BrandId" IS NULL;
```

**Paso 3.4: Aplicar segunda migraci�n (Limpieza)**
```bash
dotnet ef database update 20251215000001_RemoveOperatorIdColumns --project ../../Casino.Infrastructure
```

Esta migraci�n:
- ? Elimina columna `OperatorId` de `BackofficeUsers`
- ? Elimina columna `OperatorId` de `Brands`
- ? Elimina columna `OperatorId` de `Ledger`

### 4. Verificaci�n Post-Migraci�n
```bash
# Ejecutar script de verificaci�n
psql -h localhost -U casino_user -d casino_db -f sql-scripts/post-migration-cleanup.sql
```

### 5. Restart de la Aplicaci�n
```bash
# Detener la aplicaci�n
docker-compose down

# Rebuild y restart
docker-compose up -d
dotnet run --project apps/api/Casino.Api
```

## ?? Validaciones de �xito

Despu�s de la migraci�n, verificar:

### ? Estructura de Base de Datos
- [ ] Tabla `Operators` no existe
- [ ] Columna `BrandId` existe en `BackofficeUsers` 
- [ ] No existen columnas `OperatorId` en ninguna tabla
- [ ] Foreign keys `BackofficeUsers` ? `Brands` funcionan

### ? Datos Migrados Correctamente
```sql
-- Todos los usuarios no-SUPER_ADMIN tienen BrandId
SELECT 'Users without Brand' as issue, COUNT(*) as count
FROM "BackofficeUsers" 
WHERE "Role" != 'SUPER_ADMIN' AND "BrandId" IS NULL;

-- No hay referencias hu�rfanas
SELECT 'Orphaned Brand References' as issue, COUNT(*) as count  
FROM "BackofficeUsers" u
LEFT JOIN "Brands" b ON u."BrandId" = b."Id"
WHERE u."BrandId" IS NOT NULL AND b."Id" IS NULL;
```

### ? Funcionalidad de la Aplicaci�n
- [ ] Login de usuarios backoffice funciona
- [ ] Creaci�n de usuarios BRAND_ADMIN funciona
- [ ] Jerarqu�a de cashiers se mantiene dentro del mismo brand
- [ ] Gesti�n de jugadores funciona con brand scoping
- [ ] Auditor�a funciona correctamente

## ?? Troubleshooting

### Error: "Users without brand assignment"
```sql
-- Identificar usuarios problem�ticos
SELECT u."Id", u."Username", u."Role", u."OperatorId" 
FROM "BackofficeUsers" u 
WHERE u."Role" != 'SUPER_ADMIN' AND u."BrandId" IS NULL;

-- Asignar manualmente a un brand
UPDATE "BackofficeUsers" 
SET "BrandId" = 'GUID_DEL_BRAND_DESTINO'
WHERE "Id" = 'GUID_DEL_USUARIO';
```

### Error: "Cashier hierarchy broken"
```sql
-- Verificar jerarqu�a
SELECT 
    c."Username" as cashier,
    p."Username" as parent,
    c."BrandId" as cashier_brand,
    p."BrandId" as parent_brand
FROM "BackofficeUsers" c
LEFT JOIN "BackofficeUsers" p ON c."ParentCashierId" = p."Id"
WHERE c."ParentCashierId" IS NOT NULL 
AND c."BrandId" != p."BrandId";

-- Corregir asignando cashiers subordinados al brand del padre
UPDATE "BackofficeUsers" c
SET "BrandId" = p."BrandId"
FROM "BackofficeUsers" p
WHERE c."ParentCashierId" = p."Id" 
AND c."BrandId" != p."BrandId";
```

## ?? Rollback (Solo en caso de emergencia)

Si algo sale mal, puedes hacer rollback:

```bash
# Volver a migraci�n anterior (antes de eliminar operadores)
dotnet ef database update 20251007004455_AddCashierHierarchyFields --project ../../Casino.Infrastructure

# Restaurar backup
psql -h localhost -U casino_user -d casino_db < backup_pre_migration.sql
```

## ?? M�tricas de �xito

Despu�s de completar la migraci�n:

- **Reducci�n de complejidad**: ~30% menos tablas y relaciones
- **Simplificaci�n de c�digo**: Eliminaci�n de 500+ l�neas de c�digo operator-related
- **Mejor performance**: Menos JOINs en queries principales
- **Arquitectura m�s clara**: Jerarqu�a directa Brand ? User ? Player

## ?? Siguientes Pasos

Una vez completada la migraci�n:

1. **Actualizar documentaci�n** de la API
2. **Revisar permisos** de roles en endpoints
3. **Optimizar queries** que usaban operator scoping
4. **Actualizar frontend** para eliminar referencias a operators

---

**?? IMPORTANTE**: Esta migraci�n es **irreversible** sin un backup. Aseg�rate de hacer backup completo antes de comenzar.
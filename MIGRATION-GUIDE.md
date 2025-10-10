# Guía de Migración - Eliminación del Sistema de Operadores

Esta guía te ayudará a aplicar las migraciones necesarias para eliminar completamente el concepto de "Operador" del sistema casino y transitionar a un modelo basado únicamente en Brands.

## ?? Pasos de Migración

### 1. Pre-requisitos
- ? Backup de la base de datos actual
- ? Verificar que todos los cambios de código están aplicados
- ? Confirmar que no hay sesiones activas críticas

### 2. Análisis de Datos Actual (ANTES DE MIGRAR)
```bash
# Ejecutar script de análisis previo
psql -h localhost -U casino_user -d casino_db -f sql-scripts/pre-migration-data-setup.sql
```

Este script mostrará:
- Operadores existentes
- Usuarios backoffice y su mapping a operadores
- Brands existentes y su relación con operadores
- Posibles conflictos o datos huérfanos

### 3. Aplicar Migraciones EF Core

**Paso 3.1: Preparar el entorno**
```bash
cd apps/api/Casino.Api
dotnet build
```

**Paso 3.2: Aplicar primera migración (Estructura)**
```bash
dotnet ef database update 20251215000000_RemoveOperatorFromSystem --project ../../Casino.Infrastructure
```

Esta migración:
- ? Agrega columna `BrandId` a `BackofficeUsers`
- ? Crea relaciones Brand ? BackofficeUser
- ? Elimina tabla `Operators`
- ? Mantiene columnas `OperatorId` temporalmente para migración de datos

**Paso 3.3: Migración de datos manual (CRÍTICO)**

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

**Paso 3.4: Aplicar segunda migración (Limpieza)**
```bash
dotnet ef database update 20251215000001_RemoveOperatorIdColumns --project ../../Casino.Infrastructure
```

Esta migración:
- ? Elimina columna `OperatorId` de `BackofficeUsers`
- ? Elimina columna `OperatorId` de `Brands`
- ? Elimina columna `OperatorId` de `Ledger`

### 4. Verificación Post-Migración
```bash
# Ejecutar script de verificación
psql -h localhost -U casino_user -d casino_db -f sql-scripts/post-migration-cleanup.sql
```

### 5. Restart de la Aplicación
```bash
# Detener la aplicación
docker-compose down

# Rebuild y restart
docker-compose up -d
dotnet run --project apps/api/Casino.Api
```

## ?? Validaciones de Éxito

Después de la migración, verificar:

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

-- No hay referencias huérfanas
SELECT 'Orphaned Brand References' as issue, COUNT(*) as count  
FROM "BackofficeUsers" u
LEFT JOIN "Brands" b ON u."BrandId" = b."Id"
WHERE u."BrandId" IS NOT NULL AND b."Id" IS NULL;
```

### ? Funcionalidad de la Aplicación
- [ ] Login de usuarios backoffice funciona
- [ ] Creación de usuarios BRAND_ADMIN funciona
- [ ] Jerarquía de cashiers se mantiene dentro del mismo brand
- [ ] Gestión de jugadores funciona con brand scoping
- [ ] Auditoría funciona correctamente

## ?? Troubleshooting

### Error: "Users without brand assignment"
```sql
-- Identificar usuarios problemáticos
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
-- Verificar jerarquía
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
# Volver a migración anterior (antes de eliminar operadores)
dotnet ef database update 20251007004455_AddCashierHierarchyFields --project ../../Casino.Infrastructure

# Restaurar backup
psql -h localhost -U casino_user -d casino_db < backup_pre_migration.sql
```

## ?? Métricas de Éxito

Después de completar la migración:

- **Reducción de complejidad**: ~30% menos tablas y relaciones
- **Simplificación de código**: Eliminación de 500+ líneas de código operator-related
- **Mejor performance**: Menos JOINs en queries principales
- **Arquitectura más clara**: Jerarquía directa Brand ? User ? Player

## ?? Siguientes Pasos

Una vez completada la migración:

1. **Actualizar documentación** de la API
2. **Revisar permisos** de roles en endpoints
3. **Optimizar queries** que usaban operator scoping
4. **Actualizar frontend** para eliminar referencias a operators

---

**?? IMPORTANTE**: Esta migración es **irreversible** sin un backup. Asegúrate de hacer backup completo antes de comenzar.
# ??? Cambios en la Base de Datos - Brand Assets System

## ?? Resumen de Cambios

Después de aplicar la migración `20251113053433_AddBrandSettingsTable`, verás **1 nueva tabla** y **1 nuevo índice** en tu base de datos PostgreSQL.

---

## ? Nueva Tabla: `BrandSettings`

### Estructura de la Tabla

```sql
CREATE TABLE "BrandSettings" (
    "Id" uuid NOT NULL,
    "BrandId" uuid NOT NULL,
    "Colors" jsonb NOT NULL,
    "Images" jsonb NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_BrandSettings" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_BrandSettings_Brands_BrandId" FOREIGN KEY ("BrandId") 
        REFERENCES "Brands" ("Id") ON DELETE CASCADE
);
```

### Columnas Detalladas

| Columna | Tipo | Nullable | Default | Descripción |
|---------|------|----------|---------|-------------|
| **Id** | `uuid` | NO | - | Primary Key, identificador único del registro |
| **BrandId** | `uuid` | NO | - | Foreign Key a tabla `Brands`, relación 1:1 |
| **Colors** | `jsonb` | NO | `{}` | Paleta de colores en formato JSON |
| **Images** | `jsonb` | NO | `{...}` | URLs de banners y media en formato JSON |
| **CreatedAt** | `timestamptz` | NO | `CURRENT_TIMESTAMP` | Fecha de creación |
| **UpdatedAt** | `timestamptz` | NO | `CURRENT_TIMESTAMP` | Fecha de última actualización |

### Índices

```sql
-- Índice único para asegurar 1 BrandSettings por Brand
CREATE UNIQUE INDEX "IX_BrandSettings_BrandId" ON "BrandSettings" ("BrandId");
```

### Constraints

1. **Primary Key**: `PK_BrandSettings` en columna `Id`
2. **Foreign Key**: `FK_BrandSettings_Brands_BrandId` 
   - Referencia: `Brands.Id`
   - On Delete: **CASCADE** (si se elimina un Brand, se elimina su BrandSettings)
3. **Unique Index**: Solo puede haber 1 `BrandSettings` por `BrandId`

---

## ?? Estructura de Datos JSON

### Columna `Colors` (JSONB)

Ejemplo de contenido:
```json
{
  "--color-primary": "#ffb300",
  "--color-secondary": "#2196f3",
"--color-accent": "#e91e63",
  "--color-background": "#121212",
  "--color-text": "#ffffff"
}
```

### Columna `Images` (JSONB)

Estructura predeterminada:
```json
{
  "banners": {
    "home": [
      "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/banners/home/abc-123.jpg",
      "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/banners/home/def-456.jpg"
    ],
    "slots": [
      "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/banners/slots/ghi-789.jpg"
    ],
    "live-casino": []
  },
  "media": {
    "logo": "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/banners/media/logo.png",
"favicon": "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/banners/media/favicon.ico",
    "others": []
  }
}
```

---

## ?? Cómo Verificar los Cambios

### 1. Verificar que la tabla existe

```sql
-- Listar todas las tablas
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public'
ORDER BY table_name;

-- Debería incluir: BrandSettings
```

### 2. Ver estructura de la tabla

```sql
-- Ver columnas de BrandSettings
SELECT 
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_name = 'BrandSettings'
ORDER BY ordinal_position;
```

**Resultado esperado:**
```
column_name  | data_type          | is_nullable | column_default
-------------|--------------------------------|-------------|------------------
Id           | uuid        | NO        | 
BrandId      | uuid          | NO          | 
Colors     | jsonb         | NO          | 
Images       | jsonb    | NO          | 
CreatedAt    | timestamp with time zone       | NO          | CURRENT_TIMESTAMP
UpdatedAt    | timestamp with time zone | NO          | CURRENT_TIMESTAMP
```

### 3. Ver constraints (Foreign Keys)

```sql
-- Ver Foreign Keys
SELECT
    tc.constraint_name,
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name,
    rc.delete_rule
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
JOIN information_schema.referential_constraints AS rc
    ON tc.constraint_name = rc.constraint_name
WHERE tc.table_name = 'BrandSettings'
  AND tc.constraint_type = 'FOREIGN KEY';
```

**Resultado esperado:**
```
constraint_name   | table_name     | column_name | foreign_table_name | foreign_column_name | delete_rule
--------------------------------------|----------------|-------------|--------------------|--------------------|-------------
FK_BrandSettings_Brands_BrandId       | BrandSettings  | BrandId     | Brands        | Id       | CASCADE
```

### 4. Ver índices

```sql
-- Ver índices de la tabla
SELECT
  indexname,
 indexdef
FROM pg_indexes
WHERE tablename = 'BrandSettings';
```

**Resultado esperado:**
```
indexname              | indexdef
---------------------------------|------------------------------------------------------------
PK_BrandSettings            | CREATE UNIQUE INDEX "PK_BrandSettings" ON "BrandSettings" USING btree ("Id")
IX_BrandSettings_BrandId         | CREATE UNIQUE INDEX "IX_BrandSettings_BrandId" ON "BrandSettings" USING btree ("BrandId")
```

### 5. Verificar tabla de migraciones

```sql
-- Ver que la migración se aplicó correctamente
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
ORDER BY "MigrationId" DESC
LIMIT 5;
```

**Debería incluir:**
```
MigrationId  | ProductVersion
-----------------------------------------------|---------------
20251113053433_AddBrandSettingsTable          | 9.0.9
20251023052805_AddGameTypeField    | 9.0.9
20251023050859_AddGameCatalogAndLaunchSystem  | 9.0.9
...
```

---

## ?? Relación con Tabla `Brands`

### Diagrama de Relación

```
???????????????????          ????????????????????????
?     Brands  ?        ?   BrandSettings      ?
???????????????????          ????????????????????????
? Id (PK)         ???????????? Id (PK)   ?
? Code            ?   1:1    ? BrandId (FK, UNIQUE) ?
? Name        ?? Colors (JSONB)     ?
? Domain        ?          ? Images (JSONB)       ?
? ...             ?  ? CreatedAt            ?
???????????????????          ? UpdatedAt            ?
            ????????????????????????
```

### Comportamiento de la Relación

- **Relación**: 1 Brand ? 0 o 1 BrandSettings
- **Unique Constraint**: Un Brand solo puede tener un BrandSettings
- **Cascade Delete**: Si eliminas un Brand, se elimina automáticamente su BrandSettings
- **Bidireccional**: Puedes navegar desde Brand ? BrandSettings y viceversa en EF Core

---

## ?? Datos de Ejemplo

Después de usar el sistema, la tabla podría verse así:

```sql
SELECT 
    "Id",
    "BrandId",
    "Colors"::text,
    "Images"::text,
    "CreatedAt",
    "UpdatedAt"
FROM "BrandSettings"
LIMIT 1;
```

**Resultado:**
```
Id: 123e4567-e89b-12d3-a456-426614174000
BrandId: 987e6543-e21b-45d6-b890-123456789abc
Colors: {"--color-primary": "#ffb300", "--color-secondary": "#2196f3"}
Images: {"banners":{"home":["https://..."],"slots":[],"live-casino":[]},"media":{"logo":"https://...","favicon":"","others":[]}}
CreatedAt: 2025-01-13 10:34:33+00
UpdatedAt: 2025-01-13 12:45:00+00
```

---

## ?? Consultas Útiles

### Ver todos los Brands con sus Settings

```sql
SELECT 
    b."Code" AS brand_code,
    b."Name" AS brand_name,
  bs."Id" AS settings_id,
    bs."Colors"->>'--color-primary' AS primary_color,
    jsonb_array_length(bs."Images"->'banners'->'home') AS home_banners_count
FROM "Brands" b
LEFT JOIN "BrandSettings" bs ON bs."BrandId" = b."Id"
ORDER BY b."Code";
```

### Ver Brands sin Settings

```sql
SELECT 
    b."Id",
    b."Code",
    b."Name"
FROM "Brands" b
LEFT JOIN "BrandSettings" bs ON bs."BrandId" = b."Id"
WHERE bs."Id" IS NULL;
```

### Ver todas las URLs de imágenes de un Brand

```sql
SELECT 
    b."Code",
    jsonb_pretty(bs."Images") AS images
FROM "Brands" b
JOIN "BrandSettings" bs ON bs."BrandId" = b."Id"
WHERE b."Code" = 'bet30';
```

---

## ?? Rollback (Si es necesario)

Si necesitas deshacer la migración:

```bash
# Revertir la última migración
dotnet ef database update 20251023052805_AddGameTypeField --project apps/Casino.Infrastructure/Casino.Infrastructure.csproj --startup-project apps/api/Casino.Api/Casino.Api.csproj --context CasinoDbContext
```

Esto ejecutará el método `Down()` de la migración:
```sql
DROP TABLE IF EXISTS "BrandSettings" CASCADE;
DELETE FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251113053433_AddBrandSettingsTable';
```

---

## ? Checklist de Verificación Post-Migración

Después de aplicar la migración, verifica:

- [ ] Tabla `BrandSettings` existe
- [ ] Tabla tiene 6 columnas (Id, BrandId, Colors, Images, CreatedAt, UpdatedAt)
- [ ] Primary Key `PK_BrandSettings` existe
- [ ] Foreign Key `FK_BrandSettings_Brands_BrandId` existe
- [ ] Índice único `IX_BrandSettings_BrandId` existe
- [ ] Columnas `Colors` e `Images` son tipo `jsonb`
- [ ] Columnas `CreatedAt` y `UpdatedAt` tienen default `CURRENT_TIMESTAMP`
- [ ] Foreign Key tiene `ON DELETE CASCADE`
- [ ] Registro en tabla `__EFMigrationsHistory` existe

---

## ?? Próximos Pasos

1. **Aplicar la migración**:
   ```bash
   dotnet ef database update --project apps/Casino.Infrastructure/Casino.Infrastructure.csproj --startup-project apps/api/Casino.Api/Casino.Api.csproj --context CasinoDbContext
   ```

2. **Verificar con las queries de este documento**

3. **Probar los endpoints de la API**:
   ```bash
   POST /api/v1/admin/brands/assets/initialize
   GET /api/v1/admin/brands/assets/settings
   ```

4. **Monitorear logs de aplicación** para confirmar que el sistema funciona correctamente

---

**Fecha de creación**: 2025-01-13  
**Migración**: `20251113053433_AddBrandSettingsTable`  
**Versión EF Core**: 9.0.9

# ??? Configurar Base de Datos PostgreSQL Local

## ?? Resumen

Esta guía te ayudará a replicar la estructura completa de la base de datos en una instancia local de PostgreSQL para desarrollo.

---

## ?? Método 1: Usar Script SQL Generado (Más Rápido)

### Paso 1: Instalar PostgreSQL Local

**Windows:**
1. Descarga PostgreSQL desde: https://www.postgresql.org/download/windows/
2. Instala PostgreSQL 16+ (recomendado)
3. Durante instalación, configura:
   - Puerto: `5432` (default)
   - Password para usuario `postgres`: anota este password
   - Habilita pgAdmin 4

**macOS:**
```bash
brew install postgresql@16
brew services start postgresql@16
```

**Linux (Ubuntu/Debian):**
```bash
sudo apt update
sudo apt install postgresql postgresql-contrib
sudo systemctl start postgresql
```

### Paso 2: Crear Base de Datos Local

**Opción A: Usando pgAdmin (GUI)**
1. Abre pgAdmin 4
2. Conecta al servidor local (localhost:5432)
3. Click derecho en "Databases" ? "Create" ? "Database..."
4. Nombre: `casino_dev`
5. Owner: `postgres`
6. Click "Save"

**Opción B: Usando psql (Terminal)**
```bash
# Conectar a PostgreSQL
psql -U postgres -h localhost

# Crear database
CREATE DATABASE casino_dev;

# Salir
\q
```

### Paso 3: Aplicar el Script SQL Generado

El script `database-schema.sql` ya fue generado en la raíz del proyecto.

**Usando psql:**
```bash
cd D:\repos\casino-platform\backend

# Aplicar el script
psql -U postgres -h localhost -d casino_dev -f database-schema.sql
```

**Usando pgAdmin:**
1. Click en `casino_dev` database
2. Click en "Query Tool" (ícono de hoja de SQL)
3. Abre el archivo `database-schema.sql`
4. Click "Execute" (? o F5)

### Paso 4: Verificar Tablas Creadas

```sql
-- Ver todas las tablas
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public'
ORDER BY table_name;
```

**Deberías ver:**
```
BackofficeAudits
BackofficeUsers
BrandGames
BrandProviderConfigs
BrandSettings  ? Nueva
Brands
CashierPlayers
CommissionAccruals
GameLaunchLogs
GameProviders
GameSessions
Games
Ledger
MonthlyClosures
Players
ProviderAudits
Rounds
WalletTransactions
Wallets
__EFMigrationsHistory
```

---

## ??? Método 2: Usar EF Core Migrations (Más Control)

### Paso 1: Configurar Connection String Local

Edita `appsettings.Development.json` (o crea uno si no existe):

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=casino_dev;Username=postgres;Password=TU_PASSWORD_AQUI;SSL Mode=Disable"
  },
  "Auth": {
  "Issuer": "casino",
    "JwtKey": "supersecretdevkey-please-change-32chars!!"
  },
  "AWS": {
    "AccessKey": "",
    "SecretKey": "",
    "S3": {
      "BucketName": "casino-assets-dev-local",
      "Region": "us-east-1"
  }
  }
}
```

### Paso 2: Aplicar Migraciones con EF Core

```bash
cd D:\repos\casino-platform\backend

# Aplicar todas las migraciones a la DB local
dotnet ef database update \
  --project apps/Casino.Infrastructure/Casino.Infrastructure.csproj \
  --startup-project apps/api/Casino.Api/Casino.Api.csproj \
  --context CasinoDbContext \
  --connection "Host=localhost;Port=5432;Database=casino_dev;Username=postgres;Password=TU_PASSWORD"
```

### Paso 3: Verificar Migraciones Aplicadas

```bash
# Ver lista de migraciones
dotnet ef migrations list \
  --project apps/Casino.Infrastructure/Casino.Infrastructure.csproj \
  --startup-project apps/api/Casino.Api/Casino.Api.csproj \
  --context CasinoDbContext
```

---

## ?? Método 3: Clonar Estructura desde Base Remota (Avanzado)

Si quieres clonar SOLO la estructura (sin datos) desde tu base remota:

### Opción A: pg_dump (Schema Only)

```bash
# Exportar solo estructura (sin datos)
pg_dump -h shortline.proxy.rlwy.net \
  -p 47433 \
  -U postgres \
  -d railway \
  --schema-only \
  --no-owner \
  --no-acl \
  -f schema-only.sql

# Aplicar a base local
psql -U postgres -h localhost -d casino_dev -f schema-only.sql
```

### Opción B: pg_dump (Con Datos de Prueba)

```bash
# Exportar estructura Y datos
pg_dump -h shortline.proxy.rlwy.net \
  -p 47433 \
  -U postgres \
  -d railway \
  --no-owner \
  --no-acl \
  -f full-backup.sql

# Aplicar a base local
psql -U postgres -h localhost -d casino_dev -f full-backup.sql
```

---

## ?? Configuración de Desarrollo

### appsettings.Development.json (Recomendado)

Crea o edita `apps/api/Casino.Api/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
    "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=casino_dev;Username=postgres;Password=postgres;SSL Mode=Disable"
  },
  "Auth": {
  "Issuer": "casino-dev",
    "JwtKey": "dev-key-do-not-use-in-production-32chars!"
  },
  "Jwt": {
    "Issuer": "casino-dev",
    "Audience": "casino-dev",
    "Key": "dev-jwt-key-change-this-in-production"
  },
  "AWS": {
    "AccessKey": "",
    "SecretKey": "",
    "S3": {
      "BucketName": "casino-assets-local",
      "Region": "us-east-1"
    }
  }
}
```

### Ejecutar con Base de Datos Local

```bash
cd D:\repos\casino-platform\backend

# Ejecutar en modo Development (usa appsettings.Development.json)
dotnet run --project apps/api/Casino.Api/Casino.Api.csproj --environment Development
```

---

## ?? Seedear Datos de Prueba

### Opción 1: Crear Script de Seed Manual

Crea `seed-data.sql`:

```sql
-- Insertar Brand de prueba
INSERT INTO "Brands" ("Id", "Code", "Name", "Locale", "Status", "CreatedAt", "UpdatedAt")
VALUES 
  ('11111111-1111-1111-1111-111111111111', 'testbrand', 'Test Brand', 'en-US', 'ACTIVE', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Insertar Super Admin
INSERT INTO "BackofficeUsers" ("Id", "BrandId", "Username", "PasswordHash", "Role", "Status", "CreatedAt", "WalletBalance", "CommissionPercent")
VALUES 
  ('22222222-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111111', 'admin', 
   '$2a$11$YourHashedPasswordHere', 'SUPER_ADMIN', 'ACTIVE', CURRENT_TIMESTAMP, 0, 0);

-- Insertar BrandSettings
INSERT INTO "BrandSettings" ("Id", "BrandId", "Colors", "Images", "CreatedAt", "UpdatedAt")
VALUES 
  ('33333333-3333-3333-3333-333333333333', '11111111-1111-1111-1111-111111111111', 
   '{}', 
   '{"banners":{"home":[],"slots":[],"live-casino":[]},"media":{"logo":"","favicon":"","others":[]}}', 
   CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Insertar GameProvider de prueba
INSERT INTO "GameProviders" ("Id", "Code", "Name", "LaunchEndpointTemplate", "RequiresSessionToken", 
  "SupportsRealMode", "SupportsDemoMode", "Enabled", "CreatedAt", "UpdatedAt")
VALUES 
  ('44444444-4444-4444-4444-444444444444', 'demo', 'Demo Provider', 
   'https://demo.provider.com/launch?game={gameCode}&token={sessionToken}', 
   true, true, true, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Insertar algunos juegos de prueba
INSERT INTO "Games" ("Id", "Code", "Provider", "Name", "Type", "Enabled", "CreatedAt")
VALUES 
  ('55555555-5555-5555-5555-555555555555', 'demo-slot-1', 'demo', 'Demo Slot 1', 'SLOT', true, CURRENT_TIMESTAMP),
  ('66666666-6666-6666-6666-666666666666', 'demo-slot-2', 'demo', 'Demo Slot 2', 'SLOT', true, CURRENT_TIMESTAMP),
  ('77777777-7777-7777-7777-777777777777', 'demo-live-1', 'demo', 'Demo Live 1', 'LIVE', true, CURRENT_TIMESTAMP);
```

Aplicar:
```bash
psql -U postgres -h localhost -d casino_dev -f seed-data.sql
```

### Opción 2: Crear Seeder en C#

Crea `apps/Casino.Infrastructure/Data/DbSeeder.cs`:

```csharp
using Casino.Domain.Entities;
using Casino.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Casino.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(CasinoDbContext context)
    {
  // Verificar si ya hay datos
        if (await context.Brands.AnyAsync())
 return; // Ya está seeded

        // Crear Brand de prueba
        var brand = new Brand
        {
          Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
       Code = "testbrand",
  Name = "Test Brand",
  Locale = "en-US",
        Status = BrandStatus.ACTIVE,
     CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
        };
      context.Brands.Add(brand);

     // Crear Super Admin
  var admin = new BackofficeUser
     {
   Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
BrandId = brand.Id,
          Username = "admin",
            PasswordHash = "$2a$11$...", // Usar PasswordService para generar
       Role = BackofficeRole.SUPER_ADMIN,
            Status = UserStatus.ACTIVE,
            CreatedAt = DateTime.UtcNow,
      WalletBalance = 0,
            CommissionPercent = 0
        };
        context.BackofficeUsers.Add(admin);

        await context.SaveChangesAsync();
  }
}
```

Llamar en `Program.cs`:

```csharp
// En Program.cs, después de configurar services
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CasinoDbContext>();
    await DbSeeder.SeedAsync(context);
}
```

---

## ? Verificación Final

### 1. Verificar Conexión

```bash
dotnet run --project apps/api/Casino.Api/Casino.Api.csproj --environment Development
```

Deberías ver en los logs:
```
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (XXms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      SELECT ... FROM "__EFMigrationsHistory" ...
```

### 2. Verificar Tablas

```sql
-- Conectar a la DB local
psql -U postgres -h localhost -d casino_dev

-- Ver tablas
\dt

-- Ver estructura de BrandSettings
\d "BrandSettings"

-- Salir
\q
```

### 3. Probar API

```bash
curl http://localhost:5000/health
# Respuesta esperada: Healthy
```

---

## ?? Comparación: Local vs Remoto

| Aspecto | Base Remota (Railway) | Base Local |
|---------|----------------------|------------|
| **Host** | shortline.proxy.rlwy.net:47433 | localhost:5432 |
| **SSL** | Required | Disabled |
| **Performance** | Latencia de red | Instantáneo |
| **Datos** | Producción/Staging | Desarrollo |
| **Costo** | Railway pricing | Gratis |
| **Backup** | Automático (Railway) | Manual |
| **Migrations** | Aplicar con cuidado | Experimentar libremente |

---

## ?? Recomendaciones

### Para Desarrollo Diario
- ? Usa base de datos **local**
- ? Experimenta con migraciones sin miedo
- ? Datos de prueba que puedes resetear
- ? Sin latencia de red

### Para Testing
- ? Usa base de datos **remota** (Railway)
- ? Datos más cercanos a producción
- ? Probar con datos reales

### Workflow Recomendado
1. Desarrollar features con BD local
2. Crear migrations en local
3. Probar migrations en local
4. Commitear migrations a Git
5. Aplicar migrations a Railway (staging)
6. Verificar en remoto
7. Desplegar a producción

---

## ?? Comandos Útiles

```bash
# Ver migraciones aplicadas
dotnet ef migrations list

# Crear nueva migración
dotnet ef migrations add NombreMigracion

# Aplicar migraciones
dotnet ef database update

# Revertir migración
dotnet ef database update PreviousMigrationName

# Generar script SQL
dotnet ef migrations script --output script.sql

# Eliminar última migración (si no se aplicó)
dotnet ef migrations remove

# Ver SQL de una migración específica
dotnet ef migrations script PreviousMigration TargetMigration

# Crear base de datos desde cero
dotnet ef database update 0  # Rollback all
dotnet ef database update    # Apply all
```

---

## ?? Bonus: Usar Docker para PostgreSQL Local

Si prefieres usar Docker:

```bash
# Ejecutar PostgreSQL en Docker
docker run --name casino-postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=casino_dev \
  -p 5432:5432 \
  -d postgres:16

# Ver logs
docker logs casino-postgres

# Conectar
psql -h localhost -U postgres -d casino_dev

# Detener
docker stop casino-postgres

# Reiniciar
docker start casino-postgres

# Eliminar (CUIDADO: borra todos los datos)
docker rm -f casino-postgres
```

**docker-compose.yml:**

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:16
    container_name: casino-postgres-dev
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: casino_dev
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
   timeout: 5s
      retries: 5

volumes:
  postgres_data:
```

Ejecutar:
```bash
docker-compose up -d
```

---

## ? Checklist Final

- [ ] PostgreSQL instalado localmente (o Docker)
- [ ] Base de datos `casino_dev` creada
- [ ] Script SQL aplicado o migraciones ejecutadas
- [ ] `appsettings.Development.json` configurado
- [ ] Aplicación ejecutándose con BD local
- [ ] Tablas verificadas en pgAdmin o psql
- [ ] Datos de prueba insertados (opcional)
- [ ] Endpoint `/health` responde correctamente

---

**Fecha**: 2025-01-13  
**PostgreSQL Version**: 16+ (recomendado)  
**Script generado**: `database-schema.sql`  
**Status**: ? Listo para usar

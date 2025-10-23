# Scripts de Administración de SUPER_ADMIN

Este directorio contiene scripts para crear y gestionar usuarios SUPER_ADMIN en la plataforma Casino.

## ?? Archivos Disponibles

### 1. `create-superadmin.sql` ? (Recomendado)
Script SQL directo para crear un SUPER_ADMIN con credenciales predeterminadas.

**Credenciales:**
- **Username:** `superadmin`
- **Password:** `password`
- **Role:** `SUPER_ADMIN`

**Hash:** El archivo incluye un hash válido generado con `HashGenerator` que es 100% compatible con `PasswordService.cs`

**Uso:**
```bash
# Desde la línea de comandos
psql -h localhost -U postgres -d casino_platform -f scripts/create-superadmin.sql

# O desde pgAdmin/DBeaver
# Copiar y pegar el contenido del archivo en la consola SQL
```

### 2. `HashGenerator/` ?? (Herramienta)
Proyecto .NET que genera hashes de contraseñas usando el mismo `PasswordHasher<object>` que usa la aplicación.

**Características:**
- ? 100% compatible con `PasswordService.cs`
- ? Genera el SQL completo listo para ejecutar
- ? Verifica que el hash funciona antes de mostrarlo
- ? Permite personalizar la contraseña

**Uso:**
```bash
# Generar hash para "password" (default)
dotnet run --project scripts/HashGenerator/HashGenerator.csproj

# Generar hash para una contraseña personalizada
dotnet run --project scripts/HashGenerator/HashGenerator.csproj MiPassword123!

# El output incluye el SQL completo listo para copiar y ejecutar
```

### 3. `create-superadmin.ps1` (PowerShell - Avanzado)
Script PowerShell que genera el hash de la contraseña dinámicamente usando el mismo PasswordHasher que la aplicación.

**Ventajas:**
- Genera hash en tiempo real (no requiere hash pre-generado)
- Permite personalizar username y password
- Puede ejecutar automáticamente el SQL en la base de datos

**Requisitos:**
- PowerShell 5.1 o superior
- .NET SDK instalado (para usar PasswordHasher)
- psql instalado (opcional, para ejecución automática)

**Uso:**
```powershell
# Crear con credenciales por defecto
.\scripts\create-superadmin.ps1

# Personalizar username y password
.\scripts\create-superadmin.ps1 -Username "admin" -Password "MiPassword123!"

# Con string de conexión personalizado
.\scripts\create-superadmin.ps1 `
  -Username "admin" `
    -Password "Secure123!" `
 -ConnectionString "Host=localhost;Database=casino_platform;Username=postgres;Password=postgres"
```

## ?? Seguridad

### ?? IMPORTANTE para Producción

Este script es **solo para desarrollo y testing**. En producción:

1. **Nunca uses la contraseña por defecto** (`password`)
2. **Cambia la contraseña inmediatamente** después del primer login
3. **Usa contraseñas fuertes** (mínimo 12 caracteres, mayúsculas, minúsculas, números y símbolos)
4. **Considera usar variables de entorno** para las credenciales

### Generar Hash Personalizado

Si necesitas generar un hash para una contraseña personalizada:

**Opción 1: Usar HashGenerator (Recomendado)**
```bash
dotnet run --project scripts/HashGenerator/HashGenerator.csproj MiContraseñaSegura123!
```

El output te dará el SQL completo listo para ejecutar.

**Opción 2: Usar el script PowerShell**
```powershell
.\scripts\create-superadmin.ps1 -Password "TuContraseñaSegura123!"
```

**Opción 3: Código C# directo**
```csharp
using Microsoft.AspNetCore.Identity;

var hasher = new PasswordHasher<object>();
var hash = hasher.HashPassword(new object(), "TuContraseñaSegura");
Console.WriteLine(hash);
```

## ?? Verificación

Después de ejecutar el script, verifica que el usuario fue creado correctamente:

```sql
SELECT 
    "Id",
    "Username",
    "Role",
    "Status",
    "HierarchyLevel",
    "BrandId",
    "CreatedAt"
FROM "BackofficeUsers"
WHERE "Username" = 'superadmin';
```

Deberías ver:
- **Username:** superadmin
- **Role:** SUPER_ADMIN
- **Status:** ACTIVE
- **HierarchyLevel:** 0
- **BrandId:** NULL

## ?? Testing del Login

Después de crear el usuario, prueba el login:

```bash
# Usando curl
curl -X POST http://localhost:5000/api/v1/admin/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "superadmin",
    "password": "password"
  }'
```

Deberías recibir un token JWT válido en la respuesta.

## ?? Recrear el Usuario

Si necesitas recrear el usuario (eliminar y crear nuevamente):

```sql
-- Eliminar usuario existente
DELETE FROM "BackofficeUsers" WHERE "Username" = 'superadmin';

-- Luego ejecuta nuevamente el script create-superadmin.sql
```

## ?? Recursos Adicionales

- **Documentación de PasswordHasher:** https://docs.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/password-hashing
- **Mejores prácticas de contraseñas:** https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html

## ? Troubleshooting

### Error: "psql: command not found"
**Solución:** Instala PostgreSQL client tools o usa pgAdmin/DBeaver

### Error: "duplicate key value violates unique constraint"
**Solución:** El username ya existe. Elimínalo primero:
```sql
DELETE FROM "BackofficeUsers" WHERE "Username" = 'superadmin';
```

### Password no funciona después de la creación (Error 401)
**Causa:** El hash en el archivo SQL no es válido o no coincide con la contraseña.

**Solución:** Regenera el hash usando HashGenerator:
```bash
# 1. Genera un nuevo hash
dotnet run --project scripts/HashGenerator/HashGenerator.csproj password

# 2. Copia el SQL completo que muestra el HashGenerator
# 3. Ejecuta ese SQL en tu base de datos

# O simplemente ejecuta el archivo create-superadmin.sql actualizado
psql -h localhost -U postgres -d casino_platform -f scripts/create-superadmin.sql
```

### HashGenerator muestra error de compilación
**Solución:** Asegúrate de tener .NET 9 SDK instalado:
```bash
dotnet --version
# Debe ser 9.0.x o superior
```

## ?? Flujo de Trabajo Recomendado

1. **Primera vez / Desarrollo:**
   ```bash
   # Ejecutar el SQL directo (ya tiene hash válido)
   psql -h localhost -U postgres -d casino_platform -f scripts/create-superadmin.sql
   ```

2. **Contraseña personalizada:**
   ```bash
   # Generar nuevo hash y SQL
   dotnet run --project scripts/HashGenerator/HashGenerator.csproj TuPasswordSeguro123!
   # Copiar el SQL que muestra y ejecutarlo en la BD
   ```

3. **Producción:**
   ```bash
   # 1. Generar hash con contraseña fuerte
   dotnet run --project scripts/HashGenerator/HashGenerator.csproj "Pr0d_S3cur3_P@ssw0rd!"
   
   # 2. Ejecutar el SQL generado
   # 3. Cambiar la contraseña inmediatamente después del primer login
   ```

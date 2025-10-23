# ?? Quick Start - Crear SUPER_ADMIN

## ? Solución al Error 401

El error 401 "invalid password" ocurría porque el hash de la contraseña en el archivo SQL original no era válido.

**Ya está corregido** ? El archivo `create-superadmin.sql` ahora incluye un hash válido generado con la herramienta `HashGenerator`.

## ?? Ejecutar el Script (Elige una opción)

### Opción 1: Copiar y Pegar (Más simple)
1. Abre pgAdmin o DBeaver
2. Conecta a la base de datos `casino_platform`
3. Abre una nueva ventana de consulta SQL
4. Copia todo el contenido del archivo `scripts/create-superadmin.sql`
5. Pégalo y ejecuta (F5 o botón "Ejecutar")

### Opción 2: Línea de Comandos (PowerShell)
```powershell
.\scripts\run-create-superadmin.ps1
```

### Opción 3: Línea de Comandos (Bash/Linux)
```bash
chmod +x scripts/run-create-superadmin.sh
./scripts/run-create-superadmin.sh
```

### Opción 4: psql directo
```bash
psql -h localhost -U postgres -d casino_platform -f scripts/create-superadmin.sql
```

## ?? Credenciales Creadas

- **Username:** `superadmin`
- **Password:** `password`
- **Role:** `SUPER_ADMIN`

## ?? Probar el Login

Después de ejecutar el script, prueba el login:

```bash
curl -X POST http://localhost:5000/api/v1/admin/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "superadmin",
    "password": "password"
  }'
```

Deberías recibir un token JWT válido.

## ?? Si necesitas cambiar la contraseña

```bash
# Genera un nuevo hash con tu contraseña personalizada
dotnet run --project scripts/HashGenerator/HashGenerator.csproj MiPasswordSeguro123!

# Luego copia el SQL que muestra y ejecútalo en la BD
```

## ? Troubleshooting

### El usuario ya existe
```sql
-- Elimina el usuario existente primero
DELETE FROM "BackofficeUsers" WHERE "Username" = 'superadmin';
-- Luego ejecuta el script create-superadmin.sql nuevamente
```

### Aún me da error 401
1. Verifica que el hash en `create-superadmin.sql` sea:
```
   AQAAAAEAACcQAAAAEBKUc5OV3dSOrNs7WQkmOf8id1ddhc4spoaR7E74VWNZoj6kOEoKwLLFxFZN/VF+Qg==
   ```

2. Regenera el hash:
   ```bash
   dotnet run --project scripts/HashGenerator/HashGenerator.csproj password
   ```

3. Copia el SQL completo que muestra y ejecútalo en tu BD

## ?? Más Información

Ver `scripts/README-SUPERADMIN.md` para documentación completa.

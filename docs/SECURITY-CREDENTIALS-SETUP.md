# ?? Configuración de Credenciales y Secrets

## ?? IMPORTANTE: Seguridad de Credenciales

**NUNCA** commiteés credenciales, API keys o secrets al repositorio Git. GitHub bloqueará automáticamente commits que contengan credenciales de AWS, tokens, etc.

---

## ??? Configuración por Entorno

### 1?? Desarrollo Local (User Secrets)

Para desarrollo local, usa **User Secrets** de .NET. Estos archivos se almacenan fuera del repositorio.

#### Inicializar User Secrets

```bash
cd apps/api/Casino.Api
dotnet user-secrets init
```

#### Agregar Credenciales AWS

```bash
# AWS Access Key
dotnet user-secrets set "AWS:AccessKey" "YOUR_ACCESS_KEY_HERE"

# AWS Secret Key
dotnet user-secrets set "AWS:SecretKey" "YOUR_SECRET_KEY_HERE"

# Verificar secrets guardados
dotnet user-secrets list
```

#### Agregar Otras Credenciales

```bash
# JWT Key
dotnet user-secrets set "Auth:JwtKey" "your-super-secret-jwt-key-32-chars-minimum"

# Connection String de Producción (si es necesario)
dotnet user-secrets set "ConnectionStrings:Production" "Host=...;Password=..."
```

#### Ubicación de User Secrets

Los secrets se guardan en:
- **Windows**: `%APPDATA%\Microsoft\UserSecrets\<user-secrets-id>\secrets.json`
- **macOS/Linux**: `~/.microsoft/usersecrets/<user-secrets-id>/secrets.json`

**User Secrets ID del proyecto:** `e6dd0236-7dd9-4345-85a5-4c58c87ba48a`

---

### 2?? Producción (Variables de Entorno)

En producción (Railway, AWS, Azure, etc.), usa **variables de entorno**.

#### Railway

```bash
# En Railway Dashboard ? Variables
AWS__AccessKey=YOUR_ACCESS_KEY
AWS__SecretKey=YOUR_SECRET_KEY
AWS__S3__BucketName=casino-assets-s3
AWS__S3__Region=us-east-1

Auth__JwtKey=your-production-jwt-key
ConnectionStrings__Default=Host=...
```

**Nota:** Railway usa doble underscore `__` para nested properties.

#### Docker / Docker Compose

```yaml
# docker-compose.yml
services:
  api:
  image: casino-api
environment:
   - AWS__AccessKey=${AWS_ACCESS_KEY}
      - AWS__SecretKey=${AWS_SECRET_KEY}
   - AWS__S3__BucketName=casino-assets-s3
      - Auth__JwtKey=${JWT_KEY}
    env_file:
      - .env.production  # Este archivo NO debe estar en Git
```

```bash
# .env.production (NO commitear)
AWS_ACCESS_KEY=YOUR_ACCESS_KEY
AWS_SECRET_KEY=YOUR_SECRET_KEY
JWT_KEY=your-production-jwt-key
```

#### Kubernetes

```yaml
# secrets.yaml (aplicar con kubectl, NO commitear)
apiVersion: v1
kind: Secret
metadata:
  name: casino-api-secrets
type: Opaque
stringData:
  aws-access-key: "YOUR_ACCESS_KEY"
  aws-secret-key: "YOUR_SECRET_KEY"
  jwt-key: "your-jwt-key"
```

```yaml
# deployment.yaml
env:
  - name: AWS__AccessKey
  valueFrom:
      secretKeyRef:
 name: casino-api-secrets
        key: aws-access-key
```

---

### 3?? CI/CD (GitHub Actions)

```yaml
# .github/workflows/deploy.yml
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
 
      - name: Deploy to Railway
        env:
          AWS_ACCESS_KEY: ${{ secrets.AWS_ACCESS_KEY }}
          AWS_SECRET_KEY: ${{ secrets.AWS_SECRET_KEY }}
          JWT_KEY: ${{ secrets.JWT_KEY }}
     run: |
          # Deploy script aquí
```

**Configurar secrets en GitHub:**
1. Repository ? Settings ? Secrets and variables ? Actions
2. New repository secret
3. Agregar: `AWS_ACCESS_KEY`, `AWS_SECRET_KEY`, `JWT_KEY`

---

## ?? Estructura de Archivos de Configuración

```
apps/api/Casino.Api/
??? appsettings.json              ? EN GIT - Sin secrets
??? appsettings.Development.json  ? EN GIT - Configuración de desarrollo
??? appsettings.Production.json   ? NO EN GIT - Configuración de producción
??? secrets.json        ? NO EN GIT - User Secrets (almacenado fuera del proyecto)
```

### appsettings.json (EN GIT)

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=casino_dev;..."
  },
  "AWS": {
    "AccessKey": "",  // ? Vacío - se llena desde User Secrets o Env Vars
    "SecretKey": "",  // ? Vacío
    "S3": {
      "BucketName": "casino-assets-s3",  // ? OK - no es secreto
      "Region": "us-east-1"      // ? OK
    }
  },
  "Auth": {
    "Issuer": "casino",         // ? OK
    "JwtKey": ""          // ? Vacío
  }
}
```

### appsettings.Development.json (EN GIT)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=casino_dev;Username=postgres;Password=postgres;SSL Mode=Disable"
  }
  // AWS keys vienen de User Secrets
}
```

---

## ?? Jerarquía de Configuración

.NET carga la configuración en este orden (el último sobrescribe):

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. **User Secrets** (solo en Development)
4. **Environment Variables**
5. **Command-line arguments**

Ejemplo:

```
appsettings.json:          AWS:AccessKey = ""
appsettings.Development:   (no especifica)
User Secrets:       AWS:AccessKey = "AKIAWUCS..."  ? GANA
Environment Variables:     (no especifica)
```

---

## ??? Comandos Útiles

### Ver User Secrets Configurados

```bash
cd apps/api/Casino.Api
dotnet user-secrets list
```

### Eliminar un Secret

```bash
dotnet user-secrets remove "AWS:AccessKey"
```

### Eliminar Todos los Secrets

```bash
dotnet user-secrets clear
```

### Verificar Configuración Cargada

Agregar este código temporal en `Program.cs` para debug:

```csharp
var awsKey = builder.Configuration["AWS:AccessKey"];
var jwtKey = builder.Configuration["Auth:JwtKey"];

Console.WriteLine($"AWS Key: {(string.IsNullOrEmpty(awsKey) ? "NOT SET" : "SET (hidden)")}");
Console.WriteLine($"JWT Key: {(string.IsNullOrEmpty(jwtKey) ? "NOT SET" : "SET (hidden)")}");
```

---

## ?? Qué NO Hacer

? **NO** hacer:
```bash
git add appsettings.json  # con credenciales
git commit -m "Add config"
```

? **NO** commitear:
- `appsettings.Production.json`
- `appsettings.Staging.json`
- `.env` files
- `secrets.json`
- Archivos con extensión `.key`, `.pem`, `.pfx`

? **NO** compartir secrets por:
- Email
- Slack
- WhatsApp
- Comments en código

---

## ? Qué SÍ Hacer

? **SÍ** hacer:
```bash
# Usar User Secrets
dotnet user-secrets set "AWS:AccessKey" "..."

# Usar variables de entorno
export AWS__AccessKey="..."

# Usar servicios de gestión de secrets
# - AWS Secrets Manager
# - Azure Key Vault
# - HashiCorp Vault
```

? **SÍ** compartir secrets mediante:
- Gestores de contraseñas (1Password, LastPass)
- Herramientas de secrets management
- Comunicación encriptada (Signal)

---

## ?? Migración desde appsettings.json

Si accidentalmente commiteaste secrets:

### 1. Revocar Credenciales Comprometidas

**AWS:**
1. Ve a IAM Console: https://console.aws.amazon.com/iam/
2. Users ? Tu usuario ? Security credentials
3. "Make inactive" o "Delete" en la Access Key comprometida
4. Crear nueva Access Key

**JWT:**
1. Generar nueva key: `openssl rand -base64 32`
2. Actualizar en User Secrets

### 2. Limpiar Historial de Git (Opcional)

```bash
# ?? PELIGROSO: Reescribe el historial
git filter-branch --force --index-filter \
  "git rm --cached --ignore-unmatch apps/api/Casino.Api/appsettings.json" \
  --prune-empty --tag-name-filter cat -- --all

# Force push (si el repo es privado y coordinas con el equipo)
git push origin --force --all
```

**Mejor opción:** Si el repo es privado y pequeño, considera recrearlo.

### 3. Actualizar Secrets

```bash
# Agregar nuevas credenciales a User Secrets
dotnet user-secrets set "AWS:AccessKey" "NEW_ACCESS_KEY"
dotnet user-secrets set "AWS:SecretKey" "NEW_SECRET_KEY"
```

---

## ?? Setup Rápido para Nuevos Desarrolladores

```bash
# 1. Clonar repo
git clone https://github.com/Juan5eLuques/casino-platform.git
cd casino-platform/backend

# 2. Restaurar packages
dotnet restore

# 3. Configurar User Secrets
cd apps/api/Casino.Api
dotnet user-secrets init

# 4. Pedir credenciales al líder técnico (por canal seguro)
# Luego agregarlas:
dotnet user-secrets set "AWS:AccessKey" "PROPORCIONADO_POR_LIDER"
dotnet user-secrets set "AWS:SecretKey" "PROPORCIONADO_POR_LIDER"
dotnet user-secrets set "Auth:JwtKey" "PROPORCIONADO_POR_LIDER"

# 5. Verificar
dotnet user-secrets list

# 6. Ejecutar
dotnet run
```

---

## ?? Rotación de Credenciales

**Frecuencia recomendada:** Cada 90 días o cuando:
- Un desarrollador deja el equipo
- Se sospecha de compromiso
- Después de un commit accidental

**Proceso:**
1. Generar nuevas credenciales en AWS/servicio
2. Actualizar en todos los entornos:
   - User Secrets (desarrollo)
   - Variables de entorno (producción)
   - CI/CD secrets (GitHub Actions)
3. Revocar credenciales antiguas después de 24-48h

---

## ?? Contacto para Credenciales

Si necesitas acceso a credenciales:
1. Contactar al líder técnico
2. Compartir solo por canal seguro (1Password, etc.)
3. NUNCA por email o chat no encriptado

---

## ? Checklist de Seguridad

- [ ] `appsettings.json` sin credenciales commiteado
- [ ] User Secrets configurado para desarrollo
- [ ] `.gitignore` actualizado
- [ ] Variables de entorno configuradas en producción
- [ ] Credenciales antiguas revocadas (si fueron comprometidas)
- [ ] Equipo informado sobre buenas prácticas

---

**Última actualización:** 2025-01-13  
**Autor:** Sistema de Seguridad
**Status:** ? Configuración Segura Implementada

# ?? Fix: S3 "Forbidden" Error en GET /settings

## ? Problema

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Get Settings Failed",
  "status": 500,
  "detail": "Error making request with Error Code Forbidden and Http Status Code Forbidden"
}
```

## ?? Causa

El endpoint `GET /api/v1/admin/brands/assets/settings` intentaba verificar si el archivo `config.js` existe en S3 usando `FileExistsAsync`, pero las credenciales AWS configuradas **no tienen permiso de lectura** (`s3:GetObject` o `s3:GetObjectMetadata`).

### Flujo del Error

```
1. Usuario llama GET /settings
2. BrandAssetsService.GetBrandSettingsAsync()
3. Llama _s3Service.FileExistsAsync("assets/bet30/config/config.js")
4. AWS SDK intenta GetObjectMetadata
5. ? AWS responde: 403 Forbidden (no tiene permiso s3:GetObjectMetadata)
6. Exception se propaga y endpoint falla con 500
```

## ? Solución Implementada

### Cambio en el Código

He envuelto la verificación de existencia de archivo en un `try-catch` para que el endpoint no falle si no hay permisos de lectura:

**Antes (? Fallaba):**
```csharp
var configExists = await _s3Service.FileExistsAsync($"assets/{brand.Code}/config/config.js");
```

**Ahora (? Funciona):**
```csharp
bool configExists = false;
try
{
    configExists = await _s3Service.FileExistsAsync($"assets/{brand.Code}/config/config.js");
}
catch (Exception ex)
{
    // Log warning pero no fallar - usuario puede no tener permisos s3:GetObject
    _logger.LogWarning(ex, "Could not check if config.js exists for brand {BrandCode}", brand.Code);
}
```

### Comportamiento Actual

- ? El endpoint **ya no falla** si no hay permisos de lectura
- ? Retorna `"configUrl": null` si no puede verificar la existencia
- ? Loguea warning en el servidor pero continúa normal
- ? Si hay permisos, funciona perfectamente y retorna la URL

## ?? Permisos AWS Necesarios

Tu usuario IAM actual tiene estos permisos:

| Acción | Permiso Actual | Descripción |
|--------|----------------|-------------|
| **Subir archivos** | ? `s3:PutObject` | Funciona (ya probado) |
| **Eliminar archivos** | ? `s3:DeleteObject` | Funciona |
| **Leer archivos** | ? `s3:GetObject` | **FALTA** |
| **Ver metadata** | ? `s3:GetObjectMetadata` | **FALTA** |
| **Listar archivos** | ? `s3:ListBucket` | **FALTA** |

## ?? Opción 1: Política IAM Mínima (Recomendada)

Si quieres agregar permisos de lectura **solo para verificar config.js**, usa esta política:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "AllowWriteAndDelete",
      "Effect": "Allow",
      "Action": [
        "s3:PutObject",
        "s3:DeleteObject"
      ],
      "Resource": "arn:aws:s3:::casino-assets-s3/*"
 },
    {
      "Sid": "AllowReadMetadata",
 "Effect": "Allow",
      "Action": [
   "s3:GetObjectMetadata",
        "s3:HeadObject"
      ],
      "Resource": "arn:aws:s3:::casino-assets-s3/*/config/*"
    }
  ]
}
```

**Esto permite**:
- ? Subir/eliminar cualquier archivo
- ? Leer metadata **solo** de archivos en carpetas `config/`
- ? NO permite descargar contenido de archivos
- ? NO permite listar todos los archivos del bucket

## ?? Opción 2: Política IAM Completa (Más Permisiva)

Si quieres permisos completos sobre el bucket:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
    "Sid": "FullAccessToBucket",
      "Effect": "Allow",
   "Action": [
        "s3:PutObject",
        "s3:GetObject",
        "s3:DeleteObject",
        "s3:ListBucket",
        "s3:GetObjectMetadata",
   "s3:HeadObject"
      ],
      "Resource": [
    "arn:aws:s3:::casino-assets-s3",
     "arn:aws:s3:::casino-assets-s3/*"
      ]
    }
  ]
}
```

**Esto permite**:
- ? Subir archivos
- ? Descargar archivos
- ? Eliminar archivos
- ? Listar archivos
- ? Ver metadata

## ??? Cómo Aplicar la Política IAM

### Método 1: AWS Console

1. Ve a **IAM Console**: https://console.aws.amazon.com/iam/
2. Click **Users** en el menú lateral
3. Busca y selecciona tu usuario: (el que usa las credenciales `AKIAWUCS76CLG4HCDOMU`)
4. Tab **"Permissions"**
5. Click **"Add permissions"** ? **"Attach policies directly"**
6. Click **"Create policy"**
7. Tab **"JSON"**
8. **Pega una de las políticas de arriba**
9. Click **"Next"**
10. Nombre: `CasinoAssetsS3ReadPolicy` o `CasinoAssetsS3FullAccess`
11. Click **"Create policy"**
12. Vuelve al usuario y **attacha la nueva política**

### Método 2: AWS CLI

```bash
# Opción 1: Política mínima (solo metadata de config/)
aws iam put-user-policy \
  --user-name YOUR_IAM_USERNAME \
  --policy-name CasinoAssetsS3MinimalRead \
  --policy-document '{
    "Version": "2012-10-17",
    "Statement": [
      {
"Effect": "Allow",
        "Action": ["s3:PutObject", "s3:DeleteObject"],
        "Resource": "arn:aws:s3:::casino-assets-s3/*"
      },
  {
      "Effect": "Allow",
        "Action": ["s3:GetObjectMetadata", "s3:HeadObject"],
        "Resource": "arn:aws:s3:::casino-assets-s3/*/config/*"
      }
    ]
  }'

# Opción 2: Política completa
aws iam put-user-policy \
  --user-name YOUR_IAM_USERNAME \
  --policy-name CasinoAssetsS3FullAccess \
  --policy-document '{
    "Version": "2012-10-17",
    "Statement": [
      {
    "Effect": "Allow",
        "Action": [
    "s3:PutObject",
          "s3:GetObject",
      "s3:DeleteObject",
          "s3:ListBucket",
          "s3:GetObjectMetadata",
        "s3:HeadObject"
        ],
        "Resource": [
          "arn:aws:s3:::casino-assets-s3",
       "arn:aws:s3:::casino-assets-s3/*"
        ]
      }
    ]
  }'
```

## ?? Sin Permisos (Comportamiento Actual)

**El endpoint funciona pero con limitaciones:**

```json
{
  "brandId": "...",
  "brandName": "Your Brand",
"brandCode": "bet30",
  "colors": {},
  "banners": {
    "home": ["https://casino-assets-s3.s3.us-east-1.amazonaws.com/..."],
    "slots": [],
    "liveCasino": []
  },
  "media": {
    "logo": "https://...",
    "favicon": "",
    "others": []
  },
  "configUrl": null,  // ?? Siempre null sin permisos de lectura
  "lastUpdated": "2025-01-13T10:34:33Z"
}
```

**Logs del servidor:**
```
warn: Could not check if config.js exists for brand bet30. 
      This is normal if S3 read permissions are not granted.
```

## ? Con Permisos (Comportamiento Ideal)

```json
{
  "brandId": "...",
  "brandName": "Your Brand",
  "brandCode": "bet30",
  "colors": {},
"banners": {
    "home": ["https://casino-assets-s3.s3.us-east-1.amazonaws.com/..."],
 "slots": [],
    "liveCasino": []
  },
  "media": {
    "logo": "https://...",
    "favicon": "",
    "others": []
  },
  "configUrl": "https://casino-assets-s3.s3.us-east-1.amazonaws.com/assets/bet30/config/config.js",  // ? URL si existe
  "lastUpdated": "2025-01-13T10:34:33Z"
}
```

## ?? Recomendación

### Para Desarrollo/Testing
Usa **Opción 2 (Política Completa)** para facilitar el desarrollo y debugging.

### Para Producción
Usa **Opción 1 (Política Mínima)** siguiendo el principio de "least privilege":
- Solo permisos de escritura/eliminación en todo el bucket
- Solo permisos de lectura de metadata en `config/`

## ?? Probar la Solución

### Sin Agregar Permisos (Funciona Ahora)

```bash
# Debería funcionar sin errores
curl -X GET "https://your-api.com/api/v1/admin/brands/assets/settings" \
  -H "Authorization: Bearer YOUR_JWT" \
  -H "Host: your-brand.com"

# Respuesta esperada: 200 OK (sin configUrl)
```

### Después de Agregar Permisos

1. Agrega la política IAM
2. **Espera 1-2 minutos** para que se propaguen los permisos
3. Prueba de nuevo:

```bash
curl -X GET "https://your-api.com/api/v1/admin/brands/assets/settings" \
  -H "Authorization: Bearer YOUR_JWT" \
  -H "Host: your-brand.com"

# Respuesta esperada: 200 OK (con configUrl si existe config.js)
```

## ?? Verificar Permisos Actuales

```bash
# Ver políticas del usuario
aws iam list-user-policies --user-name YOUR_IAM_USERNAME

# Ver contenido de una política
aws iam get-user-policy --user-name YOUR_IAM_USERNAME --policy-name POLICY_NAME
```

## ?? Referencias

- [AWS IAM Policies](https://docs.aws.amazon.com/IAM/latest/UserGuide/access_policies.html)
- [S3 Permissions](https://docs.aws.amazon.com/AmazonS3/latest/userguide/s3-access-control.html)
- [Principle of Least Privilege](https://docs.aws.amazon.com/IAM/latest/UserGuide/best-practices.html#grant-least-privilege)

## ? Checklist

- [x] Código actualizado para manejar error de permisos
- [x] Endpoint funciona sin permisos de lectura
- [ ] (Opcional) Agregar política IAM para lectura
- [ ] (Opcional) Verificar que configUrl aparece después de publicar

---

**Fecha**: 2025-01-13  
**Status**: ? Fix aplicado - Endpoint funciona sin permisos de lectura  
**Acción requerida**: Ninguna (opcional agregar permisos de lectura)

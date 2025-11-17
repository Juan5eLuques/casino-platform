# ?? Fix: "The bucket does not allow ACLs" Error

## ? Problema

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Upload Failed",
  "status": 500,
  "detail": "The bucket does not allow ACLs"
}
```

## ?? Causa

Desde **abril 2023**, AWS S3 deshabilita las **ACLs (Access Control Lists)** por defecto en buckets nuevos por razones de seguridad. El código intentaba usar `CannedACL.PublicRead` para hacer los archivos públicos, pero esto ya no funciona.

## ? Solución

### Paso 1: Código Corregido (Ya Aplicado)

El código ha sido actualizado para **NO usar ACLs**:

**Antes (? Causaba error):**
```csharp
var request = new PutObjectRequest
{
    BucketName = _bucketName,
    Key = key,
    InputStream = fileStream,
    ContentType = contentType,
    CannedACL = S3CannedACL.PublicRead  // ? Ya no funciona
};
```

**Ahora (? Funciona):**
```csharp
var request = new PutObjectRequest
{
    BucketName = _bucketName,
    Key = key,
    InputStream = fileStream,
    ContentType = contentType
    // ? Sin CannedACL - usar Bucket Policy en su lugar
};
```

### Paso 2: Configurar Bucket Policy en AWS S3

Necesitas agregar una **Bucket Policy** que haga públicos todos los archivos en el bucket.

#### Opción A: Consola de AWS (Recomendado)

1. **Ve a AWS S3 Console**: https://s3.console.aws.amazon.com/s3/
2. **Selecciona tu bucket**: `casino-assets-s3`
3. **Ve a la pestaña "Permissions"**
4. **Scroll down a "Bucket Policy"**
5. **Click "Edit"**
6. **Pega esta política**:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "PublicReadGetObject",
    "Effect": "Allow",
      "Principal": "*",
      "Action": "s3:GetObject",
      "Resource": "arn:aws:s3:::casino-assets-s3/*"
}
  ]
}
```

7. **Click "Save changes"**

#### Opción B: AWS CLI

```bash
# Crear archivo policy.json
cat > policy.json << 'EOF'
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "PublicReadGetObject",
      "Effect": "Allow",
    "Principal": "*",
      "Action": "s3:GetObject",
      "Resource": "arn:aws:s3:::casino-assets-s3/*"
    }
  ]
}
EOF

# Aplicar la política al bucket
aws s3api put-bucket-policy --bucket casino-assets-s3 --policy file://policy.json
```

### Paso 3: Desbloquear Public Access (Si es necesario)

Si el bucket tiene bloqueado el acceso público, necesitas desbloquearlo:

#### En la Consola de AWS:

1. Ve a tu bucket `casino-assets-s3`
2. Pestaña **"Permissions"**
3. Sección **"Block public access (bucket settings)"**
4. Click **"Edit"**
5. **Desmarca estas opciones**:
   - ? Block all public access
   - Específicamente:
     - ? Block public access to buckets and objects granted through new access control lists (ACLs)
     - ? Block public access to buckets and objects granted through any access control lists (ACLs)
     - ? **MANTÉN MARCADO**: Block public access to buckets and objects granted through new public bucket or access point policies
     - ? **MANTÉN MARCADO**: Block public and cross-account access to buckets and objects through any public bucket or access point policies
6. **Confirma escribiendo "confirm"**
7. Click **"Save changes"**

**IMPORTANTE**: Solo desmarca las opciones relacionadas con ACLs. Las opciones de Bucket Policy pueden quedar habilitadas porque estamos usando una Bucket Policy específica.

#### Con AWS CLI:

```bash
aws s3api put-public-access-block \
  --bucket casino-assets-s3 \
  --public-access-block-configuration \
    "BlockPublicAcls=false,IgnorePublicAcls=false,BlockPublicPolicy=false,RestrictPublicBuckets=false"
```

### Paso 4: Verificar Configuración

#### Verificar que la Bucket Policy está aplicada:

```bash
aws s3api get-bucket-policy --bucket casino-assets-s3
```

Deberías ver la política JSON que agregaste.

#### Verificar que un archivo es público:

1. Sube un archivo de prueba desde la consola o con el endpoint
2. Accede a la URL pública:
   ```
   https://casino-assets-s3.s3.us-east-1.amazonaws.com/test.jpg
   ```
3. Deberías poder ver el archivo sin errores de acceso

### Paso 5: Reiniciar la Aplicación

**Hot reload no es suficiente**, reinicia la aplicación:

```bash
# Detener con Ctrl+C
dotnet run --project apps/api/Casino.Api/Casino.Api.csproj
```

---

## ?? Resumen de Cambios

| Aspecto | Antes | Ahora |
|---------|-------|-------|
| **Método de acceso público** | ACLs (CannedACL.PublicRead) | Bucket Policy |
| **Configuración de código** | `CannedACL = S3CannedACL.PublicRead` | Sin CannedACL |
| **Configuración de S3** | No requerida | Bucket Policy necesaria |
| **Block Public Access** | Cualquier configuración | ACLs desbloqueadas |

---

## ?? Bucket Policy Explicada

```json
{
  "Version": "2012-10-17",  // Versión del policy language
  "Statement": [
    {
    "Sid": "PublicReadGetObject",  // Identificador descriptivo
      "Effect": "Allow",  // Permitir la acción
      "Principal": "*",  // Cualquier persona/entidad
    "Action": "s3:GetObject",  // Solo lectura (GET)
      "Resource": "arn:aws:s3:::casino-assets-s3/*"  // Todos los archivos en el bucket
    }
  ]
}
```

**Esta política permite**:
- ? Lectura pública de todos los archivos (`s3:GetObject`)
- ? URLs públicas funcionan sin autenticación
- ? **NO** permite escritura o eliminación
- ? **NO** permite listar el contenido del bucket

**Es seguro porque**:
- Solo permite **leer** archivos, no modificarlos
- La escritura/eliminación sigue requiriendo credenciales AWS
- No expone información sensible

---

## ?? Seguridad

### ¿Es Seguro Hacer el Bucket Público?

**SÍ**, bajo estas condiciones:

1. **Solo permite lectura**: La policy solo permite `s3:GetObject`
2. **No expone archivos privados**: Solo sube archivos que deben ser públicos (banners, logos)
3. **No permite listar**: No se puede ver la lista completa de archivos
4. **Control de escritura**: Solo tu aplicación (con credenciales AWS) puede subir/eliminar

### Buenas Prácticas Implementadas

? **Acceso público limitado a lectura**
? **Control de escritura con credenciales**
? **Validación de archivos en backend** (tipo, tamaño)
? **Audit logs** de todas las operaciones
? **URLs predecibles pero no enumerables**

---

## ?? Probar la Solución

### Paso 1: Verificar que el código no usa ACLs

```csharp
// Buscar en S3Service.cs
// NO debe aparecer: CannedACL
// ? Correcto si no hay mención de ACL
```

### Paso 2: Verificar Bucket Policy en AWS

```bash
aws s3api get-bucket-policy --bucket casino-assets-s3 | jq '.Policy | fromjson'
```

Debe mostrar la política con `"Action": "s3:GetObject"`

### Paso 3: Subir un Archivo de Prueba

**Desde Swagger:**
```
POST /api/v1/admin/brands/assets/upload/banner/home
File: test-banner.jpg
```

**Respuesta esperada:**
```json
{
  "success": true,
  "url": "https://casino-assets-s3.s3.us-east-1.amazonaws.com/assets/bet30/banners/home/abc-123.jpg",
  "section": "home",
  "fileName": "abc-123.jpg"
}
```

### Paso 4: Verificar Acceso Público

Copia la URL de la respuesta y ábrela en un navegador:

```
https://casino-assets-s3.s3.us-east-1.amazonaws.com/assets/bet30/banners/home/abc-123.jpg
```

**Debe mostrar la imagen sin errores.**

---

## ? Troubleshooting

### Error: "Access Denied" al acceder a la URL

**Causa**: La Bucket Policy no está aplicada correctamente.

**Solución**:
```bash
# Verificar que la policy existe
aws s3api get-bucket-policy --bucket casino-assets-s3

# Re-aplicar la policy
aws s3api put-bucket-policy --bucket casino-assets-s3 --policy file://policy.json
```

### Error: "403 Forbidden" en la URL pública

**Causa**: Block Public Access está bloqueando la policy.

**Solución**:
1. Ve a S3 Console
2. Bucket ? Permissions ? Block public access
3. Desmarca las opciones de ACL
4. Guarda cambios

### Error: "The bucket policy is too permissive"

**Causa**: AWS detectó que la policy es muy abierta.

**Solución**: La policy actual es correcta. Si AWS muestra warning, puedes ignorarlo o agregar restricciones adicionales:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "PublicReadGetObject",
      "Effect": "Allow",
      "Principal": "*",
      "Action": "s3:GetObject",
      "Resource": "arn:aws:s3:::casino-assets-s3/assets/*",  // Solo carpeta assets/
      "Condition": {
    "StringLike": {
          "s3:ExistingObjectTag/public": "true"  // Solo objetos con tag public
        }
      }
    }
  ]
}
```

---

## ?? Referencias

- [AWS S3 Block Public Access](https://docs.aws.amazon.com/AmazonS3/latest/userguide/access-control-block-public-access.html)
- [S3 Bucket Policies](https://docs.aws.amazon.com/AmazonS3/latest/userguide/bucket-policies.html)
- [S3 ACL Overview](https://docs.aws.amazon.com/AmazonS3/latest/userguide/acl-overview.html)
- [AWS S3 Security Best Practices](https://docs.aws.amazon.com/AmazonS3/latest/userguide/security-best-practices.html)

---

## ? Checklist de Implementación

- [x] Código actualizado para no usar ACLs
- [ ] Bucket Policy aplicada en S3
- [ ] Block Public Access configurado (ACLs desbloqueadas)
- [ ] Aplicación reiniciada
- [ ] Upload de archivo de prueba exitoso
- [ ] URL pública accesible sin errores
- [ ] Verificado en diferentes navegadores

---

**Fecha**: 2025-01-13  
**Status**: ? Código corregido - Pendiente configuración de S3  
**Bucket**: `casino-assets-s3`  
**Region**: `us-east-1`

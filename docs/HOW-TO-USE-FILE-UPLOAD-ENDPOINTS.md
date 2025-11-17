# ?? Cómo Usar los Endpoints de Upload de Archivos

## ? Problema: "File is required"

Estás recibiendo este error aunque estés seleccionando un archivo. Esto ocurre porque el **nombre del campo** del archivo en el formulario debe ser exactamente **`file`**.

---

## ? Solución por Plataforma

### 1?? Swagger UI

1. **Abrir Swagger**: Navega a `https://your-api.com/swagger`
2. **Buscar el endpoint**: `POST /api/v1/admin/brands/assets/upload/banner/{section}`
3. **Click "Try it out"**
4. **Rellenar parámetros**:
   - `section`: Selecciona `home`, `slots`, o `live-casino`
   - **Headers** (si es necesario):
     - `Host`: `your-brand.com`
5. **Subir archivo**:
   - Click en "Choose File" o "Browse"
   - Selecciona tu imagen
   - **IMPORTANTE**: El campo se llama automáticamente `file` en Swagger
6. **Click "Execute"**

**? Debe funcionar correctamente en Swagger UI**

---

### 2?? Postman

#### Configuración Correcta

1. **Method**: `POST`
2. **URL**: `https://your-api.com/api/v1/admin/brands/assets/upload/banner/home`
3. **Headers**:
   ```
   Authorization: Bearer YOUR_JWT_TOKEN
   Host: your-brand.com
   ```
4. **Body**:
   - Tipo: **`form-data`** (NO `binary`, NO `raw`)
   - **KEY**: `file` (?? **DEBE SER EXACTAMENTE "file"**)
   - **TYPE**: `File` (selecciona del dropdown)
   - **VALUE**: Click "Select Files" y elige tu imagen

#### ? Configuración Incorrecta (No Funciona)

```
Body Type: binary
Body Type: raw
Key name: "image" ?
Key name: "photo" ?
Key name: "upload" ?
```

#### ? Configuración Correcta (Funciona)

```
Body Type: form-data ?
Key: file ?
Type: File ?
```

**Captura de pantalla conceptual:**
```
???????????????????????????????????????????
? Body         ?
?  ? none  ? form-data  ? x-www-form...  ?
?         ?
? KEY        TYPE    VALUE        ?
? ?????? ??????  ????????????????    ?
? ?file?    ?File?  ?Select Files...?    ?
? ??????    ??????  ????????????????    ?
?           banner.jpg   ?
???????????????????????????????????????????
```

---

### 3?? cURL (Terminal/CMD)

#### ? Comando Correcto

```bash
# Upload banner (home section)
curl -X POST "https://your-api.com/api/v1/admin/brands/assets/upload/banner/home" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Host: your-brand.com" \
  -F "file=@/path/to/banner.jpg"
  
# Upload logo
curl -X POST "https://your-api.com/api/v1/admin/brands/assets/upload/media/logo" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Host: your-brand.com" \
  -F "file=@/path/to/logo.png"

# Upload favicon
curl -X POST "https://your-api.com/api/v1/admin/brands/assets/upload/media/favicon" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Host: your-brand.com" \
  -F "file=@/path/to/favicon.ico"
```

**Explicación del comando `-F`:**
- `-F "file=@/path/to/file.jpg"` = **Form field con nombre "file"**
- El `@` indica que es un archivo
- **NO cambies "file" por otro nombre**

#### ? Comandos Incorrectos

```bash
# ? Campo con nombre incorrecto
curl -F "image=@banner.jpg" ...

# ? Usando --data-binary (no es multipart/form-data)
curl --data-binary @banner.jpg ...

# ? Falta el @ antes de la ruta
curl -F "file=banner.jpg" ...
```

---

### 4?? Insomnia

1. **Method**: `POST`
2. **URL**: `https://your-api.com/api/v1/admin/brands/assets/upload/banner/home`
3. **Headers**:
   ```
   Authorization: Bearer YOUR_JWT_TOKEN
   Host: your-brand.com
   ```
4. **Body**:
   - Type: **`Multipart Form`**
   - Click **"Add"**
   - **Name**: `file` (?? **DEBE SER "file"**)
   - **Type**: `File`
   - **Value**: Selecciona tu archivo

---

### 5?? JavaScript (Fetch API)

```javascript
// Crear FormData con el archivo
const formData = new FormData();
const fileInput = document.querySelector('input[type="file"]');
formData.append('file', fileInput.files[0]);  // ?? NOMBRE DEBE SER "file"

// Hacer el request
const response = await fetch('https://your-api.com/api/v1/admin/brands/assets/upload/banner/home', {
  method: 'POST',
  headers: {
'Authorization': `Bearer ${jwtToken}`,
    'Host': 'your-brand.com'
    // NO incluir Content-Type, el navegador lo configura automáticamente
  },
  body: formData
});

const result = await response.json();
console.log(result);
```

**? Error Común:**
```javascript
// ? Nombre incorrecto
formData.append('image', file);

// ? Nombre correcto
formData.append('file', file);
```

---

### 6?? React + Axios

```tsx
import axios from 'axios';

const UploadBanner = () => {
  const handleUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    const formData = new FormData();
    formData.append('file', file);  // ?? NOMBRE DEBE SER "file"

    try {
      const response = await axios.post(
        'https://your-api.com/api/v1/admin/brands/assets/upload/banner/home',
      formData,
        {
   headers: {
         'Authorization': `Bearer ${jwtToken}`,
            'Host': 'your-brand.com',
            'Content-Type': 'multipart/form-data'
  }
        }
      );
      
      console.log('Upload successful:', response.data);
    } catch (error) {
      console.error('Upload failed:', error);
    }
  };

  return (
    <input 
      type="file" 
      accept="image/*"
      onChange={handleUpload}
    />
  );
};
```

---

## ?? Debugging: ¿Por Qué No Funciona?

### Verificar Request en DevTools (Chrome/Edge)

1. Abre **DevTools** (F12)
2. Ve a la pestaña **Network**
3. Haz el upload
4. Click en el request
5. Ve a la pestaña **Payload** o **Request**
6. Busca la sección **Form Data**
7. **Debe aparecer**:
   ```
   file: (binary data) banner.jpg
   ```

### Verificar en Logs del Backend

Si el request llega pero falla, verifica:

```csharp
// El endpoint hace esta validación
if (!request.HasFormContentType)
    return BadRequest("Invalid content type");

var file = request.Form.Files.GetFile("file");
if (file == null)
    return BadRequest("File is required. Field name must be 'file'");
```

---

## ?? Checklist de Verificación

Antes de hacer el upload, verifica:

- [ ] El método es `POST` (NO `GET`, NO `PUT`)
- [ ] La URL es correcta (`/upload/banner/{section}` o `/upload/media/{type}`)
- [ ] El `Content-Type` es `multipart/form-data`
- [ ] El campo del archivo se llama exactamente **`file`**
- [ ] El archivo está seleccionado correctamente
- [ ] Los headers de autenticación están incluidos
- [ ] El header `Host` está configurado (si usas brand resolution)
- [ ] El archivo cumple con las validaciones:
  - Max 5MB
  - Formato: JPG, PNG, GIF, WebP, SVG

---

## ?? Ejemplos Completos por Endpoint

### Upload Home Banner

**cURL:**
```bash
curl -X POST "https://api.example.com/api/v1/admin/brands/assets/upload/banner/home" \
  -H "Authorization: Bearer eyJhbGc..." \
  -H "Host: bet30.com" \
  -F "file=@banner-home.jpg"
```

**Postman:**
```
POST /api/v1/admin/brands/assets/upload/banner/home
Headers:
  Authorization: Bearer eyJhbGc...
  Host: bet30.com
Body (form-data):
  file: [Select banner-home.jpg]
```

### Upload Logo

**cURL:**
```bash
curl -X POST "https://api.example.com/api/v1/admin/brands/assets/upload/media/logo" \
  -H "Authorization: Bearer eyJhbGc..." \
  -H "Host: bet30.com" \
  -F "file=@logo.png"
```

**Postman:**
```
POST /api/v1/admin/brands/assets/upload/media/logo
Headers:
  Authorization: Bearer eyJhbGc...
  Host: bet30.com
Body (form-data):
  file: [Select logo.png]
```

### Upload Favicon

**cURL:**
```bash
curl -X POST "https://api.example.com/api/v1/admin/brands/assets/upload/media/favicon" \
  -H "Authorization: Bearer eyJhbGc..." \
  -H "Host: bet30.com" \
-F "file=@favicon.ico"
```

---

## ?? Mensajes de Error y Soluciones

| Error | Causa | Solución |
|-------|-------|----------|
| `"File is required"` | Campo no se llama "file" | Renombrar a `file` |
| `"Invalid content type"` | No es multipart/form-data | Usar form-data en Postman |
| `"File size exceeds 5MB limit"` | Archivo muy grande | Comprimir imagen |
| `"Invalid file extension"` | Formato no permitido | Usar JPG, PNG, GIF, WebP, SVG |
| `"Brand context not resolved"` | Falta header Host | Agregar `Host: your-brand.com` |
| `401 Unauthorized` | JWT inválido o expirado | Renovar token |
| `"Maximum 5 banners per section"` | Límite alcanzado | Eliminar banners antiguos |

---

## ? Respuesta Exitosa

Cuando funciona correctamente, deberías recibir:

```json
{
  "success": true,
  "url": "https://casino-assets-s3.s3.us-east-1.amazonaws.com/assets/bet30/banners/home/abc-123.jpg",
  "section": "home",
  "fileName": "abc-123.jpg",
  "totalBannersInSection": 2
}
```

---

## ?? Si Sigue Sin Funcionar

1. **Reinicia la aplicación** (Hot reload puede no aplicar cambios)
2. **Verifica los logs del servidor**
3. **Usa cURL primero** (es más confiable que Postman para debugging)
4. **Verifica que S3 esté configurado correctamente**
5. **Confirma que las credenciales AWS son válidas**

---

**Fecha**: 2025-01-13  
**Status**: ? Fix aplicado - Campo debe ser "file"

# 🎨 Brand Assets API - Guía de Integración para Frontend

## 📋 Tabla de Contenidos

1. [Resumen del Sistema](#resumen-del-sistema)
2. [Autenticación](#autenticación)
3. [Obtener Configuración Actual](#obtener-configuración-actual)
4. [Backoffice: Logo y Favicon](#backoffice-logo-y-favicon)
5. [Frontend de Jugadores: Banners y Colores](#frontend-de-jugadores-banners-y-colores)
6. [Publicar Configuración](#publicar-configuración)
7. [Eliminar Assets](#eliminar-assets)
8. [Ejemplos Completos](#ejemplos-completos)
9. [Manejo de Errores](#manejo-de-errores)

---

## 📌 Resumen del Sistema

El sistema de Brand Assets permite gestionar:

### Para **Backoffice** (Panel de Administración):
- **Logo**: Imagen principal del brand (header, sidebar)
- **Favicon**: Icono del navegador

### Para **Frontend de Jugadores** (Sitio público):
- **Banners**: Imágenes promocionales por sección (home, slots, live-casino)
- **Colores**: Paleta de colores CSS variables
- **Logo/Favicon**: Mismos archivos que backoffice

### Características:
- ✅ Almacenamiento en AWS S3
- ✅ URLs públicas (CDN-ready)
- ✅ Validación de archivos (tipo, tamaño)
- ✅ Audit logs automáticos
- ✅ Multi-brand support

---

## 🔐 Autenticación

Todos los endpoints requieren autenticación JWT de backoffice.

### Headers Requeridos

```http
Authorization: Bearer YOUR_JWT_TOKEN
Host: your-brand.com
Content-Type: application/json (excepto file uploads)
```

### Obtener Token JWT

```http
POST /api/v1/auth/backoffice/login
Content-Type: application/json

{
  "username": "admin",
  "password": "your-password"
}
```

**Respuesta:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-01-14T10:00:00Z",
  "user": {
    "id": "...",
  "username": "admin",
    "role": "BRAND_ADMIN"
  }
}
```

---

## 📊 Obtener Configuración Actual

### Endpoint: GET /api/v1/admin/brands/assets/settings

Obtiene toda la configuración actual del brand (logo, favicon, banners, colores).

### Request

```http
GET /api/v1/admin/brands/assets/settings
Authorization: Bearer YOUR_JWT_TOKEN
Host: your-brand.com
```

### Response

```json
{
  "brandId": "11111111-1111-1111-1111-111111111111",
  "brandName": "My Casino",
  "brandCode": "mycasino",
  "colors": {
    "--color-primary": "#ffb300",
    "--color-secondary": "#2196f3",
    "--color-accent": "#e91e63",
    "--color-background": "#121212",
    "--color-text": "#ffffff"
  },
  "banners": {
    "home": [
      "https://casino-assets-s3.s3.us-east-1.amazonaws.com/assets/mycasino/banners/home/uuid1.jpg",
      "https://casino-assets-s3.s3.us-east-1.amazonaws.com/assets/mycasino/banners/home/uuid2.jpg"
    ],
    "slots": [
    "https://casino-assets-s3.s3.us-east-1.amazonaws.com/assets/mycasino/banners/slots/uuid3.jpg"
    ],
    "liveCasino": []
  },
  "media": {
  "logo": "https://casino-assets-s3.s3.us-east-1.amazonaws.com/assets/mycasino/banners/media/logo.png",
    "favicon": "https://casino-assets-s3.s3.us-east-1.amazonaws.com/assets/mycasino/banners/media/favicon.ico",
  "others": []
  },
  "configUrl": "https://casino-assets-s3.s3.us-east-1.amazonaws.com/assets/mycasino/config/config.js",
"lastUpdated": "2025-01-13T10:34:33Z"
}
```

### Uso en Backoffice

```typescript
// React/Vue/Angular example
async function loadBrandAssets() {
  const response = await fetch('/api/v1/admin/brands/assets/settings', {
    headers: {
      'Authorization': `Bearer ${getToken()}`,
      'Host': 'your-brand.com'
    }
  });
  
  const data = await response.json();
  
  // Actualizar logo en header
  document.querySelector('.header-logo').src = data.media.logo;
  
  // Actualizar favicon
  document.querySelector('link[rel="icon"]').href = data.media.favicon;
}
```

### Uso en Frontend de Jugadores

```html
<!-- Cargar config.js generado -->
<script src="https://casino-assets-s3.s3.us-east-1.amazonaws.com/assets/mycasino/config/config.js"></script>

<script>
  // Aplicar colores CSS
  Object.entries(window.gColors).forEach(([key, value]) => {
    document.documentElement.style.setProperty(key, value);
  });
  
  // Cargar banners de home
  const banners = window.gHomeBannersDesktop;
  banners.forEach(url => {
    // Crear carousel slide
  });
  
  // Actualizar logo
  document.querySelector('.logo').src = window.gLogo;
  document.querySelector('link[rel="icon"]').href = window.gFavicon;
</script>
```

---

## 🖼️ Backoffice: Logo y Favicon

### 1. Subir Logo

**Endpoint:** `POST /api/v1/admin/brands/assets/upload/media/logo`

**Restricciones:**
- Formatos: JPG, PNG, SVG, WebP
- Tamaño máximo: 5MB
- Se reemplaza el logo anterior automáticamente

**Request:**

```http
POST /api/v1/admin/brands/assets/upload/media/logo
Authorization: Bearer YOUR_JWT_TOKEN
Host: your-brand.com
Content-Type: multipart/form-data

file: [Binary file data]
```

**cURL Example:**

```bash
curl -X POST "https://your-api.com/api/v1/admin/brands/assets/upload/media/logo" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Host: your-brand.com" \
  -F "file=@logo.png"
```

**JavaScript/Fetch Example:**

```typescript
async function uploadLogo(file: File) {
  const formData = new FormData();
  formData.append('file', file);
  
  const response = await fetch('/api/v1/admin/brands/assets/upload/media/logo', {
    method: 'POST',
    headers: {
  'Authorization': `Bearer ${getToken()}`,
      'Host': 'your-brand.com'
    },
body: formData
  });
  
  const result = await response.json();
  return result;
}

// Uso con input file
document.querySelector('#logo-input').addEventListener('change', async (e) => {
  const file = e.target.files[0];
  const result = await uploadLogo(file);
  
  // Actualizar preview
  document.querySelector('#logo-preview').src = result.url;
  
  toast.success('Logo actualizado exitosamente');
});
```

**Response:**

```json
{
  "success": true,
  "url": "https://casino-assets-s3.s3.us-east-1.amazonaws.com/assets/mycasino/banners/media/logo.png",
  "type": "logo",
  "fileName": "logo.png"
}
```

---

### 2. Subir Favicon

**Endpoint:** `POST /api/v1/admin/brands/assets/upload/media/favicon`

**Restricciones:**
- Formatos recomendados: ICO, PNG (16x16, 32x32, 48x48)
- Tamaño máximo: 5MB
- Se reemplaza el favicon anterior automáticamente

**Request:**

```http
POST /api/v1/admin/brands/assets/upload/media/favicon
Authorization: Bearer YOUR_JWT_TOKEN
Host: your-brand.com
Content-Type: multipart/form-data

file: [Binary file data]
```

**JavaScript Example:**

```typescript
async function uploadFavicon(file: File) {
  const formData = new FormData();
  formData.append('file', file);
  
  const response = await fetch('/api/v1/admin/brands/assets/upload/media/favicon', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${getToken()}`,
    'Host': 'your-brand.com'
    },
    body: formData
  });
  
  return await response.json();
}
```

**Response:**

```json
{
  "success": true,
  "url": "https://casino-assets-s3.s3.us-east-1.amazonaws.com/assets/mycasino/banners/media/favicon.ico",
  "type": "favicon",
  "fileName": "favicon.ico"
}
```

---

### 3. Eliminar Logo

**Endpoint:** `DELETE /api/v1/admin/brands/assets/media/logo`

**Request:**

```http
DELETE /api/v1/admin/brands/assets/media/logo
Authorization: Bearer YOUR_JWT_TOKEN
Host: your-brand.com
```

**JavaScript Example:**

```typescript
async function deleteLogo() {
  const response = await fetch('/api/v1/admin/brands/assets/media/logo', {
    method: 'DELETE',
  headers: {
      'Authorization': `Bearer ${getToken()}`,
      'Host': 'your-brand.com'
    }
  });
  
  const result = await response.json();
  return result;
}
```

**Response:**

```json
{
  "success": true,
  "message": "Media deleted successfully"
}
```

---

### 4. Eliminar Favicon

**Endpoint:** `DELETE /api/v1/admin/brands/assets/media/favicon`

**Request:**

```http
DELETE /api/v1/admin/brands/assets/media/favicon
Authorization: Bearer YOUR_JWT_TOKEN
Host: your-brand.com
```

**Response:**

```json
{
  "success": true,
  "message": "Media deleted successfully"
}
```

---

## 🎬 Frontend de Jugadores: Banners y Colores

### 1. Subir Banner

**Endpoint:** `POST /api/v1/admin/brands/assets/upload/banner/{section}`

**Secciones disponibles:**
- `home` - Página principal
- `slots` - Sección de slots
- `live-casino` - Sección de casino en vivo

**Restricciones:**
- Formatos: JPG, PNG, GIF, WebP
- Tamaño máximo: 5MB
- **Máximo 5 banners por sección**

**Request:**

```http
POST /api/v1/admin/brands/assets/upload/banner/home
Authorization: Bearer YOUR_JWT_TOKEN
Host: your-brand.com
Content-Type: multipart/form-data

file: [Binary file data]
```

**JavaScript Example:**

```typescript
async function uploadBanner(section: 'home' | 'slots' | 'live-casino', file: File) {
  const formData = new FormData();
  formData.append('file', file);
  
  const response = await fetch(`/api/v1/admin/brands/assets/upload/banner/${section}`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${getToken()}`,
      'Host': 'your-brand.com'
    },
    body: formData
  });
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.error || 'Upload failed');
  }
  
  return await response.json();
}

// Ejemplo de uso con drag & drop
const dropZone = document.querySelector('#home-banners-dropzone');

dropZone.addEventListener('drop', async (e) => {
  e.preventDefault();
  const files = Array.from(e.dataTransfer.files);
  
  for (const file of files) {
    try {
      const result = await uploadBanner('home', file);
      addBannerToList(result);
      toast.success(`Banner subido: ${result.fileName}`);
    } catch (error) {
      toast.error(error.message);
    }
  }
});
```

**Response:**

```json
{
  "success": true,
  "url": "https://casino-assets-s3.s3.us-east-1.amazonaws.com/assets/mycasino/banners/home/uuid-123.jpg",
  "section": "home",
  "fileName": "uuid-123.jpg",
  "totalBannersInSection": 2
}
```

---

### 2. Eliminar Banner

**Endpoint:** `DELETE /api/v1/admin/brands/assets/banner/{section}/{fileName}`

**Request:**

```http
DELETE /api/v1/admin/brands/assets/banner/home/uuid-123.jpg
Authorization: Bearer YOUR_JWT_TOKEN
Host: your-brand.com
```

**JavaScript Example:**

```typescript
async function deleteBanner(section: string, fileName: string) {
  const response = await fetch(
    `/api/v1/admin/brands/assets/banner/${section}/${fileName}`,
    {
      method: 'DELETE',
    headers: {
        'Authorization': `Bearer ${getToken()}`,
        'Host': 'your-brand.com'
      }
    }
  );
  
  return await response.json();
}

// Uso con botón de eliminar
document.querySelectorAll('.delete-banner-btn').forEach(btn => {
  btn.addEventListener('click', async (e) => {
    const section = e.target.dataset.section;
    const fileName = e.target.dataset.fileName;
    
    if (confirm('¿Eliminar este banner?')) {
      await deleteBanner(section, fileName);
      e.target.closest('.banner-item').remove();
      toast.success('Banner eliminado');
    }
  });
});
```

**Response:**

```json
{
  "success": true,
  "message": "Banner deleted successfully"
}
```

---

### 3. Actualizar Colores

**Endpoint:** `PUT /api/v1/admin/brands/assets/colors`

**Request:**

```http
PUT /api/v1/admin/brands/assets/colors
Authorization: Bearer YOUR_JWT_TOKEN
Host: your-brand.com
Content-Type: application/json

{
  "colors": {
    "--color-primary": "#ffb300",
    "--color-secondary": "#2196f3",
    "--color-accent": "#e91e63",
    "--color-background": "#121212",
    "--color-surface": "#1e1e1e",
    "--color-text": "#ffffff",
    "--color-text-secondary": "#b0b0b0",
    "--color-success": "#4caf50",
    "--color-error": "#f44336",
    "--color-warning": "#ff9800"
  }
}
```

**JavaScript Example:**

```typescript
interface ColorPalette {
  [key: string]: string;
}

async function updateColors(colors: ColorPalette) {
  const response = await fetch('/api/v1/admin/brands/assets/colors', {
    method: 'PUT',
    headers: {
   'Authorization': `Bearer ${getToken()}`,
      'Host': 'your-brand.com',
  'Content-Type': 'application/json'
    },
    body: JSON.stringify({ colors })
  });
  
  return await response.json();
}

// Ejemplo con color picker
const colorInputs = document.querySelectorAll('.color-picker');
const colors: ColorPalette = {};

colorInputs.forEach(input => {
  input.addEventListener('change', (e) => {
    const varName = e.target.dataset.varName;
    colors[varName] = e.target.value;
  });
});

document.querySelector('#save-colors-btn').addEventListener('click', async () => {
  const result = await updateColors(colors);
  
  // Aplicar colores en preview
  Object.entries(colors).forEach(([key, value]) => {
    document.documentElement.style.setProperty(key, value);
  });
  
  toast.success('Colores actualizados exitosamente');
});
```

**Response:**

```json
{
  "brandId": "...",
  "brandName": "My Casino",
  "brandCode": "mycasino",
  "colors": {
    "--color-primary": "#ffb300",
    "--color-secondary": "#2196f3",
    "--color-accent": "#e91e63",
    "--color-background": "#121212",
  "--color-text": "#ffffff"
  },
  "banners": { ... },
  "media": { ... },
  "configUrl": "...",
  "lastUpdated": "2025-01-13T12:45:00Z"
}
```

---

## 📤 Publicar Configuración

### Endpoint: POST /api/v1/admin/brands/assets/publish-config

Genera el archivo `config.js` con toda la configuración (colores, banners, logo, favicon) y lo sube a S3 para ser consumido por el frontend de jugadores.

**Request:**

```http
POST /api/v1/admin/brands/assets/publish-config
Authorization: Bearer YOUR_JWT_TOKEN
Host: your-brand.com
```

**JavaScript Example:**

```typescript
async function publishConfig() {
  const response = await fetch('/api/v1/admin/brands/assets/publish-config', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${getToken()}`,
   'Host': 'your-brand.com'
    }
  });
  
  return await response.json();
}

// Uso con botón de publicar
document.querySelector('#publish-btn').addEventListener('click', async () => {
  const result = await publishConfig();
  
  toast.success('Configuración publicada exitosamente');
  
  // Mostrar URL del config.js
  document.querySelector('#config-url').textContent = result.configUrl;
  document.querySelector('#published-at').textContent = 
    new Date(result.publishedAt).toLocaleString();
});
```

**Response:**

```json
{
  "success": true,
  "configUrl": "https://casino-assets-s3.s3.us-east-1.amazonaws.com/assets/mycasino/config/config.js",
  "publishedAt": "2025-01-13T12:45:00Z"
}
```

### Contenido del config.js Generado

```javascript
// Brand Configuration - Auto-generated
// Brand: My Casino (mycasino)
// Generated: 2025-01-13 12:45:00 UTC

window.gBrandName = "mycasino";

window.gColors = {
  "--color-primary": "#ffb300",
  "--color-secondary": "#2196f3",
  "--color-accent": "#e91e63",
  "--color-background": "#121212",
  "--color-text": "#ffffff"
};

window.gHomeBannersDesktop = ["https://casino-assets-s3.s3.us-east-1.amazonaws.com/assets/mycasino/banners/home/uuid1.jpg","https://casino-assets-s3.s3.us-east-1.amazonaws.com/assets/mycasino/banners/home/uuid2.jpg"];
window.gSlotsBannersDesktop = ["https://casino-assets-s3.s3.us-east-1.amazonaws.com/assets/mycasino/banners/slots/uuid3.jpg"];
window.gLiveCasinoBannersDesktop = [];

window.gLogo = "https://casino-assets-s3.s3.us-east-1.amazonaws.com/assets/mycasino/banners/media/logo.png";
window.gFavicon = "https://casino-assets-s3.s3.us-east-1.amazonaws.com/assets/mycasino/banners/media/favicon.ico";
```

### Uso en Frontend de Jugadores

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <title>My Casino</title>
  
  <!-- Cargar config.js -->
  <script src="https://casino-assets-s3.s3.us-east-1.amazonaws.com/assets/mycasino/config/config.js"></script>
  
  <script>
    // Aplicar favicon dinámicamente
    const link = document.createElement('link');
    link.rel = 'icon';
    link.href = window.gFavicon;
    document.head.appendChild(link);
  </script>
</head>
<body>
  <header>
    <img id="logo" alt="Logo">
  </header>
  
  <div id="home-carousel"></div>
  
  <script>
    // Aplicar colores
    Object.entries(window.gColors).forEach(([key, value]) => {
      document.documentElement.style.setProperty(key, value);
    });
    
    // Cargar logo
    document.getElementById('logo').src = window.gLogo;
    
    // Cargar banners de home en carousel
    window.gHomeBannersDesktop.forEach(url => {
      const slide = document.createElement('div');
      slide.className = 'carousel-slide';
      slide.style.backgroundImage = `url(${url})`;
      document.getElementById('home-carousel').appendChild(slide);
    });
  </script>
</body>
</html>
```

---

## 🔧 Inicializar Estructura

### Endpoint: POST /api/v1/admin/brands/assets/initialize

Crea la estructura de carpetas en S3 y el registro inicial en la base de datos. **Solo se ejecuta una vez por brand**.

**Parámetros opcionales:**
- `brandId` (query parameter): UUID del brand. Si no se proporciona, se resuelve desde el header `Host`.

### Uso 1: Con Brand ID explícito (Recomendado para SUPER_ADMIN)

**Request:**

```http
POST /api/v1/admin/brands/assets/initialize?brandId=11111111-1111-1111-1111-111111111111
Authorization: Bearer YOUR_JWT_TOKEN
```

**cURL:**

```bash
curl -X POST "https://your-api.com/api/v1/admin/brands/assets/initialize?brandId=11111111-1111-1111-1111-111111111111" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**JavaScript:**

```typescript
async function initializeBrandAssets(brandId: string) {
  const response = await fetch(`/api/v1/admin/brands/assets/initialize?brandId=${brandId}`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${getToken()}`
    }
  });
  
  return await response.json();
}

// Uso
const result = await initializeBrandAssets('11111111-1111-1111-1111-111111111111');
```

### Uso 2: Resolviendo por Host (Comportamiento original)

**Request:**

```http
POST /api/v1/admin/brands/assets/initialize
Authorization: Bearer YOUR_JWT_TOKEN
Host: your-brand.com
```

**cURL:**

```bash
curl -X POST "https://your-api.com/api/v1/admin/brands/assets/initialize" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Host: your-brand.com"
```

**JavaScript:**

```typescript
async function initializeBrandAssetsFromHost() {
  const response = await fetch('/api/v1/admin/brands/assets/initialize', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${getToken()}`,
      'Host': 'your-brand.com'
    }
  });
  
  return await response.json();
}
```

**Response (ambos casos):**

```json
{
  "success": true,
  "message": "Brand assets initialized successfully",
  "foldersCreated": [
    "assets/mycasino/banners/home/",
    "assets/mycasino/banners/slots/",
    "assets/mycasino/banners/live-casino/",
    "assets/mycasino/banners/media/",
    "assets/mycasino/config/"
  ]
}
```

### Casos de Uso

**1. SUPER_ADMIN inicializando múltiples brands:**

```typescript
// Inicializar assets para varios brands
const brandIds = [
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333'
];

for (const brandId of brandIds) {
  try {
    await initializeBrandAssets(brandId);
    console.log(`✅ Brand ${brandId} initialized`);
  } catch (error) {
  console.error(`❌ Failed to initialize ${brandId}:`, error);
  }
}
```

**2. BRAND_ADMIN usando su propio brand (sin brandId):**

```typescript
// El brand se resuelve automáticamente desde el Host header
await initializeBrandAssetsFromHost();
```

### Ventajas del Parámetro Opcional

| Escenario | Método | Ventaja |
|-----------|--------|---------|
| **SUPER_ADMIN gestiona múltiples brands** | `?brandId=xxx` | Puede inicializar cualquier brand sin cambiar el Host |
| **BRAND_ADMIN gestiona su brand** | Header `Host` | Automático, no necesita conocer su brandId |
| **Scripts de migración/deployment** | `?brandId=xxx` | Más explícito y fácil de automatizar |
| **Interfaz de administración multi-brand** | `?brandId=xxx` | Dropdown para seleccionar brand |

---

## 📊 Ejemplos Completos de Integración

### Ejemplo 1: Componente React - Logo Manager

```tsx
import React, { useState, useEffect } from 'react';

interface MediaAssets {
  logo: string | null;
  favicon: string | null;
}

export function LogoManager() {
  const [assets, setAssets] = useState<MediaAssets>({ logo: null, favicon: null });
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    loadAssets();
  }, []);

  async function loadAssets() {
    const response = await fetch('/api/v1/admin/brands/assets/settings', {
      headers: {
        'Authorization': `Bearer ${getToken()}`,
        'Host': getBrandDomain()
      }
    });
    const data = await response.json();
    setAssets({
      logo: data.media.logo,
      favicon: data.media.favicon
    });
  }

  async function handleLogoUpload(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;

    setLoading(true);
    try {
      const formData = new FormData();
      formData.append('file', file);

      const response = await fetch('/api/v1/admin/brands/assets/upload/media/logo', {
        method: 'POST',
        headers: {
   'Authorization': `Bearer ${getToken()}`,
   'Host': getBrandDomain()
        },
        body: formData
   });

      const result = await response.json();
      setAssets(prev => ({ ...prev, logo: result.url }));
  toast.success('Logo actualizado exitosamente');
    } catch (error) {
      toast.error('Error al subir logo');
    } finally {
      setLoading(false);
    }
  }

  async function handleFaviconUpload(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;

    setLoading(true);
    try {
      const formData = new FormData();
      formData.append('file', file);

      const response = await fetch('/api/v1/admin/brands/assets/upload/media/favicon', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${getToken()}`,
          'Host': getBrandDomain()
        },
   body: formData
      });

      const result = await response.json();
      setAssets(prev => ({ ...prev, favicon: result.url }));
   toast.success('Favicon actualizado exitosamente');
    } catch (error) {
      toast.error('Error al subir favicon');
    } finally {
      setLoading(false);
    }
  }

async function deleteLogo() {
    if (!confirm('¿Eliminar logo actual?')) return;

    try {
      await fetch('/api/v1/admin/brands/assets/media/logo', {
        method: 'DELETE',
        headers: {
   'Authorization': `Bearer ${getToken()}`,
    'Host': getBrandDomain()
        }
      });

      setAssets(prev => ({ ...prev, logo: null }));
      toast.success('Logo eliminado');
    } catch (error) {
      toast.error('Error al eliminar logo');
    }
  }

  return (
    <div className="logo-manager">
      <h2>Logo y Favicon del Backoffice</h2>
      
      {/* Logo Section */}
      <div className="asset-section">
        <h3>Logo</h3>
      {assets.logo && (
 <div className="preview">
            <img src={assets.logo} alt="Logo actual" style={{ maxHeight: '100px' }} />
            <button onClick={deleteLogo} className="btn-danger">
        Eliminar Logo
         </button>
</div>
   )}
        <input
  type="file"
          accept="image/png,image/jpeg,image/svg+xml,image/webp"
          onChange={handleLogoUpload}
          disabled={loading}
        />
      </div>

      {/* Favicon Section */}
      <div className="asset-section">
        <h3>Favicon</h3>
        {assets.favicon && (
 <div className="preview">
            <img src={assets.favicon} alt="Favicon actual" style={{ maxHeight: '32px' }} />
     </div>
        )}
  <input
          type="file"
          accept="image/x-icon,image/png"
   onChange={handleFaviconUpload}
          disabled={loading}
        />
      </div>
    </div>
  );
}
```

---

### Ejemplo 2: Componente React - Banner Manager

```tsx
import React, { useState, useEffect } from 'react';

type Section = 'home' | 'slots' | 'live-casino';

interface BannersBySection {
  home: string[];
  slots: string[];
  'live-casino': string[];
}

export function BannerManager() {
  const [banners, setBanners] = useState<BannersBySection>({
    home: [],
    slots: [],
    'live-casino': []
  });
  const [selectedSection, setSelectedSection] = useState<Section>('home');

  useEffect(() => {
    loadBanners();
}, []);

  async function loadBanners() {
    const response = await fetch('/api/v1/admin/brands/assets/settings', {
headers: {
     'Authorization': `Bearer ${getToken()}`,
        'Host': getBrandDomain()
      }
    });
    const data = await response.json();
    
    setBanners({
      home: data.banners.home,
      slots: data.banners.slots,
      'live-casino': data.banners.liveCasino
  });
  }

  async function uploadBanner(file: File) {
    const formData = new FormData();
    formData.append('file', file);

    const response = await fetch(
      `/api/v1/admin/brands/assets/upload/banner/${selectedSection}`,
      {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${getToken()}`,
          'Host': getBrandDomain()
        },
        body: formData
      }
);

 if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error);
  }

    const result = await response.json();
    
    setBanners(prev => ({
      ...prev,
 [selectedSection]: [...prev[selectedSection], result.url]
    }));

    return result;
  }

  async function deleteBanner(url: string) {
    const fileName = url.split('/').pop();
    
    await fetch(
   `/api/v1/admin/brands/assets/banner/${selectedSection}/${fileName}`,
      {
    method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${getToken()}`,
       'Host': getBrandDomain()
        }
      }
    );

    setBanners(prev => ({
      ...prev,
      [selectedSection]: prev[selectedSection].filter(b => b !== url)
    }));
  }

  function handleDrop(e: React.DragEvent) {
    e.preventDefault();
    const files = Array.from(e.dataTransfer.files);
    
    files.forEach(async file => {
      try {
        await uploadBanner(file);
        toast.success(`Banner subido: ${file.name}`);
      } catch (error: any) {
      toast.error(error.message);
      }
    });
  }

  return (
    <div className="banner-manager">
      <h2>Banners del Frontend de Jugadores</h2>
      
    {/* Section Tabs */}
      <div className="section-tabs">
        <button
       onClick={() => setSelectedSection('home')}
          className={selectedSection === 'home' ? 'active' : ''}
  >
       Home ({banners.home.length}/5)
        </button>
        <button
   onClick={() => setSelectedSection('slots')}
  className={selectedSection === 'slots' ? 'active' : ''}
        >
       Slots ({banners.slots.length}/5)
 </button>
        <button
        onClick={() => setSelectedSection('live-casino')}
 className={selectedSection === 'live-casino' ? 'active' : ''}
     >
   Live Casino ({banners['live-casino'].length}/5)
        </button>
      </div>

    {/* Upload Zone */}
      <div
        className="upload-zone"
        onDrop={handleDrop}
        onDragOver={e => e.preventDefault()}
 >
        <p>Arrastra imágenes aquí o haz click para seleccionar</p>
    <p className="hint">Máximo 5 banners por sección • JPG, PNG, GIF, WebP • Max 5MB</p>
        <input
          type="file"
          accept="image/jpeg,image/png,image/gif,image/webp"
          multiple
          onChange={e => {
  const files = Array.from(e.target.files || []);
 files.forEach(file => uploadBanner(file));
  }}
      />
      </div>

      {/* Banner List */}
      <div className="banner-list">
        {banners[selectedSection].map((url, index) => (
          <div key={url} className="banner-item">
            <img src={url} alt={`Banner ${index + 1}`} />
   <button
   onClick={() => deleteBanner(url)}
              className="delete-btn"
 >
     🗑️ Eliminar
            </button>
          </div>
     ))}
      </div>
    </div>
  );
}
```

---

### Ejemplo 3: Componente React - Color Picker

```tsx
import React, { useState, useEffect } from 'react';

interface ColorPalette {
  [key: string]: string;
}

const DEFAULT_COLORS: ColorPalette = {
  '--color-primary': '#ffb300',
  '--color-secondary': '#2196f3',
  '--color-accent': '#e91e63',
  '--color-background': '#121212',
  '--color-surface': '#1e1e1e',
  '--color-text': '#ffffff',
  '--color-text-secondary': '#b0b0b0'
};

export function ColorManager() {
  const [colors, setColors] = useState<ColorPalette>(DEFAULT_COLORS);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    loadColors();
  }, []);

  async function loadColors() {
    const response = await fetch('/api/v1/admin/brands/assets/settings', {
      headers: {
        'Authorization': `Bearer ${getToken()}`,
        'Host': getBrandDomain()
      }
    });
    const data = await response.json();
    
    if (Object.keys(data.colors).length > 0) {
      setColors(data.colors);
      applyColorsToPreview(data.colors);
    }
  }

  function applyColorsToPreview(colorPalette: ColorPalette) {
    Object.entries(colorPalette).forEach(([key, value]) => {
      document.documentElement.style.setProperty(key, value);
    });
  }

  function handleColorChange(varName: string, value: string) {
    const newColors = { ...colors, [varName]: value };
    setColors(newColors);
    applyColorsToPreview(newColors);
  }

  async function saveColors() {
    setSaving(true);
    try {
      const response = await fetch('/api/v1/admin/brands/assets/colors', {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${getToken()}`,
        'Host': getBrandDomain(),
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ colors })
    });

      await response.json();
      toast.success('Colores guardados exitosamente');
    } catch (error) {
toast.error('Error al guardar colores');
    } finally {
      setSaving(false);
    }
  }

  async function publishConfig() {
    try {
 const response = await fetch('/api/v1/admin/brands/assets/publish-config', {
        method: 'POST',
        headers: {
        'Authorization': `Bearer ${getToken()}`,
     'Host': getBrandDomain()
        }
      });

      const result = await response.json();
      toast.success('Configuración publicada exitosamente');
    console.log('Config URL:', result.configUrl);
    } catch (error) {
toast.error('Error al publicar configuración');
    }
  }

  return (
    <div className="color-manager">
      <h2>Colores del Frontend de Jugadores</h2>
      
  <div className="color-grid">
        {Object.entries(colors).map(([varName, value]) => (
   <div key={varName} className="color-input-group">
     <label>{varName}</label>
            <div className="color-input-wrapper">
   <input
                type="color"
          value={value}
      onChange={e => handleColorChange(varName, e.target.value)}
  />
 <input
       type="text"
      value={value}
        onChange={e => handleColorChange(varName, e.target.value)}
     placeholder="#000000"
       />
      </div>
          </div>
        ))}
      </div>

      {/* Preview */}
      <div className="color-preview">
        <h3>Vista Previa</h3>
        <div className="preview-box" style={{
          backgroundColor: 'var(--color-background)',
 color: 'var(--color-text)',
  padding: '20px',
          borderRadius: '8px'
        }}>
        <button style={{
  backgroundColor: 'var(--color-primary)',
            color: 'var(--color-text)',
         padding: '10px 20px',
  border: 'none',
   borderRadius: '4px'
  }}>
            Botón Primario
   </button>
          <button style={{
    backgroundColor: 'var(--color-secondary)',
   color: 'var(--color-text)',
 padding: '10px 20px',
            border: 'none',
   borderRadius: '4px',
          marginLeft: '10px'
          }}>
            Botón Secundario
      </button>
        </div>
      </div>

      {/* Actions */}
      <div className="actions">
        <button
      onClick={saveColors}
     disabled={saving}
          className="btn-primary"
        >
   {saving ? 'Guardando...' : 'Guardar Colores'}
    </button>
  <button
     onClick={publishConfig}
          className="btn-success"
      >
      📤 Publicar Configuración
        </button>
      </div>
    </div>
  );
}
```

---

## ⚠️ Manejo de Errores

### Errores Comunes

#### 1. Archivo muy grande (413)

```json
{
  "error": "File size exceeds 5MB limit"
}
```

**Solución:** Comprimir la imagen antes de subir.

#### 2. Formato no permitido (400)

```json
{
  "error": "Invalid file extension. Allowed: .jpg, .jpeg, .png, .gif, .webp, .svg"
}
```

**Solución:** Convertir archivo al formato correcto.

#### 3. Límite de banners alcanzado (400)

```json
{
  "error": "Maximum 5 banners per section reached"
}
```

**Solución:** Eliminar un banner antes de subir uno nuevo.

#### 4. Sin autenticación (401)

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401
}
```

**Solución:** Renovar token JWT o volver a hacer login.

#### 5. Brand context no resuelto (400)

```json
{
  "error": "Brand context not resolved"
}
```

**Solución:** Asegurarse de enviar el header `Host` correcto.

---

### Ejemplo de Manejo de Errores

```typescript
async function uploadWithErrorHandling(file: File) {
  try {
    // Validación local
    if (file.size > 5 * 1024 * 1024) {
      throw new Error('El archivo es muy grande (máximo 5MB)');
    }

    const allowedTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp', 'image/svg+xml'];
if (!allowedTypes.includes(file.type)) {
      throw new Error('Formato no permitido. Usa JPG, PNG, GIF, WebP o SVG');
    }

    // Upload
    const formData = new FormData();
    formData.append('file', file);

    const response = await fetch('/api/v1/admin/brands/assets/upload/media/logo', {
      method: 'POST',
    headers: {
        'Authorization': `Bearer ${getToken()}`,
        'Host': getBrandDomain()
      },
      body: formData
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error || error.detail || 'Error al subir archivo');
    }

    const result = await response.json();
    return result;

  } catch (error: any) {
    // Logging
    console.error('Upload error:', error);
    
    // User feedback
    if (error.message.includes('413')) {
      toast.error('Archivo muy grande. Máximo 5MB');
    } else if (error.message.includes('401')) {
   toast.error('Sesión expirada. Por favor inicia sesión nuevamente');
      // Redirect to login
    } else {
      toast.error(error.message);
    }
    
    throw error;
  }
}
```

---

## 📋 Resumen de Endpoints

| Endpoint | Método | Uso | Autenticación |
|----------|--------|-----|---------------|
| `/api/v1/admin/brands/assets/settings` | GET | Obtener configuración actual | ✅ JWT |
| `/api/v1/admin/brands/assets/initialize` | POST | Inicializar estructura (una vez) | ✅ JWT |
| `/api/v1/admin/brands/assets/upload/media/logo` | POST | Subir logo (backoffice) | ✅ JWT |
| `/api/v1/admin/brands/assets/upload/media/favicon` | POST | Subir favicon (backoffice) | ✅ JWT |
| `/api/v1/admin/brands/assets/upload/banner/{section}` | POST | Subir banner (frontend jugadores) | ✅ JWT |
| `/api/v1/admin/brands/assets/media/{type}` | DELETE | Eliminar logo/favicon | ✅ JWT |
| `/api/v1/admin/brands/assets/banner/{section}/{fileName}` | DELETE | Eliminar banner | ✅ JWT |
| `/api/v1/admin/brands/assets/colors` | PUT | Actualizar colores (frontend) | ✅ JWT |
| `/api/v1/admin/brands/assets/publish-config` | POST | Publicar config.js | ✅ JWT |

---

## 🎯 Flujo Recomendado para Implementación

### Fase 1: Backoffice (Logo y Favicon)

1. **Crear componente `LogoManager`**
   - UI para subir logo
   - UI para subir favicon
   - Preview de archivos actuales
   - Botones de eliminar

2. **Integrar en settings del backoffice**
   - Pestaña "Brand Assets" o "Apariencia"
   - Mostrar logo en header/sidebar automáticamente

### Fase 2: Frontend de Jugadores (Banners)

3. **Crear componente `BannerManager`**
   - Tabs por sección (home, slots, live-casino)
   - Drag & drop para subir
   - Lista de banners con preview
   - Botones de eliminar
   - Indicador de límite (x/5)

4. **Crear componente `ColorManager`**
   - Color pickers por variable CSS
   - Preview en tiempo real
   - Botón de guardar
   - Botón de publicar

### Fase 3: Frontend de Jugadores (Consumo)

5. **Agregar `<script>` en HTML principal**
   ```html
   <script src="https://casino-assets-s3.s3.us-east-1.amazonaws.com/assets/{brandCode}/config/config.js"></script>
   ```

6. **Aplicar colores en CSS**
   ```javascript
   Object.entries(window.gColors).forEach(([key, value]) => {
     document.documentElement.style.setProperty(key, value);
   });
   ```

7. **Consumir banners en carousels**
   ```javascript
   window.gHomeBannersDesktop.forEach(url => {
     // Crear slide en carousel
   });
   ```

8. **Aplicar logo y favicon**
   ```javascript
   document.querySelector('.logo').src = window.gLogo;
   document.querySelector('link[rel="icon"]').href = window.gFavicon;
   ```

---

## 📞 Soporte y Preguntas

Si tienes dudas sobre la implementación:

1. Revisa los ejemplos de código en este documento
2. Verifica los logs en la consola del navegador
3. Revisa los logs del servidor para errores detallados
4. Consulta la documentación de Swagger: `/swagger`

---

**Última actualización:** 2025-01-13  
**Versión API:** v1  
**Estado:** ✅ Producción Ready

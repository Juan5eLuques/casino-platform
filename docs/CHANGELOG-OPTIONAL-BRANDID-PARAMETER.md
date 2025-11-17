# ?? Cambio: Parámetro Opcional `brandId` en Initialize Endpoint

## ?? Resumen del Cambio

Se ha modificado el endpoint `POST /api/v1/admin/brands/assets/initialize` para aceptar un parámetro opcional `brandId` en la query string.

---

## ? Funcionalidad Implementada

### Antes (Solo Host-based)

```http
POST /api/v1/admin/brands/assets/initialize
Authorization: Bearer JWT_TOKEN
Host: your-brand.com
```

El brandId se resolvía **únicamente** desde el header `Host`.

### Ahora (Dual: brandId o Host)

**Opción 1: Con brandId explícito (Nuevo)**

```http
POST /api/v1/admin/brands/assets/initialize?brandId=11111111-1111-1111-1111-111111111111
Authorization: Bearer JWT_TOKEN
```

**Opción 2: Con Host header (Original)**

```http
POST /api/v1/admin/brands/assets/initialize
Authorization: Bearer JWT_TOKEN
Host: your-brand.com
```

---

## ?? Lógica de Resolución

```csharp
if (brandId.HasValue)
{
    // Prioridad 1: Usar brandId explícito del query parameter
    targetBrandId = brandId.Value;
}
else
{
    // Prioridad 2: Fallback a brand resuelto desde Host header
    if (!brandContext.IsResolved)
  return BadRequest("Provide brandId parameter or valid Host header");
    
    targetBrandId = brandContext.BrandId;
}
```

---

## ?? Casos de Uso

### Caso 1: SUPER_ADMIN - Gestión Multi-Brand

**Escenario:** Un SUPER_ADMIN necesita inicializar assets para múltiples brands desde un solo panel.

**Solución:**

```typescript
async function initializeMultipleBrands() {
  const brands = await getAllBrands(); // Obtener lista de brands
  
  for (const brand of brands) {
    await fetch(`/api/v1/admin/brands/assets/initialize?brandId=${brand.id}`, {
      method: 'POST',
      headers: {
     'Authorization': `Bearer ${getSuperAdminToken()}`
   }
    });
  }
}
```

**Ventajas:**
- ? No necesita cambiar el header `Host` para cada brand
- ? Puede inicializar múltiples brands en un solo script
- ? Más fácil de automatizar

---

### Caso 2: BRAND_ADMIN - Gestión de Su Propio Brand

**Escenario:** Un BRAND_ADMIN gestiona únicamente su propio brand.

**Solución:**

```typescript
async function initializeMyBrand() {
  await fetch('/api/v1/admin/brands/assets/initialize', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${getBrandAdminToken()}`,
 'Host': 'mybrand.com'
    }
  });
}
```

**Ventajas:**
- ? Automático, no necesita conocer su brandId
- ? Más simple, menos parámetros
- ? Comportamiento original preservado

---

### Caso 3: Script de Deployment/Migración

**Escenario:** Script automatizado para inicializar assets en nuevos brands.

**Solución:**

```bash
#!/bin/bash

# Inicializar assets para todos los brands nuevos
BRAND_IDS=(
  "11111111-1111-1111-1111-111111111111"
  "22222222-2222-2222-2222-222222222222"
  "33333333-3333-3333-3333-333333333333"
)

for BRAND_ID in "${BRAND_IDS[@]}"
do
  curl -X POST "https://api.example.com/api/v1/admin/brands/assets/initialize?brandId=$BRAND_ID" \
    -H "Authorization: Bearer $JWT_TOKEN"
done
```

**Ventajas:**
- ? Explícito y claro
- ? No depende de DNS/Host resolution
- ? Fácil de debuggear

---

## ?? Interfaz de Usuario Recomendada

### Para SUPER_ADMIN Panel

```tsx
function BrandInitializer() {
  const [brands, setBrands] = useState<Brand[]>([]);
  const [selectedBrand, setSelectedBrand] = useState<string>('');

  async function handleInitialize() {
    await fetch(`/api/v1/admin/brands/assets/initialize?brandId=${selectedBrand}`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${getToken()}`
      }
    });
    
    toast.success('Brand assets initialized');
  }

  return (
    <div>
      <h2>Initialize Brand Assets</h2>
      <select value={selectedBrand} onChange={e => setSelectedBrand(e.target.value)}>
     <option value="">Select a brand...</option>
        {brands.map(brand => (
          <option key={brand.id} value={brand.id}>
         {brand.name} ({brand.code})
          </option>
     ))}
      </select>
      <button onClick={handleInitialize} disabled={!selectedBrand}>
        Initialize Assets
      </button>
    </div>
  );
}
```

### Para BRAND_ADMIN Panel

```tsx
function MyBrandInitializer() {
  async function handleInitialize() {
    // No necesita brandId, se resuelve automáticamente desde Host
    await fetch('/api/v1/admin/brands/assets/initialize', {
      method: 'POST',
      headers: {
    'Authorization': `Bearer ${getToken()}`,
        'Host': getBrandDomain()
      }
  });
    
    toast.success('Assets initialized for your brand');
  }

  return (
    <div>
      <h2>Initialize Your Brand Assets</h2>
      <p>This will create the folder structure in S3 for your brand.</p>
  <button onClick={handleInitialize}>
        Initialize My Brand Assets
      </button>
    </div>
  );
}
```

---

## ?? Consideraciones de Seguridad

### SUPER_ADMIN

- ? **Puede usar brandId**: Tiene acceso a todos los brands
- ? **Puede usar Host**: También funciona
- ? **Validación**: El servicio verifica que el brand exista

### BRAND_ADMIN

- ? **Puede usar brandId**: Pero solo de su propio brand (validado por middleware)
- ? **Puede usar Host**: Método recomendado
- ?? **Nota**: Si intenta usar brandId de otro brand, será bloqueado por la política de autorización

### CASHIER

- ? **No tiene acceso**: El endpoint requiere `BackofficePolicy` que incluye SUPER_ADMIN y BRAND_ADMIN

---

## ?? Swagger Documentation

El endpoint ahora se documenta así en Swagger:

```
POST /api/v1/admin/brands/assets/initialize

Parameters:
  - brandId (query, optional, uuid): Brand ID to initialize. 
    If not provided, resolves from Host header.

Headers:
  - Authorization: Bearer {token} (required)
  - Host: {brand-domain} (required if brandId not provided)

Responses:
  200: Success
    {
      "success": true,
      "message": "Brand assets initialized successfully",
   "foldersCreated": ["..."]
    }
  
400: Bad Request
    - Brand context not resolved (missing both brandId and valid Host)
- Brand not found
  
  401: Unauthorized
    - Invalid or missing JWT token
```

---

## ?? Testing

### Test Case 1: Initialize with brandId

```bash
# Should succeed with valid brandId
curl -X POST "http://localhost:5000/api/v1/admin/brands/assets/initialize?brandId=11111111-1111-1111-1111-111111111111" \
  -H "Authorization: Bearer YOUR_JWT"

# Expected: 200 OK + success response
```

### Test Case 2: Initialize with Host

```bash
# Should succeed with valid Host
curl -X POST "http://localhost:5000/api/v1/admin/brands/assets/initialize" \
  -H "Authorization: Bearer YOUR_JWT" \
  -H "Host: mybrand.com"

# Expected: 200 OK + success response
```

### Test Case 3: Missing both brandId and Host

```bash
# Should fail
curl -X POST "http://localhost:5000/api/v1/admin/brands/assets/initialize" \
  -H "Authorization: Bearer YOUR_JWT"

# Expected: 400 Bad Request + error message
```

### Test Case 4: Invalid brandId

```bash
# Should fail
curl -X POST "http://localhost:5000/api/v1/admin/brands/assets/initialize?brandId=00000000-0000-0000-0000-000000000000" \
  -H "Authorization: Bearer YOUR_JWT"

# Expected: 500 Internal Server Error (brand not found)
```

---

## ?? Archivos Modificados

1. **`apps/api/Casino.Api/Endpoints/BrandAssetsEndpoints.cs`**
   - Agregado parámetro `[FromQuery] Guid? brandId = null`
   - Lógica de resolución dual (brandId o Host)
   - Logging mejorado

2. **`docs/FRONTEND-BRAND-ASSETS-INTEGRATION-GUIDE.md`**
   - Sección actualizada con ejemplos de ambos usos
   - Casos de uso documentados
   - Ejemplos de código TypeScript

---

## ? Checklist de Implementación

- [x] Código modificado en `BrandAssetsEndpoints.cs`
- [x] Parámetro opcional `brandId` agregado
- [x] Lógica de fallback implementada
- [x] Logging detallado agregado
- [x] Documentación Swagger actualizada
- [x] Guía de frontend actualizada
- [x] Ejemplos de uso agregados
- [x] Casos de uso documentados
- [x] Sin errores de compilación
- [x] Backward compatible (comportamiento original preservado)

---

## ?? Próximos Pasos

1. **Reiniciar la aplicación** para aplicar cambios
2. **Probar ambos métodos** (brandId y Host)
3. **Actualizar frontend** para aprovechar la nueva funcionalidad
4. **Considerar extender** a otros endpoints si es necesario

---

**Fecha:** 2025-01-13  
**Cambio:** Feature Enhancement  
**Breaking Changes:** Ninguno (backward compatible)  
**Status:** ? Implementado y Documentado

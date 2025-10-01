# AUTH Verification Checklist (with Hashing Strategy Fix)

## Objetivo
Verificar y corregir la implementación de autenticación (login admin y players), asegurando que:
- El login no devuelva errores 500 (manejo defensivo de nulos y errores).
- La configuración de JWT esté correcta (Issuer y JwtKey).
- Se use **una sola estrategia de hashing de contraseñas** consistente entre generación e inicio de sesión.
- Se pueda autenticar correctamente a un SUPER_ADMIN y a un PLAYER.
- El sistema maneje cookies (`bk.token`, `pl.token`) y Authorization headers sin errores.

---

## Tareas para Sonnet

### 1. Revisión del endpoint de login de Admin
- Ubicación: `/api/v1/admin/auth/login`
- Validar que:
  - Si el usuario no existe o está inactivo → retornar 401 (no 500).
  - Si `PasswordHash` es nulo/vacío → retornar 401 (no 500).
  - Si la password no matchea el hash → retornar 401 (no 500).
  - Si `Auth:JwtKey` es nulo o vacío → retornar 500 con error claro ("JwtKey missing").
  - Generar logs (`ILogger`) en cada caso de fallo.

### 2. Revisión del endpoint de login de Players
- Ubicación: `/api/v1/auth/login`
- Validar las mismas condiciones:
  - El brand debe estar resuelto por `BrandContext`.
  - El player debe existir y estar activo.
  - Si tiene `PasswordHash`, verificarlo.
  - Manejar nulos y devolver 401 en lugar de 500.

### 3. Estrategia de hashing unificada
- **Problema actual**: en DB se insertó un hash de `PasswordHasher<T>` (prefijo `AQAAAA...`), pero el login usa `BCrypt.Net`, lo que lanza `Invalid salt version`.
- **Corrección**:
  - Opción A (recomendada): Usar **Microsoft.AspNetCore.Identity.PasswordHasher** en el login y mantener hashes `AQAAAA...`.
  - Opción B: Usar **BCrypt.Net** en el login y generar hashes nuevos en formato `$2a$...`.  
- Implementar solo una estrategia y aplicarla en todo el backend (admin y players).

### 4. Configuración de JWT
- En `Program.cs` revisar que:
  - Se leen las claves desde `builder.Configuration["Auth:JwtKey"]` y `["Auth:Issuer"]`.
  - `JwtKey` tenga mínimo 32 caracteres.
  - Se usen dos esquemas JWT (`BackofficeJwt`, `PlayerJwt`) con audiences distintas: `backoffice` y `player`.
  - Los eventos `OnMessageReceived` lean el token de:
    - `Authorization: Bearer ...`
    - Cookies (`bk.token` y `pl.token`)

### 5. Clave JWT fija para desarrollo
- En `appsettings.Development.json` asegurar que `Auth:JwtKey` tenga un valor válido.

Ejemplo seguro para desarrollo:
```json
"Auth": {
  "Issuer": "casino",
  "JwtKey": "supersecretdevkey-please-change-32chars!!"
}
```

### 6. Validar protección de endpoints
- Grupo `/api/v1/admin/*` → debe requerir `BackofficeJwt` + policy `BackofficePolicy`.
- Grupo `/api/v1/player/*` → debe requerir `PlayerJwt` + policy `PlayerPolicy`.

### 7. Pruebas automáticas sugeridas
- Intentar login con usuario inexistente → 401.
- Intentar login con password incorrecta → 401.
- Intentar login con password correcta → 200 + `Set-Cookie: bk.token`.
- Intentar llamar a `/api/v1/admin/users` sin token → 401.
- Intentar llamar a `/api/v1/admin/users` con token válido de player → 403.
- Intentar llamar a `/api/v1/admin/users` con token válido de admin → 200.

---

## Criterios de aceptación
- El login nunca devuelve 500 salvo error de configuración grave.
- `Auth:JwtKey` está presente, es seguro y >=32 caracteres.
- Solo se usa **una estrategia de hashing** (PasswordHasher o BCrypt).
- El flujo de login admin y player funciona end-to-end con tokens/cookies.
- Los endpoints exigen el esquema y policy correctos.

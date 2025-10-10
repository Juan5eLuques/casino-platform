# ? CORS y Autenticaci�n - Problemas Solucionados

## ?? Cambios Realizados

### 1. **Correcci�n en AuthEndpoints.cs**
- **Problema**: Los par�metros en `VerifyPassword` estaban en orden incorrecto
- **Soluci�n**: Cambiado de `VerifyPassword(user.PasswordHash, request.Password)` a `VerifyPassword(request.Password, user.PasswordHash)`

### 2. **Mejoras en DynamicCorsMiddleware.cs**
- **Agregado**: Soporte mejorado para desarrollo con origins comunes de localhost
- **Agregado**: Mejor logging para debugging de CORS
- **Agregado**: Configuraci�n permisiva para endpoints de autenticaci�n
- **Agregado**: Headers CORS adicionales (`X-Requested-With`)

### 3. **Mejoras en BrandResolverMiddleware.cs**
- **Agregado**: Exclusi�n de endpoints de autenticaci�n (`/api/v1/admin/auth`) de la resoluci�n de brand
- **Agregado**: Modo permisivo para desarrollo en localhost
- **Agregado**: Mejor logging para debugging

### 4. **Scripts de Configuraci�n**
- **Creado**: `configure-bet30-brand.sql` - Configura el brand BET30 con CORS apropiados
- **Creado**: `setup-development.ps1` - Script completo de configuraci�n para desarrollo
- **Creado**: Scripts adicionales para creaci�n de usuarios admin

## ??? Configuraci�n del Brand BET30

El brand BET30 ahora incluye estos CORS origins permitidos:
- `http://localhost:5173`
- `http://localhost:3000`
- `http://admin.bet30.local:5173`
- `https://admin.bet30.local:5173`
- `http://bet30.local:5173`
- `https://bet30.local:5173`
- `http://127.0.0.1:5173`

## ?? Usuarios Creados

Todos con password: `admin123`
- **superadmin** (SUPER_ADMIN)
- **operator_admin** (OPERATOR_ADMIN)
- **cashier_user** (CASHIER)

## ?? Para Ejecutar

1. **Configurar base de datos**:
   ```powershell
   .\scripts\setup-development.ps1
   ```

2. **Verificar archivo hosts**:
   ```
   127.0.0.1 bet30.local
   127.0.0.1 admin.bet30.local
   ```

3. **Iniciar API**:
   ```bash
   dotnet watch --project apps/api/Casino.Api
   ```

4. **Acceder desde frontend**:
   - Frontend: `http://admin.bet30.local:5173`
   - API: `https://admin.bet30.local:7182`

## ?? Verificaci�n CORS

Los logs del middleware mostrar�n:
```
CORS Request: POST /api/v1/admin/auth/login from Origin: http://admin.bet30.local:5173
Brand resolved: bet30 (uuid) for host: admin.bet30.local, CORS origins: http://localhost:5173, ...
CORS allowed for origin http://admin.bet30.local:5173 on brand bet30
```

## ??? Troubleshooting

### Error "brand_not_resolved"
- Ejecutar el script `configure-bet30-brand.sql`
- Verificar que el host header coincida con `domain` o `admin_domain` en la tabla `brands`

### Error CORS
- Verificar que el origin del frontend est� en la lista `cors_origins` del brand
- En desarrollo, el middleware es m�s permisivo para localhost

### Error 401 en login
- Verificar que los usuarios existan en la base de datos
- Verificar que la password sea correcta (`admin123` para usuarios de prueba)
- Verificar los logs de la API para m�s detalles

## ? Estado Actual

- ? CORS din�mico funcionando
- ? Brand resolution funcionando  
- ? Autenticaci�n corregida
- ? Usuarios de prueba creados
- ? Middleware optimizado para desarrollo
- ? Scripts de configuraci�n listos

El sistema ahora deber�a funcionar correctamente con el frontend en `http://admin.bet30.local:5173` llamando a la API en `https://admin.bet30.local:7182`.
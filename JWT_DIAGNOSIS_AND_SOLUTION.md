# Diagnóstico y Solución JWT - Casino Platform

## Problemas Identificados y Corregidos

### ? 1. Configuración JWT Mejorada
- **Problema**: Configuración JWT básica sin logging detallado
- **Solución**: Agregado logging completo y configuración de `RoleClaimType`
- **Archivo**: `src\Casino.Infrastructure\DependencyInjection.cs`

### ? 2. CORS Agregado
- **Problema**: Falta configuración CORS para peticiones desde frontend
- **Solución**: Agregada configuración CORS permisiva para desarrollo
- **Archivo**: `src\Casino.Api\Program.cs`

### ? 3. Orden de Middlewares Corregido
- **Problema**: Orden incorrecto de middlewares podía causar problemas de autorización
- **Solución**: Orden correcto: CORS ? Authentication ? Authorization
- **Archivo**: `src\Casino.Api\Program.cs`

### ? 4. Endpoints de Diagnóstico
- **Solución**: Creado `TestController` con endpoints para diagnóstico JWT
- **Archivo**: `src\Casino.Api\Controllers\TestController.cs`

## Cómo Diagnosticar Problemas JWT

### Paso 1: Verificar Endpoint Público
```bash
curl -X GET http://localhost:7202/api/test/public
```
**Esperado**: Respuesta 200 OK sin autenticación

### Paso 2: Hacer Login y Obtener Token
```bash
curl -X POST http://localhost:7202/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"tu@email.com","password":"tupassword"}'
```
**Esperado**: Respuesta con token JWT

### Paso 3: Probar Endpoint Protegido
```bash
curl -X GET http://localhost:7202/api/test/protected \
  -H "Authorization: Bearer TU_TOKEN_AQUI"
```
**Esperado**: Respuesta 200 OK con información del usuario

### Paso 4: Probar Endpoint con Roles
```bash
curl -X GET http://localhost:7202/api/test/admin-only \
  -H "Authorization: Bearer TU_TOKEN_AQUI"
```
**Esperado**: 200 OK si es admin, 403 Forbidden si no

## Configuración de Swagger para JWT

1. **Haz login** en `/api/auth/login`
2. **Copia el token** de la respuesta
3. **En Swagger UI** click en "Authorize" (??)
4. **Ingresa**: `Bearer TU_TOKEN_AQUI` (con espacio después de Bearer)
5. **Click "Authorize"**
6. **Prueba endpoints protegidos**

## Logging para Debugging

Los logs JWT ahora muestran:
- ? **Token válido**: "JWT Token validated successfully for user: {email}"
- ? **Authentication failed**: "JWT Authentication failed: {mensaje}"
- ?? **Challenge triggered**: "JWT Challenge triggered: {error}"

Revisa los logs de la consola para diagnosticar problemas específicos.

## Configuración JWT Actual

```json
{
  "Jwt": {
    "Issuer": "casino-api",
    "Audience": "casino-client", 
    "Key": "C@5in0-Platf0rm-2025-SecretKey-ChangeMe!",
    "AccessTokenMinutes": 60
  }
}
```

## Endpoints para Probar

### Públicos (Sin Autenticación)
- `GET /api/test/public` - Test básico
- `POST /api/users/register` - Registro de jugadores
- `POST /api/auth/login` - Login

### Protegidos (Requieren JWT)
- `GET /api/test/protected` - Test con cualquier usuario autenticado
- `GET /api/auth/me` - Información personal
- `GET /api/users/my-users` - Usuarios propios
- `GET /api/users/hierarchy` - Jerarquía personal

### Requieren Roles Específicos
- `GET /api/test/admin-only` - Solo ADMIN/SUPERADMIN
- `GET /api/users` - Solo ADMIN/SUPERADMIN
- `POST /api/users` - ADMIN/SUPERADMIN/CASHIER

## Problemas Comunes y Soluciones

### Error 401 Unauthorized
1. **Verificar token**: ¿El token es válido y no ha expirado?
2. **Verificar header**: ¿Es `Authorization: Bearer TOKEN`?
3. **Verificar configuración**: ¿Issuer/Audience coinciden?
4. **Revisar logs**: Buscar mensajes de "JWT Authentication failed"

### Error 403 Forbidden
1. **Verificar rol**: ¿El usuario tiene el rol requerido?
2. **Verificar claims**: Usar `/api/test/protected` para ver claims
3. **Verificar configuración de roles**: `RoleClaimType = ClaimTypes.Role`

### Error 500 Internal Server Error
1. **Verificar base de datos**: ¿Las columnas nuevas existen?
2. **Ejecutar SQL**: Los comandos del documento de migración
3. **Revisar logs**: Buscar stack traces en la consola

## Comandos SQL Requeridos (Si aún no ejecutados)

```sql
-- 1. Agregar columnas para jerarquía y comisión
ALTER TABLE users ADD COLUMN parent_user_id INTEGER;
ALTER TABLE users ADD COLUMN commission_rate DECIMAL(5,2) DEFAULT 0.0;

-- 2. Agregar foreign key constraint para la jerarquía
ALTER TABLE users ADD CONSTRAINT FK_users_parent_user_id 
    FOREIGN KEY (parent_user_id) REFERENCES users(id) 
    ON DELETE RESTRICT;

-- 3. Crear índice para mejor performance en consultas jerárquicas
CREATE INDEX IX_users_parent_user_id ON users(parent_user_id);
```

## Test de Flujo Completo

```bash
# 1. Test público
curl -X GET http://localhost:7202/api/test/public

# 2. Login (reemplaza con credenciales reales)
curl -X POST http://localhost:7202/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@test.com","password":"password"}'

# 3. Test protegido (reemplaza TOKEN)
curl -X GET http://localhost:7202/api/test/protected \
  -H "Authorization: Bearer TOKEN"

# 4. Test de rol (reemplaza TOKEN)  
curl -X GET http://localhost:7202/api/test/admin-only \
  -H "Authorization: Bearer TOKEN"
```

El sistema JWT ahora debería funcionar correctamente. Si persisten problemas, revisa los logs detallados en la consola de la aplicación.
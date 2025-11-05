# Instrucciones para Migración y Actualización del Sistema

## 1. Ejecutar Comandos SQL en la Base de Datos

Ejecuta estos comandos SQL directamente en tu base de datos PostgreSQL:

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

## 2. Endpoints Consolidados

### Registro y Autenticación

#### `/api/users/register` (POST) - Registro Público
- **Función**: Registro abierto para crear jugadores
- **Sin autenticación requerida**
- **Cuerpo de la petición**:
```json
{
  "email": "player@example.com",
  "password": "password123"
}
```

#### `/api/auth/login` (POST) - Login General
- **Función**: Login para todos los tipos de usuarios
- **Cuerpo de la petición**:
```json
{
  "email": "user@example.com",
  "password": "password123"
}
```

#### `/api/auth/admin-login` (POST) - Login de Administradores
- **Función**: Login específico para administradores (puede tener validaciones adicionales)
- **Cuerpo de la petición**: Igual que login normal

### Gestión de Usuarios

#### `/api/users` (POST) - Crear Usuario (Con Jerarquía)
- **Requiere**: Rol SUPERADMIN, ADMIN, o CASHIER
- **Función**: Crear usuarios según permisos de jerarquía
- **Cuerpo de la petición**:
```json
{
  "email": "newuser@example.com",
  "password": "password123",
  "role": "CASHIER",
  "commissionRate": 15.5  // Opcional, solo para cajeros
}
```

#### `/api/users` (GET) - Ver Todos los Usuarios
- **Requiere**: Rol SUPERADMIN o ADMIN
- **Función**: Ver todos los usuarios del sistema

#### `/api/users/my-users` (GET) - Ver Mis Usuarios Directos
- **Requiere**: Autenticación
- **Función**: Ver solo los usuarios creados directamente por ti

#### `/api/users/hierarchy` (GET) - Ver Jerarquía Completa
- **Requiere**: Autenticación
- **Función**: Ver toda tu red de usuarios (directo e indirecto)

## 3. Permisos de Creación por Rol

| Rol Actual | Puede Crear |
|------------|-------------|
| SUPERADMIN | SUPERADMIN, ADMIN, CASHIER, PLAYER |
| ADMIN      | ADMIN, CASHIER, PLAYER |
| CASHIER    | CASHIER (con comisión), PLAYER |
| PLAYER     | Ninguno |

## 4. Flujo de Uso Recomendado

### Para el primer setup del sistema:
1. `POST /api/users/register` con rol "SUPERADMIN" (solo funciona si no existe ningún SUPERADMIN)
2. Login como SUPERADMIN
3. Crear ADMINs con `POST /api/users`
4. Los ADMINs crean cajeros y jugadores

### Para operaciones regulares:
1. Login con `/api/auth/login`
2. Crear usuarios subordinados con `POST /api/users`
3. Ver tu red con `/api/users/hierarchy`
4. Gestionar fichas con `/api/transactions/*`

## 5. Diferencias Importantes

### Antes (Problemas):
- ? Dos endpoints para lo mismo: `/api/auth/register` y `/api/users`
- ? No validación de jerarquía
- ? Error de base de datos por columnas faltantes

### Ahora (Solucionado):
- ? `/api/users/register` - Solo para registro público de jugadores
- ? `/api/users` - Para creación con jerarquía por usuarios autenticados
- ? Validación estricta de permisos según rol
- ? Base de datos actualizada con nuevas columnas
- ? Sistema de comisiones para cajeros
- ? Jerarquía completa implementada

## 6. Ejemplo de Flujo Completo

```bash
# 1. Registro público de un jugador
curl -X POST http://localhost:5000/api/users/register \
  -H "Content-Type: application/json" \
  -d '{"email":"player@test.com","password":"pass123"}'

# 2. Login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@test.com","password":"pass123"}'

# 3. Crear un cajero (como admin)
curl -X POST http://localhost:5000/api/users \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{"email":"cashier@test.com","password":"pass123","role":"CASHIER","commissionRate":12.5}'

# 4. Ver mi jerarquía
curl -X GET http://localhost:5000/api/users/hierarchy \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

El sistema ahora está completamente funcional y sin duplicaciones de endpoints!
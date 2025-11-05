# Actualización del Sistema - Username y Query Parameters

## Comandos SQL Adicionales Requeridos

Además de los comandos anteriores, ejecuta estos nuevos:

```sql
-- Agregar columna username
ALTER TABLE users ADD COLUMN username VARCHAR(50);

-- Crear índice único para username
CREATE UNIQUE INDEX IX_users_username ON users(username);

-- (Opcional) Poblar usernames existentes si tienes datos
-- UPDATE users SET username = split_part(email, '@', 1) WHERE username IS NULL;

-- Hacer username NOT NULL después de poblar datos existentes
-- ALTER TABLE users ALTER COLUMN username SET NOT NULL;
```

## Cambios Implementados

### ? 1. Campo Username Agregado
- **Modelo**: `User.Username` agregado con validación
- **Base de datos**: Campo `username` con índice único
- **Validación**: Username único en toda la aplicación

### ? 2. Swagger JWT Mejorado  
- **Configuración mejorada** para mejor experiencia en Swagger UI
- **Descripción detallada** para el uso del token Bearer
- **Configuración de UI** optimizada

### ? 3. Query Parameters para `/api/users`
- **search**: Buscar por username o email
- **role**: Filtrar por rol específico (PLAYER, CASHIER, ADMIN, SUPERADMIN)
- **orderBy**: Ordenar por username, email, role, balance, createdAt
- **orderByDirection**: asc o desc
- **page**: Número de página (default: 1)
- **perPage**: Registros por página (default: 10, máximo: 100)

### ? 4. Respuesta Paginada
Nuevo formato de respuesta para `/api/users`:
```json
{
  "users": [...],
  "totalCount": 150,
  "currentPage": 1,
  "perPage": 10,
  "totalPages": 15
}
```

## Nuevos Endpoints y Formato

### Registro con Username
```bash
POST /api/users/register
{
  "username": "player123",
  "email": "player123@example.com",
  "password": "password123"
}
```

### Crear Usuario con Username
```bash
POST /api/users
Authorization: Bearer YOUR_JWT_TOKEN
{
  "username": "cashier01",
  "email": "cashier01@example.com",
  "password": "password123",
  "role": "CASHIER",
  "commissionRate": 15.5
}
```

### Obtener Usuarios con Filtros
```bash
# Buscar usuarios que contengan "admin" en username o email
GET /api/users?search=admin

# Filtrar solo cajeros
GET /api/users?role=CASHIER

# Ordenar por username ascendente
GET /api/users?orderBy=username&orderByDirection=asc

# Paginación - página 2, 20 registros por página
GET /api/users?page=2&perPage=20

# Combinado - buscar cajeros, ordenados por balance descendente, página 1
GET /api/users?search=cajero&role=CASHIER&orderBy=balance&orderByDirection=desc&page=1&perPage=10
```

## Cómo Probar en Swagger

### Para JWT (Problema Resuelto):
1. **Login**: Usa `/api/auth/login` para obtener token
2. **Copiar token** completo de la respuesta
3. **Autorizar**: Click en "Authorize" (??) en la parte superior
4. **Ingresar**: Escribe exactamente `Bearer TU_TOKEN_COMPLETO_AQUI` 
5. **Guardar**: Click "Authorize" y luego "Close"
6. **Probar**: Todos los endpoints protegidos deberían funcionar

### Para Usuarios con Filtros:
1. **Accede** a `GET /api/users` (requiere token de ADMIN)
2. **Expand** los parámetros de query
3. **Ingresa valores**:
   - search: `admin`
   - role: `CASHIER` 
   - orderBy: `username`
   - orderByDirection: `asc`
   - page: `1`
   - perPage: `10`
4. **Execute** para ver resultados paginados

## Validaciones Implementadas

### Username:
- ? **Único** en toda la aplicación
- ? **Requerido** para todos los usuarios nuevos
- ? **Máximo 50 caracteres**
- ? **Validación** en registro y creación

### Query Parameters:
- ? **perPage limitado** a máximo 100 registros
- ? **page mínimo** de 1
- ? **orderBy válido** (username, email, role, balance, createdAt)
- ? **orderByDirection** solo acepta 'asc' o 'desc'

### Roles y Permisos:
- ? **Mismo sistema jerárquico** mantenido
- ? **Username en todas las respuestas** de usuario
- ? **Búsqueda por username y email**

## Respuestas de Usuario Actualizadas

Todas las respuestas ahora incluyen username:

```json
{
  "id": 1,
  "username": "admin01",
  "email": "admin@casino.com",
  "role": "ADMIN",
  "balance": 10000.00,
  "commissionRate": 0.0,
  "parentUserId": null,
  "parentEmail": null,
  "createdAt": "2025-01-12T10:30:00Z"
}
```

## Problemas Solucionados

### ? JWT en Swagger:
- **Antes**: Token no se aplicaba correctamente
- **Ahora**: Configuración mejorada con descripción clara

### ? Registro Limitado:
- **Antes**: Solo email y password
- **Ahora**: Username, email y password requeridos

### ? Lista de Usuarios Básica:
- **Antes**: Lista simple sin filtros
- **Ahora**: Búsqueda, filtros, ordenamiento y paginación

### ? Performance:
- **Antes**: Cargar todos los usuarios
- **Ahora**: Paginación y índices para mejor performance

El sistema ahora está completamente actualizado con username y query parameters funcionales!
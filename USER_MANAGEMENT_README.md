# Sistema de Gestión de Usuarios - Casino Platform

## Descripción General

Este sistema implementa un modelo jerárquico de usuarios para una plataforma de casino, donde diferentes roles pueden crear y gestionar usuarios subordinados con validaciones de permisos estrictas.

## Roles del Sistema

### ?? SUPERADMIN
- **Permisos**: Acceso total al sistema
- **Puede crear**: Todos los roles (SUPERADMIN, ADMIN, CASHIER, PLAYER)
- **Funcionalidades especiales**:
  - Puede cargar/descargar fichas de cualquier usuario
  - Acceso completo a todos los endpoints
  - Ver jerarquía completa del sistema

### ?? ADMIN
- **Permisos**: Administración general
- **Puede crear**: ADMIN, CASHIER, PLAYER
- **Limitaciones**: No puede crear otros SUPERADMIN
- **Funcionalidades**:
  - Ver todos los usuarios del sistema
  - Gestionar usuarios de su jerarquía
  - Cargar/descargar fichas a usuarios de su red

### ?? CASHIER (Cajero)
- **Permisos**: Operaciones de fichas y gestión limitada
- **Puede crear**: CASHIER (con comisión), PLAYER
- **Características especiales**:
  - Puede tener comisión configurada (0-100%)
  - Solo puede ver usuarios de su propia red
  - Puede crear otros cajeros subordinados
- **Funcionalidades**:
  - Cargar/descargar fichas a usuarios de su red
  - Ver solo usuarios que creó directa o indirectamente

### ?? PLAYER (Jugador)
- **Permisos**: Solo operaciones básicas
- **No puede crear usuarios**
- **Funcionalidades**:
  - Transferir fichas entre jugadores
  - Ver su propio historial de transacciones

## Estructura Jerárquica

```
SUPERADMIN
??? ADMIN_1
?   ??? CASHIER_1 (comisión: 15%)
?   ?   ??? CASHIER_2 (comisión: 8%)
?   ?   ?   ??? PLAYER_1
?   ?   ??? PLAYER_2
?   ??? PLAYER_3
??? ADMIN_2
    ??? CASHIER_3 (comisión: 12%)
        ??? PLAYER_4
```

## Endpoints de la API

### Autenticación
- `POST /api/auth/register` - Registro público (solo PLAYER, excepto primer SUPERADMIN)
- `POST /api/auth/login` - Login general
- `POST /api/auth/admin-login` - Login para administradores
- `GET /api/auth/me` - Información del usuario actual

### Gestión de Usuarios
- `POST /api/users` - Crear usuario (requiere permisos según jerarquía)
- `GET /api/users` - Ver todos los usuarios (solo ADMIN/SUPERADMIN)
- `GET /api/users/my-users` - Ver usuarios creados directamente
- `GET /api/users/hierarchy` - Ver jerarquía completa personal

### Transacciones
- `POST /api/transactions/transfer` - Transferencia entre jugadores
- `POST /api/transactions/load` - Cargar fichas (CASHIER+)
- `POST /api/transactions/unload` - Descargar fichas (CASHIER+)
- `GET /api/transactions/history` - Historial personal

## Validaciones de Seguridad

### Creación de Usuarios
1. **SUPERADMIN**: Puede crear cualquier rol sin restricciones
2. **ADMIN**: Puede crear ADMIN, CASHIER, PLAYER
3. **CASHIER**: Puede crear CASHIER (con comisión), PLAYER
4. **PLAYER**: No puede crear usuarios

### Operaciones de Fichas
- Solo usuarios con rol CASHIER+ pueden cargar/descargar fichas
- Las operaciones están limitadas a la jerarquía del usuario
- SUPERADMIN puede operar con cualquier usuario
- Validación de saldo suficiente antes de operaciones

### Comisiones
- Solo aplicable a usuarios con rol CASHIER
- Rango válido: 0% - 100%
- Se configura al momento de creación del cajero

## Modelos de Datos

### Usuario
```csharp
public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public Role Role { get; set; }
    public decimal Balance { get; set; }
    
    // Jerarquía
    public int? ParentUserId { get; set; }
    public User? ParentUser { get; set; }
    public List<User> ChildUsers { get; set; }
    
    // Comisiones
    public decimal CommissionRate { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

## Casos de Uso Típicos

### 1. Configuración Inicial del Sistema
1. Crear primer SUPERADMIN mediante registro con rol especial
2. SUPERADMIN crea ADMINs principales
3. ADMINs crean estructura de cajeros y jugadores

### 2. Operación de Cajero
1. ADMIN crea CASHIER con comisión del 15%
2. CASHIER crea sub-cajeros con comisión menor (ej: 8%)
3. CASHIER carga fichas a jugadores de su red
4. CASHIER puede ver y gestionar toda su jerarquía

### 3. Red de Jugadores
1. CASHIER crea múltiples PLAYER
2. PLAYER pueden transferir fichas entre ellos
3. CASHIER controla carga/descarga de fichas
4. Historial completo de transacciones

## Seguridad Implementada

- **Autenticación JWT**: Todos los endpoints protegidos requieren token válido
- **Autorización por Roles**: Decoradores `[Authorize(Roles = "...")]`
- **Validación de Jerarquía**: Los usuarios solo pueden operar dentro de su red
- **Encriptación de Contraseñas**: BCrypt para hash seguro
- **Validación de Entrada**: Sanitización de emails y validación de datos

## Base de Datos

### Migración Requerida
```bash
cd src/Casino.Infrastructure
dotnet ef migrations add AddUserHierarchyAndCommissions
dotnet ef database update
```

### Nuevas Columnas
- `parent_user_id` - FK para jerarquía
- `commission_rate` - Porcentaje de comisión para cajeros

## Testing

El sistema incluye tests de integración que validan:
- Creación de usuarios según permisos
- Validación de jerarquía en operaciones
- Restricciones de rol correctas
- Funcionalidad de comisiones

```bash
dotnet test
```
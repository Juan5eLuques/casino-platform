# Guía Completa: Crear un Sitio de Casino desde Cero

## ?? **Resumen de Configuración**

Esta guía te permitirá crear un sitio de casino completo con:
- ? **1 Brand/Sitio** con dominio configurado
- ? **1 Admin (OPERATOR_ADMIN)** con acceso total al sitio
- ? **2 Cajeros (CASHIER)** para gestionar jugadores
- ? **4 Jugadores** con saldo inicial
- ? **Configuración de CORS** y dominios
- ? **Sistema de autenticación** completamente funcional

---

## ??? **Prerequisitos**

### **1. Base de Datos PostgreSQL**
Asegúrate de tener PostgreSQL corriendo y configurado en `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=casino_platform;Username=postgres;Password=tu_password"
  },
  "Auth": {
    "Issuer": "casino",
    "JwtKey": "REEMPLAZAR_POR_CLAVE_DE_32_O_MAS_CARACTERES_EN_PRODUCCION"
  }
}
```

### **2. Migraciones Aplicadas**
```bash
dotnet ef migrations add InitialCreate --project apps/Casino.Infrastructure --startup-project apps/api/Casino.Api
dotnet ef database update --project apps/Casino.Infrastructure --startup-project apps/api/Casino.Api
```

---

## ?? **Paso 1: Crear Operador Base**

Ejecuta estos scripts SQL para crear la estructura base:

```sql
-- 1. Crear el operador principal
INSERT INTO "Operators" ("Id", "Name", "Status", "CreatedAt")
VALUES 
    ('550e8400-e29b-41d4-a716-446655440000', 'MiCasino Corp', 'ACTIVE', CURRENT_TIMESTAMP);
```

---

## ?? **Paso 2: Crear Brand/Sitio**

```sql
-- 2. Crear el brand con configuración de dominio y CORS
INSERT INTO "Brands" ("Id", "OperatorId", "Code", "Name", "Locale", "Domain", "AdminDomain", "CorsOrigins", "Status", "Theme", "Settings", "CreatedAt", "UpdatedAt")
VALUES 
    (
        '661e8400-e29b-41d4-a716-446655440001',
        '550e8400-e29b-41d4-a716-446655440000',
        'mycasino',
        'MiCasino',
        'es-ES',
        'mycasino.local',                          -- Dominio principal del sitio
        'admin.mycasino.local',                    -- Dominio del backoffice
        'http://localhost:3000,https://mycasino.local,https://admin.mycasino.local', -- CORS origins
        'ACTIVE',
        '{"primaryColor": "#1a73e8", "logo": "/assets/logo.png"}',  -- Tema personalizable
        '{"maxBetAmount": 10000, "currency": "EUR", "timezone": "Europe/Madrid"}', -- Configuraciones
        CURRENT_TIMESTAMP,
        CURRENT_TIMESTAMP
    );
```

---

## ?? **Paso 3: Crear Usuarios de Backoffice**

### **3.1 Crear Admin Principal**
```sql
-- Hash para password "admin123" (usa bcrypt en producción)
INSERT INTO "BackofficeUsers" ("Id", "OperatorId", "Username", "PasswordHash", "Role", "Status", "CreatedAt")
VALUES 
    (
        '771e8400-e29b-41d4-a716-446655440002',
        '550e8400-e29b-41d4-a716-446655440000',
        'admin_mycasino',
        '$2a$11$3rQ8P.Qx5l1ZzKwA9HqbOeBK7QzC8VUJc6kR5xE8nP2uQ1mT9oXXu', -- password: admin123
        'OPERATOR_ADMIN',
        'ACTIVE',
        CURRENT_TIMESTAMP
    );
```

### **3.2 Crear Cajeros**
```sql
-- Cajero 1
INSERT INTO "BackofficeUsers" ("Id", "OperatorId", "Username", "PasswordHash", "Role", "Status", "CreatedAt")
VALUES 
    (
        '881e8400-e29b-41d4-a716-446655440003',
        '550e8400-e29b-41d4-a716-446655440000',
        'cajero1_mycasino',
        '$2a$11$3rQ8P.Qx5l1ZzKwA9HqbOeBK7QzC8VUJc6kR5xE8nP2uQ1mT9oXXu', -- password: admin123
        'CASHIER',
        'ACTIVE',
        CURRENT_TIMESTAMP
    );

-- Cajero 2  
INSERT INTO "BackofficeUsers" ("Id", "OperatorId", "Username", "PasswordHash", "Role", "Status", "CreatedAt")
VALUES 
    (
        '991e8400-e29b-41d4-a716-446655440004',
        '550e8400-e29b-41d4-a716-446655440000',
        'cajero2_mycasino',
        '$2a$11$3rQ8P.Qx5l1ZzKwA9HqbOeBK7QzC8VUJc6kR5xE8nP2uQ1mT9oXXu', -- password: admin123
        'CASHIER',
        'ACTIVE',
        CURRENT_TIMESTAMP
    );
```

---

## ?? **Paso 4: Crear Jugadores con Saldo**

### **4.1 Crear 4 Jugadores**
```sql
-- Jugador 1
INSERT INTO "Players" ("Id", "BrandId", "Username", "Email", "ExternalId", "Status", "CreatedAt")
VALUES 
    (
        'aa1e8400-e29b-41d4-a716-446655440005',
        '661e8400-e29b-41d4-a716-446655440001',
        'jugador1',
        'jugador1@mycasino.local',
        'ext_001',
        'ACTIVE',
        CURRENT_TIMESTAMP
    );

-- Jugador 2
INSERT INTO "Players" ("Id", "BrandId", "Username", "Email", "ExternalId", "Status", "CreatedAt")
VALUES 
    (
        'bb1e8400-e29b-41d4-a716-446655440006',
        '661e8400-e29b-41d4-a716-446655440001',
        'jugador2',
        'jugador2@mycasino.local',
        'ext_002',
        'ACTIVE',
        CURRENT_TIMESTAMP
    );

-- Jugador 3
INSERT INTO "Players" ("Id", "BrandId", "Username", "Email", "ExternalId", "Status", "CreatedAt")
VALUES 
    (
        'cc1e8400-e29b-41d4-a716-446655440007',
        '661e8400-e29b-41d4-a716-446655440001',
        'jugador3',
        'jugador3@mycasino.local',
        'ext_003',
        'ACTIVE',
        CURRENT_TIMESTAMP
    );

-- Jugador 4
INSERT INTO "Players" ("Id", "BrandId", "Username", "Email", "ExternalId", "Status", "CreatedAt")
VALUES 
    (
        'dd1e8400-e29b-41d4-a716-446655440008',
        '661e8400-e29b-41d4-a716-446655440001',
        'jugador4',
        'jugador4@mycasino.local',
        'ext_004',
        'ACTIVE',
        CURRENT_TIMESTAMP
    );
```

### **4.2 Crear Wallets con Saldo Inicial**
```sql
-- Wallets con 100.00 EUR = 10000 centavos cada uno
INSERT INTO "Wallets" ("PlayerId", "BalanceBigint")
VALUES 
    ('aa1e8400-e29b-41d4-a716-446655440005', 10000),
    ('bb1e8400-e29b-41d4-a716-446655440006', 10000),
    ('cc1e8400-e29b-41d4-a716-446655440007', 10000),
    ('dd1e8400-e29b-41d4-a716-446655440008', 10000);
```

### **4.3 Registrar Saldo Inicial en Ledger**
```sql
-- Entradas de ledger para el saldo inicial
INSERT INTO "Ledger" ("OperatorId", "BrandId", "PlayerId", "DeltaBigint", "Reason", "ExternalRef", "Meta", "CreatedAt")
VALUES 
    (
        '550e8400-e29b-41d4-a716-446655440000',
        '661e8400-e29b-41d4-a716-446655440001',
        'aa1e8400-e29b-41d4-a716-446655440005',
        10000,
        'ADMIN_GRANT',
        'initial_balance_jugador1',
        '{"reason": "Saldo inicial de bienvenida", "admin": "system"}',
        CURRENT_TIMESTAMP
    ),
    (
        '550e8400-e29b-41d4-a716-446655440000',
        '661e8400-e29b-41d4-a716-446655440001',
        'bb1e8400-e29b-41d4-a716-446655440006',
        10000,
        'ADMIN_GRANT',
        'initial_balance_jugador2',
        '{"reason": "Saldo inicial de bienvenida", "admin": "system"}',
        CURRENT_TIMESTAMP
    ),
    (
        '550e8400-e29b-41d4-a716-446655440000',
        '661e8400-e29b-41d4-a716-446655440001',
        'cc1e8400-e29b-41d4-a716-446655440007',
        10000,
        'ADMIN_GRANT',
        'initial_balance_jugador3',
        '{"reason": "Saldo inicial de bienvenida", "admin": "system"}',
        CURRENT_TIMESTAMP
    ),
    (
        '550e8400-e29b-41d4-a716-446655440000',
        '661e8400-e29b-41d4-a716-446655440001',
        'dd1e8400-e29b-41d4-a716-446655440008',
        10000,
        'ADMIN_GRANT',
        'initial_balance_jugador4',
        '{"reason": "Saldo inicial de bienvenida", "admin": "system"}',
        CURRENT_TIMESTAMP
    );
```

---

## ?? **Paso 5: Asignar Jugadores a Cajeros**

```sql
-- Cajero 1 gestiona jugadores 1 y 2
INSERT INTO "CashierPlayers" ("CashierId", "PlayerId", "AssignedAt")
VALUES 
    ('881e8400-e29b-41d4-a716-446655440003', 'aa1e8400-e29b-41d4-a716-446655440005', CURRENT_TIMESTAMP),
    ('881e8400-e29b-41d4-a716-446655440003', 'bb1e8400-e29b-41d4-a716-446655440006', CURRENT_TIMESTAMP);

-- Cajero 2 gestiona jugadores 3 y 4
INSERT INTO "CashierPlayers" ("CashierId", "PlayerId", "AssignedAt")
VALUES 
    ('991e8400-e29b-41d4-a716-446655440004', 'cc1e8400-e29b-41d4-a716-446655440007', CURRENT_TIMESTAMP),
    ('991e8400-e29b-41d4-a716-446655440004', 'dd1e8400-e29b-41d4-a716-446655440008', CURRENT_TIMESTAMP);
```

---

## ?? **Paso 6: Configurar Juegos de Ejemplo**

### **6.1 Crear Juegos Base**
```sql
-- Crear algunos juegos de ejemplo
INSERT INTO "Games" ("Id", "Code", "Provider", "Name", "Enabled", "CreatedAt")
VALUES 
    ('game001-0000-0000-0000-000000000001', 'slot_777', 'dummy', 'Lucky 777 Slot', true, CURRENT_TIMESTAMP),
    ('game002-0000-0000-0000-000000000002', 'blackjack_classic', 'dummy', 'Blackjack Clásico', true, CURRENT_TIMESTAMP),
    ('game003-0000-0000-0000-000000000003', 'roulette_european', 'dummy', 'Ruleta Europea', true, CURRENT_TIMESTAMP),
    ('game004-0000-0000-0000-000000000004', 'poker_holdem', 'dummy', 'Texas Hold\'em Poker', true, CURRENT_TIMESTAMP);
```

### **6.2 Asignar Juegos al Brand**
```sql
-- Habilitar todos los juegos para nuestro brand
INSERT INTO "BrandGames" ("BrandId", "GameId", "Enabled", "DisplayOrder", "Tags")
VALUES 
    ('661e8400-e29b-41d4-a716-446655440001', 'game001-0000-0000-0000-000000000001', true, 1, 'slots,popular'),
    ('661e8400-e29b-41d4-a716-446655440001', 'game002-0000-0000-0000-000000000002', true, 2, 'table,cards'),
    ('661e8400-e29b-41d4-a716-446655440001', 'game003-0000-0000-0000-000000000003', true, 3, 'table,classic'),
    ('661e8400-e29b-41d4-a716-446655440001', 'game004-0000-0000-0000-000000000004', true, 4, 'poker,cards');
```

---

## ?? **Paso 7: Configurar Provider HMAC**

```sql
-- Configurar el proveedor dummy con su secreto HMAC
INSERT INTO "BrandProviderConfigs" ("BrandId", "ProviderCode", "Secret", "AllowNegativeOnRollback", "Meta", "CreatedAt", "UpdatedAt")
VALUES 
    (
        '661e8400-e29b-41d4-a716-446655440001',
        'dummy',
        'mi_secreto_hmac_super_seguro_32_chars',
        false,
        '{"webhookUrl": "https://mycasino.local/api/v1/gateway", "timeout": 30}',
        CURRENT_TIMESTAMP,
        CURRENT_TIMESTAMP
    );
```

---

## ?? **Paso 8: Configuración de Hosts (Local)**

### **8.1 Archivo hosts (Windows: C:\Windows\System32\drivers\etc\hosts)**
```
# Casino Platform - MiCasino
127.0.0.1    mycasino.local
127.0.0.1    admin.mycasino.local
```

### **8.2 Configuración de CORS en appsettings.json**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=casino_platform;Username=postgres;Password=tu_password"
  },
  "Auth": {
    "Issuer": "casino",
    "JwtKey": "mi_clave_jwt_super_segura_de_32_caracteres_minimo"
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "https://mycasino.local",
      "https://admin.mycasino.local"
    ]
  }
}
```

---

## ?? **Paso 9: Testing & Verificación**

### **9.1 Verificar Configuración de Base de Datos**
```sql
-- Verificar que todo está configurado correctamente
SELECT 
    o."Name" as operator_name,
    b."Code" as brand_code,
    b."Domain" as domain,
    b."AdminDomain" as admin_domain,
    b."Status" as brand_status
FROM "Brands" b
JOIN "Operators" o ON b."OperatorId" = o."Id"
WHERE b."Code" = 'mycasino';

-- Verificar usuarios de backoffice
SELECT 
    "Username",
    "Role",
    "Status"
FROM "BackofficeUsers"
WHERE "OperatorId" = '550e8400-e29b-41d4-a716-446655440000'
ORDER BY "Role";

-- Verificar jugadores y saldos
SELECT 
    p."Username",
    p."Email",
    p."Status",
    w."BalanceBigint" / 100.0 as balance_euros
FROM "Players" p
JOIN "Wallets" w ON p."Id" = w."PlayerId"
WHERE p."BrandId" = '661e8400-e29b-41d4-a716-446655440001'
ORDER BY p."Username";

-- Verificar asignaciones cajero-jugador
SELECT 
    bu."Username" as cajero,
    p."Username" as jugador
FROM "CashierPlayers" cp
JOIN "BackofficeUsers" bu ON cp."CashierId" = bu."Id"
JOIN "Players" p ON cp."PlayerId" = p."Id"
ORDER BY bu."Username", p."Username";
```

### **9.2 Testing de Autenticación**

#### **Login Admin**
```bash
curl -X POST \
  -H "Host: admin.mycasino.local" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin_mycasino","password":"admin123"}' \
  http://localhost:5000/api/v1/admin/auth/login
```

#### **Login Cajeros**
```bash
# Cajero 1
curl -X POST \
  -H "Host: admin.mycasino.local" \
  -H "Content-Type: application/json" \
  -d '{"username":"cajero1_mycasino","password":"admin123"}' \
  http://localhost:5000/api/v1/admin/auth/login

# Cajero 2
curl -X POST \
  -H "Host: admin.mycasino.local" \
  -H "Content-Type: application/json" \
  -d '{"username":"cajero2_mycasino","password":"admin123"}' \
  http://localhost:5000/api/v1/admin/auth/login
```

#### **Acceso a Recursos Protegidos**
```bash
# Listar brands (requiere admin token)
curl -H "Host: admin.mycasino.local" \
     -H "Authorization: Bearer <ADMIN_TOKEN>" \
     http://localhost:5000/api/v1/admin/brands

# Listar jugadores (requiere admin o cajero token)
curl -H "Host: admin.mycasino.local" \
     -H "Authorization: Bearer <TOKEN>" \
     http://localhost:5000/api/v1/admin/players
```

### **9.3 Testing de Resolución de Brand**
```bash
# Catálogo público (resuelve brand por host)
curl -H "Host: mycasino.local" \
     http://localhost:5000/api/v1/catalog/games

# Debe devolver solo los juegos habilitados para el brand 'mycasino'
```

---

## ?? **Resultado Final**

Al completar esta guía tendrás:

### **? Estructura Organizacional**
- **1 Operador**: MiCasino Corp
- **1 Brand**: mycasino (con dominio mycasino.local)
- **Configuración CORS**: Lista de orígenes permitidos
- **Tema y Settings**: Personalizables vía JSON

### **? Usuarios de Backoffice**
- **1 Admin**: `admin_mycasino` (OPERATOR_ADMIN) - Acceso total
- **2 Cajeros**: `cajero1_mycasino`, `cajero2_mycasino` (CASHIER) - Gestión de jugadores asignados

### **? Jugadores Activos**
- **4 Jugadores**: jugador1, jugador2, jugador3, jugador4
- **Saldo inicial**: 100.00 EUR cada uno (10000 centavos)
- **Emails configurados**: Para notificaciones futuras
- **External IDs**: Para integración con sistemas externos

### **? Asignaciones**
- **Cajero 1**: Gestiona jugador1 y jugador2
- **Cajero 2**: Gestiona jugador3 y jugador4

### **? Catálogo de Juegos**
- **4 Juegos habilitados**: Slot, Blackjack, Ruleta, Poker
- **Configuración de proveedor**: HMAC y settings para 'dummy'
- **Tags organizados**: Para filtrado y categorización

### **? Seguridad Configurada**
- **Autenticación JWT**: Separada para admin y players
- **Cookies HttpOnly**: Para SPAs y navegadores
- **CORS dinámico**: Basado en configuración de brand
- **HMAC Gateway**: Para callbacks de proveedores

---

## ?? **Próximos Pasos**

1. **Frontend Development**: Crear interfaces React para backoffice y site
2. **Game Integration**: Integrar proveedores reales de juegos
3. **Payment Gateway**: Configurar métodos de pago
4. **Reporting**: Implementar dashboards y reportes
5. **Monitoring**: Agregar logging y alertas
6. **Mobile**: Optimizar para dispositivos móviles

---

## ?? **Credenciales de Acceso**

### **Backoffice (admin.mycasino.local)**
| Usuario | Password | Rol | Permisos |
|---------|----------|-----|----------|
| admin_mycasino | admin123 | OPERATOR_ADMIN | Gestión completa del operador |
| cajero1_mycasino | admin123 | CASHIER | Gestión jugador1, jugador2 |
| cajero2_mycasino | admin123 | CASHIER | Gestión jugador3, jugador4 |

### **Players (mycasino.local)**
| Usuario | External ID | Email | Saldo Inicial |
|---------|-------------|-------|---------------|
| jugador1 | ext_001 | jugador1@mycasino.local | 100.00 EUR |
| jugador2 | ext_002 | jugador2@mycasino.local | 100.00 EUR |
| jugador3 | ext_003 | jugador3@mycasino.local | 100.00 EUR |
| jugador4 | ext_004 | jugador4@mycasino.local | 100.00 EUR |

**¡Tu sitio de casino está listo para funcionar! ???**
# Casino Platform - Gu�a Completa del Proyecto

## ?? **�Qu� es Casino Platform?**

Casino Platform es una **plataforma B2B de casino online con fichas virtuales** desarrollada con **.NET 9**, **PostgreSQL** y **React**. Est� dise�ada para operadores que quieren ofrecer servicios de casino a m�ltiples marcas (multi-tenant) con un wallet virtual seguro y integraciones con proveedores de juegos externos.

### ??? **Arquitectura del Sistema**

```mermaid
flowchart TB
    subgraph "Frontend Layer"
        WEB[React Web App]
        BACKOFFICE[React Backoffice]
    end
    
    subgraph "API Layer"
        API[ASP.NET Core 9 API<br/>Minimal APIs + JWT Auth]
        MIDDLEWARE[BrandResolver + CORS<br/>Dynamic Middleware]
    end
    
    subgraph "Business Layer"
        WALLET[Wallet Service<br/>Ledger Append-Only]
        GAMES[Game Service<br/>Sessions & Rounds]
        GATEWAY[Gateway Service<br/>HMAC Security]
        BRAND[Brand Service<br/>Multi-tenant]
    end
    
    subgraph "Data Layer"
        DB[(PostgreSQL<br/>Multi-tenant Schema)]
    end
    
    subgraph "External"
        PROVIDERS[Game Providers<br/>HMAC Callbacks]
    end
    
    WEB --> API
    BACKOFFICE --> API
    API --> MIDDLEWARE
    MIDDLEWARE --> WALLET
    MIDDLEWARE --> GAMES
    MIDDLEWARE --> GATEWAY
    MIDDLEWARE --> BRAND
    WALLET --> DB
    GAMES --> DB
    GATEWAY --> DB
    BRAND --> DB
    PROVIDERS --> GATEWAY
```

---

## ?? **Funcionalidades Principales**

### ?? **Multi-Tenancy**
- **Operadores**: Clientes B2B que poseen m�ltiples marcas
- **Marcas**: Sites individuales con dominios propios (ej: `casinoA.com`, `bet30test.netlify.app`)
- **Resoluci�n Autom�tica**: Cada dominio se resuelve autom�ticamente a su marca correspondiente

### ?? **Sistema de Usuarios**
- **Players**: Usuarios finales que juegan en las marcas
- **Backoffice Users**: Administradores con diferentes roles:
  - `SUPER_ADMIN`: Acceso total al sistema
  - `OPERATOR_ADMIN`: Gesti�n de marcas de su operador
  - `CASHIER`: Gesti�n de jugadores asignados

### ?? **Wallet Virtual**
- **Fichas virtuales**: Moneda interna manejada como enteros
- **Ledger append-only**: Historial inmutable de transacciones
- **Saldo nunca negativo**: Validaci�n estricta de fondos
- **Idempotencia**: Transacciones �nicas por `external_ref`

### ?? **Game Gateway**
- **Sesiones de juego**: Control de sesiones activas
- **Callbacks HMAC**: Seguridad en comunicaciones con proveedores
- **Bet/Win/Rollback**: Operaciones transaccionales est�ndar
- **Rounds**: Agrupaci�n de transacciones por ronda de juego

### ?? **Autenticaci�n H�brida**
- **JWT + Cookies**: Soporte para SPAs y navegadores tradicionales
- **Separaci�n por audiencia**: Admin (`backoffice`) y Player (`player`)
- **M�ltiples esquemas**: Bearer tokens y cookies HttpOnly

---

## ??? **Modelo de Datos**

### **Entidades Principales**

```sql
-- Operadores (clientes B2B)
CREATE TABLE operators (
    id UUID PRIMARY KEY,
    name TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'ACTIVE',
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Marcas (sites individuales)
CREATE TABLE brands (
    id UUID PRIMARY KEY,
    operator_id UUID REFERENCES operators(id),
    code TEXT UNIQUE NOT NULL,
    name TEXT NOT NULL,
    domain TEXT UNIQUE, -- Dominio principal (bet30test.netlify.app)
    admin_domain TEXT UNIQUE, -- Dominio admin (admin.bet30test.netlify.app)
    cors_origins TEXT[], -- Or�genes CORS permitidos
    theme JSONB,
    settings JSONB,
    status TEXT NOT NULL DEFAULT 'ACTIVE',
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Jugadores
CREATE TABLE players (
    id UUID PRIMARY KEY,
    brand_id UUID REFERENCES brands(id),
    external_id TEXT,
    username TEXT NOT NULL,
    email TEXT,
    status TEXT NOT NULL DEFAULT 'ACTIVE',
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    UNIQUE(brand_id, username),
    UNIQUE(brand_id, external_id) NULLS NOT DISTINCT
);

-- Wallet (saldo por jugador)
CREATE TABLE wallets (
    player_id UUID PRIMARY KEY REFERENCES players(id),
    balance_bigint BIGINT NOT NULL DEFAULT 0
);

-- Ledger (historial append-only)
CREATE TABLE ledger (
    id BIGSERIAL PRIMARY KEY,
    operator_id UUID REFERENCES operators(id),
    brand_id UUID REFERENCES brands(id),
    player_id UUID REFERENCES players(id),
    delta_bigint BIGINT NOT NULL,
    reason TEXT NOT NULL, -- BET, WIN, BONUS, ADMIN_GRANT, etc.
    round_id UUID,
    game_code TEXT,
    provider TEXT,
    external_ref TEXT,
    meta JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    UNIQUE(external_ref) WHERE external_ref IS NOT NULL
);
```

---

## ?? **C�mo Usar el Sistema**

### **1. Configuraci�n Inicial**

#### **Paso 1: Levantar Infrastructure**
```bash
# Levantar PostgreSQL
docker compose -f infra/docker-compose.yml up -d

# Migrar base de datos
dotnet ef database update --project apps/Casino.Infrastructure --startup-project apps/api/Casino.Api
```

#### **Paso 2: Configurar Autenticaci�n**
```json
// appsettings.json
{
  "Auth": {
    "Issuer": "casino",
    "JwtKey": "REEMPLAZAR_POR_CLAVE_DE_32_O_MAS_CARACTERES_EN_PRODUCCION"
  },
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=casino_platform;Username=postgres;Password=postgres"
  }
}
```

#### **Paso 3: Ejecutar la API**
```bash
dotnet watch --project apps/api/Casino.Api
```

La API estar� disponible en: `http://localhost:5000`  
Swagger UI disponible en: `http://localhost:5000`

### **2. Autenticaci�n**

#### **Login Admin**
```bash
curl -X POST -H "Host: admin.bet30test.netlify.app" \
     -H "Content-Type: application/json" \
     -d '{"username":"superadmin","password":"password123"}' \
     http://localhost:5000/api/v1/admin/auth/login
```

#### **Login Player**
```bash
curl -X POST -H "Host: bet30test.netlify.app" \
     -H "Content-Type: application/json" \
     -d '{"username":"player1_bet30","password":"demo"}' \
     http://localhost:5000/api/v1/auth/login
```

### **3. Testing Game Gateway (HMAC)**

#### **Generar HMAC Signature**
```bash
# Ejemplo en bash (requiere openssl)
PROVIDER="dummy"
SECRET="your-hmac-secret-key"
PAYLOAD='{"sessionId":"550e8400-e29b-41d4-a716-446655440000","playerId":"123","amount":1000,"roundId":"550e8400-e29b-41d4-a716-446655440001","txId":"tx_001"}'

SIGNATURE=$(echo -n "$PAYLOAD" | openssl dgst -sha256 -hmac "$SECRET" -binary | base64)

curl -X POST http://localhost:5000/api/v1/gateway/bet \
     -H "Content-Type: application/json" \
     -H "X-Provider: $PROVIDER" \
     -H "X-Signature: $SIGNATURE" \
     -d "$PAYLOAD"
```

---

## ?? **C�mo Crear un Nuevo Site (Marca)**

### **Requisitos Completos para un Nuevo Site**

Para crear un site completamente funcional, necesitas:

1. ? **Operador** (tabla `operators`)
2. ? **Marca** (tabla `brands`) con dominio configurado
3. ? **Juegos** (tabla `games` + `brand_games`) 
4. ? **Usuarios Admin** (tabla `backoffice_users`)
5. ? **Jugadores** (tabla `players` + `wallets`)
6. ? **Configuraci�n de Proveedores** (tabla `brand_provider_configs`)

### **Proceso Paso a Paso**

#### **Paso 1: Crear Operador**
```bash
# ? IMPLEMENTADO - Endpoint disponible
# POST /api/v1/admin/operators
curl -X POST -H "Authorization: Bearer <SUPER_ADMIN_JWT>" \
     -H "Content-Type: application/json" \
     -d '{"name": "Mi Casino Corp", "status": "ACTIVE"}' \
     http://localhost:5000/api/v1/admin/operators
```

#### **Paso 2: Crear Marca**
```bash
curl -X POST -H "Authorization: Bearer <ADMIN_JWT>" \
     -H "Content-Type: application/json" \
     -d '{
       "operatorId": "operator-uuid",
       "code": "mycasino",
       "name": "Mi Casino Online",
       "domain": "mycasino.com",
       "adminDomain": "admin.mycasino.com",
       "corsOrigins": ["https://mycasino.com", "https://admin.mycasino.com"],
       "locale": "es-ES",
       "theme": {
         "primaryColor": "#FF6B35",
         "logo": "https://mycasino.com/logo.png"
       },
       "settings": {
         "allowRegistration": true,
         "maxBetAmount": 100000,
         "currency": "EUR"
       }
     }' \
     http://localhost:5000/api/v1/admin/brands
```

#### **Paso 3: Configurar Juegos para la Marca**
```bash
# ? IMPLEMENTADO - Endpoint disponible
# POST /api/v1/admin/brands/{brandId}/games
curl -X POST -H "Authorization: Bearer <ADMIN_JWT>" \
     -H "Content-Type: application/json" \
     -d '{"gameId": "game-uuid", "enabled": true, "displayOrder": 1, "tags": ["slots", "popular"]}' \
     http://localhost:5000/api/v1/admin/brands/{brandId}/games
```

#### **Paso 4: Crear Usuario Admin de la Marca**
```bash
# ? IMPLEMENTADO - Endpoint disponible
# POST /api/v1/admin/users
curl -X POST -H "Authorization: Bearer <SUPER_ADMIN_JWT>" \
     -H "Content-Type: application/json" \
     -d '{"username": "admin_mycasino", "password": "SecurePass123!", "role": "OPERATOR_ADMIN", "operatorId": "operator-uuid"}' \
     http://localhost:5000/api/v1/admin/users
```

#### **Paso 5: Configurar Proveedor HMAC**
```bash
curl -X PUT -H "Authorization: Bearer <ADMIN_JWT>" \
     -H "Content-Type: application/json" \
     -d '{
       "secret": "mycasino-hmac-secret-key-256bit",
       "allowNegativeOnRollback": false,
       "meta": {
         "environment": "production",
         "webhookUrl": "https://provider.example.com/webhook"
       }
     }' \
     http://localhost:5000/api/v1/admin/brands/{brandId}/providers/dummy
```

#### **Paso 6: Crear Jugadores de Prueba**
```bash
# ? IMPLEMENTADO - Endpoint disponible
# POST /api/v1/admin/players
curl -X POST -H "Authorization: Bearer <ADMIN_JWT>" \
     -H "Content-Type: application/json" \
     -d '{"brandId": "brand-uuid", "username": "player1", "email": "player1@mycasino.com", "initialBalance": 100000}' \
     http://localhost:5000/api/v1/admin/players
```

### **?? Endpoints Faltantes para Crear un Site Completo**

~~Los siguientes endpoints **NO EST�N IMPLEMENTADOS** y son necesarios:~~

**? TODOS LOS ENDPOINTS EST�N AHORA IMPLEMENTADOS:**

1. **Gesti�n de Operadores**
   - `POST /api/v1/admin/operators` - ? **IMPLEMENTADO**
   - `GET /api/v1/admin/operators` - ? **IMPLEMENTADO** 
   - `PATCH /api/v1/admin/operators/{id}` - ? **IMPLEMENTADO**

2. **Gesti�n de Juegos a Nivel Global**
   - `POST /api/v1/admin/games` - ? **IMPLEMENTADO**
   - `GET /api/v1/admin/games` - ? **IMPLEMENTADO**

3. **Asignaci�n de Juegos a Marcas**
   - `POST /api/v1/admin/brands/{brandId}/games` - ? **IMPLEMENTADO**
   - `DELETE /api/v1/admin/brands/{brandId}/games/{gameId}` - ? **IMPLEMENTADO**
   - `PATCH /api/v1/admin/brands/{brandId}/games/{gameId}` - ? **IMPLEMENTADO**

4. **Gesti�n de Usuarios Backoffice**
   - `POST /api/v1/admin/users` - ? **IMPLEMENTADO**
   - `GET /api/v1/admin/users` - ? **IMPLEMENTADO**
   - `PATCH /api/v1/admin/users/{id}` - ? **IMPLEMENTADO**

5. **Gesti�n de Jugadores**
   - `POST /api/v1/admin/players` - ? **IMPLEMENTADO**
   - `GET /api/v1/admin/players` - ? **IMPLEMENTADO**
   - `PATCH /api/v1/admin/players/{id}/status` - ? **IMPLEMENTADO**
   - `POST /api/v1/admin/players/{id}/wallet/adjust` - ? **IMPLEMENTADO**

6. **Gesti�n de Sesiones de Juego**
   - `POST /api/v1/catalog/games/{code}/launch` - ? **IMPLEMENTADO**
   - `POST /api/v1/admin/sessions/close-expired` - ? **OPCIONAL** (para el futuro)

---

## ?? **Endpoints Disponibles**

### **?? P�blicos (Sin Autenticaci�n)**

#### **Gateway (HMAC Protegido)**
- `POST /api/v1/gateway/balance` - Consultar saldo
- `POST /api/v1/gateway/bet` - Realizar apuesta
- `POST /api/v1/gateway/win` - Procesar ganancia
- `POST /api/v1/gateway/rollback` - Revertir transacci�n
- `POST /api/v1/gateway/closeRound` - Cerrar ronda

#### **Cat�logo P�blico**
- `GET /api/v1/catalog/games` - Listar juegos (brand-scoped)
- `POST /api/v1/catalog/games/{code}/launch` - Lanzar juego

#### **Autenticaci�n**
- `POST /api/v1/admin/auth/login` - Login admin
- `POST /api/v1/auth/login` - Login player

### **?? Protegidos - Admin (Backoffice)**

#### **Autenticaci�n Admin**
- `GET /api/v1/admin/auth/me` - Perfil admin actual
- `POST /api/v1/admin/auth/logout` - Logout admin

#### **Gesti�n de Marcas**
- `POST /api/v1/admin/brands` - Crear marca
- `GET /api/v1/admin/brands` - Listar marcas
- `GET /api/v1/admin/brands/{id}` - Obtener marca
- `PATCH /api/v1/admin/brands/{id}` - Actualizar marca
- `DELETE /api/v1/admin/brands/{id}` - Eliminar marca
- `POST /api/v1/admin/brands/{id}/status` - Cambiar status
- `GET /api/v1/admin/brands/by-host/{host}` - Buscar por host
- `GET /api/v1/admin/brands/{id}/catalog` - Cat�logo de la marca

#### **Configuraci�n de Marcas**
- `GET /api/v1/admin/brands/{id}/settings` - Obtener configuraci�n
- `PUT /api/v1/admin/brands/{id}/settings` - Reemplazar configuraci�n
- `PATCH /api/v1/admin/brands/{id}/settings` - Actualizar configuraci�n parcial

#### **Configuraci�n de Proveedores**
- `GET /api/v1/admin/brands/{id}/providers` - Listar proveedores
- `PUT /api/v1/admin/brands/{id}/providers/{code}` - Configurar proveedor
- `POST /api/v1/admin/brands/{id}/providers/{code}/rotate-secret` - Rotar secreto

#### **Gesti�n de Juegos**
- `GET /api/v1/admin/games` - Listar juegos globales
- `POST /api/v1/admin/games` - Crear juego

#### **Gesti�n de Jugadores**
- `GET /api/v1/admin/players` - Listar jugadores
- `PATCH /api/v1/admin/players/{id}/status` - Cambiar status jugador

#### **Wallet Interno**
- `POST /api/v1/admin/wallet/balance` - Consultar saldo
- `POST /api/v1/admin/wallet/debit` - D�bito interno
- `POST /api/v1/admin/wallet/credit` - Cr�dito interno
- `POST /api/v1/admin/wallet/rollback` - Rollback interno

#### **Sesiones y Rondas**
- `POST /api/v1/admin/sessions` - Crear sesi�n
- `GET /api/v1/admin/sessions/{id}` - Obtener sesi�n
- `POST /api/v1/admin/sessions/{id}/close` - Cerrar sesi�n
- `POST /api/v1/admin/rounds` - Crear ronda
- `POST /api/v1/admin/rounds/{id}/close` - Cerrar ronda

### **?? Protegidos - Player**

#### **Autenticaci�n Player**
- `GET /api/v1/auth/me` - Perfil player actual
- `POST /api/v1/auth/logout` - Logout player

---

## ?? **Estado Actual del Proyecto**

### ? **Implementado (100%)**
- **Autenticaci�n h�brida JWT + Cookies**
- **Multi-tenancy con resoluci�n por Host**
- **Wallet virtual con ledger append-only**
- **Game Gateway con seguridad HMAC**
- **Gesti�n completa de marcas**
- **Configuraci�n de proveedores por marca**
- **Cat�logo p�blico de juegos**
- **Lanzamiento de sesiones de juego**
- **Auditor�a de acciones admin y proveedor**
- **Gesti�n completa de operadores** ? **NUEVO**
- **Gesti�n completa de usuarios backoffice** ? **NUEVO**
- **Creaci�n de jugadores con balance inicial** ? **NUEVO**
- **Ajustes de wallet por admin/cajero** ? **NUEVO**
- **Asignaci�n de juegos a marcas** ? **NUEVO**

### ?? **Parcialmente Implementado**
- ~~**Gesti�n de jugadores** (falta creaci�n y ajustes de wallet)~~ ? **COMPLETADO**
- ~~**Gesti�n de juegos** (falta asignaci�n a marcas)~~ ? **COMPLETADO**

### ? **Faltante para Producci�n**
- ~~**Gesti�n de operadores** (CRUD completo)~~ ? **COMPLETADO**
- ~~**Gesti�n de usuarios backoffice** (CRUD completo)~~ ? **COMPLETADO**
- ~~**Creaci�n de jugadores desde backoffice**~~ ? **COMPLETADO**
- ~~**Ajustes de wallet por admin/cajero**~~ ? **COMPLETADO**
- **Asignaci�n cajero ? jugadores** (para implementar en el futuro)
- **Dashboard de reportes y KPIs** (para implementar en el futuro)
- **Gesti�n de sesiones expiradas** (para implementar en el futuro)
- **Password hashing para players** (recomendado para producci�n)
- **Rate limiting en endpoints cr�ticos** (recomendado para producci�n)
- **Refresh tokens** (recomendado para producci�n)

---

## ?? **Ventajas del Sistema**

### ?? **Seguridad Robusta**
- Autenticaci�n separada admin/player
- Cookies HttpOnly + Bearer tokens
- Firmas HMAC en game gateway
- Validaciones de negocio estrictas

### ?? **Multi-Site Ready**
- Resoluci�n autom�tica por dominio
- CORS din�mico por marca
- Configuraci�n aislada por site
- Temas y settings personalizables

### ?? **Wallet Confiable**
- Ledger inmutable (append-only)
- Idempotencia estricta
- Saldo nunca negativo
- Auditor�a completa

### ?? **Game Integration**
- Est�ndar de proveedores
- Sesiones y rondas controladas
- Callbacks seguros HMAC
- Soporte para rollbacks

### ??? **Arquitectura Escalable**
- Clean Architecture
- Minimal APIs performantes
- Servicios inyectados
- Logs estructurados

---

## ?? **Pr�ximos Pasos Recomendados**

### **Prioridad Alta**
1. **Implementar gesti�n de operadores** (CRUD completo)
2. **Completar gesti�n de usuarios backoffice**
3. **A�adir creaci�n de jugadores con wallet inicial**
4. **Implementar ajustes de wallet por admin**
5. **Password hashing para players**

### **Prioridad Media**
1. **Dashboard de reportes b�sicos**
2. **Gesti�n de sesiones expiradas**
3. **Rate limiting en autenticaci�n**
4. **Relaci�n cajero ? jugadores**

### **Prioridad Baja**
1. **Refresh tokens**
2. **2FA para admins**
3. **Integraci�n con proveedores reales**
4. **Frontend React completo**

---

## ?? **Recursos Adicionales**

- **Swagger UI**: `http://localhost:5000` (cuando la API est� corriendo)
- **Base de datos**: PostgreSQL en `localhost:5432` (docker)
- **Logs**: Estructurados con correlation IDs
- **Tests**: Cobertura en servicios cr�ticos (wallet, gateway)

**�El sistema est� listo para desarrollo activo con una base s�lida y arquitectura escalable! ???**
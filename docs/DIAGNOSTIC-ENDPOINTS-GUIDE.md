# ?? Guía de Diagnóstico y Corrección del Dashboard

## ?? Objetivo

Esta guía te permite **diagnosticar y corregir** el problema del dashboard desde el código, sin tocar SQL manualmente.

---

## ?? **Nuevos Endpoints Creados**

### 1. **`GET /api/v1/admin/diagnostics/system-status`**

**Propósito**: Obtener un resumen completo del estado del sistema.

**Autenticación**: Requiere SUPER_ADMIN

**Response**:
```json
{
  "brands": [
    {
      "id": "11111111-1111-1111-1111-111111111111",
      "code": "bet30",
      "name": "Bet30 Casino",
      "status": "ACTIVE"
    }
  ],
  "backofficeUsers": [
    {
      "id": "...",
   "username": "superadmin",
      "role": "SUPER_ADMIN",
 "walletBalance": 490.00,
   "brandId": "...",
    "brandName": "Bet30 Casino",
      "parentAdminId": null,
      "parentAdminUsername": null,
      "commissionPercent": 0,
      "hierarchyLevel": 0,
      "createdByUserId": null,
      "createdAt": "2025-01-22T..."
    }
  ],
  "players": [
    {
      "id": "...",
      "username": "player1",
      "walletBalance": 500.00,
      "status": "ACTIVE",
   "brandId": "...",
    "brandName": "Bet30 Casino",
      "createdByUserId": "...",
      "createdByUsername": "cashier1",
      "createdAt": "2025-01-22T..."
    }
  ],
  "walletTransactions": [
    {
      "id": "...",
   "transactionType": "TRANSFER",
      "fromUserId": "...",
   "fromUserType": "BACKOFFICE",
"toUserId": "...",
      "toUserType": "PLAYER",
      "amount": 500.00,
      "description": "Initial transfer to Player1",
      "createdByUserId": "...",
      "createdByRole": "CASHIER",
      "createdAt": "2025-01-22T..."
    }
  ],
  "ledger": [...],
  "summary": {
    "totalBrands": 1,
    "totalBackofficeUsers": 3,
    "superAdmins": 1,
    "brandAdmins": 0,
    "cashiers": 2,
    "totalPlayers": 2,
    "playersWithoutCreator": 0,
    "totalWalletTransactions": 5,
    "totalLedgerEntries": 0,
    "totalBalanceBackoffice": 8750.00,
    "totalBalancePlayers": 1250.00
  }
}
```

### 2. **`POST /api/v1/admin/diagnostics/reset-and-initialize`**

**Propósito**: **Resetear todo el sistema** y crear una estructura inicial correcta.

**Autenticación**: Requiere SUPER_ADMIN

**?? ADVERTENCIA**: Este endpoint **BORRA TODOS LOS DATOS** y crea una estructura limpia.

**Qué hace**:
1. Borra todos los registros de:
   - `Ledger`
   - `WalletTransactions`
   - `CommissionAccruals`
   - `CashierPlayers`
   - `GameSessions`
   - `Rounds`
   - `Wallets`
   - `Players`
   - `BackofficeUsers`

2. Crea o verifica que existe el brand `bet30`.

3. Crea la siguiente estructura:
   - **1 SUPER_ADMIN** (`superadmin` / `password`)
     - Balance: $10,000
     - Con transacción MINT inicial

   - **2 Cashiers** (`cashier1` / `password`, `cashier2` / `password`)
     - cashier1: $1,500 (después de transfer)
     - cashier2: $2,250 (después de transfer)
     - Con comisiones: 10% y 15%
     - ParentAdminId = SUPER_ADMIN
     - Con transacciones TRANSFER desde SUPER_ADMIN

   - **2 Players** (`player1`, `player2`)
     - player1: $500 (creado por cashier1)
     - player2: $750 (creado por cashier2)
     - Con transacciones TRANSFER desde sus cashiers respectivos

4. Asigna players a cashiers en `CashierPlayers`.

**Response**:
```json
{
  "success": true,
  "message": "System reset and initialized successfully",
  "structure": {
    "brand": {
      "id": "11111111-1111-1111-1111-111111111111",
  "code": "bet30",
      "name": "Bet30 Casino"
    },
    "superAdmin": {
      "id": "...",
      "username": "superadmin",
      "balance": 5000.00
    },
    "cashiers": [
      {
  "id": "...",
        "username": "cashier1",
        "balance": 1500.00,
        "commission": 10
      },
 {
     "id": "...",
        "username": "cashier2",
        "balance": 2250.00,
        "commission": 15
      }
    ],
    "players": [
      {
        "id": "...",
     "username": "player1",
        "balance": 500.00,
     "createdBy": "cashier1"
      },
      {
        "id": "...",
      "username": "player2",
        "balance": 750.00,
        "createdBy": "cashier2"
      }
    ],
    "transactions": {
      "total": 5,
 "MINT": 1,
      "TRANSFER": 4
    },
    "balances": {
      "superAdmin": 5000.00,
      "cashier1": 1500.00,
  "cashier2": 2250.00,
      "player1": 500.00,
      "player2": 750.00,
      "total": 10000.00
    }
  }
}
```

---

## ?? **Cómo Usar**

### **Paso 1: Diagnosticar el Estado Actual**

Primero, inicia sesión como SUPER_ADMIN y obtén tu token:

```bash
# 1. Login
curl -X POST "http://localhost:5000/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "superadmin",
    "password": "password"
  }'

# Response incluirá:
# {
# "token": "eyJhbGc...",
#   "expiresAt": "..."
# }
```

Ahora consulta el estado del sistema:

```bash
# 2. Get system status
curl -X GET "http://localhost:5000/api/v1/admin/diagnostics/system-status" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" | jq
```

**Analiza el response**:
- ¿Hay usuarios sin `createdByUserId`?
- ¿Hay balances sin transacciones en `walletTransactions`?
- ¿`playersWithoutCreator` > 0?
- ¿`totalWalletTransactions` = 0 aunque haya balances?

### **Paso 2: Resetear y Crear Estructura Limpia**

Si hay problemas, ejecuta el reset:

```bash
# 3. Reset and initialize
curl -X POST "http://localhost:5000/api/v1/admin/diagnostics/reset-and-initialize" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" | jq
```

**?? CUIDADO**: Esto **BORRARÁ TODOS LOS DATOS**.

### **Paso 3: Verificar Dashboard**

Ahora inicia sesión nuevamente (porque se crearon nuevos usuarios) y verifica el dashboard:

```bash
# 4. Login con nuevo SUPER_ADMIN
curl -X POST "http://localhost:5000/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "superadmin",
    "password": "password"
  }'

# 5. Get dashboard
curl -X GET "http://localhost:5000/api/v1/admin/dashboard/overview?scope=TREE" \
  -H "Authorization: Bearer NEW_TOKEN_HERE" | jq
```

**Valores esperados**:
```json
{
  "finanzas": {
    "fichas": {
      "balanceActual": 1000000, // $10,000 en centavos
      "breakdown": {
        "houseBalance": 500000,   // $5,000 SUPER_ADMIN
     "cashiersBalance": 375000, // $3,750 total cashiers
  "playersBalance": 125000 // $1,250 total players
      }
    },
    "cargas": {
      "total": 125000, // $1,250 (transfers a players)
      "count": 2
    },
    "depositosA": {
      "total": 1000000, // $10,000 (MINT inicial)
   "count": 1
    }
  },
  "usuarios": {
    "totalJugadores": 2,
    "totalAgentes": 2,
    "jugadoresDirectos": 0,
    "agentesDirectos": 2
  },
  "casino": {
"jugado": 0,  // Sin apuestas aún
    "pagado": 0,
    "netwin": 0
  }
}
```

---

## ?? **Estructura de Datos Creada**

### Jerarquía

```
SUPER_ADMIN (superadmin)
??? Balance: $5,000
??? Comisión: 0%
??? Descendientes:
    ??? CASHIER (cashier1)
    ?   ??? Balance: $1,500
    ?   ??? Comisión: 10%
    ?   ??? Players:
    ?       ??? player1 (Balance: $500)
    ?
    ??? CASHIER (cashier2)
        ??? Balance: $2,250
        ??? Comisión: 15%
        ??? Players:
            ??? player2 (Balance: $750)
```

### Transacciones Creadas

1. **MINT** (SUPER_ADMIN):
   - `null` ? SUPER_ADMIN: $10,000

2. **TRANSFER** (Cashier1):
   - SUPER_ADMIN ? cashier1: $2,000

3. **TRANSFER** (Cashier2):
   - SUPER_ADMIN ? cashier2: $3,000

4. **TRANSFER** (Player1):
   - cashier1 ? player1: $500

5. **TRANSFER** (Player2):
   - cashier2 ? player2: $750

**Total**: 5 transacciones, balance total = $10,000

---

## ?? **Troubleshooting**

### Error: "Unauthorized"

**Causa**: Token expirado o inválido.

**Solución**: Haz login nuevamente:
```bash
curl -X POST "http://localhost:5000/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username": "superadmin", "password": "password"}'
```

### Error: "Access Denied"

**Causa**: No eres SUPER_ADMIN.

**Solución**: Estos endpoints solo funcionan con rol SUPER_ADMIN. Verifica tu token:
```bash
# Decodifica tu JWT en https://jwt.io
# Busca el claim "role": "SUPER_ADMIN"
```

### Dashboard sigue mostrando 0

**Posibles causas**:
1. **Scope incorrecto**: Usa `scope=TREE` en la query.
2. **Token antiguo**: Haz login nuevamente después del reset.
3. **Fecha incorrecta**: Verifica que `from` y `to` incluyan la fecha de creación.

**Solución**:
```bash
# Usar fecha actual
curl -X GET "http://localhost:5000/api/v1/admin/dashboard/overview?scope=TREE&from=$(date -u +%Y-%m-%dT00:00:00Z)&to=$(date -u +%Y-%m-%dT23:59:59Z)" \
  -H "Authorization: Bearer TOKEN"
```

---

## ? **Checklist de Validación**

Después del reset, verifica:

- [ ] **Brands**: Existe `bet30` con status ACTIVE
- [ ] **Users**: 1 SUPER_ADMIN + 2 Cashiers
- [ ] **Players**: 2 players con `createdByUserId` establecido
- [ ] **Transactions**: 5 transacciones (1 MINT + 4 TRANSFER)
- [ ] **Balances**: Total = $10,000
- [ ] **Jerarquía**: Players asignados a cashiers
- [ ] **Dashboard**: Muestra datos correctos

---

## ?? **Próximos Pasos**

### Crear Actividad de Casino

Si quieres ver datos en la sección **Casino** del dashboard:

```bash
# 1. Crear sesión para player1
curl -X POST "http://localhost:5000/api/v1/internal/sessions" \
  -H "Content-Type: application/json" \
  -d '{
    "playerId": "PLAYER1_ID",
    "gameCode": "slot-game-01",
    "provider": "pragmatic"
  }'

# 2. Hacer apuesta
curl -X POST "http://localhost:5000/api/v1/gateway/bet" \
  -H "Content-Type: application/json" \
  -H "X-Provider: pragmatic" \
  -H "X-Signature: ..." \
  -d '{
    "sessionId": "SESSION_ID",
"playerId": "PLAYER1_ID",
    "amount": 5000,
    "roundId": "ROUND_ID",
    "txId": "bet-001"
  }'
```

### Agregar Más Usuarios

Usa los endpoints de administración:

```bash
# Crear nuevo cashier
curl -X POST "http://localhost:5000/api/v1/admin/users" \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "cashier3",
    "password": "password",
    "role": "CASHIER",
    "commissionPercent": 12
  }'

# Crear nuevo player
curl -X POST "http://localhost:5000/api/v1/admin/users" \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "player3",
    "email": "player3@test.com"
  }'
```

---

**Archivo**: `docs/DIAGNOSTIC-ENDPOINTS-GUIDE.md`  
**Fecha**: 2025-01-22  
**Versión**: 1.0

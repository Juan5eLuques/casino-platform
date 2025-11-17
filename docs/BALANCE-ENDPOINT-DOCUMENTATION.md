# ?? Endpoint `/api/v1/balance` - Balance del Usuario Logueado

## ?? **Información General**

Endpoint unificado que retorna el balance del usuario logueado **automáticamente**, sin necesidad de pasar el `userId` en la URL. Funciona tanto para usuarios **BACKOFFICE** como **PLAYER**.

---

## ?? **Endpoint**

### **GET `/api/v1/balance`**

**Método**: `GET`  
**URL**: `https://api.tudominio.com/api/v1/balance`  
**Autenticación**: **Requerida** (JWT Bearer Token)  
**Soporta**: BackofficeJwt Y PlayerJwt

---

## ?? **Response Structure**

```typescript
interface UserBalanceResponse {
  userId: string;          // UUID del usuario
  userType: string;        // "BACKOFFICE" o "PLAYER"
  username: string;        // Nombre de usuario
  balance: number;  // Balance actual
  role: string | null;     // Rol (solo para BACKOFFICE)
  brandId: string | null;  // UUID del brand
  brandName: string | null; // Nombre del brand
}
```

---

## ?? **Ejemplos de Uso**

### **1. Como Usuario BACKOFFICE (Admin/Cashier)**

```sh
curl -X GET "https://api.tudominio.com/api/v1/balance" \
  -H "Authorization: Bearer <BACKOFFICE_JWT_TOKEN>"
```

**Response (200)**:
```json
{
  "userId": "456e7890-e89b-12d3-a456-426614174001",
  "userType": "BACKOFFICE",
  "username": "admin_user",
  "balance": 1500.00,
  "role": "BRAND_ADMIN",
  "brandId": "789e0123-e89b-12d3-a456-426614174002",
  "brandName": "Bet30 Casino"
}
```

---

### **2. Como PLAYER (Jugador)**

```sh
curl -X GET "https://api.tudominio.com/api/v1/balance" \
  -H "Authorization: Bearer <PLAYER_JWT_TOKEN>"
```

**Response (200)**:
```json
{
  "userId": "901e2345-e89b-12d3-a456-426614174003",
  "userType": "PLAYER",
  "username": "player123",
  "balance": 150.50,
  "role": null,
  "brandId": "789e0123-e89b-12d3-a456-426614174002",
  "brandName": "Bet30 Casino"
}
```

---

### **3. JavaScript/TypeScript (Fetch)**

```typescript
async function getMyBalance() {
  const response = await fetch('https://api.tudominio.com/api/v1/balance', {
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${token}`, // JWT del usuario logueado
    'Content-Type': 'application/json'
    },
    credentials: 'include' // Si usas cookies
  });

  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }

  return await response.json();
}

// Uso
const balance = await getMyBalance();
console.log(`Tu balance es: ${balance.balance}`);
```

---

### **4. React Hook**

```typescript
import { useState, useEffect } from 'react';

interface BalanceResponse {
  userId: string;
  userType: string;
  username: string;
  balance: number;
  role: string | null;
  brandId: string | null;
  brandName: string | null;
}

function useBalance(token: string) {
  const [balance, setBalance] = useState<BalanceResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchBalance = async () => {
      try {
        const response = await fetch('https://api.tudominio.com/api/v1/balance', {
       headers: {
   'Authorization': `Bearer ${token}`
          }
        });

        if (!response.ok) {
          throw new Error('Failed to fetch balance');
        }

        const data = await response.json();
        setBalance(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Unknown error');
      } finally {
        setLoading(false);
      }
    };

    fetchBalance();
  }, [token]);

  return { balance, loading, error };
}

// Componente
function BalanceDisplay({ token }: { token: string }) {
  const { balance, loading, error } = useBalance(token);

  if (loading) return <div>Loading balance...</div>;
  if (error) return <div>Error: {error}</div>;
  if (!balance) return null;

  return (
    <div>
      <h2>Balance: ${balance.balance.toFixed(2)}</h2>
      <p>Username: {balance.username}</p>
      {balance.role && <p>Role: {balance.role}</p>}
    </div>
  );
}
```

---

## ?? **Autenticación**

El endpoint detecta automáticamente el tipo de usuario desde el **JWT token**:

### **Detección por Audience**
- `aud: "backoffice"` ? Usuario BACKOFFICE
- `aud: "player"` ? Usuario PLAYER

### **Detección por Role (fallback)**
- `role: "SUPER_ADMIN" | "BRAND_ADMIN" | "CASHIER"` ? BACKOFFICE
- `role: "PLAYER"` ? PLAYER

### **Claims Usados**

| Claim | Descripción |
|-------|-------------|
| `aud` | Audience del token (backoffice/player) |
| `sub` o `NameIdentifier` | UserId del usuario |
| `role` | Rol del usuario |

---

## ?? **Códigos de Error**

| Código | Descripción | Solución |
|--------|-------------|----------|
| 401 | Token JWT faltante o inválido | Verificar header Authorization |
| 404 | Usuario no encontrado | El userId del token no existe en la DB |
| 500 | Error interno del servidor | Revisar logs del backend |

---

## ?? **Comparación con el Endpoint Anterior**

### **? Antes** (Complejo)
```sh
# Tenías que saber tu userId y especificarlo manualmente
GET /api/v1/admin/users/{userId}/balance
```

### **? Ahora** (Simplificado)
```sh
# El endpoint detecta automáticamente tu userId del token
GET /api/v1/balance
```

---

## ?? **Ventajas**

1. ? **Más Simple**: No necesitas pasar el `userId` manualmente
2. ? **Más Seguro**: Solo puedes ver TU propio balance
3. ? **Universal**: Funciona para BACKOFFICE y PLAYER con el mismo endpoint
4. ? **Consistente**: Siempre usa el usuario del token JWT

---

## ?? **Testing**

### **1. Como BACKOFFICE Admin**
```sh
# Login como admin
curl -X POST "http://localhost:5000/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin_user",
    "password": "admin123"
  }'

# Obtener balance
curl -X GET "http://localhost:5000/api/v1/balance" \
  -H "Authorization: Bearer <TOKEN_RECIBIDO>"
```

### **2. Como PLAYER**
```sh
# Login como player (si existe endpoint de player login)
curl -X POST "http://localhost:5000/api/v1/auth/player/login" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "player123",
  "password": "player123"
  }'

# Obtener balance
curl -X GET "http://localhost:5000/api/v1/balance" \
  -H "Authorization: Bearer <TOKEN_RECIBIDO>"
```

---

## ?? **Notas Importantes**

1. **Seguridad**: El endpoint SOLO retorna el balance del usuario autenticado en el token
2. **No Admin Override**: Un admin NO puede ver el balance de otro usuario con este endpoint (usa el endpoint de admin para eso)
3. **Idempotente**: Puedes llamarlo múltiples veces sin efectos secundarios
4. **Cache Friendly**: Considera cachear la respuesta en el frontend (30-60 segundos)

---

## ?? **Endpoints Relacionados**

| Endpoint | Uso | Auth |
|----------|-----|------|
| `GET /api/v1/balance` | **Tu balance** (recomendado) | User JWT |
| `GET /api/v1/admin/users/{userId}/balance` | Balance de cualquier usuario | Admin JWT |
| `POST /api/v1/admin/transactions` | Crear transacción (depósito/retiro) | Admin JWT |
| `GET /api/v1/admin/transactions` | Listar transacciones | Admin JWT |

---

**Archivo**: `docs/BALANCE-ENDPOINT-DOCUMENTATION.md`  
**Fecha**: 2025-01-24  
**Versión**: 1.0

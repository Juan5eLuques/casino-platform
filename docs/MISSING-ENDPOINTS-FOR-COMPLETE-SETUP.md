# Endpoints Faltantes para Setup Completo via API

Para que la guía de `SITE-SETUP-GUIDE.md` pueda ejecutarse completamente desde la API sin inserts manuales, necesitas implementar estos endpoints adicionales:

## ?? **1. Gestión de Asignaciones Cajero-Jugador**

### **POST /api/v1/admin/cashiers/{cashierId}/players/{playerId}**
```json
{
  "assignedAt": "2024-01-01T00:00:00Z" // opcional, default: now
}
```
**Response:** `201 Created`
```json
{
  "cashierId": "uuid",
  "playerId": "uuid", 
  "assignedAt": "datetime",
  "cashierUsername": "string",
  "playerUsername": "string"
}
```

### **GET /api/v1/admin/cashiers/{cashierId}/players**
**Response:** `200 OK`
```json
{
  "cashierId": "uuid",
  "cashierUsername": "string",
  "players": [
    {
      "playerId": "uuid",
      "username": "string",
      "email": "string",
      "status": "ACTIVE",
      "currentBalance": 10000,
      "assignedAt": "datetime"
    }
  ]
}
```

### **DELETE /api/v1/admin/cashiers/{cashierId}/players/{playerId}**
**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Player unassigned successfully"
}
```

### **GET /api/v1/admin/players/{playerId}/cashiers**
Listar qué cajeros están asignados a un jugador
**Response:** `200 OK`
```json
{
  "playerId": "uuid",
  "playerUsername": "string",
  "assignedCashiers": [
    {
      "cashierId": "uuid",
      "username": "string",
      "role": "CASHIER",
      "assignedAt": "datetime"
    }
  ]
}
```

---

## ?? **2. Gestión de Passwords**

### **POST /api/v1/admin/users/{userId}/password**
Cambiar password de usuario backoffice
```json
{
  "currentPassword": "string", // opcional para SUPER_ADMIN
  "newPassword": "string"
}
```
**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Password updated successfully",
  "lastPasswordChangeAt": "datetime"
}
```

### **POST /api/v1/admin/users/{userId}/reset-password**
Reset password (solo SUPER_ADMIN y OPERATOR_ADMIN)
```json
{
  "newPassword": "string",
  "forceChangeOnNextLogin": true
}
```
**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Password reset successfully",
  "temporaryPassword": false
}
```

---

## ?? **3. Creación de Jugadores con Password**

### **Modificar POST /api/v1/admin/players**
Agregar campo opcional para password inicial:
```json
{
  "brandId": "uuid",
  "username": "string",
  "email": "string",
  "externalId": "string",
  "status": "ACTIVE",
  "initialBalance": 10000, // en centavos
  "password": "demo123" // NUEVO: password opcional para jugadores
}
```

### **POST /api/v1/admin/players/{playerId}/password**
Cambiar password de jugador
```json
{
  "newPassword": "string"
}
```

---

## ?? **4. Endpoints de Setup/Utilidad**

### **POST /api/v1/admin/setup/demo-site**
Crear sitio completo de demo con un solo endpoint
```json
{
  "operatorName": "MiCasino Corp",
  "brandCode": "mycasino", 
  "brandName": "MiCasino",
  "domain": "mycasino.local",
  "adminDomain": "admin.mycasino.local",
  "corsOrigins": ["http://localhost:3000"],
  "locale": "es-ES",
  "adminCredentials": {
    "username": "admin_mycasino",
    "password": "admin123"
  },
  "cashiers": [
    {"username": "cajero1_mycasino", "password": "admin123"},
    {"username": "cajero2_mycasino", "password": "admin123"}
  ],
  "players": [
    {"username": "jugador1", "password": "demo", "email": "jugador1@mycasino.local", "initialBalance": 10000},
    {"username": "jugador2", "password": "demo", "email": "jugador2@mycasino.local", "initialBalance": 10000},
    {"username": "jugador3", "password": "demo", "email": "jugador3@mycasino.local", "initialBalance": 10000},
    {"username": "jugador4", "password": "demo", "email": "jugador4@mycasino.local", "initialBalance": 10000}
  ],
  "assignCashiersToPlayers": {
    "cajero1_mycasino": ["jugador1", "jugador2"],
    "cajero2_mycasino": ["jugador3", "jugador4"]
  },
  "includeGames": true,
  "includeProviderConfig": {
    "provider": "dummy",
    "secret": "mi_secreto_hmac_super_seguro_32_chars"
  }
}
```

**Response:** `201 Created`
```json
{
  "success": true,
  "message": "Demo site created successfully",
  "operator": {
    "id": "uuid",
    "name": "MiCasino Corp"
  },
  "brand": {
    "id": "uuid", 
    "code": "mycasino",
    "domain": "mycasino.local",
    "adminDomain": "admin.mycasino.local"
  },
  "users": {
    "admin": {"id": "uuid", "username": "admin_mycasino"},
    "cashiers": [
      {"id": "uuid", "username": "cajero1_mycasino"},
      {"id": "uuid", "username": "cajero2_mycasino"}
    ]
  },
  "players": [
    {"id": "uuid", "username": "jugador1", "balance": 10000},
    {"id": "uuid", "username": "jugador2", "balance": 10000},
    {"id": "uuid", "username": "jugador3", "balance": 10000},
    {"id": "uuid", "username": "jugador4", "balance": 10000}
  ],
  "assignments": {
    "cajero1_mycasino": ["jugador1", "jugador2"],
    "cajero2_mycasino": ["jugador3", "jugador4"]
  },
  "games": {
    "total": 4,
    "enabled": 4
  },
  "provider": {
    "code": "dummy",
    "configured": true
  }
}
```

### **GET /api/v1/admin/setup/validate**
Validar que un sitio está completamente configurado
```json
{
  "brandCode": "mycasino",
  "domain": "mycasino.local"
}
```

**Response:** `200 OK`
```json
{
  "valid": true,
  "brand": {
    "configured": true,
    "domain": "mycasino.local",
    "adminDomain": "admin.mycasino.local", 
    "status": "ACTIVE"
  },
  "users": {
    "adminCount": 1,
    "cashierCount": 2,
    "totalUsers": 3
  },
  "players": {
    "totalCount": 4,
    "activeCount": 4,
    "totalBalance": 40000
  },
  "assignments": {
    "totalAssignments": 4,
    "cashiersWithPlayers": 2
  },
  "games": {
    "totalGames": 4,
    "enabledGames": 4
  },
  "providers": {
    "configuredProviders": ["dummy"]
  },
  "missing": [] // array de elementos que faltan
}
```

---

## ?? **Implementación Prioritaria**

Para que la guía funcione 100% vía API, implementa en este orden:

### **Prioridad 1 (Crítico):**
1. **Asignaciones Cajero-Jugador** (POST, GET, DELETE)
2. **Gestión de Passwords** para usuarios backoffice
3. **Password inicial para jugadores** (modificar CreatePlayer)

### **Prioridad 2 (Muy útil):**
4. **Endpoint de Setup Completo** (`POST /admin/setup/demo-site`)
5. **Validación de Setup** (`GET /admin/setup/validate`)

### **Prioridad 3 (Nice to have):**
6. **Gestión de passwords para jugadores**
7. **Endpoints de utilidad adicionales**

---

## ?? **DTOs Necesarios**

Crear estos DTOs en `Casino.Application.DTOs`:

```csharp
// Casino.Application.DTOs.Cashier/
public record AssignPlayerToCashierRequest();
public record AssignPlayerToCashierResponse(Guid CashierId, Guid PlayerId, string CashierUsername, string PlayerUsername, DateTime AssignedAt);
public record GetCashierPlayersResponse(Guid CashierId, string CashierUsername, IEnumerable<CashierPlayerDto> Players);
public record CashierPlayerDto(Guid PlayerId, string Username, string Email, PlayerStatus Status, long CurrentBalance, DateTime AssignedAt);

// Casino.Application.DTOs.Admin/
public record ChangePasswordRequest(string? CurrentPassword, string NewPassword);
public record ResetPasswordRequest(string NewPassword, bool ForceChangeOnNextLogin);
public record PasswordChangeResponse(bool Success, string Message, DateTime? LastPasswordChangeAt);

// Casino.Application.DTOs.Setup/
public record CreateDemoSiteRequest(...); // como se definió arriba
public record CreateDemoSiteResponse(...);
public record ValidateSetupRequest(string BrandCode, string Domain);
public record ValidateSetupResponse(...);
```

---

## ?? **Servicios Adicionales**

Crear estos servicios:

```csharp
// Casino.Application.Services/
public interface ICashierPlayerService
{
    Task<AssignPlayerToCashierResponse> AssignPlayerAsync(Guid cashierId, Guid playerId, Guid currentUserId);
    Task<GetCashierPlayersResponse> GetCashierPlayersAsync(Guid cashierId, Guid? operatorScope);
    Task<bool> UnassignPlayerAsync(Guid cashierId, Guid playerId, Guid currentUserId, Guid? operatorScope);
}

public interface IPasswordService  
{
    Task<PasswordChangeResponse> ChangeUserPasswordAsync(Guid userId, ChangePasswordRequest request, Guid currentUserId);
    Task<PasswordChangeResponse> ResetUserPasswordAsync(Guid userId, ResetPasswordRequest request, Guid currentUserId);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public interface ISetupService
{
    Task<CreateDemoSiteResponse> CreateDemoSiteAsync(CreateDemoSiteRequest request, Guid currentUserId);
    Task<ValidateSetupResponse> ValidateSetupAsync(ValidateSetupRequest request);
}
```

**Una vez implementados estos endpoints, toda la guía de `SITE-SETUP-GUIDE.md` podrá ejecutarse 100% via API sin necesidad de inserts manuales en base de datos.**
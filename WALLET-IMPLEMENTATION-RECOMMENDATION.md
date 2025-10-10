# ?? RECOMENDACIÓN: Implementación Gradual de Wallets

## ?? **PROBLEMA IDENTIFICADO:**
Tu backend actual NO está completamente preparado para wallets con decimal para todos los usuarios. Falta:
- Wallets para usuarios de backoffice (SUPER_ADMIN, BRAND_ADMIN, CASHIER)
- Soporte nativo para tipo decimal en lugar de bigint
- Sistema de transferencias entre usuarios

## ? **OPCIÓN RECOMENDADA: MIGRACIÓN GRADUAL**

### **Paso 1: Agregar soporte básico (30 min)**

```sql
-- Agregar balance decimal a usuarios de backoffice
ALTER TABLE "BackofficeUsers" ADD "WalletBalance" numeric(18,2) DEFAULT 0.00;

-- Agregar balance decimal a players (paralelo al existente)
ALTER TABLE "Players" ADD "WalletBalanceDecimal" numeric(18,2) DEFAULT 0.00;

-- Migrar balances existentes de bigint a decimal
UPDATE "Players" 
SET "WalletBalanceDecimal" = (
  SELECT w."BalanceBigint" / 100.0 
  FROM "Wallets" w 
  WHERE w."PlayerId" = "Players"."Id"
);
```

### **Paso 2: Tabla simple de transferencias (15 min)**

```sql
CREATE TABLE "SimpleWalletTransfers" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "FromUserId" uuid,
    "FromUserType" text CHECK ("FromUserType" IN ('BACKOFFICE', 'PLAYER')),
    "ToUserId" uuid NOT NULL,
    "ToUserType" text NOT NULL CHECK ("ToUserType" IN ('BACKOFFICE', 'PLAYER')),
    "Amount" numeric(18,2) NOT NULL CHECK ("Amount" > 0),
    "Description" text,
    "CreatedByUserId" uuid NOT NULL,
    "CreatedAt" timestamp DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY ("CreatedByUserId") REFERENCES "BackofficeUsers"("Id")
);

CREATE INDEX "IX_SimpleWalletTransfers_CreatedAt" ON "SimpleWalletTransfers"("CreatedAt");
CREATE INDEX "IX_SimpleWalletTransfers_FromUserId" ON "SimpleWalletTransfers"("FromUserId");
CREATE INDEX "IX_SimpleWalletTransfers_ToUserId" ON "SimpleWalletTransfers"("ToUserId");
```

### **Paso 3: Endpoint simple de transferencias (45 min)**

```csharp
// POST /api/v1/admin/wallet-transfer
public record SimpleTransferRequest(
    Guid? FromUserId,
    string? FromUserType, // null = MINT
    Guid ToUserId,
    string ToUserType,
    decimal Amount,
    string? Description
);

// Lógica de autorización:
// SUPER_ADMIN: puede hacer MINT (FromUserId=null) y cualquier transferencia
// BRAND_ADMIN: puede transferir entre usuarios de su brand
// CASHIER: puede transferir solo con players de su brand
```

### **Paso 4: Actualizar respuestas de usuarios (30 min)**

```csharp
// Agregar balance a GetUnifiedUserResponse
public record GetUnifiedUserResponse(
    // ... campos existentes ...
    decimal? WalletBalance = null // Para todos los tipos de usuario
);
```

## ?? **BENEFICIOS DE ESTA OPCIÓN:**

1. **? Rápida implementación** - 2 horas vs 8+ horas del sistema complejo
2. **? Sin conflictos** - No rompe código existente
3. **? Funcional** - Cumple con todos tus requerimientos básicos
4. **? Escalable** - Puedes migrar al sistema complejo después

## ?? **FUNCIONALIDAD RESULTANTE:**

### **Capacidades por rol:**

**SUPER_ADMIN:**
- ? MINT dinero a cualquier usuario (FromUserId=null)
- ? Transferir entre cualquier usuario de cualquier brand
- ? Ver wallets de todos los usuarios

**BRAND_ADMIN:**
- ? Transferir entre usuarios de su brand
- ? Ver wallets de usuarios de su brand

**CASHIER:**
- ? Transferir con players de su brand
- ? Ver wallets de players asignados

**PLAYER:**
- ? Solo ver su propio balance

### **Endpoints resultantes:**

```
GET /api/v1/admin/users           # Incluye balance en response
POST /api/v1/admin/wallet-transfer # Transferencias simples
GET /api/v1/admin/wallet-transfers # Historial de transferencias
```

## ?? **¿QUIERES IMPLEMENTAR ESTO EN LUGAR DEL SISTEMA COMPLEJO?**

Es mucho más simple, rápido y funcional para tus necesidades actuales. El sistema complejo que implementé es "over-engineering" para lo que necesitas ahora.

**¿Procedo con la implementación simple? ?**
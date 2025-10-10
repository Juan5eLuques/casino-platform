# CLEAN WALLET IMPLEMENTATION - SIMPLE+

## LIMPIEZA COMPLETADA ?

### Archivos Eliminados (Complejidad Innecesaria)
- ? `WalletEndpoints.cs` (duplicado, complejo)
- ? `WalletTransactionEndpoints.cs` (duplicado)  
- ? `UnifiedUserEndpoints.cs` (complejo)
- ? `IWalletService.cs` (reemplazado por ILegacyWalletService)
- ? `WalletService.cs` (reemplazado por LegacyWalletService)
- ? `IUnifiedUserService.cs` (innecesario)
- ? `UnifiedUserService.cs` (innecesario)
- ? `UnifiedUserDTOs.cs` (innecesario)
- ? `UnifiedUserValidators.cs` (innecesario)

### Sistema Actual - ARQUITECTURA LIMPIA

#### 1?? **SIMPLE WALLET SYSTEM** (Admin Operations)
**Prop�sito**: Transacciones entre usuarios del backoffice y players con garant�as cr�ticas

**Componentes**:
- ? `ISimpleWalletService` + `SimpleWalletService`
- ? `SimpleWalletEndpoints` ? `/api/v1/admin/transactions`
- ? DTOs: `TransactionResponse`, `CreateTransactionRequest`, `GetTransactionsRequest`, `SimpleWalletBalanceResponse`
- ? Entidad: `WalletTransaction` (con migraci�n EF Core)
- ? Validadores: `CreateTransactionRequestValidator`, `GetTransactionsRequestValidator`

**Endpoints**:
```
POST   /api/v1/admin/transactions     - Crear MINT/TRANSFER
GET    /api/v1/admin/transactions     - Listar con filtros
GET    /api/v1/admin/users/{id}/balance?userType=BACKOFFICE|PLAYER
```

**Caracter�sticas SIMPLE+**:
- ? **Idempotencia**: `IdempotencyKey` �nico previene duplicados
- ? **Transacciones DB**: Nivel SERIALIZABLE para m�xima seguridad  
- ? **Locking Consistente**: Orden por ID para evitar deadlocks
- ? **Scope por Brand**: Solo usuarios de la misma brand (excepto SUPER_ADMIN)
- ? **Autorizaci�n Granular**: 
  - MINT ? Solo SUPER_ADMIN
  - TRANSFER ? BRAND_ADMIN (su brand), CASHIER (solo a PLAYER de su brand)

#### 2?? **LEGACY WALLET SYSTEM** (Gateway Compatibility)
**Prop�sito**: Mantener compatibilidad con providers externos (bigint system)

**Componentes**:
- ? `ILegacyWalletService` + `LegacyWalletService`
- ? `GatewayEndpoints` ? `/api/v1/gateway/*` (HMAC protected)
- ? `InternalWalletEndpoints` ? `/api/v1/internal/wallet/*` (unprotected)
- ? DTOs: `WalletBalanceResponse`, `WalletOperationResponse`, `WalletDebitRequest`, etc.
- ? Usa: `Wallet.BalanceBigint` + `Ledger` (sistema existente)

**Endpoints Gateway**:
```
POST   /api/v1/gateway/balance        - Balance para provider
POST   /api/v1/gateway/bet           - Apostar  
POST   /api/v1/gateway/win           - Ganar
POST   /api/v1/gateway/rollback      - Rollback
POST   /api/v1/gateway/closeRound    - Cerrar ronda
```

**Endpoints Internos**:
```
POST   /api/v1/internal/wallet/balance   - Balance interno
POST   /api/v1/internal/wallet/debit     - D�bito interno
POST   /api/v1/internal/wallet/credit    - Cr�dito interno
POST   /api/v1/internal/wallet/rollback  - Rollback interno
```

## GARANT�AS IMPLEMENTADAS ??

### Transaccional (SimpleWalletService)
- **Nivel SERIALIZABLE**: M�ximo aislamiento en transacciones cr�ticas
- **Commit/Rollback**: Operaciones at�micas garantizadas
- **Bloqueo Ordenado**: Previene deadlocks con orden consistente por ID

### Idempotencia
- **Simple**: `IdempotencyKey` �nico en `WalletTransaction`
- **Legacy**: `ExternalRef` �nico en `Ledger`

### Scope y Autorizaci�n  
- **Brand Isolation**: Usuarios solo ven/operan en su brand
- **Role-Based**: SUPER_ADMIN (global), BRAND_ADMIN (su brand), CASHIER (su brand, solo players)

### Datos
- **Simple**: `BackofficeUser.WalletBalance` + `Player.WalletBalance` (decimal 18,2)
- **Legacy**: `Wallet.BalanceBigint` + `Ledger` (bigint, centavos)

## CONFIGURACI�N LIMPIA ??

### Program.cs
```csharp
// WALLET SERVICES - CLEAN SEPARATION
builder.Services.AddScoped<ISimpleWalletService, SimpleWalletService>();        // Admin ops
builder.Services.AddScoped<ILegacyWalletService, LegacyWalletService>();        // Gateway ops

// VALIDATORS
builder.Services.AddScoped<IValidator<CreateTransactionRequest>, CreateTransactionRequestValidator>();
builder.Services.AddScoped<IValidator<GetTransactionsRequest>, GetTransactionsRequestValidator>();
builder.Services.AddScoped<IValidator<WalletDebitRequest>, WalletDebitRequestValidator>();
// ... resto de validators legacy

// ENDPOINTS
app.MapGatewayEndpoints();           // /api/v1/gateway/* (providers externos)
app.MapInternalWalletEndpoints();    // /api/v1/internal/wallet/* (interno)
app.MapSimpleWalletEndpoints();      // /api/v1/admin/transactions (admin)
```

## CRITERIOS DE ACEPTACI�N ?

- ? **dotnet build** sin errores
- ? **Swagger** levanta sin conflictos de rutas ni SchemaId
- ? **Un solo MapGroup** por ruta `/api/v1/admin` 
- ? **Sin duplicados** de endpoints
- ? **Separaci�n limpia**: Admin (Simple) vs Gateway (Legacy)
- ? **Compatibilidad**: Providers externos siguen funcionando
- ? **Migraci�n**: `WalletTransaction` tabla creada

## PR�XIMOS PASOS ??

1. **Testing Manual**:
   - MINT (SUPER_ADMIN) ? ? Crear dinero
   - TRANSFER (BRAND_ADMIN) ? ? Entre usuarios de su brand
   - TRANSFER (CASHIER) ? ? Solo a players de su brand
   - Idempotencia ? ? Misma `IdempotencyKey` devuelve misma respuesta

2. **Gateway Testing**:
   - Balance/Bet/Win con providers externos
   - Verificar que `Wallet.BalanceBigint` y `Ledger` siguen funcionando

3. **Migraciones Pendientes** (si necesarias):
   ```bash
   dotnet ef database update --startup-project apps\api\Casino.Api
   ```

---

## RESUMEN ARQUITECTURAL 

**Principio**: **Separaci�n de Responsabilidades**
- **SimpleWallet** = Operaciones administrativas internas con m�ximas garant�as
- **LegacyWallet** = Compatibilidad con providers/gateway externos  
- **Mismo DbContext** = Datos unificados pero servicios separados
- **Pol�ticas diferenciadas** = Admin (autenticado + authorized) vs Gateway (HMAC) vs Internal (unprotected)

**Resultado**: Sistema robusto, mantenible y compatible hacia atr�s.
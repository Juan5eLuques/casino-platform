# Plan de Migraci�n: Sistema de Wallet Unificado

## ?? Objetivo de la Migraci�n

Unificar el sistema de wallet para que **TODAS las transacciones** (administrativas y de juegos/gateway) usen:
- ? `Player.WalletBalance` como **�nica fuente de verdad** para balance de jugadores
- ? `WalletTransactions` para registrar **TODOS los movimientos** con `TransactionType`
- ? Mantener tabla `Ledger` solo por compatibilidad legacy (registro secundario)
- ? **Sin modificar las rutas de los endpoints** actuales
- ? **Sin breaking changes** en la API

---

## ?? Situaci�n Actual (Problem�tica)

### Sistema Dual Desincronizado

Actualmente existen **DOS sistemas paralelos** de gesti�n de balance:

#### Sistema 1: SimpleWallet (Admin/Backoffice)
- **Tabla**: `WalletTransactions`
- **Balance**: `Player.WalletBalance` (decimal) + `BackofficeUser.WalletBalance` (decimal)
- **Uso**: Transferencias administrativas (MINT, TRANSFER)
- **TransactionType**: ? Implementado

#### Sistema 2: LegacyWallet (Gateway/Juegos)
- **Tabla**: `Wallet` + `Ledger`
- **Balance**: `Wallet.BalanceBigint` (long, centavos)
- **Uso**: Transacciones de juegos (BET, WIN, ROLLBACK)
- **TransactionType**: ? No se usa

### ? Problemas Identificados

1. **Duplicaci�n de balance**: 
   - `Player.WalletBalance` (decimal)
   - `Wallet.BalanceBigint` (long, centavos)
   - ?? **NO est�n sincronizados**

2. **Fragmentaci�n de auditor�a**:
   - Transacciones admin ? `WalletTransactions`
   - Transacciones juegos ? `Ledger`
   - ?? No hay vista unificada

3. **TransactionType incompleto**:
   - Solo se usa para MINT, TRANSFER
   - ?? BET, WIN, ROLLBACK no se categorizan

4. **Tabla `Wallet` innecesaria**:
   - Solo agrega complejidad
   - No aporta valor sobre `Player.WalletBalance`

---

## ? Soluci�n: Sistema Unificado

### Nuevo Dise�o

```
???????????????????????????????????????????????????????????
?                  PLAYER.WALLETBALANCE                    ?
?            (�nica Fuente de Verdad - Decimal)           ?
???????????????????????????????????????????????????????????
                           ?
                           ?
         ?????????????????????????????????????
         ?                                   ?
???????????????????              ??????????????????????
? SimpleWallet    ?              ? UnifiedWallet      ?
? Service         ?              ? Service            ?
?                 ?              ?                    ?
? � MINT          ?              ? � BET              ?
? � TRANSFER      ?              ? � WIN              ?
?                 ?              ? � ROLLBACK         ?
???????????????????              ??????????????????????
         ?                                  ?
         ????????????????????????????????????
                       ?
         ?????????????????????????????????
         ?    WALLETTRANSACTIONS         ?
         ?  (Registro �nico Unificado)   ?
         ?                               ?
         ?  TransactionType:             ?
         ?  � MINT, TRANSFER             ?
         ?  � BET, WIN, ROLLBACK         ?
         ?  � DEPOSIT, WITHDRAWAL, etc.  ?
         ?????????????????????????????????
                       ?
                       ? (opcional)
                       ?
              ??????????????????
              ?    LEDGER      ?
              ? (Compatibilidad?
              ?     Legacy)    ?
              ??????????????????
```

---

## ?? Tareas de Implementaci�n

### 1?? Crear Interfaz `IWalletService`

**Archivo**: `apps/Casino.Application/Services/IWalletService.cs`

**C�digo**:
```csharp
using Casino.Application.DTOs.Wallet;

namespace Casino.Application.Services;

/// <summary>
/// Servicio unificado de wallet para operaciones de juegos y gateway
/// Usa Player.WalletBalance como source of truth y registra en WalletTransactions
/// </summary>
public interface IWalletService
{
    /// <summary>
    /// Obtener balance de jugador
    /// </summary>
    Task<WalletBalanceResponse> GetBalanceAsync(WalletBalanceRequest request);

    /// <summary>
    /// Debitar saldo (BET)
    /// </summary>
    Task<WalletOperationResponse> DebitAsync(WalletDebitRequest request);

    /// <summary>
    /// Acreditar saldo (WIN)
    /// </summary>
    Task<WalletOperationResponse> CreditAsync(WalletCreditRequest request);

    /// <summary>
    /// Revertir transacci�n (ROLLBACK)
    /// </summary>
    Task<WalletOperationResponse> RollbackAsync(WalletRollbackRequest request);
}
```

**Requisitos**:
- Interfaz id�ntica a `ILegacyWalletService` (compatibilidad)
- Sin cambios en firmas de m�todos (sin breaking changes)

---

### 2?? Crear `UnifiedWalletService`

**Archivo**: `apps/Casino.Application/Services/Implementations/UnifiedWalletService.cs`

**Responsabilidades**:

#### A. GetBalanceAsync
```csharp
public async Task<WalletBalanceResponse> GetBalanceAsync(WalletBalanceRequest request)
{
    // 1. Obtener player de base de datos
    var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == request.PlayerId);
    
    // 2. Validar que existe
    if (player == null) throw new InvalidOperationException("Player not found");
    
    // 3. Retornar Player.WalletBalance (NO Wallet.BalanceBigint)
    return new WalletBalanceResponse(player.WalletBalance);
}
```

**Puntos clave**:
- ? Usar `Player.WalletBalance` como fuente de verdad
- ? NO usar `Wallet.BalanceBigint`
- ?? Si el player no existe, lanzar excepci�n clara

#### B. DebitAsync (BET)
```csharp
public async Task<WalletOperationResponse> DebitAsync(WalletDebitRequest request)
{
    // 1. IDEMPOTENCIA: Verificar por ExternalRef en WalletTransactions
    if (!string.IsNullOrEmpty(request.ExternalRef))
    {
        var existing = await _context.WalletTransactions
            .FirstOrDefaultAsync(t => t.IdempotencyKey == request.ExternalRef);
        if (existing != null)
        {
            // Retornar balance actual del player (idempotencia)
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == request.PlayerId);
            return new WalletOperationResponse(true, existing.Id, "Already processed", 
                player?.WalletBalance ?? 0, null);
        }
    }

    // 2. TRANSACCI�N DB: Usar IsolationLevel.Serializable
    await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
    try
    {
        // 3. LOCK: Obtener y bloquear player
        var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == request.PlayerId);
        if (player == null) throw new InvalidOperationException("Player not found");

        // 4. AUDITOR�A: Capturar balance ANTES
        var previousBalance = player.WalletBalance;

        // 5. VALIDACI�N: Verificar saldo suficiente
        if (player.WalletBalance < request.Amount)
        {
            throw new InvalidOperationException(
                $"Insufficient balance. Required: {request.Amount}, Available: {player.WalletBalance}"
            );
        }

        // 6. ACTUALIZAR: D�bito en Player.WalletBalance
        player.WalletBalance -= request.Amount;
        var newBalance = player.WalletBalance;

        // 7. REGISTRAR: Crear WalletTransaction con TransactionType.BET
        var walletTransaction = new WalletTransaction
        {
            Id = Guid.NewGuid(),
            BrandId = player.BrandId,
            FromUserId = player.Id,
            FromUserType = "PLAYER",
            ToUserId = null, // "La casa"
            ToUserType = null,
            Amount = request.Amount,
            TransactionType = TransactionType.BET, // ? IMPORTANTE
            PreviousBalanceFrom = previousBalance,
            NewBalanceFrom = newBalance,
            PreviousBalanceTo = null,
            NewBalanceTo = null,
            Description = $"Bet on game {request.GameCode ?? "unknown"}, round {request.RoundId}",
            CreatedByUserId = player.Id,
            CreatedByRole = "PLAYER",
            IdempotencyKey = request.ExternalRef ?? Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        };
        _context.WalletTransactions.Add(walletTransaction);

        // 8. COMPATIBILIDAD: Crear registro en Ledger (opcional, legacy)
        var ledgerEntry = new Ledger
        {
            BrandId = player.BrandId,
            PlayerId = player.Id,
            RoundId = request.RoundId,
            AmountBigint = -(long)(request.Amount * 100), // Negativo para d�bito
            BalanceBeforeBigint = (long)(previousBalance * 100),
            BalanceAfterBigint = (long)(newBalance * 100),
            Reason = LedgerReason.BET,
            GameCode = request.GameCode,
            Provider = "unified",
            ExternalRef = request.ExternalRef,
            Meta = null,
            CreatedAt = DateTime.UtcNow
        };
        _context.Ledger.Add(ledgerEntry);

        // 9. COMMIT: Guardar cambios
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        // 10. RETORNAR: Response exitoso
        return new WalletOperationResponse(true, walletTransaction.Id, "Debit successful", 
            newBalance, null);
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "Error debiting wallet - Player: {PlayerId}", request.PlayerId);
        throw;
    }
}
```

**Puntos clave**:
- ? Idempotencia por `ExternalRef` en `WalletTransactions`
- ? Transacci�n DB con `SERIALIZABLE` (m�xima seguridad)
- ? Registrar `TransactionType.BET`
- ? Auditor�a completa con `PreviousBalance` y `NewBalance`
- ? Mantener `Ledger` por compatibilidad
- ?? NO usar ni actualizar `Wallet.BalanceBigint`

#### C. CreditAsync (WIN)
```csharp
public async Task<WalletOperationResponse> CreditAsync(WalletCreditRequest request)
{
    // Similar a DebitAsync pero:
    // 1. SIN validaci�n de saldo suficiente
    // 2. INCREMENTAR balance: player.WalletBalance += request.Amount
    // 3. TransactionType = TransactionType.WIN
    // 4. FromUserId = null, ToUserId = player.Id
    // 5. PreviousBalanceTo / NewBalanceTo (en lugar de From)
}
```

**Diferencias con DebitAsync**:
- ? Incremento en lugar de decremento
- ? `TransactionType.WIN`
- ? Invertir From/To (casa ? jugador)
- ? NO validar saldo suficiente

#### D. RollbackAsync
```csharp
public async Task<WalletOperationResponse> RollbackAsync(WalletRollbackRequest request)
{
    // 1. Buscar transacci�n original por ExternalRefOriginal en WalletTransactions
    var originalTx = await _context.WalletTransactions
        .FirstOrDefaultAsync(t => t.IdempotencyKey == request.ExternalRefOriginal);
    
    if (originalTx == null) 
        throw new InvalidOperationException("Original transaction not found");

    // 2. Verificar que no se haya revertido previamente
    var existingRollback = await _context.WalletTransactions
        .AnyAsync(t => t.TransactionType == TransactionType.ROLLBACK && 
                      t.Description.Contains(request.ExternalRefOriginal));
    
    if (existingRollback)
    {
        // Retornar idempotente
        // ...
    }

    // 3. Determinar operaci�n inversa
    bool wasDebit = originalTx.TransactionType == TransactionType.BET;
    Guid playerId = wasDebit ? originalTx.FromUserId!.Value : originalTx.ToUserId;

    // 4. Transacci�n DB
    await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
    try
    {
        var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == playerId);
        if (player == null) throw new InvalidOperationException("Player not found");

        var previousBalance = player.WalletBalance;

        // 5. Invertir operaci�n
        if (wasDebit)
        {
            player.WalletBalance += originalTx.Amount; // Revertir d�bito = cr�dito
        }
        else
        {
            // Revertir cr�dito = d�bito (validar saldo)
            if (player.WalletBalance < originalTx.Amount)
                throw new InvalidOperationException("Insufficient balance for rollback");
            player.WalletBalance -= originalTx.Amount;
        }

        var newBalance = player.WalletBalance;

        // 6. Crear WalletTransaction con TransactionType.ROLLBACK
        var rollbackTx = new WalletTransaction
        {
            Id = Guid.NewGuid(),
            BrandId = player.BrandId,
            FromUserId = wasDebit ? null : player.Id,
            FromUserType = wasDebit ? null : "PLAYER",
            ToUserId = wasDebit ? player.Id : null,
            ToUserType = wasDebit ? "PLAYER" : null,
            Amount = originalTx.Amount,
            TransactionType = TransactionType.ROLLBACK, // ? IMPORTANTE
            PreviousBalanceFrom = wasDebit ? null : previousBalance,
            NewBalanceFrom = wasDebit ? null : newBalance,
            PreviousBalanceTo = wasDebit ? previousBalance : null,
            NewBalanceTo = wasDebit ? newBalance : null,
            Description = $"Rollback of transaction {request.ExternalRefOriginal}",
            CreatedByUserId = player.Id,
            CreatedByRole = "PLAYER",
            IdempotencyKey = $"rollback-{request.ExternalRefOriginal}-{Guid.NewGuid()}",
            CreatedAt = DateTime.UtcNow
        };
        _context.WalletTransactions.Add(rollbackTx);

        // 7. Compatibilidad: Ledger
        var ledgerEntry = new Ledger
        {
            BrandId = player.BrandId,
            PlayerId = player.Id,
            RoundId = null,
            AmountBigint = wasDebit ? (long)(originalTx.Amount * 100) : -(long)(originalTx.Amount * 100),
            BalanceBeforeBigint = (long)(previousBalance * 100),
            BalanceAfterBigint = (long)(newBalance * 100),
            Reason = LedgerReason.ROLLBACK,
            GameCode = null,
            Provider = "unified",
            ExternalRef = $"rollback-{request.ExternalRefOriginal}",
            Meta = null,
            CreatedAt = DateTime.UtcNow
        };
        _context.Ledger.Add(ledgerEntry);

        // 8. Commit
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new WalletOperationResponse(true, rollbackTx.Id, "Rollback successful", 
            newBalance, null);
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

**Puntos clave**:
- ? Buscar transacci�n original en `WalletTransactions` (NO en Ledger)
- ? Idempotencia de rollback (no duplicar)
- ? Invertir correctamente la operaci�n
- ? `TransactionType.ROLLBACK`
- ? Validar saldo si se revierte WIN

---

### 3?? Actualizar `Program.cs`

**Archivo**: `apps/api/Casino.Api/Program.cs`

**Cambio**:
```csharp
// ANTES:
builder.Services.AddScoped<ILegacyWalletService, LegacyWalletService>();

// DESPU�S:
builder.Services.AddScoped<IWalletService, UnifiedWalletService>();
// Opcional: Comentar legacy service
// builder.Services.AddScoped<ILegacyWalletService, LegacyWalletService>();
```

**Punto clave**:
- ? Registrar `IWalletService` en lugar de `ILegacyWalletService`
- ?? NO eliminar `ILegacyWalletService` inmediatamente (mantener para rollback si es necesario)

---

### 4?? Actualizar `InternalWalletEndpoints.cs`

**Archivo**: `apps/api/Casino.Api/Endpoints/InternalWalletEndpoints.cs`

**Cambios**:

#### A. Actualizar comentario de clase
```csharp
// ANTES:
/// <summary>
/// SONNET: Endpoints internos de wallet para compatibilidad con gateway
/// Usa LegacyWalletService para mantener sistema bigint
/// </summary>

// DESPU�S:
/// <summary>
/// SONNET: Endpoints internos de wallet UNIFICADO
/// Usa UnifiedWalletService con Player.WalletBalance + WalletTransactions
/// </summary>
```

#### B. Cambiar TODAS las referencias de `ILegacyWalletService` a `IWalletService`

**Buscar y reemplazar** en TODO el archivo:
```csharp
// ANTES:
[FromServices] ILegacyWalletService walletService

// DESPU�S:
[FromServices] IWalletService walletService
```

**M�todos afectados**:
- `GetBalance`
- `DebitWallet`
- `CreditWallet`
- `RollbackTransaction`

**?? IMPORTANTE**: El m�todo `RollbackTransaction` actualmente tiene `ILegacyWalletService`, cambiar a `IWalletService`.

---

### 5?? Actualizar Documentaci�n

**Archivo**: `docs/transaction-types-guide.md`

**Agregar al final**:

```markdown
---

## Sistema Unificado de Wallet

### ? Implementado (octubre 2024)

El sistema de wallet ha sido **unificado** para que todas las transacciones (administrativas y de juegos) se manejen de forma coherente.

### Balance �nico
- **Source of Truth**: `Player.WalletBalance` (decimal)
- **Tipo de dato**: Decimal con 2 decimales
- **Tabla legacy `Wallet`**: Ya no se usa para balance, solo referencia hist�rica
- **Tabla `Ledger`**: Se mantiene por compatibilidad, pero es secundaria

### Transacciones Unificadas

Todas las operaciones se registran en `WalletTransactions` con `TransactionType`:

| Tipo | Origen | Descripci�n | Servicio |
|------|--------|-------------|----------|
| **MINT** | Admin | Emisi�n de fondos por SUPER_ADMIN | SimpleWalletService |
| **TRANSFER** | Admin | Transferencia entre usuarios | SimpleWalletService |
| **BET** | Gateway | Apuesta de jugador | UnifiedWalletService |
| **WIN** | Gateway | Ganancia de jugador | UnifiedWalletService |
| **ROLLBACK** | Gateway | Reversi�n de transacci�n | UnifiedWalletService |
| **DEPOSIT** | Futuro | Dep�sito externo | - |
| **WITHDRAWAL** | Futuro | Retiro externo | - |
| **BONUS** | Futuro | Bono o promoci�n | - |
| **ADJUSTMENT** | Futuro | Ajuste manual | - |

### Servicios

#### UnifiedWalletService
- **Uso**: Operaciones de juegos/gateway (BET, WIN, ROLLBACK)
- **Balance**: `Player.WalletBalance`
- **Registro**: `WalletTransactions` (principal) + `Ledger` (secundario)
- **Endpoints**: `/api/v1/internal/wallet/*`

#### SimpleWalletService
- **Uso**: Operaciones administrativas (MINT, TRANSFER)
- **Balance**: `Player.WalletBalance` + `BackofficeUser.WalletBalance`
- **Registro**: `WalletTransactions`
- **Endpoints**: `/api/v1/admin/transactions`

### Beneficios de la Unificaci�n

? **Un solo source of truth**: No hay discrepancias entre sistemas  
? **Auditor�a completa**: Todos los movimientos en `WalletTransactions`  
? **TransactionType consistente**: Categorizaci�n est�ndar para an�lisis  
? **Mejor performance**: Una sola consulta para obtener balance  
? **Escalabilidad**: Preparado para nuevos tipos de transacciones  
? **Sin breaking changes**: Los endpoints mantienen la misma firma  

### Migraci�n

No fue necesaria migraci�n de datos ya que el sistema `Wallet` legacy no se usaba activamente. El campo `Player.WalletBalance` es el �nico y oficial desde el inicio.

### Compatibilidad

- **Tabla `Wallet`**: Se mantiene en el modelo pero no se usa para balance
- **Tabla `Ledger`**: Se sigue poblando para compatibilidad con reportes legacy
- **Endpoints**: Sin cambios en firmas, solo cambio interno de servicio
```

---

## ? Checklist de Verificaci�n Post-Implementaci�n

### 1. Compilaci�n
```bash
dotnet build
```
- [ ] Build exitoso sin errores
- [ ] Sin warnings relacionados con `IWalletService`

### 2. Verificar Endpoints Funcionan
- [ ] `POST /api/v1/internal/wallet/balance` retorna `Player.WalletBalance`
- [ ] `POST /api/v1/internal/wallet/debit` crea transacci�n BET en `WalletTransactions`
- [ ] `POST /api/v1/internal/wallet/credit` crea transacci�n WIN en `WalletTransactions`
- [ ] `POST /api/v1/internal/wallet/rollback` crea transacci�n ROLLBACK en `WalletTransactions`

### 3. Verificar Transacciones en Base de Datos

**Query SQL**:
```sql
SELECT 
    "Id",
    "TransactionType",
    "FromUserId",
    "FromUserType",
    "ToUserId",
    "ToUserType",
    "Amount",
    "Description",
    "PreviousBalanceFrom",
    "NewBalanceFrom",
    "PreviousBalanceTo",
    "NewBalanceTo",
    "CreatedAt"
FROM "WalletTransactions"
WHERE "TransactionType" IN ('BET', 'WIN', 'ROLLBACK')
ORDER BY "CreatedAt" DESC
LIMIT 10;
```

**Verificar**:
- [ ] Aparecen transacciones con `TransactionType = 'BET'`
- [ ] Aparecen transacciones con `TransactionType = 'WIN'`
- [ ] Aparecen transacciones con `TransactionType = 'ROLLBACK'`
- [ ] Los balances `Previous` y `New` est�n correctos

### 4. Verificar Balance Consistente

**Query SQL**:
```sql
SELECT 
    p."Id",
    p."Username",
    p."WalletBalance" as "CurrentBalance",
    (
        SELECT COALESCE(SUM(
            CASE 
                WHEN wt."ToUserId" = p."Id" THEN wt."Amount"
                WHEN wt."FromUserId" = p."Id" THEN -wt."Amount"
                ELSE 0
            END
        ), 0)
        FROM "WalletTransactions" wt
        WHERE wt."FromUserId" = p."Id" OR wt."ToUserId" = p."Id"
    ) as "CalculatedBalance"
FROM "Players" p
WHERE p."WalletBalance" > 0 OR EXISTS (
    SELECT 1 FROM "WalletTransactions" wt 
    WHERE wt."FromUserId" = p."Id" OR wt."ToUserId" = p."Id"
)
LIMIT 10;
```

**Verificar**:
- [ ] `CurrentBalance` = `CalculatedBalance` para todos los players
- [ ] Si hay diferencias, investigar causa

### 5. Verificar Idempotencia

**Test 1: Debit duplicado**
```bash
# Enviar mismo request dos veces con mismo ExternalRef
curl -X POST http://localhost:5000/api/v1/internal/wallet/debit \
  -H "Content-Type: application/json" \
  -d '{
    "playerId": "xxx",
    "amount": 10.00,
    "externalRef": "test-bet-001",
    "gameCode": "slots",
    "roundId": "round-001"
  }'
```
- [ ] Primera llamada crea transacci�n
- [ ] Segunda llamada retorna mismo balance sin duplicar

**Test 2: Rollback duplicado**
```bash
# Enviar rollback dos veces del mismo ExternalRef
curl -X POST http://localhost:5000/api/v1/internal/wallet/rollback \
  -H "Content-Type: application/json" \
  -d '{
    "externalRefOriginal": "test-bet-001"
  }'
```
- [ ] Primera llamada revierte transacci�n
- [ ] Segunda llamada retorna idempotente

### 6. Verificar Integraci�n con Admin

**Verificar que admin endpoints muestran transacciones de juegos**:
```bash
curl -X GET "http://localhost:5000/api/v1/admin/transactions?page=1&pageSize=20" \
  -H "Authorization: Bearer {admin_token}"
```

**Verificar response incluye**:
- [ ] Transacciones con `transactionType: "BET"`
- [ ] Transacciones con `transactionType: "WIN"`
- [ ] Transacciones con `transactionType: "ROLLBACK"`
- [ ] Transacciones con `transactionType: "MINT"`
- [ ] Transacciones con `transactionType: "TRANSFER"`

### 7. Tests de Stress (Opcional)

**Concurrencia**:
- [ ] M�ltiples BETs simult�neos en mismo player no causan race conditions
- [ ] Balance final es correcto despu�s de N transacciones concurrentes

---

## ?? Resumen de Archivos

### Archivos NUEVOS
- [ ] `apps/Casino.Application/Services/IWalletService.cs`
- [ ] `apps/Casino.Application/Services/Implementations/UnifiedWalletService.cs`

### Archivos MODIFICADOS
- [ ] `apps/api/Casino.Api/Program.cs` (registro de servicio)
- [ ] `apps/api/Casino.Api/Endpoints/InternalWalletEndpoints.cs` (cambiar interfaz)
- [ ] `docs/transaction-types-guide.md` (documentar unificaci�n)

### Archivos a DEPRECAR (futuro)
- ?? `apps/Casino.Application/Services/ILegacyWalletService.cs`
- ?? `apps/Casino.Application/Services/Implementations/LegacyWalletService.cs`
- ?? `apps/Casino.Domain/Entities/Wallet.cs` (mantener pero no usar para balance)

---

## ?? Resultado Esperado

### Antes de la Migraci�n
```
Admin Endpoints                Gateway Endpoints
      ?                               ?
SimpleWalletService          LegacyWalletService
      ?                               ?
WalletTransactions               Wallet + Ledger
      ?                               ?
Player.WalletBalance          Wallet.BalanceBigint
                                      ?
                              ? Desincronizados
```

### Despu�s de la Migraci�n
```
Admin Endpoints                Gateway Endpoints
      ?                               ?
SimpleWalletService          UnifiedWalletService
      ?                               ?
      ?????????????????????????????????
                 ?
        WalletTransactions
        (TransactionType)
                 ?
        Player.WalletBalance
        (�nica Fuente de Verdad)
                 ?
              Ledger ?
        (Compatibilidad)
```

---

## ?? Consideraciones Importantes

### 1. Sin Breaking Changes
- ? Las rutas de API NO cambian
- ? Los DTOs NO cambian
- ? Los requests/responses mantienen misma estructura
- ? Los clientes externos NO requieren modificaciones

### 2. Tabla Wallet
- ?? NO eliminar inmediatamente
- ?? NO actualizar `Wallet.BalanceBigint`
- ? Mantener por referencia hist�rica
- ? Deprecar en futuro (cuando se confirme estabilidad)

### 3. Tabla Ledger
- ? Seguir creando registros por compatibilidad
- ? �til para reportes legacy
- ?? NO usar como source of truth

### 4. Rollback de la Migraci�n
Si es necesario revertir:
```csharp
// En Program.cs, restaurar:
builder.Services.AddScoped<ILegacyWalletService, LegacyWalletService>();

// Y en InternalWalletEndpoints.cs, restaurar:
[FromServices] ILegacyWalletService walletService
```

---

## ?? Comparaci�n Final

| Aspecto | ANTES | DESPU�S |
|---------|-------|---------|
| **Balance de jugadores** | `Player.WalletBalance` + `Wallet.BalanceBigint` (desincronizados) | `Player.WalletBalance` (�nico) ? |
| **Transacciones admin** | `WalletTransactions` | `WalletTransactions` ? |
| **Transacciones juegos** | Solo `Ledger` | `WalletTransactions` + `Ledger` (secundario) ? |
| **TransactionType** | Solo MINT, TRANSFER | MINT, TRANSFER, BET, WIN, ROLLBACK ? |
| **Auditor�a** | Fragmentada | Unificada en `WalletTransactions` ? |
| **Consultas** | 2 tablas separadas | 1 tabla �nica ? |
| **Tabla Wallet** | Usada activamente | Deprecada ? |
| **Complejidad** | Alta (2 sistemas) | Baja (1 sistema) ? |

---

## ?? Comandos de Implementaci�n

```bash
# 1. Crear archivos
touch apps/Casino.Application/Services/IWalletService.cs
touch apps/Casino.Application/Services/Implementations/UnifiedWalletService.cs

# 2. Verificar compilaci�n
dotnet build

# 3. Ejecutar tests (si existen)
dotnet test

# 4. Verificar endpoints
dotnet run --project apps/api/Casino.Api

# 5. Probar con curl
curl -X POST http://localhost:5000/api/v1/internal/wallet/balance \
  -H "Content-Type: application/json" \
  -d '{"playerId": "xxx"}'
```

---

**Fecha de creaci�n**: 13 de octubre, 2024  
**Prioridad**: Alta  
**Complejidad**: Media  
**Impacto**: Alto (unifica sistemas, mejora auditor�a, prepara escalabilidad)  
**Breaking Changes**: ? Ninguno  
**Tiempo estimado**: 2-4 horas de implementaci�n + testing

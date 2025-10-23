# ? Campo `Type` Agregado al Sistema de Catálogo

## ?? **Cambio Implementado**

Se agregó el campo `Type` a la entidad `Game` para clasificar juegos en dos categorías principales:
- **`SLOT`** - Juegos de slots/tragamonedas (RNG)
- **`LIVE_CASINO`** - Juegos con dealers en vivo

---

## ?? **Archivos Modificados**

| Archivo | Cambios |
|---------|---------|
| `Casino.Domain/Enums/GameType.cs` | ? Enum creado con `SLOT` y `LIVE_CASINO` |
| `Casino.Domain/Entities/Game.cs` | ? Campo `Type` agregado (enum) |
| `Casino.Infrastructure/Data/CasinoDbContext.cs` | ? Configuración EF Core para `Type` |
| `Casino.Application/DTOs/Game/GameDTOs.cs` | ? DTOs actualizados con `Type` |
| `Casino.Application/Services/Models/ServiceModels.cs` | ? `GetBrandGameResult` con `Type` |
| `Casino.Application/Services/Implementations/GameService.cs` | ? CRUD actualizado |
| `Casino.Application/Services/Implementations/BrandService.cs` | ? Catalog con `Type` |
| `Casino.Api/Endpoints/CatalogEndpoints.cs` | ? Filtro por `Type` agregado |
| `Casino.Application/Mappers/GameMappers.cs` | ? Mappers actualizados |

---

## ?? **Endpoints Actualizados**

### **GET /api/v1/catalog/games**

**Nuevos parámetros de query**:
```
?type=SLOT   # Filtra solo slots
?type=LIVE_CASINO # Filtra solo live casino
```

**Ejemplo completo**:
```bash
# Solo slots
curl "http://localhost:5000/api/v1/catalog/games?type=SLOT&page=1&pageSize=20"

# Solo live casino
curl "http://localhost:5000/api/v1/catalog/games?type=LIVE_CASINO&page=1&pageSize=20"

# Slots de alta volatilidad
curl "http://localhost:5000/api/v1/catalog/games?type=SLOT&volatility=HIGH"
```

**Response JSON**:
```json
{
  "games": [
    {
      "gameId": "uuid",
      "code": "sweet-bonanza",
    "name": "Sweet Bonanza",
  "provider": "pragmatic",
      "type": "SLOT",    // ? NUEVO
      "category": "video-slots",
      "imageUrl": "...",
      "rtp": 96.51,
      "volatility": "HIGH",
   "minBet": 0.20,
      "maxBet": 100.00,
      "isFeatured": true,
      "isNew": false,
      "enabled": true,
  "displayOrder": 1,
      "tags": ["multipliers", "cascading"]
 }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 150
}
```

---

## ??? **Migración de Base de Datos**

### **Opción 1: EF Core Migration** (Recomendado)

```powershell
cd apps\api\Casino.Api

# Crear migración
dotnet ef migrations add AddGameTypeField --project ..\..\Casino.Infrastructure --startup-project .

# Aplicar a la base de datos
dotnet ef database update --project ..\..\Casino.Infrastructure --startup-project .
```

### **Opción 2: SQL Manual**

```sql
-- Agregar columna Type a la tabla Games
ALTER TABLE "Games" 
ADD COLUMN "Type" varchar(20) NOT NULL DEFAULT 'SLOT';

-- Crear índice para performance
CREATE INDEX "IX_Games_Type" ON "Games"("Type");

-- Actualizar juegos existentes según su naturaleza
-- Ejemplo: marcar juegos de Evolution como LIVE_CASINO
UPDATE "Games" 
SET "Type" = 'LIVE_CASINO' 
WHERE "Provider" = 'evolution' 
  AND ("Category" LIKE '%live%' OR "Category" LIKE '%roulette%' OR "Category" LIKE '%blackjack%');

-- Los demás quedan como SLOT por defecto
```

---

## ?? **Clasificación de Juegos**

### **Criterios**:

| Tipo | Descripción | Ejemplos |
|------|-------------|----------|
| `SLOT` | Juegos RNG sin dealer | Sweet Bonanza, Book of Dead, Starburst |
| `LIVE_CASINO` | Juegos con dealer en vivo | Live Roulette, Live Blackjack, Crazy Time |

### **Otros Tipos de Juegos**:
- **Table games (ruleta, blackjack RNG)** ? `Type = SLOT`, `Category = table`
- **Crash games (Aviator)** ? `Type = SLOT`, `Category = crash`
- **Game shows (Crazy Time)** ? `Type = LIVE_CASINO`, `Category = game-shows`

---

## ?? **Testing**

### **1. Crear un juego de tipo SLOT**
```bash
curl -X POST "http://localhost:5000/api/v1/admin/games" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <token>" \
  -d '{
    "code": "sweet-bonanza",
    "provider": "pragmatic",
    "name": "Sweet Bonanza",
    "type": "SLOT",
    "category": "video-slots",
    "rtp": 96.51,
    "volatility": "HIGH",
    "enabled": true
  }'
```

### **2. Crear un juego de tipo LIVE_CASINO**
```bash
curl -X POST "http://localhost:5000/api/v1/admin/games" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <token>" \
  -d '{
    "code": "lightning-roulette",
    "provider": "evolution",
    "name": "Lightning Roulette",
    "type": "LIVE_CASINO",
    "category": "roulette",
    "rtp": 97.30,
    "enabled": true
}'
```

### **3. Filtrar por tipo**
```bash
# Solo slots
curl "http://localhost:5000/api/v1/catalog/games?type=SLOT"

# Solo live casino
curl "http://localhost:5000/api/v1/catalog/games?type=LIVE_CASINO"
```

---

## ?? **Importante**

1. **Reinicia el backend** después de los cambios:
   ```powershell
   # Detener (Ctrl+C) y reiniciar
   cd apps\api\Casino.Api
   dotnet run
   ```

2. **Aplica la migración** a la base de datos antes de usar el backend

3. **El campo `Type` es obligatorio** - valor por defecto: `SLOT`

4. **Frontend debe manejar los dos tipos**:
   - Pestaña "Slots" ? `?type=SLOT`
   - Pestaña "Live Casino" ? `?type=LIVE_CASINO`

---

## ? **Estado**

- ? **Código actualizado**
- ? **DTOs extendidos**
- ? **Endpoints con filtro**
- ? **Compilación exitosa**
- ? **Pendiente**: Aplicar migración SQL

---

**Próximo paso**: Ejecutar la migración de base de datos con EF Core o SQL manual.

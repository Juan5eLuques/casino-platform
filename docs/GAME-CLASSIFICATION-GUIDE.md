# ?? Clasificación de Juegos - Sistema de Catálogo

## ?? **Jerarquía de Clasificación**

### **1. Type (Tipo Principal)** - Campo `Type` (Enum)
**Propósito**: Clasificación fundamental del juego
**Valores permitidos**: 
- `SLOT` - Juegos de slots/tragamonedas (RNG)
- `LIVE_CASINO` - Juegos con dealers en vivo

**Uso en el frontend**:
- Navegación principal (pestaña Slots vs Casino en Vivo)
- Filtros principales
- Separación de secciones

**Ejemplos**:
```json
{
  "type": "SLOT",
  "name": "Sweet Bonanza",
  "category": "video-slots"
}

{
  "type": "LIVE_CASINO",
  "name": "Live Roulette",
  "category": "roulette"
}
```

---

### **2. Category (Categoría)** - Campo `Category` (String)
**Propósito**: Subcategoría dentro del tipo principal
**Valores sugeridos**:

#### **Para SLOT**:
- `video-slots` - Slots de video modernas
- `classic-slots` - Slots clásicas de 3 rodillos
- `megaways` - Slots con mecánica Megaways
- `jackpot` - Slots con jackpot progresivo
- `branded` - Slots con licencias (películas, series)

#### **Para LIVE_CASINO**:
- `roulette` - Ruleta en vivo
- `blackjack` - Blackjack en vivo
- `baccarat` - Baccarat en vivo
- `poker` - Poker en vivo
- `game-shows` - Game shows (Crazy Time, Monopoly)
- `other-table` - Otros juegos de mesa

**Uso en el frontend**:
- Filtros secundarios
- Secciones dentro de cada tipo
- Breadcrumbs

---

### **3. Tags (Etiquetas)** - Campo `AdditionalTags` (String[])
**Propósito**: Características adicionales, temáticas y mecánicas
**Valores sugeridos**:

#### **Tags Generales**:
- `popular` - Juegos populares
- `new` - Juegos nuevos
- `exclusive` - Exclusivos del casino
- `high-rtp` - RTP superior a 96%
- `high-volatility` - Alta volatilidad
- `low-volatility` - Baja volatilidad

#### **Tags Temáticos**:
- `egyptian` - Temática egipcia
- `fruits` - Temática de frutas
- `adventure` - Temática de aventura
- `asian` - Temática asiática
- `mythology` - Mitología

#### **Tags de Mecánicas**:
- `multipliers` - Con multiplicadores
- `free-spins` - Con giros gratis
- `cascading` - Rodillos en cascada
- `expanding-wilds` - Wilds expansivos
- `bonus-buy` - Compra de bonus

**Uso en el frontend**:
- Búsqueda avanzada
- Filtros múltiples
- Recomendaciones
- SEO

---

## ?? **Ejemplos Completos**

### **Ejemplo 1: Sweet Bonanza (Slot)**
```json
{
  "code": "sweet-bonanza",
  "name": "Sweet Bonanza",
  "provider": "pragmatic",
  "type": "SLOT",
  "category": "video-slots",
  "rtp": 96.51,
  "volatility": "HIGH",
  "minBet": 0.20,
  "maxBet": 100.00,
  "isFeatured": true,
  "isNew": false,
  "additionalTags": [
    "multipliers",
    "cascading",
    "fruits",
    "popular"
  ]
}
```

### **Ejemplo 2: Live Roulette (Casino en Vivo)**
```json
{
  "code": "evolution-lightning-roulette",
  "name": "Lightning Roulette",
  "provider": "evolution",
  "type": "LIVE_CASINO",
  "category": "roulette",
  "rtp": 97.30,
  "volatility": null,
  "minBet": 0.20,
  "maxBet": 1000.00,
  "isFeatured": true,
  "isNew": false,
  "additionalTags": [
    "multipliers",
    "popular",
    "exclusive"
  ]
}
```

### **Ejemplo 3: Book of Dead (Slot)**
```json
{
  "code": "book-of-dead",
  "name": "Book of Dead",
  "provider": "playngo",
  "type": "SLOT",
  "category": "video-slots",
  "rtp": 96.21,
  "volatility": "HIGH",
  "minBet": 0.10,
  "maxBet": 50.00,
  "isFeatured": true,
  "isNew": false,
  "additionalTags": [
    "egyptian",
 "free-spins",
    "expanding-wilds",
    "high-rtp",
    "popular"
  ]
}
```

### **Ejemplo 4: Crazy Time (Game Show en Vivo)**
```json
{
  "code": "evolution-crazy-time",
  "name": "Crazy Time",
  "provider": "evolution",
  "type": "LIVE_CASINO",
  "category": "game-shows",
  "rtp": 95.41,
  "volatility": null,
  "minBet": 0.10,
  "maxBet": 10000.00,
  "isFeatured": true,
  "isNew": false,
  "additionalTags": [
    "multipliers",
    "bonus-rounds",
  "popular",
    "exclusive"
  ]
}
```

---

## ?? **Queries de Filtrado**

### **Ejemplo 1: Slots de alta volatilidad**
```
GET /api/v1/catalog/games?type=SLOT&volatility=HIGH
```

### **Ejemplo 2: Ruletas en vivo**
```
GET /api/v1/catalog/games?type=LIVE_CASINO&category=roulette
```

### **Ejemplo 3: Slots con temática egipcia**
```
GET /api/v1/catalog/games?type=SLOT&tags=egyptian
```

### **Ejemplo 4: Juegos populares**
```
GET /api/v1/catalog/games?tags=popular
```

### **Ejemplo 5: Slots con alto RTP**
```
GET /api/v1/catalog/games?type=SLOT&tags=high-rtp
```

---

## ? **Resumen de Campos**

| Campo | Tipo | Valores | Propósito |
|-------|------|---------|-----------|
| `Type` | Enum | `SLOT`, `LIVE_CASINO` | Clasificación principal |
| `Category` | String | `video-slots`, `roulette`, etc. | Subcategoría |
| `Volatility` | String | `LOW`, `MEDIUM`, `HIGH` | Volatilidad (solo slots) |
| `AdditionalTags` | String[] | `popular`, `egyptian`, etc. | Características adicionales |
| `IsFeatured` | Boolean | `true`, `false` | Destacado |
| `IsNew` | Boolean | `true`, `false` | Nuevo |
| `RTP` | Decimal | `96.51` | Return to Player % |

---

## ?? **Migración de Datos Existentes**

Si ya tienes juegos con `Type = TABLE`, `CRASH`, o `OTHER`, debes:

1. **Reclasificar según su naturaleza**:
   - `TABLE` (ruleta, blackjack sin dealer) ? `Type = SLOT`, `Category = table`
   - `CRASH` (Aviator, etc.) ? `Type = SLOT`, `Category = crash`, `Tags = ["crash"]`
   - `OTHER` ? Evaluar caso por caso

2. **Usar Category y Tags**:
 - Los tipos de juego que no son slots puros ni live casino se clasifican como `SLOT` con categorías específicas
   - Los tags complementan la clasificación

---

## ?? **Notas Importantes**

1. ? **Type es obligatorio** y solo puede ser `SLOT` o `LIVE_CASINO`
2. ? **Category es opcional** pero recomendado para mejor UX
3. ? **Tags son ilimitados** - agrega todos los que sean relevantes
4. ? **Volatility solo aplica a slots** - null para live casino
5. ? **RTP aplica a todos los juegos**

---

**Última actualización**: 2025-01-24  
**Versión**: 1.0

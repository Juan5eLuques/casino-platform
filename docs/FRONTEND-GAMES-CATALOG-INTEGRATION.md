# ?? Frontend - Integración del Catálogo de Juegos

## ?? **Información General**

Este documento describe cómo integrar el catálogo de juegos en el frontend para listar y filtrar los juegos disponibles para el brand actual.

---

## ?? **Endpoint Principal**

### **GET `/api/v1/catalog/games`**

**URL Base**: `https://api.tudominio.com/api/v1/catalog/games`

**Método**: `GET`

**Autenticación**: No requiere autenticación (público)

**Brand Resolution**: El backend resuelve automáticamente el brand basado en el dominio del frontend (usando el header `Origin` o `Referer`)

---

## ?? **Parámetros de Query**

| Parámetro | Tipo | Requerido | Default | Descripción |
|-----------|------|-----------|---------|-------------|
| `page` | `number` | No | `1` | Número de página para paginación |
| `pageSize` | `number` | No | `20` | Cantidad de juegos por página (máx: 100) |
| `type` | `string` | No | - | Filtrar por tipo: `SLOT` o `LIVE_CASINO` |
| `category` | `string` | No | - | Filtrar por categoría (ej: `video-slots`, `roulette`) |
| `provider` | `string` | No | - | Filtrar por proveedor (ej: `pragmatic`, `evolution`) |
| `featured` | `boolean` | No | - | Solo juegos destacados (`true`) |
| `enabled` | `boolean` | No | - | Solo juegos habilitados (`true`) |

---

## ?? **Response Structure**

```typescript
interface CatalogGamesResponse {
  games: Game[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

interface Game {
  gameId: string;           // UUID del juego
  code: string;    // Código único del juego
  name: string;             // Nombre del juego
  provider: string;         // Proveedor (pragmatic, evolution, etc.)
  type: 'SLOT' | 'LIVE_CASINO'; // Tipo de juego
  category: string | null;  // Categoría (video-slots, roulette, etc.)
  imageUrl: string | null;  // URL de la imagen/thumbnail
  rtp: number | null;       // Return to Player % (ej: 96.51)
  volatility: string | null; // LOW, MEDIUM, HIGH (solo para slots)
  minBet: number | null;    // Apuesta mínima
  maxBet: number | null; // Apuesta máxima
  isFeatured: boolean;      // Si es destacado
  isNew: boolean;       // Si es nuevo
  enabled: boolean;  // Si está habilitado
  displayOrder: number;  // Orden de visualización
  tags: string[];           // Tags adicionales
}
```

---

## ?? **Ejemplos de Implementación**

### **1. Fetch Básico (Vanilla JS)**

```typescript
async function getCatalogGames(params = {}) {
  const queryParams = new URLSearchParams({
    page: params.page || '1',
    pageSize: params.pageSize || '20',
    ...(params.type && { type: params.type }),
    ...(params.category && { category: params.category }),
    ...(params.provider && { provider: params.provider }),
    ...(params.featured && { featured: 'true' }),
  });

  const response = await fetch(
    `https://api.tudominio.com/api/v1/catalog/games?${queryParams}`,
    {
    method: 'GET',
      headers: {
        'Content-Type': 'application/json',
    },
      credentials: 'include', // Importante para brand resolution
    }
  );

  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }

  return await response.json();
}

// Uso
const games = await getCatalogGames({ 
  type: 'SLOT', 
  page: 1, 
  pageSize: 20 
});
console.log(games);
```

---

### **2. React con useState/useEffect**

```typescript
import { useState, useEffect } from 'react';

interface CatalogFilters {
  page: number;
  pageSize: number;
  type?: 'SLOT' | 'LIVE_CASINO';
  category?: string;
  provider?: string;
  featured?: boolean;
}

function GamesCatalog() {
  const [games, setGames] = useState<Game[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [totalPages, setTotalPages] = useState(0);
  const [filters, setFilters] = useState<CatalogFilters>({
    page: 1,
    pageSize: 20,
    type: 'SLOT',
  });

  useEffect(() => {
    const fetchGames = async () => {
      setLoading(true);
      setError(null);

      try {
        const queryParams = new URLSearchParams({
  page: filters.page.toString(),
          pageSize: filters.pageSize.toString(),
  ...(filters.type && { type: filters.type }),
 ...(filters.category && { category: filters.category }),
  ...(filters.provider && { provider: filters.provider }),
          ...(filters.featured && { featured: 'true' }),
   });

        const response = await fetch(
          `https://api.tudominio.com/api/v1/catalog/games?${queryParams}`,
   {
    method: 'GET',
       headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
      }
        );

        if (!response.ok) {
   throw new Error('Failed to fetch games');
        }

        const data = await response.json();
        setGames(data.games);
        setTotalPages(data.totalPages);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'An error occurred');
      } finally {
        setLoading(false);
      }
    };

    fetchGames();
  }, [filters]);

  if (loading) return <div>Loading...</div>;
  if (error) return <div>Error: {error}</div>;

  return (
    <div>
    <h1>Games Catalog</h1>
      
    {/* Filtros */}
      <div>
      <button onClick={() => setFilters({ ...filters, type: 'SLOT' })}>
          Slots
   </button>
        <button onClick={() => setFilters({ ...filters, type: 'LIVE_CASINO' })}>
          Live Casino
</button>
      </div>

      {/* Grid de juegos */}
      <div className="games-grid">
        {games.map((game) => (
          <div key={game.gameId} className="game-card">
            <img src={game.imageUrl || '/placeholder.jpg'} alt={game.name} />
   <h3>{game.name}</h3>
     <p>{game.provider}</p>
   {game.rtp && <span>RTP: {game.rtp}%</span>}
          {game.isFeatured && <span className="badge">Featured</span>}
            {game.isNew && <span className="badge">New</span>}
    </div>
        ))}
      </div>

   {/* Paginación */}
      <div>
        <button 
       disabled={filters.page === 1}
   onClick={() => setFilters({ ...filters, page: filters.page - 1 })}
      >
   Previous
        </button>
        <span>Page {filters.page} of {totalPages}</span>
   <button 
 disabled={filters.page === totalPages}
      onClick={() => setFilters({ ...filters, page: filters.page + 1 })}
        >
        Next
        </button>
      </div>
    </div>
  );
}

export default GamesCatalog;
```

---

### **3. React con Custom Hook**

```typescript
import { useState, useEffect } from 'react';

interface UseGamesOptions {
  page?: number;
  pageSize?: number;
  type?: 'SLOT' | 'LIVE_CASINO';
  category?: string;
  provider?: string;
  featured?: boolean;
}

function useGames(options: UseGamesOptions = {}) {
  const [games, setGames] = useState<Game[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pagination, setPagination] = useState({
    page: 1,
    pageSize: 20,
    totalCount: 0,
    totalPages: 0,
  });

  useEffect(() => {
    const fetchGames = async () => {
      setLoading(true);
      setError(null);

      try {
        const queryParams = new URLSearchParams({
          page: (options.page || 1).toString(),
      pageSize: (options.pageSize || 20).toString(),
          ...(options.type && { type: options.type }),
          ...(options.category && { category: options.category }),
          ...(options.provider && { provider: options.provider }),
        ...(options.featured && { featured: 'true' }),
        });

        const response = await fetch(
 `https://api.tudominio.com/api/v1/catalog/games?${queryParams}`,
          {
            method: 'GET',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
          }
        );

  if (!response.ok) {
   throw new Error('Failed to fetch games');
        }

        const data = await response.json();
        setGames(data.games);
        setPagination({
      page: data.page,
    pageSize: data.pageSize,
   totalCount: data.totalCount,
          totalPages: data.totalPages,
        });
      } catch (err) {
        setError(err instanceof Error ? err.message : 'An error occurred');
      } finally {
        setLoading(false);
      }
    };

    fetchGames();
  }, [options.page, options.pageSize, options.type, options.category, options.provider, options.featured]);

  return { games, loading, error, pagination };
}

// Uso
function GamesList() {
  const { games, loading, error, pagination } = useGames({ 
    type: 'SLOT',
    page: 1,
    pageSize: 20 
  });

  if (loading) return <div>Loading...</div>;
  if (error) return <div>Error: {error}</div>;

  return (
    <div>
      {games.map((game) => (
        <div key={game.gameId}>{game.name}</div>
      ))}
      <p>Showing {games.length} of {pagination.totalCount} games</p>
  </div>
  );
}
```

---

### **4. Vue 3 Composition API**

```typescript
import { ref, computed, watch } from 'vue';

interface Game {
  gameId: string;
  code: string;
  name: string;
  provider: string;
  type: 'SLOT' | 'LIVE_CASINO';
  category: string | null;
  imageUrl: string | null;
  rtp: number | null;
  volatility: string | null;
  minBet: number | null;
  maxBet: number | null;
  isFeatured: boolean;
  isNew: boolean;
  enabled: boolean;
  displayOrder: number;
  tags: string[];
}

export function useGames() {
  const games = ref<Game[]>([]);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const page = ref(1);
  const pageSize = ref(20);
  const totalPages = ref(0);
  const type = ref<'SLOT' | 'LIVE_CASINO' | undefined>('SLOT');

  const fetchGames = async () => {
    loading.value = true;
    error.value = null;

    try {
      const queryParams = new URLSearchParams({
        page: page.value.toString(),
     pageSize: pageSize.value.toString(),
        ...(type.value && { type: type.value }),
      });

      const response = await fetch(
  `https://api.tudominio.com/api/v1/catalog/games?${queryParams}`,
        {
          method: 'GET',
       headers: { 'Content-Type': 'application/json' },
  credentials: 'include',
        }
      );

      if (!response.ok) {
    throw new Error('Failed to fetch games');
      }

      const data = await response.json();
      games.value = data.games;
      totalPages.value = data.totalPages;
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'An error occurred';
    } finally {
      loading.value = false;
    }
  };

  watch([page, pageSize, type], fetchGames, { immediate: true });

  return {
    games,
    loading,
    error,
    page,
    pageSize,
    totalPages,
    type,
    fetchGames,
  };
}
```

---

## ?? **Casos de Uso Comunes**

### **1. Listar Solo Slots**
```typescript
const slots = await getCatalogGames({ type: 'SLOT' });
```

### **2. Listar Solo Live Casino**
```typescript
const liveCasino = await getCatalogGames({ type: 'LIVE_CASINO' });
```

### **3. Juegos Destacados**
```typescript
const featured = await getCatalogGames({ featured: true });
```

### **4. Slots de un Proveedor Específico**
```typescript
const pragmaticSlots = await getCatalogGames({ 
  type: 'SLOT', 
  provider: 'pragmatic' 
});
```

### **5. Ruletas en Vivo**
```typescript
const roulettes = await getCatalogGames({ 
  type: 'LIVE_CASINO', 
  category: 'roulette' 
});
```

### **6. Buscar con Múltiples Filtros**
```typescript
const filteredGames = await getCatalogGames({ 
  type: 'SLOT',
  provider: 'pragmatic',
  featured: true,
  page: 1,
  pageSize: 50
});
```

---

## ?? **UI Component Example (React + Tailwind)**

```typescript
import { useState } from 'react';

function GameCard({ game }: { game: Game }) {
  return (
    <div className="bg-white rounded-lg shadow-md overflow-hidden hover:shadow-xl transition-shadow">
      <div className="relative">
        <img 
   src={game.imageUrl || '/placeholder.jpg'} 
    alt={game.name}
          className="w-full h-48 object-cover"
   />
        {game.isFeatured && (
   <span className="absolute top-2 left-2 bg-yellow-500 text-white px-2 py-1 rounded text-xs font-bold">
       Featured
          </span>
        )}
    {game.isNew && (
          <span className="absolute top-2 right-2 bg-green-500 text-white px-2 py-1 rounded text-xs font-bold">
            New
          </span>
        )}
</div>
      <div className="p-4">
        <h3 className="font-bold text-lg mb-1">{game.name}</h3>
   <p className="text-gray-600 text-sm mb-2">{game.provider}</p>
        <div className="flex justify-between items-center">
          {game.rtp && (
            <span className="text-xs bg-blue-100 text-blue-800 px-2 py-1 rounded">
     RTP: {game.rtp}%
            </span>
          )}
   {game.volatility && (
  <span className="text-xs bg-gray-100 text-gray-800 px-2 py-1 rounded">
     {game.volatility}
         </span>
          )}
        </div>
        <button className="w-full mt-3 bg-blue-600 text-white py-2 rounded hover:bg-blue-700 transition-colors">
          Play Now
        </button>
  </div>
    </div>
  );
}

function GamesCatalog() {
  const [activeTab, setActiveTab] = useState<'SLOT' | 'LIVE_CASINO'>('SLOT');
  const { games, loading, pagination } = useGames({ type: activeTab });

  return (
    <div className="container mx-auto px-4 py-8">
      {/* Tabs */}
      <div className="flex space-x-4 mb-8 border-b">
        <button
          onClick={() => setActiveTab('SLOT')}
      className={`pb-2 px-4 ${
       activeTab === 'SLOT'
  ? 'border-b-2 border-blue-600 text-blue-600'
           : 'text-gray-600'
          }`}
        >
          Slots
        </button>
      <button
      onClick={() => setActiveTab('LIVE_CASINO')}
    className={`pb-2 px-4 ${
    activeTab === 'LIVE_CASINO'
  ? 'border-b-2 border-blue-600 text-blue-600'
       : 'text-gray-600'
          }`}
        >
          Live Casino
        </button>
      </div>

   {/* Loading State */}
      {loading && (
        <div className="text-center py-12">
 <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
        </div>
      )}

   {/* Games Grid */}
      <div className="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-4 gap-6">
        {games.map((game) => (
        <GameCard key={game.gameId} game={game} />
        ))}
      </div>

      {/* Pagination */}
    <div className="mt-8 flex justify-center">
      <span className="text-gray-600">
    Showing {games.length} of {pagination.totalCount} games
        </span>
    </div>
    </div>
  );
}

export default GamesCatalog;
```

---

## ?? **Consideraciones Importantes**

### **1. Brand Resolution**
- El backend detecta automáticamente el brand desde el header `Origin` o `Referer`
- **Importante**: Usa `credentials: 'include'` en las peticiones fetch para enviar cookies y headers

### **2. CORS**
- El backend debe tener configurado el dominio del frontend en `Brand.CorsOrigins`
- Ejemplo: `["https://slots.tudominio.com", "http://localhost:3000"]`

### **3. Performance**
- El endpoint soporta paginación - úsala para cargar juegos en lotes
- Máximo recomendado: 50 juegos por página
- Implementa scroll infinito o paginación clásica

### **4. Caching**
- Considera cachear la respuesta del catálogo (5-10 minutos)
- Usa `React Query` o `SWR` para mejor gestión de cache

### **5. Imágenes**
- Las URLs de imágenes pueden ser `null` - usa placeholders
- Implementa lazy loading para mejorar performance

---

## ?? **URLs de Ejemplo**

```
# Todos los juegos (paginado)
GET /api/v1/catalog/games?page=1&pageSize=20

# Solo slots
GET /api/v1/catalog/games?type=SLOT

# Solo live casino
GET /api/v1/catalog/games?type=LIVE_CASINO

# Juegos destacados
GET /api/v1/catalog/games?featured=true

# Slots de Pragmatic Play
GET /api/v1/catalog/games?type=SLOT&provider=pragmatic

# Ruletas en vivo
GET /api/v1/catalog/games?type=LIVE_CASINO&category=roulette

# Slots con alta volatilidad (requiere filtro custom en frontend)
GET /api/v1/catalog/games?type=SLOT
# Luego filtrar en frontend: games.filter(g => g.volatility === 'HIGH')
```

---

## ?? **Recursos Adicionales**

- **Documentación de clasificación**: `docs/GAME-CLASSIFICATION-GUIDE.md`
- **Implementación backend**: `docs/GAME-CATALOG-AND-LAUNCH-IMPLEMENTATION-SUMMARY.md`
- **Launch de juegos**: Ver siguiente documento para integrar el launch en iframe

---

**Última actualización**: 2025-01-24  
**Versión**: 1.0  
**API Version**: v1

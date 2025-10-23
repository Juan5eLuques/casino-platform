# Dashboard del Casino - Implementación Frontend

## ?? Contexto

Estás desarrollando el dashboard principal del backoffice de un casino multi-site. El dashboard muestra KPIs financieros, estadísticas de casino, conteos de usuarios y alertas operativas en tiempo real.

---

## ?? API Endpoint

### Endpoint Principal

```
GET /api/v1/admin/dashboard/overview
```

### Query Parameters

| Parámetro | Tipo | Requerido | Default | Descripción |
|-----------|------|-----------|---------|-------------|
| `scope` | string | ? | - | `DIRECT` \| `TREE` \| `GLOBAL` |
| `from` | ISO 8601 | ? | Hoy 00:00 | Fecha inicio |
| `to` | ISO 8601 | ? | Hoy 23:59:59 | Fecha fin |
| `timezone` | string | ? | UTC | Zona horaria |

#### Scopes Disponibles

- **DIRECT**: Solo datos del usuario actual
- **TREE**: Datos del usuario y todos sus subordinados (árbol jerárquico)
- **GLOBAL**: Todos los datos del brand (solo SUPER_ADMIN)

### Ejemplo de Request

```http
GET /api/v1/admin/dashboard/overview?scope=TREE&from=2025-01-01T00:00:00Z&to=2025-01-31T23:59:59Z
Authorization: Cookie (bk.token.localhost_dev=...)
```

---

## ?? Estructura de Respuesta

### Interface TypeScript Completa

```typescript
interface DashboardOverviewResponse {
  finanzas: FinancesSummary;
  casino: CasinoSummary;
usuarios: UsersCountsResponse;
  alertas: AlertsSummary;
}

interface FinancesSummary {
  period: {
    from: string;        // ISO 8601
    to: string;    // ISO 8601
    timezone: string;
  };
  scope: {
    type: string;        // "DIRECT" | "TREE" | "GLOBAL"
    userId: string; // GUID
    brandId: string;     // GUID
  };
  fichas: {
  balanceActual: number;      // Balance total actual
    deltaDelDia: number;   // Cambio neto del día
 breakdown: {
      houseBalance: number;     // Balance de admins
      cashiersBalance: number;  // Balance de cajeros
      playersBalance: number;// Balance de jugadores
    };
  };
  cargas: {
    total: number;       // Total de cargas internas
    count: number;              // Cantidad de transacciones
    promedio: number;   // Promedio por transacción
  };
  depositosA: {
    total: number;              // Total de depósitos a admins (MINT)
    count: number;
    promedio: number;
  };
  retiros: {
    total: number;  // Total de retiros/burns
    count: number;
    promedio: number;
  };
  links: {
    reporteMensual: string;     // URL para reporte mensual
  };
}

interface CasinoSummary {
  period: { from: string; to: string; timezone: string; };
  jugado: number;      // Total apostado
  pagado: number;      // Total pagado en premios
  netwin: number;         // Jugado - Pagado
  comisionPorcentaje: number;   // % de comisión promedio
  comision: number;          // Comisión calculada
  totalAPagar: number;          // NetWin - Comisión
  kpIs: {
    holdPercentage: number;     // % de retención
    rondasTotales: number;      // Cantidad de rondas
    apuestaPromedio: number;    // Apuesta promedio
    jugadoresActivos: number;   // Jugadores con actividad
  };
  links: {
    reporteMensual: string;
  };
}

interface UsersCountsResponse {
  jugadoresDirectos: number;// Jugadores creados por el usuario actual
  agentesDirectos: number;   // Cajeros hijos directos
  totalJugadores: number;   // Total de jugadores en el árbol
  totalAgentes: number;         // Total de cajeros en el árbol
  breakdown: {
    jugadoresActivos: number;
    jugadoresInactivos: number;
    agentesPorNivel: {
      [key: string]: number;    // ej: "nivel2": 5, "nivel3": 3
    };
  };
}

interface AlertsSummary {
  alertas: Alert[];
  estadoOperativo: {
    cajerosActivos: number;      // Cajeros con actividad últimas 24h
    jugadoresOnline: number;  // Jugadores con sesión activa
    floatTotal: number;   // Balance total de cajeros
    transaccionesPendientes: number;
  };
}

interface Alert {
  tipo: string;// "FLOAT_BAJO", "SALDO_NEGATIVO", etc.
  severidad: string;      // "LOW", "MEDIUM", "HIGH", "CRITICAL"
  count: number;          // Cantidad de ocurrencias
  total?: number;// Monto total (opcional)
  link?: string;   // URL para más detalles (opcional)
  mensaje: string;        // Descripción legible
}
```

---

## ?? Ejemplo de Respuesta Real

```json
{
  "finanzas": {
    "period": {
      "from": "2025-10-23T00:00:00Z",
   "to": "2025-10-23T23:59:59.9999999Z",
  "timezone": "UTC"
    },
 "scope": {
      "type": "TREE",
    "userId": "a8e3149b-9e79-4e36-88d9-6ca032420607",
      "brandId": "11111111-1111-1111-1111-111111111111"
    },
  "fichas": {
   "balanceActual": 50000,
      "deltaDelDia": 0,
      "breakdown": {
        "houseBalance": 49000,
 "cashiersBalance": 500,
        "playersBalance": 500
      }
  },
    "cargas": {
   "total": 0,
      "count": 0,
  "promedio": 0
    },
    "depositosA": {
  "total": 0,
      "count": 0,
  "promedio": 0
    },
    "retiros": {
      "total": 0,
      "count": 0,
      "promedio": 0
    },
    "links": {
      "reporteMensual": "/api/v1/admin/reports/finances/monthly?year=2025&month=10"
    }
  },
  "usuarios": {
    "jugadoresDirectos": 0,
  "agentesDirectos": 0,
    "totalJugadores": 2,
    "totalAgentes": 3,
    "breakdown": {
      "jugadoresActivos": 2,
      "jugadoresInactivos": 0,
      "agentesPorNivel": {
        "nivel2": 2,
 "nivel3": 1
      }
  }
  },
  "casino": {
    "period": {
      "from": "2025-10-23T00:00:00Z",
   "to": "2025-10-23T23:59:59.9999999Z",
      "timezone": "UTC"
},
    "jugado": 0,
    "pagado": 0,
    "netwin": 0,
    "comisionPorcentaje": 5,
    "comision": 0,
    "totalAPagar": 0,
    "kpIs": {
      "holdPercentage": 0,
   "rondasTotales": 0,
    "apuestaPromedio": 0,
      "jugadoresActivos": 0
    },
    "links": {
 "reporteMensual": "/api/v1/admin/reports/casino/monthly?year=2025&month=10"
    }
  },
  "alertas": {
    "alertas": [
      {
        "tipo": "FLOAT_BAJO",
        "severidad": "HIGH",
  "count": 3,
        "mensaje": "3 cajeros con saldo < 10000"
      }
    ],
  "estadoOperativo": {
      "cajerosActivos": 1,
      "jugadoresOnline": 0,
    "floatTotal": 500,
   "transaccionesPendientes": 0
    }
  }
}
```

---

## ?? Requerimientos de UI

### 1. Layout Principal

#### Header del Dashboard
- **Selector de scope**: Botones radio para DIRECT / TREE / GLOBAL
- **Selector de período**: 
  - Botones quick: Hoy / Esta semana / Este mes / Personalizado
  - DateRangePicker para selección personalizada
- **Botón de actualizar**: Recarga manual
- **Auto-refresh checkbox**: "Actualizar cada 30s"
- **Indicador de última actualización**: "Última actualización: hace 2 min"

#### Grid de Cards (Responsive)
- **Desktop**: 4 cards en fila (25% cada uno)
- **Tablet**: 2 cards en fila (50% cada uno)
- **Mobile**: 1 card por columna (100%)

### 2. Diseño de Cards

#### Card de Fichas (Verde)

```
?????????????????????????????????????????????
? ?? Fichas      [?]      ?
?????????????????????????????????????????????
?      ?
?   Balance Actual:        $50,000         ?
?   ? Hoy:     $0              ?
?        ?
?   ????????????????????????????????????  ?
?        ?
?   Breakdown: ?
?   ?? House:       $49,000  (98%)   ?
?   ?? Cajeros:           $500     (1%)    ?
?   ?? Jugadores:  $500     (1%)    ?
?          ?
?   ????????????????????????????????????  ?
?    ?
?   ?? Cargas:      $0 (0 trans.)  ?
?   ?? Depósitos:   $0 (0 trans.)  ?
?   ?? Retiros:     $0 (0 trans.)          ?
?           ?
?????????????????????????????????????????????
```

**Elementos**:
- Título con icono ??
- Botón de refresh en la esquina
- Balance principal destacado (font grande)
- Delta del día con color (verde si +, rojo si -)
- Breakdown con barra de progreso visual
- Transacciones resumidas al final

#### Card de Casino (Azul)

```
?????????????????????????????????????????????
? ?? Casino   [?]      ?
?????????????????????????????????????????????
?       ?
?   Jugado:  $0         ?
?   Pagado:          $0              ?
?   ????????????????????????????????????  ?
?   NetWin:    $0              ?
?          ?
?   Comisión (5%):         $0       ?
?   Total a Pagar: $0          ?
?            ?
?   ????????????????????????????????????  ?
?  ?
?   ?? KPIs:             ?
??? Hold:           0.00%           ?
?   ?? Rondas:  0             ?
?   ?? Apuesta Avg:        $0   ?
?   ?? Jugadores:  0         ?
?       ?
?????????????????????????????????????????????
```

**Elementos**:
- Título con icono ??
- Métricas principales (Jugado, Pagado, NetWin)
- Comisión destacada con porcentaje
- KPIs en lista con iconos
- Color de fondo azul suave

#### Card de Usuarios (Púrpura)

```
?????????????????????????????????????????????
? ?? Usuarios          [?]      ?
?????????????????????????????????????????????
?         ?
?   ?? Jugadores         ?
?   ?? Directos:           0   ?
?   ?? Total:              2          ?
?      ?? Activos:  2    (100%)     ?
?      ?? Inactivos:       0    (0%)       ?
?         ?
?   ????????????????????????????????????  ?
?              ?
?   ?? Agentes (Cajeros)    ?
?   ?? Directos:         0   ?
?   ?? Total:        3     ?
?      ?? Nivel 2:         2       ?
?      ?? Nivel 3:         1          ?
?       ?
?????????????????????????????????????????????
```

**Elementos**:
- Título con icono ??
- Sección de jugadores con breakdown
- Sección de agentes con niveles
- Barras de progreso para activos/inactivos
- Color de fondo púrpura suave

#### Card de Alertas (Rojo/Amarillo según severidad)

```
?????????????????????????????????????????????
? ?? Alertas (1)         [?]      ?
?????????????????????????????????????????????
?     ?
?   ?? HIGH: Float Bajo?
?   3 cajeros con saldo < $10,000   ?
?   [Ver detalles ?]               ?
?         ?
?   ????????????????????????????????????  ?
?      ?
?   ?? Estado Operativo:         ?
?   ?? Cajeros activos:    1         ?
?   ?? Jugadores online:   0   ?
?   ?? Float total:     $500   ?
?   ?? Trans. pendientes:  0      ?
?               ?
?????????????????????????????????????????????
```

**Elementos**:
- Título con contador de alertas
- Alertas con color según severidad:
  - ?? CRITICAL: Rojo (#dc2626)
  - ?? HIGH: Naranja (#ef4444)
  - ?? MEDIUM: Amarillo (#f59e0b)
  - ?? LOW: Azul (#3b82f6)
- Estado operativo con métricas clave
- Enlaces a vistas detalladas

### 3. Colores y Estilos

#### Paleta de Colores

```css
/* Cards */
.card-fichas    { background: linear-gradient(135deg, #10b981 0%, #059669 100%); }
.card-casino    { background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%); }
.card-usuarios  { background: linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%); }
.card-alertas   { background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%); }

/* Severidades de Alertas */
.alert-critical { background: #dc2626; color: white; }
.alert-high     { background: #ef4444; color: white; }
.alert-medium   { background: #f59e0b; color: white; }
.alert-low      { background: #3b82f6; color: white; }

/* Estados */
.positive { color: #10b981; } /* Verde para aumentos */
.negative { color: #ef4444; } /* Rojo para disminuciones */
```

### 4. Formato de Números

```typescript
// Monedas
formatCurrency(50000) ? "$50,000.00"

// Porcentajes
formatPercent(5.25) ? "5.25%"

// Enteros
formatNumber(1234) ? "1,234"

// Compactos (para grandes números)
formatCompact(50000) ? "$50K"
formatCompact(1500000) ? "$1.5M"
```

---

## ?? Implementación Frontend

### 1. Hook Personalizado

```typescript
// hooks/useDashboard.ts
import { useQuery } from '@tanstack/react-query';

interface UseDashboardParams {
  scope: 'DIRECT' | 'TREE' | 'GLOBAL';
  from?: Date;
  to?: Date;
  autoRefresh?: boolean;
}

export function useDashboard({ 
  scope, 
  from, 
  to, 
  autoRefresh = false 
}: UseDashboardParams) {
  return useQuery({
    queryKey: ['dashboard', scope, from, to],
    queryFn: async () => {
      const params = new URLSearchParams({
        scope,
        ...(from && { from: from.toISOString() }),
        ...(to && { to: to.toISOString() }),
      });
  
      const res = await fetch(
        `/api/v1/admin/dashboard/overview?${params}`,
        {
          credentials: 'include', // ? Importante para cookies
        }
      );
      
      if (!res.ok) {
        if (res.status === 401) {
          window.location.href = '/login';
          throw new Error('Unauthorized');
    }
        throw new Error('Failed to fetch dashboard');
      }
    
      return res.json() as Promise<DashboardOverviewResponse>;
    },
    refetchInterval: autoRefresh ? 30000 : false, // 30 segundos
    staleTime: 30000, // Cache por 30s
  });
}
```

### 2. Componentes

#### DashboardHeader

```typescript
// components/DashboardHeader.tsx
import { useState } from 'react';
import { DateRangePicker } from './DateRangePicker';

interface DashboardHeaderProps {
  scope: 'DIRECT' | 'TREE' | 'GLOBAL';
  onScopeChange: (scope: 'DIRECT' | 'TREE' | 'GLOBAL') => void;
  onPeriodChange: (from: Date, to: Date) => void;
  autoRefresh: boolean;
  onAutoRefreshChange: (enabled: boolean) => void;
  lastUpdate?: Date;
}

export function DashboardHeader({
  scope,
  onScopeChange,
  onPeriodChange,
  autoRefresh,
  onAutoRefreshChange,
  lastUpdate,
}: DashboardHeaderProps) {
  const [showCustomPeriod, setShowCustomPeriod] = useState(false);
  
  const handleQuickPeriod = (type: 'today' | 'week' | 'month') => {
    const now = new Date();
    let from: Date;
    
    switch (type) {
      case 'today':
        from = new Date(now.setHours(0, 0, 0, 0));
        break;
  case 'week':
        from = new Date(now.setDate(now.getDate() - 7));
        break;
      case 'month':
  from = new Date(now.setMonth(now.getMonth() - 1));
        break;
    }
    
    onPeriodChange(from, new Date());
  };
  
  return (
    <div className="bg-white shadow rounded-lg p-4 mb-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
{/* Selector de Scope */}
        <div className="flex gap-2">
 <button
            className={`px-4 py-2 rounded ${scope === 'DIRECT' ? 'bg-blue-500 text-white' : 'bg-gray-200'}`}
            onClick={() => onScopeChange('DIRECT')}
          >
     Directo
    </button>
    <button
            className={`px-4 py-2 rounded ${scope === 'TREE' ? 'bg-blue-500 text-white' : 'bg-gray-200'}`}
  onClick={() => onScopeChange('TREE')}
   >
  Árbol
          </button>
       <button
className={`px-4 py-2 rounded ${scope === 'GLOBAL' ? 'bg-blue-500 text-white' : 'bg-gray-200'}`}
            onClick={() => onScopeChange('GLOBAL')}
          >
   Global
          </button>
        </div>
        
 {/* Selector de Período */}
    <div className="flex gap-2">
          <button
       className="px-4 py-2 rounded bg-gray-200 hover:bg-gray-300"
            onClick={() => handleQuickPeriod('today')}
    >
            Hoy
 </button>
   <button
            className="px-4 py-2 rounded bg-gray-200 hover:bg-gray-300"
      onClick={() => handleQuickPeriod('week')}
   >
            Semana
          </button>
          <button
         className="px-4 py-2 rounded bg-gray-200 hover:bg-gray-300"
            onClick={() => handleQuickPeriod('month')}
          >
            Mes
          </button>
    <button
     className="px-4 py-2 rounded bg-gray-200 hover:bg-gray-300"
            onClick={() => setShowCustomPeriod(!showCustomPeriod)}
     >
            Personalizado
      </button>
        </div>
        
        {/* Auto-refresh */}
        <label className="flex items-center gap-2">
<input
   type="checkbox"
            checked={autoRefresh}
          onChange={(e) => onAutoRefreshChange(e.target.checked)}
          />
     <span className="text-sm">Auto-refresh (30s)</span>
     </label>
        
        {/* Última actualización */}
        {lastUpdate && (
          <span className="text-sm text-gray-500">
    Última actualización: {formatTimeAgo(lastUpdate)}
 </span>
        )}
      </div>
      
      {/* DateRangePicker (condicional) */}
      {showCustomPeriod && (
      <div className="mt-4">
     <DateRangePicker
  onSelect={(from, to) => {
       onPeriodChange(from, to);
    setShowCustomPeriod(false);
      }}
          />
    </div>
 )}
    </div>
  );
}

function formatTimeAgo(date: Date): string {
  const seconds = Math.floor((new Date().getTime() - date.getTime()) / 1000);
  
  if (seconds < 60) return 'Hace unos segundos';
  if (seconds < 3600) return `Hace ${Math.floor(seconds / 60)} min`;
  if (seconds < 86400) return `Hace ${Math.floor(seconds / 3600)} h`;
  return `Hace ${Math.floor(seconds / 86400)} días`;
}
```

#### FichasCard

```typescript
// components/FichasCard.tsx
import { FinancesSummary } from '../types/dashboard';
import { formatCurrency } from '../utils/formatters';

interface FichasCardProps {
  data: FinancesSummary;
}

export function FichasCard({ data }: FichasCardProps) {
  const { fichas, cargas, depositosA, retiros } = data;
  const delta = fichas.deltaDelDia;
  const deltaClass = delta >= 0 ? 'text-green-600' : 'text-red-600';
  
  return (
    <div className="bg-gradient-to-br from-green-500 to-green-600 text-white rounded-lg shadow-lg p-6">
      <div className="flex justify-between items-start mb-4">
      <h3 className="text-xl font-bold">?? Fichas</h3>
        <button className="text-white/80 hover:text-white">
          ?
        </button>
      </div>
      
      <div className="space-y-4">
     {/* Balance Actual */}
        <div>
       <div className="text-sm opacity-80">Balance Actual</div>
   <div className="text-3xl font-bold">
      {formatCurrency(fichas.balanceActual)}
 </div>
        </div>
        
   {/* Delta del Día */}
        <div>
          <div className="text-sm opacity-80">? Hoy</div>
     <div className={`text-xl font-semibold ${deltaClass}`}>
            {delta >= 0 ? '+' : ''}{formatCurrency(delta)}
 </div>
        </div>
        
        <hr className="border-white/20" />
  
        {/* Breakdown */}
        <div>
      <div className="text-sm font-medium mb-2">Breakdown</div>
     <div className="space-y-2">
            <div className="flex justify-between text-sm">
   <span>House:</span>
  <span className="font-semibold">
       {formatCurrency(fichas.breakdown.houseBalance)}
    </span>
      </div>
            <div className="flex justify-between text-sm">
     <span>Cajeros:</span>
      <span className="font-semibold">
        {formatCurrency(fichas.breakdown.cashiersBalance)}
     </span>
    </div>
         <div className="flex justify-between text-sm">
         <span>Jugadores:</span>
    <span className="font-semibold">
      {formatCurrency(fichas.breakdown.playersBalance)}
   </span>
   </div>
 </div>
     </div>
        
  <hr className="border-white/20" />
        
        {/* Transacciones */}
    <div className="space-y-1 text-sm">
          <div className="flex justify-between">
            <span>?? Cargas:</span>
     <span>{formatCurrency(cargas.total)} ({cargas.count})</span>
   </div>
       <div className="flex justify-between">
     <span>?? Depósitos:</span>
            <span>{formatCurrency(depositosA.total)} ({depositosA.count})</span>
  </div>
   <div className="flex justify-between">
        <span>?? Retiros:</span>
    <span>{formatCurrency(retiros.total)} ({retiros.count})</span>
  </div>
  </div>
    </div>
    </div>
  );
}
```

#### CasinoCard

```typescript
// components/CasinoCard.tsx
import { CasinoSummary } from '../types/dashboard';
import { formatCurrency, formatPercent, formatNumber } from '../utils/formatters';

interface CasinoCardProps {
  data: CasinoSummary;
}

export function CasinoCard({ data }: CasinoCardProps) {
  return (
    <div className="bg-gradient-to-br from-blue-500 to-blue-600 text-white rounded-lg shadow-lg p-6">
      <div className="flex justify-between items-start mb-4">
        <h3 className="text-xl font-bold">?? Casino</h3>
        <button className="text-white/80 hover:text-white">
        ?
        </button>
      </div>
      
      <div className="space-y-4">
 {/* Métricas Principales */}
        <div className="space-y-2">
          <div className="flex justify-between text-sm">
  <span>Jugado:</span>
 <span className="font-semibold">{formatCurrency(data.jugado)}</span>
     </div>
  <div className="flex justify-between text-sm">
    <span>Pagado:</span>
            <span className="font-semibold">{formatCurrency(data.pagado)}</span>
          </div>
        </div>
      
    <hr className="border-white/20" />
        
        <div>
          <div className="text-sm opacity-80">NetWin</div>
    <div className="text-2xl font-bold">
  {formatCurrency(data.netwin)}
          </div>
        </div>
        
   {/* Comisión */}
        <div className="space-y-1 text-sm">
   <div className="flex justify-between">
<span>Comisión ({formatPercent(data.comisionPorcentaje)}):</span>
  <span className="font-semibold">{formatCurrency(data.comision)}</span>
        </div>
          <div className="flex justify-between font-bold">
          <span>Total a Pagar:</span>
            <span>{formatCurrency(data.totalAPagar)}</span>
      </div>
      </div>
  
        <hr className="border-white/20" />
        
        {/* KPIs */}
        <div>
          <div className="text-sm font-medium mb-2">?? KPIs</div>
          <div className="space-y-1 text-sm">
      <div className="flex justify-between">
         <span>Hold:</span>
     <span>{formatPercent(data.kpIs.holdPercentage)}</span>
            </div>
            <div className="flex justify-between">
        <span>Rondas:</span>
         <span>{formatNumber(data.kpIs.rondasTotales)}</span>
      </div>
            <div className="flex justify-between">
        <span>Apuesta Avg:</span>
        <span>{formatCurrency(data.kpIs.apuestaPromedio)}</span>
  </div>
            <div className="flex justify-between">
    <span>Jugadores:</span>
      <span>{formatNumber(data.kpIs.jugadoresActivos)}</span>
    </div>
     </div>
        </div>
      </div>
    </div>
  );
}
```

#### UsuariosCard

```typescript
// components/UsuariosCard.tsx
import { UsersCountsResponse } from '../types/dashboard';
import { formatNumber } from '../utils/formatters';

interface UsuariosCardProps {
  data: UsersCountsResponse;
}

export function UsuariosCard({ data }: UsuariosCardProps) {
  const { breakdown } = data;
  const activePercent = data.totalJugadores > 0 
    ? (breakdown.jugadoresActivos / data.totalJugadores * 100).toFixed(0)
    : 0;
  
  return (
    <div className="bg-gradient-to-br from-purple-500 to-purple-600 text-white rounded-lg shadow-lg p-6">
 <div className="flex justify-between items-start mb-4">
        <h3 className="text-xl font-bold">?? Usuarios</h3>
     <button className="text-white/80 hover:text-white">
?
        </button>
      </div>
      
      <div className="space-y-4">
        {/* Jugadores */}
        <div>
          <div className="text-sm font-medium mb-2">?? Jugadores</div>
        <div className="space-y-1 text-sm">
  <div className="flex justify-between">
     <span>Directos:</span>
   <span className="font-semibold">{formatNumber(data.jugadoresDirectos)}</span>
         </div>
            <div className="flex justify-between">
          <span>Total:</span>
  <span className="font-semibold">{formatNumber(data.totalJugadores)}</span>
      </div>
        <div className="ml-4 space-y-1 text-xs">
    <div className="flex justify-between">
      <span>?? Activos:</span>
            <span>{formatNumber(breakdown.jugadoresActivos)} ({activePercent}%)</span>
              </div>
         <div className="flex justify-between">
          <span>?? Inactivos:</span>
           <span>{formatNumber(breakdown.jugadoresInactivos)}</span>
   </div>
       </div>
</div>
        </div>
        
        <hr className="border-white/20" />
        
        {/* Agentes */}
        <div>
          <div className="text-sm font-medium mb-2">?? Agentes (Cajeros)</div>
   <div className="space-y-1 text-sm">
            <div className="flex justify-between">
  <span>Directos:</span>
         <span className="font-semibold">{formatNumber(data.agentesDirectos)}</span>
    </div>
            <div className="flex justify-between">
              <span>Total:</span>
              <span className="font-semibold">{formatNumber(data.totalAgentes)}</span>
   </div>
        {Object.entries(breakdown.agentesPorNivel).map(([nivel, count]) => (
         <div key={nivel} className="ml-4 flex justify-between text-xs">
         <span>?? {nivel.replace('nivel', 'Nivel ')}:</span>
      <span>{formatNumber(count)}</span>
           </div>
      ))}
          </div>
        </div>
      </div>
    </div>
  );
}
```

#### AlertasCard

```typescript
// components/AlertasCard.tsx
import { AlertsSummary, Alert } from '../types/dashboard';
import { formatCurrency, formatNumber } from '../utils/formatters';

interface AlertasCardProps {
  data: AlertsSummary;
}

export function AlertasCard({ data }: AlertasCardProps) {
  const { alertas, estadoOperativo } = data;
  
  const getSeverityColor = (severidad: string) => {
    switch (severidad) {
      case 'CRITICAL': return 'bg-red-600';
      case 'HIGH': return 'bg-orange-500';
      case 'MEDIUM': return 'bg-yellow-500';
  case 'LOW': return 'bg-blue-500';
      default: return 'bg-gray-500';
    }
  };
  
  const getSeverityIcon = (severidad: string) => {
    switch (severidad) {
      case 'CRITICAL': return '??';
   case 'HIGH': return '??';
      case 'MEDIUM': return '??';
      case 'LOW': return '??';
      default: return '?';
  }
  };
  
  return (
    <div className="bg-gradient-to-br from-red-500 to-red-600 text-white rounded-lg shadow-lg p-6">
      <div className="flex justify-between items-start mb-4">
        <h3 className="text-xl font-bold">
    ?? Alertas ({alertas.length})
  </h3>
        <button className="text-white/80 hover:text-white">
          ?
        </button>
      </div>
      
      <div className="space-y-4">
 {/* Alertas */}
  {alertas.length > 0 ? (
          <div className="space-y-2">
            {alertas.map((alerta, index) => (
              <div
                key={index}
className="bg-white/20 rounded p-3 text-sm"
       >
         <div className="flex items-start gap-2">
          <span className="text-lg">
         {getSeverityIcon(alerta.severidad)}
          </span>
          <div className="flex-1">
        <div className="font-semibold">
            {alerta.severidad}: {alerta.tipo.replace('_', ' ')}
           </div>
  <div className="text-xs opacity-90">
          {alerta.mensaje}
      </div>
            {alerta.link && (
         <a
         href={alerta.link}
    className="text-xs underline hover:no-underline"
       >
     Ver detalles ?
 </a>
         )}
        </div>
          </div>
      </div>
   ))}
          </div>
        ) : (
          <div className="text-center py-4 text-white/60">
            ? Sin alertas activas
    </div>
        )}
        
        <hr className="border-white/20" />
        
        {/* Estado Operativo */}
 <div>
          <div className="text-sm font-medium mb-2">?? Estado Operativo</div>
          <div className="space-y-1 text-sm">
       <div className="flex justify-between">
 <span>Cajeros activos:</span>
      <span className="font-semibold">
       {formatNumber(estadoOperativo.cajerosActivos)}
           </span>
        </div>
     <div className="flex justify-between">
     <span>Jugadores online:</span>
          <span className="font-semibold">
  {formatNumber(estadoOperativo.jugadoresOnline)}
        </span>
</div>
        <div className="flex justify-between">
           <span>Float total:</span>
      <span className="font-semibold">
       {formatCurrency(estadoOperativo.floatTotal)}
              </span>
  </div>
  <div className="flex justify-between">
   <span>Trans. pendientes:</span>
              <span className="font-semibold">
     {formatNumber(estadoOperativo.transaccionesPendientes)}
</span>
        </div>
     </div>
        </div>
      </div>
    </div>
  );
}
```

### 3. Utils - Formatters

```typescript
// utils/formatters.ts

export function formatCurrency(amount: number): string {
  return new Intl.NumberFormat('es-AR', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  }).format(amount);
}

export function formatPercent(value: number): string {
  return `${value.toFixed(2)}%`;
}

export function formatNumber(value: number): string {
  return new Intl.NumberFormat('es-AR').format(value);
}

export function formatCompact(amount: number): string {
  if (amount >= 1_000_000) {
    return `$${(amount / 1_000_000).toFixed(1)}M`;
  }
  if (amount >= 1_000) {
    return `$${(amount / 1_000).toFixed(1)}K`;
  }
  return formatCurrency(amount);
}
```

### 4. Página Principal del Dashboard

```typescript
// pages/DashboardPage.tsx
import { useState } from 'react';
import { useDashboard } from '../hooks/useDashboard';
import { DashboardHeader } from '../components/DashboardHeader';
import { FichasCard } from '../components/FichasCard';
import { CasinoCard } from '../components/CasinoCard';
import { UsuariosCard } from '../components/UsuariosCard';
import { AlertasCard } from '../components/AlertasCard';

export function DashboardPage() {
  const [scope, setScope] = useState<'DIRECT' | 'TREE' | 'GLOBAL'>('TREE');
  const [from, setFrom] = useState<Date>();
  const [to, setTo] = useState<Date>();
  const [autoRefresh, setAutoRefresh] = useState(false);
  
  const { data, isLoading, error, dataUpdatedAt } = useDashboard({
    scope,
    from,
    to,
    autoRefresh,
  });
  
  const handlePeriodChange = (newFrom: Date, newTo: Date) => {
    setFrom(newFrom);
    setTo(newTo);
  };
  
  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-screen">
    <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-blue-500" />
      </div>
    );
  }
  
  if (error) {
    return (
   <div className="flex items-center justify-center h-screen">
   <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded">
       <p className="font-bold">Error al cargar el dashboard</p>
          <p>{error.message}</p>
        </div>
      </div>
    );
  }
  
  if (!data) return null;
  
  return (
    <div className="min-h-screen bg-gray-100 p-6">
      <DashboardHeader
        scope={scope}
     onScopeChange={setScope}
        onPeriodChange={handlePeriodChange}
        autoRefresh={autoRefresh}
        onAutoRefreshChange={setAutoRefresh}
        lastUpdate={new Date(dataUpdatedAt)}
      />
      
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <FichasCard data={data.finanzas} />
        <CasinoCard data={data.casino} />
    <UsuariosCard data={data.usuarios} />
        <AlertasCard data={data.alertas} />
      </div>
    </div>
  );
}
```

---

## ?? Tareas de Implementación

### Fase 1: Setup y Estructura (Prioridad Alta ?)

1. **Crear tipos TypeScript**
   - [ ] `types/dashboard.ts` con todas las interfaces
   - [ ] `types/api.ts` para respuestas de API

2. **Crear hook personalizado**
   - [ ] `hooks/useDashboard.ts`
   - [ ] Integrar React Query
   - [ ] Manejar errores y estados de carga

3. **Crear utils**
   - [ ] `utils/formatters.ts` (currency, percent, number)
 - [ ] `utils/dates.ts` (manejo de fechas)

### Fase 2: Componentes Básicos (Prioridad Alta ?)

4. **Crear componentes de cards**
   - [ ] `FichasCard.tsx`
   - [ ] `CasinoCard.tsx`
   - [ ] `UsuariosCard.tsx`
   - [ ] `AlertasCard.tsx`

5. **Crear DashboardHeader**
   - [ ] Selectores de scope
   - [ ] Botones de período quick
   - [ ] Auto-refresh toggle

### Fase 3: Funcionalidad Avanzada (Prioridad Media ??)

6. **DateRangePicker**
 - [ ] Componente de selección de rango personalizado
   - [ ] Validaciones (from < to)

7. **Loading States**
   - [ ] Skeleton loaders para cada card
   - [ ] Spinner global

8. **Error Handling**
   - [ ] Toast notifications para errores
   - [ ] Retry automático
   - [ ] Fallback UI

### Fase 4: Extras Opcionales (Prioridad Baja ??)

9. **Gráficos**
   - [ ] Gráfico de evolución de balance
   - [ ] Gráfico de breakdown (pie chart)
   - [ ] Gráfico de comisiones

10. **Exportación**
    - [ ] Botón "Exportar a PDF"
    - [ ] Botón "Exportar a Excel"

11. **Comparaciones**
    - [ ] Mostrar comparación con período anterior
    - [ ] Indicadores de tendencia (? ?)

---

## ??? Stack Tecnológico Recomendado

### Core
- **Framework**: React 18+ con TypeScript
- **Build**: Vite o Next.js
- **Styling**: Tailwind CSS

### Data Fetching
- **React Query** (`@tanstack/react-query`) para:
  - Caché automático
  - Auto-refresh
  - Retry logic
  - Loading/Error states

### UI Components (Opcional)
- **Headless UI** o **Radix UI** para componentes accesibles
- **Recharts** o **Chart.js** para gráficos
- **date-fns** o **dayjs** para manejo de fechas

---

## ? Validaciones y Errores

### Validaciones de Scope

```typescript
// Validar que el usuario tenga permisos para el scope
if (scope === 'GLOBAL' && userRole !== 'SUPER_ADMIN') {
  return (
    <div className="alert alert-error">
      Solo SUPER_ADMIN puede usar scope GLOBAL
    </div>
  );
}
```

### Manejo de Errores HTTP

```typescript
// En useDashboard hook
if (res.status === 401) {
  // Redirigir a login
  window.location.href = '/login';
  throw new Error('Unauthorized');
}

if (res.status === 403) {
  // Mostrar error de permisos
  throw new Error('No tienes permisos para este scope');
}

if (res.status === 404) {
  throw new Error('Brand no encontrado');
}
```

### Validación de Fechas

```typescript
// Validar que from < to
if (from && to && from >= to) {
  setError('La fecha de inicio debe ser menor a la fecha de fin');
  return;
}

// Validar que no sea futuro
if (to && to > new Date()) {
  setError('No puedes seleccionar fechas futuras');
  return;
}
```

---

## ?? Uso Final

### Flujo de Usuario

1. **Usuario ingresa al dashboard**
   - Se carga con scope `TREE` y período `HOY` por default
   - Muestra los 4 cards con datos actuales

2. **Usuario cambia scope a GLOBAL**
   - Si es SUPER_ADMIN: recarga con datos globales
   - Si no: muestra error de permisos

3. **Usuario selecciona período "Este mes"**
   - Recalcula desde día 1 del mes hasta hoy
   - Actualiza los 4 cards

4. **Usuario activa auto-refresh**
   - Cada 30 segundos consulta la API
   - Actualiza indicador "Última actualización"

5. **Usuario hace clic en "Ver reporte mensual"**
   - Navega a vista detallada de reportes

---

## ?? Despliegue

### Variables de Entorno

```env
VITE_API_BASE_URL=https://api.casino.com
VITE_AUTO_REFRESH_INTERVAL=30000
VITE_DEFAULT_SCOPE=TREE
```

### Build para Producción

```bash
# Instalar dependencias
npm install

# Build
npm run build

# Preview
npm run preview
```

---

## ?? Recursos Adicionales

- **Documentación API Backend**: `docs/FIX-DASHBOARD-BALANCE-COMMISSIONS.md`
- **Jerarquía de Usuarios**: `docs/FIX-HIERARCHY-AUTO-CREATION.md`
- **React Query Docs**: https://tanstack.com/query/latest
- **Tailwind CSS Docs**: https://tailwindcss.com/docs

---

**Última actualización**: 2025-01-23  
**Versión**: 1.0  
**Autor**: Backend Team

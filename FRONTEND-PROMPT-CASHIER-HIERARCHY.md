# ?? PROMPT PARA EQUIPO FRONTEND - Sistema de Jerarquía de Cashiers

## ?? RESUMEN EJECUTIVO

Se ha implementado un **sistema de jerarquía de cashiers con estructura de árbol N-ario** en el backend. Los cashiers pueden crear otros cashiers subordinados con comisiones configurables, formando una red de múltiples niveles.

**Tu tarea**: Implementar la interfaz de usuario para:
1. Crear cashiers subordinados
2. Visualizar la jerarquía en forma de árbol
3. Gestionar permisos según el rol del usuario autenticado

---

## ?? COMPONENTES A IMPLEMENTAR

### 1. **Formulario de Creación de Cashier Subordinado**

**Ubicación sugerida**: `components/admin/CreateSubordinateCashier.tsx`

**Campos del formulario**:
- `username` (string, required, 3-50 chars)
- `password` (string, required, min 8 chars)
- `commissionRate` (number, 0-100, default 0) - Porcentaje de comisión

**Endpoint**:
```
POST /api/v1/admin/users
Content-Type: application/json
Authorization: Bearer {token}
```

**Request Body**:
```typescript
{
  username: string;
  password: string;
  role: "CASHIER";  // Siempre CASHIER
  operatorId: string;  // ID del operador del usuario actual
  parentCashierId: string;  // ID del usuario actual (cashier padre)
  commissionRate: number;  // 0-100
}
```

**Comportamiento**:
- Solo visible para usuarios con rol `CASHIER` o superior
- El campo `parentCashierId` debe ser el ID del usuario autenticado
- Validar que `commissionRate` esté entre 0 y 100
- Mostrar mensajes de error claros (username duplicado, validaciones, etc.)

---

### 2. **Visualizador de Árbol Jerárquico**

**Ubicación sugerida**: `components/admin/CashierHierarchyTree.tsx`

**Endpoint**:
```
GET /api/v1/admin/users/{userId}/hierarchy
Authorization: Bearer {token}
```

**Response Type**:
```typescript
interface CashierNode {
  id: string;
  username: string;
  role: "CASHIER" | "OPERATOR_ADMIN" | "SUPER_ADMIN";
  status: "ACTIVE" | "INACTIVE";
  parentCashierId: string | null;
  commissionRate: number;
  createdAt: string;
  subordinates: CashierNode[];  // Recursivo
}
```

**Visualización sugerida**:
- Árbol expandible/colapsable
- Mostrar username, comisión y cantidad de subordinados
- Color diferente para el nodo raíz
- Iconos para indicar niveles
- Resaltar el nodo actual (usuario autenticado)

**Librerías recomendadas**:
- `react-d3-tree` - Para visualización de árbol
- `recharts` - Si quieres gráficos adicionales
- O CSS puro con indentación y bordes

**Ejemplo visual**:
```
?? Mi Jerarquía de Cashiers
?? ?? root_cashier (yo) - 0% comisión
?  ?? ?? cashier_sub1 - 10% comisión
?  ?  ?? ?? cashier_sub1_1 - 5% comisión
?  ?? ?? cashier_sub2 - 15% comisión
```

---

### 3. **Tabla de Subordinados Directos**

**Ubicación sugerida**: `components/admin/SubordinatesList.tsx`

**Endpoint**:
```
GET /api/v1/admin/users?parentCashierId={currentUserId}&role=CASHIER&page=1&pageSize=20
Authorization: Bearer {token}
```

**Response Type**:
```typescript
interface SubordinatesResponse {
  users: Array<{
    id: string;
    username: string;
    role: string;
    status: "ACTIVE" | "INACTIVE";
    operatorId: string;
    operatorName: string;
    parentCashierId: string;
    parentCashierUsername: string;
    commissionRate: number;
    subordinatesCount: number;  // Cuántos subordinados tiene este cashier
    createdAt: string;
    lastLoginAt: string | null;
  }>;
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
```

**Columnas de la tabla**:
- Username
- Status (badge con color)
- Comisión (%)
- Subordinados (cantidad)
- Fecha de creación
- Último login
- Acciones (ver detalles, ver jerarquía)

**Comportamiento**:
- Paginación
- Filtro por status (ACTIVE/INACTIVE)
- Click en fila para ver detalles
- Botón "Ver Jerarquía" para ver el árbol de ese subordinado

---

### 4. **Dashboard de Cashier**

**Ubicación sugerida**: `pages/admin/CashierDashboard.tsx`

**Métricas a mostrar**:
1. **Mis subordinados directos** (count)
2. **Total de subordinados en mi red** (recursivo)
3. **Comisiones totales acumuladas** (si tienes endpoint de comisiones)
4. **Players asignados a mí y mi red**

**Secciones**:
```
???????????????????????????????????????
?  ?? Mi Dashboard                    ?
???????????????????????????????????????
?  ?? Subordinados directos: 5        ?
?  ?? Total en mi red: 15             ?
?  ?? Comisión promedio: 12.5%        ?
?  ?? Players en mi red: 150          ?
???????????????????????????????????????

???????????????????????????????????????
?  ? Crear Nuevo Subordinado         ?
?     [Formulario inline]             ?
???????????????????????????????????????

???????????????????????????????????????
?  ?? Mis Subordinados Directos       ?
?     [Tabla con lista]               ?
???????????????????????????????????????

???????????????????????????????????????
?  ?? Visualizar Mi Jerarquía         ?
?     [Botón para abrir árbol]        ?
???????????????????????????????????????
```

---

## ?? PERMISOS Y AUTENTICACIÓN

### Headers Requeridos
```typescript
const headers = {
  'Content-Type': 'application/json',
  'Authorization': `Bearer ${accessToken}`
};

// O usar cookies automáticamente
fetch(url, {
  credentials: 'include',  // Incluye cookie bk.token
  headers: { 'Content-Type': 'application/json' }
});
```

### Roles y Permisos

| Rol | Puede Crear | Puede Ver | Puede Editar | Puede Eliminar |
|-----|-------------|-----------|--------------|----------------|
| **SUPER_ADMIN** | Cualquier usuario | Todos | Todos | Todos |
| **OPERATOR_ADMIN** | OPERATOR_ADMIN, CASHIER raíz | Su operador | Su operador | Su operador (sin subordinados) |
| **CASHIER** | CASHIER subordinado | Su jerarquía | ? No | ? No |

### Validación en Frontend
```typescript
// Obtener rol del usuario del contexto/state
const userRole = useAuth().user.role;

// Condicional de renderizado
{userRole === 'CASHIER' && (
  <CreateSubordinateButton />
)}

{['OPERATOR_ADMIN', 'SUPER_ADMIN'].includes(userRole) && (
  <CreateRootCashierButton />
)}
```

---

## ??? CÓDIGO DE EJEMPLO

### Hook Personalizado - useCashierHierarchy
```typescript
// hooks/useCashierHierarchy.ts
import { useState, useEffect } from 'react';

interface CashierNode {
  id: string;
  username: string;
  role: string;
  status: string;
  parentCashierId: string | null;
  commissionRate: number;
  createdAt: string;
  subordinates: CashierNode[];
}

export const useCashierHierarchy = (userId: string) => {
  const [hierarchy, setHierarchy] = useState<CashierNode | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchHierarchy = async () => {
      try {
        const response = await fetch(
          `/api/v1/admin/users/${userId}/hierarchy`,
          {
            headers: {
              'Authorization': `Bearer ${getAccessToken()}`
            },
            credentials: 'include'
          }
        );

        if (!response.ok) {
          throw new Error('Failed to fetch hierarchy');
        }

        const data = await response.json();
        setHierarchy(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Unknown error');
      } finally {
        setLoading(false);
      }
    };

    fetchHierarchy();
  }, [userId]);

  return { hierarchy, loading, error };
};
```

### Componente de Formulario
```typescript
// components/admin/CreateSubordinateForm.tsx
import { useState } from 'react';
import { useAuth } from '@/hooks/useAuth';

interface FormData {
  username: string;
  password: string;
  commissionRate: number;
}

export const CreateSubordinateForm = ({ onSuccess }: { onSuccess: () => void }) => {
  const { user } = useAuth();
  const [formData, setFormData] = useState<FormData>({
    username: '',
    password: '',
    commissionRate: 0
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/v1/admin/users', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        credentials: 'include',
        body: JSON.stringify({
          username: formData.username,
          password: formData.password,
          role: 'CASHIER',
          operatorId: user.operatorId,
          parentCashierId: user.id,  // ID del usuario actual
          commissionRate: formData.commissionRate
        })
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.detail || 'Failed to create cashier');
      }

      const newCashier = await response.json();
      onSuccess();
      alert(`Cashier "${newCashier.username}" creado exitosamente!`);
      
      // Reset form
      setFormData({ username: '', password: '', commissionRate: 0 });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error creating cashier');
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div>
        <label htmlFor="username" className="block text-sm font-medium">
          Username
        </label>
        <input
          id="username"
          type="text"
          value={formData.username}
          onChange={(e) => setFormData({ ...formData, username: e.target.value })}
          className="mt-1 block w-full rounded-md border-gray-300 shadow-sm"
          required
          minLength={3}
          maxLength={50}
          pattern="[a-zA-Z0-9_.-]+"
        />
      </div>

      <div>
        <label htmlFor="password" className="block text-sm font-medium">
          Password
        </label>
        <input
          id="password"
          type="password"
          value={formData.password}
          onChange={(e) => setFormData({ ...formData, password: e.target.value })}
          className="mt-1 block w-full rounded-md border-gray-300 shadow-sm"
          required
          minLength={8}
        />
      </div>

      <div>
        <label htmlFor="commissionRate" className="block text-sm font-medium">
          Comisión (%) - Opcional
        </label>
        <input
          id="commissionRate"
          type="number"
          value={formData.commissionRate}
          onChange={(e) => setFormData({ ...formData, commissionRate: parseFloat(e.target.value) })}
          className="mt-1 block w-full rounded-md border-gray-300 shadow-sm"
          min={0}
          max={100}
          step={0.1}
        />
        <p className="mt-1 text-sm text-gray-500">
          Porcentaje de comisión que recibirás de las operaciones de este subordinado
        </p>
      </div>

      {error && (
        <div className="rounded-md bg-red-50 p-4">
          <p className="text-sm text-red-800">{error}</p>
        </div>
      )}

      <button
        type="submit"
        disabled={loading}
        className="w-full px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50"
      >
        {loading ? 'Creando...' : 'Crear Cashier Subordinado'}
      </button>
    </form>
  );
};
```

### Componente de Árbol (Simple)
```typescript
// components/admin/HierarchyTreeView.tsx
import { useCashierHierarchy } from '@/hooks/useCashierHierarchy';

interface TreeNodeProps {
  node: CashierNode;
  level: number;
}

const TreeNode = ({ node, level }: TreeNodeProps) => {
  const [expanded, setExpanded] = useState(true);

  return (
    <div style={{ marginLeft: level * 24 }}>
      <div className="flex items-center gap-2 p-2 border rounded mb-2 bg-white shadow-sm">
        {node.subordinates.length > 0 && (
          <button
            onClick={() => setExpanded(!expanded)}
            className="text-gray-500 hover:text-gray-700"
          >
            {expanded ? '?' : '?'}
          </button>
        )}
        
        <div className="flex-1">
          <span className="font-semibold">{node.username}</span>
          <span className="ml-2 text-sm text-gray-500">({node.role})</span>
          {node.commissionRate > 0 && (
            <span className="ml-2 text-sm text-green-600">
              ?? {node.commissionRate}%
            </span>
          )}
          <div className="text-xs text-gray-400">
            Subordinados: {node.subordinates.length}
          </div>
        </div>
      </div>

      {expanded && node.subordinates.map(child => (
        <TreeNode key={child.id} node={child} level={level + 1} />
      ))}
    </div>
  );
};

export const HierarchyTreeView = ({ userId }: { userId: string }) => {
  const { hierarchy, loading, error } = useCashierHierarchy(userId);

  if (loading) return <div>Cargando jerarquía...</div>;
  if (error) return <div>Error: {error}</div>;
  if (!hierarchy) return <div>No se encontró jerarquía</div>;

  return (
    <div className="bg-gray-50 p-4 rounded-lg">
      <h3 className="text-lg font-bold mb-4">?? Jerarquía de Cashiers</h3>
      <TreeNode node={hierarchy} level={0} />
    </div>
  );
};
```

---

## ?? CASOS DE PRUEBA

### Test 1: Crear subordinado como CASHIER
```
1. Login como CASHIER
2. Ir a dashboard
3. Click en "Crear Subordinado"
4. Llenar form:
   - username: "test_sub1"
   - password: "test12345"
   - commissionRate: 10
5. Submit
6. Verificar que aparece en la lista de subordinados
```

### Test 2: Ver jerarquía completa
```
1. Login como CASHIER con subordinados
2. Click en "Ver Mi Jerarquía"
3. Verificar que se muestra el árbol completo
4. Verificar que se muestran las comisiones
5. Verificar que se pueden expandir/colapsar nodos
```

### Test 3: Permisos - CASHIER no puede ver otros usuarios
```
1. Login como CASHIER
2. Intentar acceder a GET /api/v1/admin/users (sin parentCashierId)
3. Verificar que solo ve sus subordinados
4. Intentar ver jerarquía de otro cashier
5. Verificar que recibe 403 Forbidden
```

---

## ?? ENDPOINTS RESUMIDOS

| Método | Endpoint | Descripción | Rol Mínimo |
|--------|----------|-------------|------------|
| `POST` | `/api/v1/admin/users` | Crear subordinado | CASHIER |
| `GET` | `/api/v1/admin/users?parentCashierId={id}` | Listar subordinados | CASHIER |
| `GET` | `/api/v1/admin/users/{id}/hierarchy` | Ver árbol completo | CASHIER |
| `GET` | `/api/v1/admin/users/{id}` | Ver detalle de usuario | CASHIER |
| `PATCH` | `/api/v1/admin/users/{id}` | Actualizar usuario | OPERATOR_ADMIN |
| `DELETE` | `/api/v1/admin/users/{id}` | Eliminar usuario | OPERATOR_ADMIN |

---

## ?? PRIORIDADES DE IMPLEMENTACIÓN

### Fase 1 (MVP) - ALTA PRIORIDAD ???
1. ? Formulario de creación de subordinado
2. ? Lista simple de subordinados directos (tabla)
3. ? Validaciones básicas de formulario
4. ? Manejo de errores y mensajes

### Fase 2 (Visualización) - MEDIA PRIORIDAD ??
1. ? Visualizador de árbol jerárquico (simple con CSS)
2. ? Dashboard con métricas básicas
3. ? Filtros en lista de subordinados

### Fase 3 (Avanzado) - BAJA PRIORIDAD ?
1. Árbol interactivo con D3.js o similar
2. Gráficos de comisiones
3. Exportar jerarquía (PDF/Excel)
4. Búsqueda en árbol

---

## ? PREGUNTAS FRECUENTES

### ¿Cuántos niveles puede tener el árbol?
**R:** Ilimitados. El backend soporta recursión infinita.

### ¿Qué pasa si elimino un cashier con subordinados?
**R:** El backend lo bloquea. Primero debes eliminar los subordinados.

### ¿Los cashiers pueden crear jugadores?
**R:** Sí, mediante el endpoint existente `/api/v1/admin/players`. Ese endpoint ya existe y funciona.

### ¿Cómo se calculan las comisiones?
**R:** El `commissionRate` es solo un porcentaje almacenado. El cálculo real de comisiones debe implementarse en el módulo de transacciones/reportes (fuera de scope actual).

### ¿Puedo cambiar el parent de un cashier?
**R:** No directamente en el endpoint de update. Si necesitas esta funcionalidad, solicítala al backend.

---

## ?? CONTACTO Y SOPORTE

Si tienes dudas sobre:
- Endpoints o respuestas: Revisar `CASHIER-HIERARCHY-FRONTEND-GUIDE.md`
- Permisos y validaciones: Revisar `CASHIER-HIERARCHY-CHANGES-SUMMARY.md`
- Errores 4xx/5xx: Revisar sección de manejo de errores en la guía

**Backend está listo y probado ?**

¡Buena suerte con la implementación! ??

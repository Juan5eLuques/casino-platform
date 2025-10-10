# ?? PROMPT PARA EQUIPO FRONTEND - Sistema de Jerarqu�a de Cashiers

## ?? RESUMEN EJECUTIVO

Se ha implementado un **sistema de jerarqu�a de cashiers con estructura de �rbol N-ario** en el backend. Los cashiers pueden crear otros cashiers subordinados con comisiones configurables, formando una red de m�ltiples niveles.

**Tu tarea**: Implementar la interfaz de usuario para:
1. Crear cashiers subordinados
2. Visualizar la jerarqu�a en forma de �rbol
3. Gestionar permisos seg�n el rol del usuario autenticado

---

## ?? COMPONENTES A IMPLEMENTAR

### 1. **Formulario de Creaci�n de Cashier Subordinado**

**Ubicaci�n sugerida**: `components/admin/CreateSubordinateCashier.tsx`

**Campos del formulario**:
- `username` (string, required, 3-50 chars)
- `password` (string, required, min 8 chars)
- `commissionRate` (number, 0-100, default 0) - Porcentaje de comisi�n

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
- Validar que `commissionRate` est� entre 0 y 100
- Mostrar mensajes de error claros (username duplicado, validaciones, etc.)

---

### 2. **Visualizador de �rbol Jer�rquico**

**Ubicaci�n sugerida**: `components/admin/CashierHierarchyTree.tsx`

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

**Visualizaci�n sugerida**:
- �rbol expandible/colapsable
- Mostrar username, comisi�n y cantidad de subordinados
- Color diferente para el nodo ra�z
- Iconos para indicar niveles
- Resaltar el nodo actual (usuario autenticado)

**Librer�as recomendadas**:
- `react-d3-tree` - Para visualizaci�n de �rbol
- `recharts` - Si quieres gr�ficos adicionales
- O CSS puro con indentaci�n y bordes

**Ejemplo visual**:
```
?? Mi Jerarqu�a de Cashiers
?? ?? root_cashier (yo) - 0% comisi�n
?  ?? ?? cashier_sub1 - 10% comisi�n
?  ?  ?? ?? cashier_sub1_1 - 5% comisi�n
?  ?? ?? cashier_sub2 - 15% comisi�n
```

---

### 3. **Tabla de Subordinados Directos**

**Ubicaci�n sugerida**: `components/admin/SubordinatesList.tsx`

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
    subordinatesCount: number;  // Cu�ntos subordinados tiene este cashier
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
- Comisi�n (%)
- Subordinados (cantidad)
- Fecha de creaci�n
- �ltimo login
- Acciones (ver detalles, ver jerarqu�a)

**Comportamiento**:
- Paginaci�n
- Filtro por status (ACTIVE/INACTIVE)
- Click en fila para ver detalles
- Bot�n "Ver Jerarqu�a" para ver el �rbol de ese subordinado

---

### 4. **Dashboard de Cashier**

**Ubicaci�n sugerida**: `pages/admin/CashierDashboard.tsx`

**M�tricas a mostrar**:
1. **Mis subordinados directos** (count)
2. **Total de subordinados en mi red** (recursivo)
3. **Comisiones totales acumuladas** (si tienes endpoint de comisiones)
4. **Players asignados a m� y mi red**

**Secciones**:
```
???????????????????????????????????????
?  ?? Mi Dashboard                    ?
???????????????????????????????????????
?  ?? Subordinados directos: 5        ?
?  ?? Total en mi red: 15             ?
?  ?? Comisi�n promedio: 12.5%        ?
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
?  ?? Visualizar Mi Jerarqu�a         ?
?     [Bot�n para abrir �rbol]        ?
???????????????????????????????????????
```

---

## ?? PERMISOS Y AUTENTICACI�N

### Headers Requeridos
```typescript
const headers = {
  'Content-Type': 'application/json',
  'Authorization': `Bearer ${accessToken}`
};

// O usar cookies autom�ticamente
fetch(url, {
  credentials: 'include',  // Incluye cookie bk.token
  headers: { 'Content-Type': 'application/json' }
});
```

### Roles y Permisos

| Rol | Puede Crear | Puede Ver | Puede Editar | Puede Eliminar |
|-----|-------------|-----------|--------------|----------------|
| **SUPER_ADMIN** | Cualquier usuario | Todos | Todos | Todos |
| **OPERATOR_ADMIN** | OPERATOR_ADMIN, CASHIER ra�z | Su operador | Su operador | Su operador (sin subordinados) |
| **CASHIER** | CASHIER subordinado | Su jerarqu�a | ? No | ? No |

### Validaci�n en Frontend
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

## ??? C�DIGO DE EJEMPLO

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
          Comisi�n (%) - Opcional
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
          Porcentaje de comisi�n que recibir�s de las operaciones de este subordinado
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

### Componente de �rbol (Simple)
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

  if (loading) return <div>Cargando jerarqu�a...</div>;
  if (error) return <div>Error: {error}</div>;
  if (!hierarchy) return <div>No se encontr� jerarqu�a</div>;

  return (
    <div className="bg-gray-50 p-4 rounded-lg">
      <h3 className="text-lg font-bold mb-4">?? Jerarqu�a de Cashiers</h3>
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

### Test 2: Ver jerarqu�a completa
```
1. Login como CASHIER con subordinados
2. Click en "Ver Mi Jerarqu�a"
3. Verificar que se muestra el �rbol completo
4. Verificar que se muestran las comisiones
5. Verificar que se pueden expandir/colapsar nodos
```

### Test 3: Permisos - CASHIER no puede ver otros usuarios
```
1. Login como CASHIER
2. Intentar acceder a GET /api/v1/admin/users (sin parentCashierId)
3. Verificar que solo ve sus subordinados
4. Intentar ver jerarqu�a de otro cashier
5. Verificar que recibe 403 Forbidden
```

---

## ?? ENDPOINTS RESUMIDOS

| M�todo | Endpoint | Descripci�n | Rol M�nimo |
|--------|----------|-------------|------------|
| `POST` | `/api/v1/admin/users` | Crear subordinado | CASHIER |
| `GET` | `/api/v1/admin/users?parentCashierId={id}` | Listar subordinados | CASHIER |
| `GET` | `/api/v1/admin/users/{id}/hierarchy` | Ver �rbol completo | CASHIER |
| `GET` | `/api/v1/admin/users/{id}` | Ver detalle de usuario | CASHIER |
| `PATCH` | `/api/v1/admin/users/{id}` | Actualizar usuario | OPERATOR_ADMIN |
| `DELETE` | `/api/v1/admin/users/{id}` | Eliminar usuario | OPERATOR_ADMIN |

---

## ?? PRIORIDADES DE IMPLEMENTACI�N

### Fase 1 (MVP) - ALTA PRIORIDAD ???
1. ? Formulario de creaci�n de subordinado
2. ? Lista simple de subordinados directos (tabla)
3. ? Validaciones b�sicas de formulario
4. ? Manejo de errores y mensajes

### Fase 2 (Visualizaci�n) - MEDIA PRIORIDAD ??
1. ? Visualizador de �rbol jer�rquico (simple con CSS)
2. ? Dashboard con m�tricas b�sicas
3. ? Filtros en lista de subordinados

### Fase 3 (Avanzado) - BAJA PRIORIDAD ?
1. �rbol interactivo con D3.js o similar
2. Gr�ficos de comisiones
3. Exportar jerarqu�a (PDF/Excel)
4. B�squeda en �rbol

---

## ? PREGUNTAS FRECUENTES

### �Cu�ntos niveles puede tener el �rbol?
**R:** Ilimitados. El backend soporta recursi�n infinita.

### �Qu� pasa si elimino un cashier con subordinados?
**R:** El backend lo bloquea. Primero debes eliminar los subordinados.

### �Los cashiers pueden crear jugadores?
**R:** S�, mediante el endpoint existente `/api/v1/admin/players`. Ese endpoint ya existe y funciona.

### �C�mo se calculan las comisiones?
**R:** El `commissionRate` es solo un porcentaje almacenado. El c�lculo real de comisiones debe implementarse en el m�dulo de transacciones/reportes (fuera de scope actual).

### �Puedo cambiar el parent de un cashier?
**R:** No directamente en el endpoint de update. Si necesitas esta funcionalidad, solic�tala al backend.

---

## ?? CONTACTO Y SOPORTE

Si tienes dudas sobre:
- Endpoints o respuestas: Revisar `CASHIER-HIERARCHY-FRONTEND-GUIDE.md`
- Permisos y validaciones: Revisar `CASHIER-HIERARCHY-CHANGES-SUMMARY.md`
- Errores 4xx/5xx: Revisar secci�n de manejo de errores en la gu�a

**Backend est� listo y probado ?**

�Buena suerte con la implementaci�n! ??

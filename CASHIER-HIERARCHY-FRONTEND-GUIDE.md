# Guía de Implementación Frontend - Jerarquía de Cashiers

## Resumen de la Funcionalidad

Se ha implementado un sistema de **jerarquía de cashiers en forma de árbol**, donde:

- **CASHIER** puede crear otros **CASHIER** subordinados (como nodos hijos en el árbol)
- Cada cashier subordinado puede tener una **comisión** (0-100%) que el cashier padre recibe sobre sus operaciones
- Los cashiers pueden crear **Players** (jugadores)
- La estructura es de **múltiples niveles** (árbol N-ario)
- **OPERATOR_ADMIN** puede crear cashiers raíz (sin parent)
- **SUPER_ADMIN** puede crear cualquier tipo de usuario

## Estructura de Datos

### BackofficeUser (con jerarquía)

```typescript
interface BackofficeUser {
  id: string;
  username: string;
  role: 'SUPER_ADMIN' | 'OPERATOR_ADMIN' | 'CASHIER';
  status: 'ACTIVE' | 'INACTIVE';
  operatorId: string | null;
  operatorName: string | null;
  parentCashierId: string | null;  // ID del cashier padre (null si es raíz)
  parentCashierUsername: string | null;
  commissionRate: number;  // 0-100
  subordinatesCount: number;  // Cantidad de cashiers subordinados
  createdAt: string;
  lastLoginAt: string | null;
}
```

### Jerarquía de Usuario (árbol)

```typescript
interface BackofficeUserHierarchy {
  id: string;
  username: string;
  role: 'SUPER_ADMIN' | 'OPERATOR_ADMIN' | 'CASHIER';
  status: 'ACTIVE' | 'INACTIVE';
  parentCashierId: string | null;
  commissionRate: number;
  createdAt: string;
  subordinates: BackofficeUserHierarchy[];  // Recursivo - árbol N-ario
}
```

## Endpoints Disponibles

### 1. Crear Usuario (con soporte de jerarquía)

**POST** `/api/v1/admin/users`

**Request Body:**
```json
{
  "username": "cashier_sub1",
  "password": "securePass123",
  "role": "CASHIER",
  "operatorId": "907d6f5a-475d-42b8-b040-1475e0ea334a",
  "parentCashierId": "parent-cashier-id-here",  // Opcional: ID del cashier padre
  "commissionRate": 15.5  // Opcional: 0-100, default 0
}
```

**Response (201):**
```json
{
  "id": "new-user-id",
  "username": "cashier_sub1",
  "role": "CASHIER",
  "status": "ACTIVE",
  "operatorId": "907d6f5a-475d-42b8-b040-1475e0ea334a",
  "operatorName": "My Operator",
  "parentCashierId": "parent-cashier-id-here",
  "parentCashierUsername": "parent_cashier",
  "commissionRate": 15.5,
  "subordinatesCount": 0,
  "createdAt": "2024-01-15T10:30:00Z",
  "lastLoginAt": null
}
```

**Reglas de Autorización:**
- **SUPER_ADMIN**: Puede crear cualquier usuario
- **OPERATOR_ADMIN**: Puede crear OPERATOR_ADMIN y CASHIER en su operador
- **CASHIER**: Solo puede crear CASHIER subordinados bajo sí mismo (debe especificar su propio ID como `parentCashierId`)

### 2. Listar Usuarios (con filtro de jerarquía)

**GET** `/api/v1/admin/users?page=1&pageSize=20&role=CASHIER&parentCashierId={id}`

**Query Parameters:**
- `username`: Filtrar por nombre de usuario (contiene)
- `role`: Filtrar por rol (SUPER_ADMIN, OPERATOR_ADMIN, CASHIER)
- `status`: Filtrar por estado (ACTIVE, INACTIVE)
- `operatorId`: Filtrar por operador
- `parentCashierId`: **Filtrar por cashier padre** (obtener subordinados directos)
- `page`: Número de página (default: 1)
- `pageSize`: Tamaño de página (default: 20, max: 100)

**Response (200):**
```json
{
  "users": [
    {
      "id": "user-id-1",
      "username": "cashier1",
      "role": "CASHIER",
      "status": "ACTIVE",
      "operatorId": "operator-id",
      "operatorName": "My Operator",
      "parentCashierId": null,
      "parentCashierUsername": null,
      "commissionRate": 0,
      "subordinatesCount": 3,
      "createdAt": "2024-01-15T10:30:00Z",
      "lastLoginAt": "2024-01-20T14:20:00Z"
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

**Reglas de Autorización:**
- **SUPER_ADMIN**: Ve todos los usuarios
- **OPERATOR_ADMIN**: Ve usuarios de su operador
- **CASHIER**: Solo ve sus subordinados directos

### 3. Obtener Jerarquía Completa de un Usuario (árbol)

**GET** `/api/v1/admin/users/{userId}/hierarchy`

**Response (200):**
```json
{
  "id": "root-cashier-id",
  "username": "root_cashier",
  "role": "CASHIER",
  "status": "ACTIVE",
  "parentCashierId": null,
  "commissionRate": 0,
  "createdAt": "2024-01-15T10:30:00Z",
  "subordinates": [
    {
      "id": "sub1-id",
      "username": "cashier_sub1",
      "role": "CASHIER",
      "status": "ACTIVE",
      "parentCashierId": "root-cashier-id",
      "commissionRate": 10,
      "createdAt": "2024-01-16T11:00:00Z",
      "subordinates": [
        {
          "id": "sub1-sub1-id",
          "username": "cashier_sub1_sub1",
          "role": "CASHIER",
          "status": "ACTIVE",
          "parentCashierId": "sub1-id",
          "commissionRate": 5,
          "createdAt": "2024-01-17T09:00:00Z",
          "subordinates": []
        }
      ]
    },
    {
      "id": "sub2-id",
      "username": "cashier_sub2",
      "role": "CASHIER",
      "status": "ACTIVE",
      "parentCashierId": "root-cashier-id",
      "commissionRate": 15,
      "createdAt": "2024-01-16T12:00:00Z",
      "subordinates": []
    }
  ]
}
```

**Reglas de Autorización:**
- **SUPER_ADMIN** y **OPERATOR_ADMIN**: Pueden ver la jerarquía de cualquier usuario de su scope
- **CASHIER**: Solo puede ver su propia jerarquía

### 4. Obtener Usuario Individual

**GET** `/api/v1/admin/users/{userId}`

**Response (200):** (Mismo formato que en la lista)

### 5. Actualizar Usuario

**PATCH** `/api/v1/admin/users/{userId}`

**Request Body (todos opcionales):**
```json
{
  "username": "new_username",
  "password": "newPassword123",
  "role": "CASHIER",
  "status": "INACTIVE",
  "operatorId": "operator-id",
  "commissionRate": 20
}
```

**Reglas Importantes:**
- No se puede cambiar el rol de CASHIER a otro si tiene subordinados
- No se puede cambiar a SUPER_ADMIN si tiene operador asignado
- CASHIER no puede actualizar usuarios

### 6. Eliminar Usuario

**DELETE** `/api/v1/admin/users/{userId}`

**Response (200):**
```json
{
  "success": true,
  "message": "User deleted successfully"
}
```

**Reglas Importantes:**
- No se puede eliminar un cashier con subordinados (primero eliminar los subordinados)
- No se puede eliminar al propio usuario
- CASHIER no puede eliminar usuarios

## Ejemplos de Implementación Frontend

### Componente React - Crear Cashier Subordinado

```tsx
import { useState } from 'react';

interface CreateCashierForm {
  username: string;
  password: string;
  commissionRate: number;
}

const CreateSubordinateCashier = ({ 
  currentUserId, 
  operatorId 
}: { 
  currentUserId: string; 
  operatorId: string;
}) => {
  const [form, setForm] = useState<CreateCashierForm>({
    username: '',
    password: '',
    commissionRate: 0
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      const response = await fetch('/api/v1/admin/users', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${getToken()}`  // Tu función para obtener token
        },
        credentials: 'include',  // Para cookies
        body: JSON.stringify({
          username: form.username,
          password: form.password,
          role: 'CASHIER',
          operatorId: operatorId,
          parentCashierId: currentUserId,  // El cashier actual es el padre
          commissionRate: form.commissionRate
        })
      });

      if (response.ok) {
        const newUser = await response.json();
        alert(`Cashier creado: ${newUser.username}`);
        // Actualizar lista o árbol
      } else {
        const error = await response.json();
        alert(`Error: ${error.detail}`);
      }
    } catch (error) {
      console.error('Error creating cashier:', error);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <input
        type="text"
        placeholder="Username"
        value={form.username}
        onChange={(e) => setForm({ ...form, username: e.target.value })}
        required
      />
      <input
        type="password"
        placeholder="Password"
        value={form.password}
        onChange={(e) => setForm({ ...form, password: e.target.value })}
        required
      />
      <input
        type="number"
        placeholder="Commission Rate (0-100)"
        min="0"
        max="100"
        step="0.1"
        value={form.commissionRate}
        onChange={(e) => setForm({ ...form, commissionRate: parseFloat(e.target.value) })}
      />
      <button type="submit">Crear Cashier Subordinado</button>
    </form>
  );
};
```

### Componente React - Visualizar Árbol de Jerarquía

```tsx
import { useEffect, useState } from 'react';

interface HierarchyNode {
  id: string;
  username: string;
  role: string;
  status: string;
  parentCashierId: string | null;
  commissionRate: number;
  createdAt: string;
  subordinates: HierarchyNode[];
}

const CashierHierarchyTree = ({ rootUserId }: { rootUserId: string }) => {
  const [hierarchy, setHierarchy] = useState<HierarchyNode | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchHierarchy();
  }, [rootUserId]);

  const fetchHierarchy = async () => {
    try {
      const response = await fetch(
        `/api/v1/admin/users/${rootUserId}/hierarchy`,
        {
          headers: { 'Authorization': `Bearer ${getToken()}` },
          credentials: 'include'
        }
      );

      if (response.ok) {
        const data = await response.json();
        setHierarchy(data);
      }
    } catch (error) {
      console.error('Error fetching hierarchy:', error);
    } finally {
      setLoading(false);
    }
  };

  const renderNode = (node: HierarchyNode, level: number = 0) => (
    <div key={node.id} style={{ marginLeft: level * 20 }}>
      <div style={{ 
        padding: '10px', 
        border: '1px solid #ccc', 
        marginBottom: '5px',
        backgroundColor: level === 0 ? '#e3f2fd' : '#f5f5f5'
      }}>
        <strong>{node.username}</strong> ({node.role})
        {node.commissionRate > 0 && (
          <span style={{ color: 'green', marginLeft: '10px' }}>
            Comisión: {node.commissionRate}%
          </span>
        )}
        <div style={{ fontSize: '12px', color: '#666' }}>
          Status: {node.status} | Subordinados: {node.subordinates.length}
        </div>
      </div>
      {node.subordinates.map(sub => renderNode(sub, level + 1))}
    </div>
  );

  if (loading) return <div>Cargando jerarquía...</div>;
  if (!hierarchy) return <div>No se encontró la jerarquía</div>;

  return (
    <div>
      <h3>Jerarquía de Cashiers</h3>
      {renderNode(hierarchy)}
    </div>
  );
};
```

### Componente React - Lista de Subordinados Directos

```tsx
const SubordinatesList = ({ parentCashierId }: { parentCashierId: string }) => {
  const [subordinates, setSubordinates] = useState<BackofficeUser[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchSubordinates();
  }, [parentCashierId]);

  const fetchSubordinates = async () => {
    try {
      const response = await fetch(
        `/api/v1/admin/users?parentCashierId=${parentCashierId}&role=CASHIER&pageSize=100`,
        {
          headers: { 'Authorization': `Bearer ${getToken()}` },
          credentials: 'include'
        }
      );

      if (response.ok) {
        const data = await response.json();
        setSubordinates(data.users);
      }
    } catch (error) {
      console.error('Error fetching subordinates:', error);
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <div>Cargando subordinados...</div>;

  return (
    <div>
      <h4>Cashiers Subordinados ({subordinates.length})</h4>
      <table>
        <thead>
          <tr>
            <th>Username</th>
            <th>Status</th>
            <th>Comisión</th>
            <th>Sus Subordinados</th>
            <th>Creado</th>
          </tr>
        </thead>
        <tbody>
          {subordinates.map(sub => (
            <tr key={sub.id}>
              <td>{sub.username}</td>
              <td>{sub.status}</td>
              <td>{sub.commissionRate}%</td>
              <td>{sub.subordinatesCount}</td>
              <td>{new Date(sub.createdAt).toLocaleDateString()}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};
```

## Casos de Uso Comunes

### 1. OPERATOR_ADMIN crea cashier raíz

```bash
POST /api/v1/admin/users
{
  "username": "root_cashier",
  "password": "secure123",
  "role": "CASHIER",
  "operatorId": "operator-id",
  "parentCashierId": null,  # Sin padre = cashier raíz
  "commissionRate": 0
}
```

### 2. Cashier crea cashier subordinado con comisión

```bash
POST /api/v1/admin/users
{
  "username": "sub_cashier",
  "password": "secure123",
  "role": "CASHIER",
  "operatorId": "operator-id",
  "parentCashierId": "my-cashier-id",  # ID del cashier que hace la petición
  "commissionRate": 15.5  # Cashier padre recibe 15.5% de comisión
}
```

### 3. Ver mis subordinados (como CASHIER)

```bash
GET /api/v1/admin/users?parentCashierId=my-cashier-id&role=CASHIER
```

### 4. Ver toda mi jerarquía (como CASHIER)

```bash
GET /api/v1/admin/users/my-cashier-id/hierarchy
```

### 5. OPERATOR_ADMIN ve todos los cashiers de su operador

```bash
GET /api/v1/admin/users?role=CASHIER&operatorId=my-operator-id
```

## Validaciones y Restricciones

### Al crear usuarios:
- ? CASHIER solo puede crear CASHIER subordinados bajo sí mismo
- ? `parentCashierId` debe ser el ID del cashier autenticado (para CASHIER)
- ? `commissionRate` debe estar entre 0 y 100
- ? El parent cashier debe pertenecer al mismo operador
- ? Solo CASHIER puede tener `parentCashierId` y `commissionRate`

### Al actualizar usuarios:
- ? No se puede cambiar rol de CASHIER si tiene subordinados
- ? CASHIER no puede actualizar otros usuarios

### Al eliminar usuarios:
- ? No se puede eliminar cashier con subordinados (primero eliminar subordinados)
- ? No se puede eliminar propio usuario
- ? CASHIER no puede eliminar usuarios

## Flujo de Trabajo Recomendado

1. **OPERATOR_ADMIN** crea cashiers raíz (sin parent)
2. **Cashier raíz** crea cashiers subordinados (nivel 1)
3. **Cashier nivel 1** puede crear más subordinados (nivel 2)
4. Y así sucesivamente... (árbol N-ario ilimitado)

## Estructura de Árbol Visual

```
OPERATOR_ADMIN
    |
    ??? ROOT_CASHIER_1 (comisión: 0%)
    |    |
    |    ??? SUB_CASHIER_1_1 (comisión: 10%)
    |    |    ??? SUB_SUB_CASHIER_1_1_1 (comisión: 5%)
    |    |
    |    ??? SUB_CASHIER_1_2 (comisión: 15%)
    |
    ??? ROOT_CASHIER_2 (comisión: 0%)
         ??? SUB_CASHIER_2_1 (comisión: 20%)
```

## Headers Requeridos

```
Authorization: Bearer <jwt-token>
Content-Type: application/json
```

O usar cookies automáticamente con `credentials: 'include'`

## Manejo de Errores

```typescript
try {
  const response = await fetch('/api/v1/admin/users', { ... });
  
  if (!response.ok) {
    const error = await response.json();
    
    switch (response.status) {
      case 400:
        // Validación fallida
        console.error('Validation errors:', error.errors);
        break;
      case 403:
        // Sin permisos
        console.error('Access denied:', error.detail);
        break;
      case 409:
        // Conflicto (username existe, tiene subordinados, etc.)
        console.error('Conflict:', error.detail);
        break;
      default:
        console.error('Error:', error.detail);
    }
  }
} catch (error) {
  console.error('Network error:', error);
}
```

## Notas Importantes

1. **Autenticación**: Todos los endpoints requieren autenticación JWT (cookie `bk.token` o header `Authorization`)
2. **Enums**: Los valores de `role` y `status` deben ser strings en UPPER_CASE (ej: "CASHIER", "ACTIVE")
3. **Recursividad**: El endpoint `/hierarchy` devuelve toda la estructura recursiva (cuidado con árboles muy grandes)
4. **Paginación**: La lista de usuarios tiene paginación (max 100 por página)
5. **Scope**: Los cashiers solo pueden ver/gestionar su propia jerarquía

---

**¿Tienes alguna pregunta sobre la implementación?** ??

# User Tree Endpoint - Árbol Genealógico de Usuarios

## ?? Descripción General

El endpoint `/api/v1/admin/tree/{userId}` permite visualizar el árbol genealógico de usuarios, mostrando todos los usuarios que fueron creados por un usuario específico de forma jerárquica.

## ?? Caso de Uso

Este endpoint es útil para:
- Ver qué usuarios fueron creados por un SUPER_ADMIN, BRAND_ADMIN o CASHIER
- Visualizar la estructura jerárquica de creación de usuarios
- Entender la cadena de responsabilidad en la creación de usuarios
- Mostrar un árbol expandible en el frontend donde cada nodo puede tener hijos

## ?? Autenticación y Autorización

**Política requerida**: `AnyBackofficeUser` (SUPER_ADMIN, BRAND_ADMIN, CASHIER)

**Scoping**:
- **SUPER_ADMIN**: Puede ver el árbol de cualquier usuario
- **BRAND_ADMIN**: Solo puede ver árboles de usuarios de su brand
- **CASHIER**: Solo puede ver árboles de usuarios de su brand

## ?? Endpoint

### GET `/api/v1/admin/tree/{userId}`

Obtiene el árbol genealógico de un usuario específico.

#### Path Parameters

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `userId` | `Guid` | ? | ID del usuario raíz del árbol |

#### Query Parameters

| Parámetro | Tipo | Requerido | Default | Descripción |
|-----------|------|-----------|---------|-------------|
| `maxDepth` | `int` | ? | `1` | Profundidad máxima del árbol (1-10) |
| `includeInactive` | `bool` | ? | `false` | Incluir usuarios inactivos en el árbol |

#### Headers

```http
Authorization: Bearer <jwt_token>
```

## ?? Response

### Success Response (200 OK)

```json
{
  "rootUserId": "123e4567-e89b-12d3-a456-426614174000",
  "rootUsername": "admin_user",
  "rootUserType": "BACKOFFICE",
  "role": "BRAND_ADMIN",
  "tree": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "username": "admin_user",
    "userType": "BACKOFFICE",
    "role": "BRAND_ADMIN",
    "status": "ACTIVE",
    "createdAt": "2024-01-15T10:30:00Z",
    "balance": 5000.00,
    "commissionPercent": null,
    "hasChildren": true,
    "directChildrenCount": 5,
    "children": [
      {
        "id": "234e5678-e89b-12d3-a456-426614174001",
        "username": "cashier_001",
        "userType": "BACKOFFICE",
        "role": "CASHIER",
        "status": "ACTIVE",
        "createdAt": "2024-01-20T14:20:00Z",
        "balance": 1200.50,
        "commissionPercent": 5.00,
        "hasChildren": true,
        "directChildrenCount": 3,
        "children": null
      },
      {
        "id": "345e6789-e89b-12d3-a456-426614174002",
        "username": "player_001",
        "userType": "PLAYER",
        "role": null,
        "status": "ACTIVE",
        "createdAt": "2024-01-25T09:15:00Z",
        "balance": 250.75,
        "commissionPercent": null,
        "hasChildren": false,
        "directChildrenCount": 0,
        "children": null
      }
    ]
  }
}
```

### Response Schema

#### GetUserTreeResponse

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `rootUserId` | `Guid` | ID del usuario raíz |
| `rootUsername` | `string` | Username del usuario raíz |
| `rootUserType` | `string` | Tipo de usuario: "BACKOFFICE" o "PLAYER" |
| `role` | `string?` | Rol si es backoffice (null para players) |
| `tree` | `UserTreeNode` | Nodo raíz del árbol con sus hijos |

#### UserTreeNode

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `id` | `Guid` | ID del usuario |
| `username` | `string` | Username del usuario |
| `userType` | `string` | "BACKOFFICE" o "PLAYER" |
| `role` | `string?` | Rol (solo para backoffice) |
| `status` | `string` | Estado del usuario |
| `createdAt` | `DateTime` | Fecha de creación |
| `balance` | `decimal` | ?? Balance actual del usuario |
| `commissionPercent` | `decimal?` | ?? Comisión (solo para CASHIER con padre) |
| `hasChildren` | `bool` | ?? **TRUE** si tiene hijos (mostrar icono expandible) |
| `directChildrenCount` | `int` | Cantidad de hijos directos |
| `children` | `UserTreeNode[]?` | Array de hijos (null si no se cargaron) |

## ?? Uso en Frontend

### Ejemplo de Lógica para Árbol Expandible

```typescript
interface TreeNode {
  id: string;
  username: string;
  userType: string;
  role?: string;
  status: string;
  createdAt: string;
  balance: number; // ? Balance del usuario
  commissionPercent?: number; // ? Comisión (solo CASHIER con padre)
  hasChildren: boolean; // ? Usar para mostrar icono de expandir
  directChildrenCount: number;
  children?: TreeNode[];
}

function TreeNodeComponent({ node }: { node: TreeNode }) {
  const [expanded, setExpanded] = useState(false);
  const [children, setChildren] = useState(node.children);

  const loadChildren = async () => {
    if (!children && node.hasChildren) {
      // Cargar hijos con maxDepth=1 para este nodo específico
      const response = await api.get(`/api/v1/admin/tree/${node.id}?maxDepth=1`);
      setChildren(response.data.tree.children);
    }
    setExpanded(!expanded);
  };

  return (
    <div>
      <div className="tree-node">
        {/* Mostrar icono solo si hasChildren es true */}
        {node.hasChildren && (
          <button onClick={loadChildren}>
            {expanded ? '?' : '?'} ({node.directChildrenCount})
          </button>
        )}
        <span>
          {node.username} ({node.userType})
          {node.role && ` - ${node.role}`}
        </span>
        <span className="balance">
          ?? ${node.balance.toFixed(2)}
        </span>
        {node.commissionPercent && (
          <span className="commission">
            ?? {node.commissionPercent}% comisión
          </span>
        )}
      </div>
      
      {/* Renderizar hijos si están expandidos */}
      {expanded && children && (
        <div className="tree-children">
          {children.map(child => (
            <TreeNodeComponent key={child.id} node={child} />
          ))}
        </div>
      )}
    </div>
  );
}
```

## ?? Ejemplos de Request/Response

### Ejemplo 1: Ver solo hijos directos (maxDepth=1)

**Request:**
```http
GET /api/v1/admin/tree/123e4567-e89b-12d3-a456-426614174000?maxDepth=1
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response:**
```json
{
  "rootUserId": "123e4567-e89b-12d3-a456-426614174000",
  "rootUsername": "super_admin",
  "rootUserType": "BACKOFFICE",
  "role": "SUPER_ADMIN",
  "tree": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "username": "super_admin",
    "userType": "BACKOFFICE",
    "role": "SUPER_ADMIN",
    "status": "ACTIVE",
    "createdAt": "2024-01-01T00:00:00Z",
    "balance": 10000.00,
    "commissionPercent": null,
    "hasChildren": true,
    "directChildrenCount": 3,
    "children": [
      {
        "id": "234e5678-e89b-12d3-a456-426614174001",
        "username": "brand_admin_1",
        "userType": "BACKOFFICE",
        "role": "BRAND_ADMIN",
        "status": "ACTIVE",
        "createdAt": "2024-01-10T00:00:00Z",
        "balance": 3500.00,
        "commissionPercent": null,
        "hasChildren": true,
        "directChildrenCount": 5,
        "children": null
      }
    ]
  }
}
```

### Ejemplo 2: Ver árbol completo hasta nietos (maxDepth=2)

**Request:**
```http
GET /api/v1/admin/tree/123e4567-e89b-12d3-a456-426614174000?maxDepth=2
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response:**
```json
{
  "rootUserId": "123e4567-e89b-12d3-a456-426614174000",
  "rootUsername": "super_admin",
  "rootUserType": "BACKOFFICE",
  "role": "SUPER_ADMIN",
  "tree": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "username": "super_admin",
    "userType": "BACKOFFICE",
    "role": "SUPER_ADMIN",
    "status": "ACTIVE",
    "createdAt": "2024-01-01T00:00:00Z",
    "balance": 10000.00,
    "commissionPercent": null,
    "hasChildren": true,
    "directChildrenCount": 2,
    "children": [
      {
        "id": "234e5678-e89b-12d3-a456-426614174001",
        "username": "brand_admin_1",
        "userType": "BACKOFFICE",
        "role": "BRAND_ADMIN",
        "status": "ACTIVE",
        "createdAt": "2024-01-10T00:00:00Z",
        "balance": 3500.00,
        "commissionPercent": null,
        "hasChildren": true,
        "directChildrenCount": 2,
        "children": [
          {
            "id": "345e6789-e89b-12d3-a456-426614174002",
            "username": "cashier_1",
            "userType": "BACKOFFICE",
            "role": "CASHIER",
            "status": "ACTIVE",
            "createdAt": "2024-01-15T00:00:00Z",
            "balance": 800.00,
            "commissionPercent": 10.00,
            "hasChildren": true,
            "directChildrenCount": 10,
            "children": null
          },
          {
            "id": "456e7890-e89b-12d3-a456-426614174003",
            "username": "player_001",
            "userType": "PLAYER",
            "role": null,
            "status": "ACTIVE",
            "createdAt": "2024-01-20T00:00:00Z",
            "balance": 125.50,
            "commissionPercent": null,
            "hasChildren": false,
            "directChildrenCount": 0,
            "children": null
          }
        ]
      }
    ]
  }
}
```

### Ejemplo 3: Incluir usuarios inactivos

**Request:**
```http
GET /api/v1/admin/tree/123e4567-e89b-12d3-a456-426614174000?maxDepth=1&includeInactive=true
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## ? Error Responses

### 404 - User Not Found
```json
{
  "error": "user_not_found",
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "message": "User not found or access denied"
}
```

### 403 - Access Denied
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Access Denied",
  "status": 403,
  "detail": "Access denied: User not in your scope"
}
```

### 400 - Invalid Max Depth
```json
{
  "error": "invalid_max_depth",
  "message": "MaxDepth must be between 1 and 10"
}
```

## ?? Casos de Uso Específicos

### Caso 1: Árbol de SUPER_ADMIN
Un SUPER_ADMIN puede ver todo el árbol de usuarios que creó (BRAND_ADMINs, CASHIERs, PLAYERs).

### Caso 2: Árbol de BRAND_ADMIN
Un BRAND_ADMIN puede ver el árbol de CASHIERs y PLAYERs que creó dentro de su brand.

### Caso 3: Árbol de CASHIER
Un CASHIER puede ver el árbol de PLAYERs que creó.

### Caso 4: Árbol de PLAYER
Un PLAYER típicamente no tendrá hijos (`hasChildren: false`), pero el endpoint sigue funcionando.

## ?? Información Financiera en el Árbol

### Balance
- **Todos los usuarios** (BACKOFFICE y PLAYER) tienen un campo `balance` que muestra su saldo actual
- El balance proviene de `WalletBalance` (campo en ambas tablas: `BackofficeUsers` y `Players`)
- Útil para ver la distribución de fondos en la jerarquía

### Comisión (CommissionPercent)
- Solo aparece en usuarios **CASHIER** que tienen un `ParentCashierId` configurado
- Representa el porcentaje de comisión que se le asigna a ese cajero
- Para otros roles (SUPER_ADMIN, BRAND_ADMIN, PLAYER), el campo es `null`
- Ejemplo: Un cajero con `commissionPercent: 10.00` recibe el 10% de comisión

### Ejemplo de Estructura Financiera

```
SUPER_ADMIN ($10,000)
?? BRAND_ADMIN ($3,500)
?  ?? CASHIER ($800, 10% comisión) ? Tiene comisión porque tiene ParentCashierId
?  ?  ?? PLAYER ($100)
?  ?  ?? PLAYER ($250)
?  ?? CASHIER ($650, 8% comisión)
?     ?? PLAYER ($75)
?? BRAND_ADMIN ($2,200)
   ?? PLAYER ($500) ? Creado directamente por BRAND_ADMIN
```

## ?? Diseño de UI Recomendado

```
?? Árbol de Usuarios: super_admin
?? ?? super_admin (SUPER_ADMIN) - ?? $10,000.00 - 3 usuarios creados
   ?? ? ?? brand_admin_1 (BRAND_ADMIN) - ?? $3,500.00 - 5 usuarios [expandir]
   ?? ? ?? brand_admin_2 (BRAND_ADMIN) - ?? $2,200.00 - 2 usuarios [expandir]
   ?? ?? cashier_direct (CASHIER) - ?? $500.00 - ?? 15% comisión - Sin usuarios creados
```

Al hacer clic en "expandir":
```
?? Árbol de Usuarios: super_admin
?? ?? super_admin (SUPER_ADMIN) - ?? $10,000.00 - 3 usuarios creados
   ?? ? ?? brand_admin_1 (BRAND_ADMIN) - ?? $3,500.00 - 5 usuarios
   ?  ?? ? ?? cashier_1 (CASHIER) - ?? $800.00 - ?? 10% comisión - 10 players [expandir]
   ?  ?? ? ?? cashier_2 (CASHIER) - ?? $650.00 - ?? 8% comisión - 8 players [expandir]
   ?  ?? ?? player_001 (PLAYER) - ?? $125.50
   ?  ?? ?? player_002 (PLAYER) - ?? $89.25
   ?  ?? ?? player_003 (PLAYER) - ?? $450.00
   ?? ...
```

## ?? Performance

### Optimizaciones Implementadas

- **? Eliminación de N+1 Queries**: 
  - **Antes**: ~33+ queries para árbol de 10 usuarios (1 usuario + 3 queries × 10 hijos)
  - **Ahora**: Solo 2-4 queries totales (1 usuario raíz + carga batch de todos los niveles)
  
- **? Carga en Batch**: 
  - Todos los usuarios del árbol se cargan en 2 queries principales (BackofficeUsers + Players)
  - Los datos se cachean en memoria durante la construcción del árbol
  
- **? Sin Queries Recursivas**:
  - El árbol se construye desde el cache, sin consultas adicionales a la base de datos
  
- **? Lazy Loading Frontend**: 
  - Solo carga hijos cuando `maxDepth` lo permite
  - El frontend puede cargar nodos individuales bajo demanda
  
- **?? Performance Esperada**:
  - Árbol de 10 usuarios: **~50-200ms** (vs 12 segundos antes)
  - Árbol de 100 usuarios: **~200-500ms**
  - Árbol de 1000 usuarios: **~1-2 segundos**

### Logging de Performance

El servicio ahora registra automáticamente el tiempo de carga:
```
User tree loaded in 125ms
Loaded 8 backoffice users and 15 players for tree
```

### Recomendaciones

- **Paginación futura**: Considerar agregar paginación si un nodo tiene más de 100 hijos directos
- **Cache de Redis**: Para árboles muy grandes (>1000 usuarios), considerar cachear en Redis
- **Índices de BD**: Asegurar índices en `CreatedByUserId` para ambas tablas

## ?? Componente UI Completo con Balance y Comisión

```typescript
interface TreeNode {
  id: string;
  username: string;
  userType: string;
  role?: string;
  status: string;
  createdAt: string;
  balance: number;
  commissionPercent?: number;
  hasChildren: boolean;
  directChildrenCount: number;
  children?: TreeNode[];
}

function UserTreeNode({ node, depth = 0 }: { node: TreeNode; depth?: number }) {
  const [expanded, setExpanded] = useState(false);
  const [children, setChildren] = useState(node.children);
  const [loading, setLoading] = useState(false);

  const loadChildren = async () => {
    if (!children && node.hasChildren && !loading) {
      setLoading(true);
      try {
        const response = await api.get(`/api/v1/admin/tree/${node.id}?maxDepth=1`);
        setChildren(response.data.tree.children);
        setExpanded(true);
      } catch (error) {
        console.error('Error loading children:', error);
      } finally {
        setLoading(false);
      }
    } else {
      setExpanded(!expanded);
    }
  };

  const getIcon = () => {
    if (node.userType === 'PLAYER') return '??';
    if (node.role === 'SUPER_ADMIN') return '??';
    if (node.role === 'BRAND_ADMIN') return '?????';
    if (node.role === 'CASHIER') return '??';
    return '??';
  };

  const formatMoney = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  };

  return (
    <div className="tree-node-container" style={{ marginLeft: `${depth * 20}px` }}>
      <div className={`tree-node ${node.status.toLowerCase()}`}>
        {/* Expand/Collapse Button */}
        <div className="node-expand">
          {node.hasChildren ? (
            <button 
              onClick={loadChildren} 
              disabled={loading}
              className="expand-btn"
            >
              {loading ? '?' : expanded ? '?' : '?'}
            </button>
          ) : (
            <span className="no-children">•</span>
          )}
        </div>

        {/* User Info */}
        <div className="node-info">
          <span className="node-icon">{getIcon()}</span>
          <span className="node-username">{node.username}</span>
          {node.role && (
            <span className="node-role badge">{node.role}</span>
          )}
          <span className="node-type">{node.userType}</span>
        </div>

        {/* Financial Info */}
        <div className="node-financial">
          <span className="node-balance" title="Balance actual">
            ?? {formatMoney(node.balance)}
          </span>
          {node.commissionPercent && (
            <span className="node-commission badge-success" title="Comisión">
              ?? {node.commissionPercent}%
            </span>
          )}
        </div>

        {/* Children Count */}
        {node.hasChildren && (
          <span className="node-children-count" title="Usuarios creados">
            {node.directChildrenCount} {node.directChildrenCount === 1 ? 'usuario' : 'usuarios'}
          </span>
        )}

        {/* Status */}
        <span className={`node-status badge-${node.status.toLowerCase()}`}>
          {node.status}
        </span>
      </div>

      {/* Render Children */}
      {expanded && children && (
        <div className="tree-children">
          {children.map(child => (
            <UserTreeNode key={child.id} node={child} depth={depth + 1} />
          ))}
        </div>
      )}
    </div>
  );
}

// Ejemplo de CSS
const styles = `
.tree-node {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px 12px;
  border-radius: 6px;
  background: #f8f9fa;
  margin-bottom: 4px;
  transition: background 0.2s;
}

.tree-node:hover {
  background: #e9ecef;
}

.tree-node.inactive {
  opacity: 0.6;
}

.expand-btn {
  background: none;
  border: none;
  cursor: pointer;
  font-size: 12px;
  padding: 4px;
  width: 20px;
}

.node-balance {
  font-weight: 600;
  color: #28a745;
}

.node-commission {
  background: #ffc107;
  color: #000;
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 600;
}

.node-children-count {
  color: #6c757d;
  font-size: 12px;
}

.badge {
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 11px;
  font-weight: 600;
}

.badge-success {
  background: #d4edda;
  color: #155724;
}

.badge-active {
  background: #d4edda;
  color: #155724;
}

.badge-inactive {
  background: #f8d7da;
  color: #721c24;
}
`;
```

## ? Resumen

El endpoint `/api/v1/admin/tree/{userId}` permite:
- ? Ver el árbol genealógico de usuarios
- ? Cargar hijos de forma progresiva (lazy loading)
- ? Saber si un nodo tiene hijos sin cargarlos (`hasChildren`)
- ? Controlar la profundidad de carga (`maxDepth`)
- ? Incluir/excluir usuarios inactivos
- ? Respeta scoping por brand y rol

**Perfecto para implementar un árbol expandible en el frontend donde cada nodo muestra un icono de expandir solo si `hasChildren: true`.** ??

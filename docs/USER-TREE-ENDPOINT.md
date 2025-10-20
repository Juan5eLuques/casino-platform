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
        <span>{node.username} ({node.userType})</span>
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

## ?? Diseño de UI Recomendado

```
?? Árbol de Usuarios: super_admin
?? ?? super_admin (SUPER_ADMIN) - 3 usuarios creados
   ?? ? ?? brand_admin_1 (BRAND_ADMIN) - 5 usuarios [expandir]
   ?? ? ?? brand_admin_2 (BRAND_ADMIN) - 2 usuarios [expandir]
   ?? ?? cashier_direct (CASHIER) - Sin usuarios creados
```

Al hacer clic en "expandir":
```
?? Árbol de Usuarios: super_admin
?? ?? super_admin (SUPER_ADMIN) - 3 usuarios creados
   ?? ? ?? brand_admin_1 (BRAND_ADMIN) - 5 usuarios
   ?  ?? ? ?? cashier_1 (CASHIER) - 10 players [expandir]
   ?  ?? ? ?? cashier_2 (CASHIER) - 8 players [expandir]
   ?  ?? ?? player_001 (PLAYER)
   ?  ?? ?? player_002 (PLAYER)
   ?  ?? ?? player_003 (PLAYER)
   ?? ...
```

## ?? Performance

- **Lazy Loading**: Solo carga hijos cuando `maxDepth` lo permite
- **Paginación futura**: Considerar agregar paginación si un nodo tiene muchos hijos
- **Cache**: Considerar cachear árboles frecuentemente consultados

## ? Resumen

El endpoint `/api/v1/admin/tree/{userId}` permite:
- ? Ver el árbol genealógico de usuarios
- ? Cargar hijos de forma progresiva (lazy loading)
- ? Saber si un nodo tiene hijos sin cargarlos (`hasChildren`)
- ? Controlar la profundidad de carga (`maxDepth`)
- ? Incluir/excluir usuarios inactivos
- ? Respeta scoping por brand y rol

**Perfecto para implementar un árbol expandible en el frontend donde cada nodo muestra un icono de expandir solo si `hasChildren: true`.** ??

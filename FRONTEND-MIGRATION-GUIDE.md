# ?? API Changes: Transparent OperatorId Resolution

## ?? Overview

The Casino Platform API has been updated to implement **transparent OperatorId resolution**. This change simplifies frontend integration by automatically resolving the `operatorId` from the brand context (domain/URL), making it transparent to users except for `SUPER_ADMIN`.

## ?? Breaking Changes

### 1. **Player Management Endpoints**

#### `POST /api/v1/admin/players` - Create Player

**BEFORE:**
```json
{
  "brandId": "22222222-2222-2222-2222-222222222222", // Required
  "username": "player123",
  "email": "player@example.com",
  "initialBalance": 1000
}
```

**NOW:**
```json
{
  "username": "player123",
  "email": "player@example.com", 
  "initialBalance": 1000
  // brandId is optional - resolved automatically from context
}
```

#### For `SUPER_ADMIN` Only:
```json
{
  "brandId": "22222222-2222-2222-2222-222222222222", // Can specify any brand
  "username": "player123",
  "email": "player@example.com",
  "initialBalance": 1000
}
```

### 2. **Automatic Scope Resolution**

All listing endpoints (GET operations) now automatically filter by the user's operator/brand scope:

- **SUPER_ADMIN**: Sees all data across all operators/brands
- **OPERATOR_ADMIN**: Only sees data from their operator and associated brands
- **CASHIER**: Only sees data from their operator/brand and assigned players

## ??? Frontend Implementation Guide

### 1. **Role-Based UI Logic**

```typescript
// Frontend should adapt UI based on user role
interface UserContext {
  role: 'SUPER_ADMIN' | 'OPERATOR_ADMIN' | 'CASHIER';
  brandId?: string;
  operatorId?: string;
}

// Example React hook
const useUserPermissions = () => {
  const { user } = useAuth();
  
  return {
    canSpecifyBrand: user.role === 'SUPER_ADMIN',
    canCreateUsers: ['SUPER_ADMIN', 'OPERATOR_ADMIN'].includes(user.role),
    canCreatePlayers: ['SUPER_ADMIN', 'OPERATOR_ADMIN', 'CASHIER'].includes(user.role),
    canAdjustWallets: ['SUPER_ADMIN', 'OPERATOR_ADMIN', 'CASHIER'].includes(user.role),
  };
};
```

### 2. **API Request Adaptation**

```typescript
// Player creation service
interface CreatePlayerRequest {
  username: string;
  email?: string;
  initialBalance?: number;
  brandId?: string; // Only include for SUPER_ADMIN
}

const createPlayer = async (data: CreatePlayerRequest, userRole: string) => {
  const requestBody = { ...data };
  
  // Remove brandId for non-SUPER_ADMIN users - it will be resolved automatically
  if (userRole !== 'SUPER_ADMIN') {
    delete requestBody.brandId;
  }
  
  return fetch('/api/v1/admin/players', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include', // Important for cookies
    body: JSON.stringify(requestBody)
  });
};
```

### 3. **Form Components**

```tsx
// Example React form component
const CreatePlayerForm = () => {
  const { user } = useAuth();
  const { canSpecifyBrand } = useUserPermissions();
  const [brands, setBrands] = useState([]);

  // Only load brands for SUPER_ADMIN
  useEffect(() => {
    if (canSpecifyBrand) {
      loadAvailableBrands().then(setBrands);
    }
  }, [canSpecifyBrand]);

  return (
    <form>
      {/* Brand selector - only for SUPER_ADMIN */}
      {canSpecifyBrand && (
        <select name="brandId" required>
          <option value="">Select Brand</option>
          {brands.map(brand => (
            <option key={brand.id} value={brand.id}>
              {brand.name} ({brand.code})
            </option>
          ))}
        </select>
      )}
      
      <input name="username" placeholder="Username" required />
      <input name="email" type="email" placeholder="Email" />
      <input name="initialBalance" type="number" placeholder="Initial Balance" />
      
      <button type="submit">Create Player</button>
    </form>
  );
};
```

### 4. **Error Handling**

```typescript
// Enhanced error handling for the new authorization system
const handleApiError = (error: any) => {
  if (error.status === 400 && error.detail?.includes('Brand Not Resolved')) {
    // This shouldn't happen in normal flow, but handle gracefully
    toast.error('Brand context error. Please refresh the page.');
    window.location.reload();
  } else if (error.status === 403) {
    // Role-based access denied
    toast.error('You do not have permission for this action.');
  } else if (error.status === 404 && error.detail?.includes('access denied')) {
    // Trying to access resource outside of scope
    toast.error('Resource not found or access denied.');
  }
};
```

## ?? Domain-Based Context

### URL Structure

The API automatically resolves brand context from the domain:

```
https://admin.bet30.local:7182     ? Brand: bet30
https://admin.casino2.local:7182   ? Brand: casino2
https://admin.example.com:7182     ? Brand: example
```

### Frontend Routing

```typescript
// Ensure your frontend routing works with the domain-based approach
const BrandRouter = () => {
  const brandFromDomain = window.location.hostname.split('.')[1]; // Extract brand from subdomain
  
  return (
    <Routes>
      <Route path="/admin/players" element={<PlayerManagement />} />
      <Route path="/admin/users" element={<UserManagement />} />
      {/* No need to include brandId in routes - it's automatic */}
    </Routes>
  );
};
```

## ?? Data Filtering Changes

### Automatic Filtering

All list endpoints now apply automatic filtering:

```typescript
// GET /api/v1/admin/players
// No longer need to pass brandId or operatorId parameters
// The API automatically filters based on user's role and brand context

const fetchPlayers = async (filters: PlayerFilters = {}) => {
  const searchParams = new URLSearchParams();
  
  // Only include actual search filters - no brandId/operatorId needed
  if (filters.username) searchParams.set('username', filters.username);
  if (filters.status) searchParams.set('status', filters.status);
  if (filters.page) searchParams.set('page', filters.page.toString());
  
  return fetch(`/api/v1/admin/players?${searchParams}`, {
    credentials: 'include'
  });
};
```

## ?? Authentication Requirements

### Cookie-Based Authentication

The API uses HttpOnly cookies for authentication. Ensure your requests include credentials:

```typescript
// Always include credentials for authenticated requests
const apiCall = (url: string, options: RequestInit = {}) => {
  return fetch(url, {
    ...options,
    credentials: 'include', // This is crucial
    headers: {
      'Content-Type': 'application/json',
      ...options.headers
    }
  });
};
```

### CORS Configuration

The API now handles CORS dynamically based on brand configuration. Ensure your frontend origin is configured in the brand's `cors_origins`:

```json
// Brand configuration in database should include:
{
  "cors_origins": [
    "http://admin.bet30.local:5173",
    "https://admin.bet30.local:5173",
    "http://localhost:5173"
  ]
}
```

## ?? Migration Checklist

### For Frontend Developers:

- [ ] **Remove explicit `brandId`/`operatorId` from requests** (except for SUPER_ADMIN forms)
- [ ] **Update form components** to conditionally show brand selector for SUPER_ADMIN
- [ ] **Implement role-based permissions** in UI components
- [ ] **Update API service calls** to handle automatic scoping
- [ ] **Test with different user roles** to ensure proper data isolation
- [ ] **Verify domain-based routing** works correctly
- [ ] **Update error handling** for new authorization responses
- [ ] **Ensure `credentials: 'include'`** is set on all authenticated requests

### Testing Scenarios:

1. **SUPER_ADMIN**: Should see all brands, can specify brandId in forms
2. **OPERATOR_ADMIN**: Should only see their brand's data, no brand selector needed
3. **CASHIER**: Should only see their brand's data and assigned players
4. **Cross-brand access**: Should be impossible (test returns 404/403)
5. **Domain switching**: Changing subdomain should change brand context

## ?? Benefits for Frontend

1. **Simplified API calls**: No need to track/pass operatorId
2. **Better UX**: Users don't need to understand operator/brand relationships
3. **Automatic security**: Impossible to accidentally access wrong brand's data
4. **Cleaner code**: Less parameters to manage in forms and API calls
5. **Role-based UI**: Clear separation of capabilities by user role

## ?? Support

If you encounter issues during migration:

1. Check that your user has the correct role in JWT token
2. Verify the brand is configured correctly in the database
3. Ensure CORS origins include your frontend domain
4. Check that `credentials: 'include'` is set on requests
5. Verify the domain resolves to the correct brand in `BrandContext`

The backend automatically handles all scoping and authorization - the frontend just needs to adapt to the simplified API contract.
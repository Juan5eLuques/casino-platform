# Prompt para Claude Sonnet 4.5 - Backoffice Casino "Bet 30"

## ?? **Configuración Específica del Proyecto**

Desarrolla un **Sistema de Backoffice completo** para el casino "Bet 30" usando React + TypeScript que se conecte correctamente con la API backend existente.

### **Configuración del Brand "Bet 30"**
```json
{
  "id": "d2710f8c-6764-46be-a094-3591b49b273e",
  "operatorId": "fdf3a461-6aee-4d6d-bb4a-a2023ac99653",
  "code": "bet30",
  "name": "Bet 30",
  "domain": "bet30.local",
  "adminDomain": "admin.bet30.local",
  "corsOrigins": "http://bet30.local:5173,http://admin.bet30.local:5173",
  "locale": "es",
  "status": "ACTIVE"
}
```

## ?? **Configuración de Desarrollo Específica**

### **URLs y Dominios**
- **API Backend**: `http://localhost:5000/api/v1`
- **Frontend Backoffice**: `http://admin.bet30.local:5173`
- **Frontend Casino Site**: `http://bet30.local:5173`
- **Host Header Requerido**: `admin.bet30.local` (para brand resolution)

### **Archivo hosts requerido**
Agrega esto a tu archivo hosts (`C:\Windows\System32\drivers\etc\hosts` en Windows):
```
127.0.0.1    bet30.local
127.0.0.1    admin.bet30.local
```

## ??? **Configuración del Cliente API**

### **Cliente HTTP con configuración específica**
```typescript
// src/lib/api-client.ts
import axios from 'axios';

const API_BASE_URL = 'http://localhost:5000/api/v1';
const ADMIN_HOST = 'admin.bet30.local';

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  withCredentials: true, // CRÍTICO: Para cookies HttpOnly "bk.token"
  headers: {
    'Host': ADMIN_HOST, // CRÍTICO: Para brand resolution
    'Content-Type': 'application/json',
  },
  timeout: 10000,
});

// Interceptor para manejar errores de autenticación
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Token expirado o inválido
      window.location.href = '/login';
    }
    if (error.response?.status === 403) {
      // Sin permisos para la operación
      console.error('Access denied:', error.response.data);
    }
    return Promise.reject(error);
  }
);

// Interceptor para logging en desarrollo
if (import.meta.env.DEV) {
  apiClient.interceptors.request.use(
    (config) => {
      console.log(`?? API Request: ${config.method?.toUpperCase()} ${config.url}`, {
        headers: config.headers,
        data: config.data
      });
      return config;
    }
  );
  
  apiClient.interceptors.response.use(
    (response) => {
      console.log(`? API Response: ${response.status}`, response.data);
      return response;
    },
    (error) => {
      console.error(`? API Error: ${error.response?.status}`, error.response?.data);
      return Promise.reject(error);
    }
  );
}
```

### **Variables de Entorno (.env.local)**
```env
# API Configuration
VITE_API_BASE_URL=http://localhost:5000/api/v1
VITE_ADMIN_HOST=admin.bet30.local
VITE_BRAND_CODE=bet30
VITE_BRAND_NAME=Bet 30

# Development
VITE_NODE_ENV=development
VITE_ENABLE_API_LOGGING=true

# Brand Specific
VITE_BRAND_ID=d2710f8c-6764-46be-a094-3591b49b273e
VITE_OPERATOR_ID=fdf3a461-6aee-4d6d-bb4a-a2023ac99653
```

## ?? **Configuración de Autenticación**

### **Servicio de Autenticación**
```typescript
// src/services/auth.service.ts
interface LoginCredentials {
  username: string;
  password: string;
}

interface BackofficeUser {
  id: string;
  username: string;
  role: 'SUPER_ADMIN' | 'OPERATOR_ADMIN' | 'CASHIER';
  operatorId: string;
  status: 'ACTIVE' | 'INACTIVE' | 'SUSPENDED';
  createdAt: string;
  lastLoginAt?: string;
}

interface LoginResponse {
  success: boolean;
  user?: BackofficeUser;
  expiresAt?: string;
  errorMessage?: string;
}

class AuthService {
  async login(credentials: LoginCredentials): Promise<LoginResponse> {
    try {
      const response = await apiClient.post<LoginResponse>('/admin/auth/login', credentials);
      return response.data;
    } catch (error: any) {
      throw new Error(error.response?.data?.errorMessage || 'Login failed');
    }
  }

  async getCurrentUser(): Promise<BackofficeUser> {
    const response = await apiClient.get<BackofficeUser>('/admin/auth/me');
    return response.data;
  }

  async logout(): Promise<void> {
    await apiClient.post('/admin/auth/logout');
  }
}

export const authService = new AuthService();
```

### **Store de Autenticación con Zustand**
```typescript
// src/stores/auth.store.ts
import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface AuthStore {
  user: BackofficeUser | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  
  // Actions
  login: (credentials: LoginCredentials) => Promise<void>;
  logout: () => Promise<void>;
  checkAuth: () => Promise<void>;
  setUser: (user: BackofficeUser | null) => void;
}

export const useAuthStore = create<AuthStore>()(
  persist(
    (set, get) => ({
      user: null,
      isAuthenticated: false,
      isLoading: false,

      login: async (credentials) => {
        set({ isLoading: true });
        try {
          const response = await authService.login(credentials);
          if (response.success && response.user) {
            set({ 
              user: response.user, 
              isAuthenticated: true,
              isLoading: false 
            });
          } else {
            throw new Error(response.errorMessage || 'Login failed');
          }
        } catch (error) {
          set({ user: null, isAuthenticated: false, isLoading: false });
          throw error;
        }
      },

      logout: async () => {
        try {
          await authService.logout();
        } catch (error) {
          console.error('Logout error:', error);
        } finally {
          set({ user: null, isAuthenticated: false });
          window.location.href = '/login';
        }
      },

      checkAuth: async () => {
        set({ isLoading: true });
        try {
          const user = await authService.getCurrentUser();
          set({ user, isAuthenticated: true, isLoading: false });
        } catch (error) {
          set({ user: null, isAuthenticated: false, isLoading: false });
        }
      },

      setUser: (user) => {
        set({ user, isAuthenticated: !!user });
      },
    }),
    {
      name: 'bet30-auth-storage',
      partialize: (state) => ({ 
        user: state.user, 
        isAuthenticated: state.isAuthenticated 
      }),
    }
  )
);
```

## ?? **Configuración de Desarrollo**

### **Vite Configuration (vite.config.ts)**
```typescript
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    host: 'admin.bet30.local',
    cors: true,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        secure: false,
        headers: {
          'Host': 'admin.bet30.local'
        }
      }
    }
  },
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
});
```

### **Package.json Scripts**
```json
{
  "scripts": {
    "dev": "vite --host admin.bet30.local --port 5173",
    "build": "tsc && vite build",
    "preview": "vite preview --host admin.bet30.local --port 5173",
    "hosts:setup": "echo 'Add these lines to your hosts file:' && echo '127.0.0.1 bet30.local' && echo '127.0.0.1 admin.bet30.local'"
  }
}
```

## ?? **Configuración Específica del Brand Bet 30**

### **Tema y Colores**
```typescript
// src/config/brand.config.ts
export const brandConfig = {
  name: 'Bet 30',
  code: 'bet30',
  locale: 'es',
  
  // Colores específicos de Bet 30
  theme: {
    primary: {
      50: '#fef2f2',
      500: '#dc2626', // Rojo característico de Bet 30
      900: '#7f1d1d',
    },
    secondary: {
      50: '#f8fafc',
      500: '#1e40af',
      900: '#1e3a8a',
    },
    success: {
      500: '#10b981',
    },
    warning: {
      500: '#f59e0b',
    },
    danger: {
      500: '#ef4444',
    }
  },

  // URLs
  urls: {
    api: import.meta.env.VITE_API_BASE_URL,
    site: 'http://bet30.local:5173',
    admin: 'http://admin.bet30.local:5173',
  },

  // Configuración de la API
  api: {
    baseURL: import.meta.env.VITE_API_BASE_URL,
    hostHeader: import.meta.env.VITE_ADMIN_HOST,
    timeout: 10000,
  }
};
```

### **Configuración de Tailwind con tema Bet 30**
```javascript
// tailwind.config.js
/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        // Colores específicos de Bet 30
        brand: {
          primary: '#dc2626',
          secondary: '#1e40af',
          accent: '#f59e0b',
        },
        bet30: {
          red: {
            50: '#fef2f2',
            500: '#dc2626',
            900: '#7f1d1d',
          },
          blue: {
            50: '#eff6ff',
            500: '#1e40af',
            900: '#1e3a8a',
          }
        }
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
        mono: ['JetBrains Mono', 'monospace'],
      },
    },
  },
  plugins: [],
};
```

## ?? **Servicios de API Específicos**

### **Ejemplo: Servicio de Jugadores**
```typescript
// src/services/players.service.ts
export interface Player {
  id: string;
  brandId: string;
  username: string;
  email: string;
  externalId: string;
  status: 'ACTIVE' | 'INACTIVE' | 'SUSPENDED' | 'BANNED';
  balance: number;
  createdAt: string;
  lastLoginAt?: string;
}

export interface PlayerFilters {
  status?: Player['status'];
  search?: string;
  assignedToCashier?: string;
  hasBalance?: boolean;
  page?: number;
  limit?: number;
}

export interface PlayersResponse {
  players: Player[];
  pagination: {
    page: number;
    limit: number;
    total: number;
    pages: number;
  };
}

class PlayersService {
  async getPlayers(filters: PlayerFilters = {}): Promise<PlayersResponse> {
    const params = new URLSearchParams();
    
    // Filtros específicos para Bet 30
    params.append('brandId', import.meta.env.VITE_BRAND_ID);
    
    Object.entries(filters).forEach(([key, value]) => {
      if (value !== undefined) {
        params.append(key, value.toString());
      }
    });

    const response = await apiClient.get<PlayersResponse>(`/admin/players?${params}`);
    return response.data;
  }

  async getPlayer(playerId: string): Promise<Player> {
    const response = await apiClient.get<Player>(`/admin/players/${playerId}`);
    return response.data;
  }

  async createPlayer(playerData: Omit<Player, 'id' | 'createdAt'>): Promise<Player> {
    const response = await apiClient.post<Player>('/admin/players', {
      ...playerData,
      brandId: import.meta.env.VITE_BRAND_ID, // Forzar brand Bet 30
    });
    return response.data;
  }

  async updatePlayerStatus(playerId: string, status: Player['status'], reason?: string): Promise<Player> {
    const response = await apiClient.patch<Player>(`/admin/players/${playerId}/status`, {
      status,
      reason,
    });
    return response.data;
  }
}

export const playersService = new PlayersService();
```

## ?? **Testing de Conexión API**

### **Componente de Test de Conexión**
```typescript
// src/components/ApiConnectionTest.tsx
import { useState } from 'react';
import { apiClient } from '@/lib/api-client';

export function ApiConnectionTest() {
  const [status, setStatus] = useState<'idle' | 'testing' | 'success' | 'error'>('idle');
  const [result, setResult] = useState<any>(null);

  const testConnection = async () => {
    setStatus('testing');
    try {
      // Test 1: Verificar que la API responde
      const healthResponse = await fetch('http://localhost:5000/health');
      if (!healthResponse.ok) throw new Error('API not responding');

      // Test 2: Verificar brand resolution
      const brandsResponse = await apiClient.get('/admin/brands');
      
      setResult({
        apiHealth: 'OK',
        brandResolution: 'OK',
        hostHeader: 'admin.bet30.local',
        brandsCount: brandsResponse.data.brands?.length || 0,
      });
      setStatus('success');
    } catch (error: any) {
      setResult({
        error: error.message,
        stack: error.stack,
      });
      setStatus('error');
    }
  };

  if (import.meta.env.PROD) return null; // Solo en desarrollo

  return (
    <div className="fixed bottom-4 right-4 p-4 bg-white border rounded shadow">
      <h3 className="font-bold mb-2">API Connection Test</h3>
      <button
        onClick={testConnection}
        disabled={status === 'testing'}
        className="px-3 py-1 bg-blue-500 text-white rounded"
      >
        {status === 'testing' ? 'Testing...' : 'Test API'}
      </button>
      
      {result && (
        <pre className="mt-2 text-xs overflow-auto max-w-sm">
          {JSON.stringify(result, null, 2)}
        </pre>
      )}
    </div>
  );
}
```

## ?? **Checklist de Configuración**

### **Antes de desarrollar**
- [ ] Agregar entradas al archivo hosts
- [ ] Configurar variables de entorno (.env.local)
- [ ] Verificar que el backend esté corriendo en puerto 5000
- [ ] Verificar que el brand "bet30" existe en la base de datos
- [ ] Configurar CORS en el backend para incluir admin.bet30.local:5173

### **Durante el desarrollo**
- [ ] Usar siempre `admin.bet30.local:5173` para acceder al backoffice
- [ ] Verificar que el Host header se esté enviando correctamente
- [ ] Monitorear la consola del navegador para errores de CORS
- [ ] Probar login con usuarios de prueba del brand bet30
- [ ] Verificar que las cookies HttpOnly se estén manejando correctamente

### **Comandos de desarrollo**
```bash
# Setup inicial
npm create vite@latest bet30-backoffice -- --template react-ts
cd bet30-backoffice
npm install

# Dependencias específicas
npm install axios @tanstack/react-query zustand react-router-dom
npm install @headlessui/react @heroicons/react
npm install react-hook-form @hookform/resolvers zod
npm install tailwindcss @tailwindcss/forms
npm install lucide-react recharts
npm install react-hot-toast

# Desarrollo
npm run dev # Iniciará en admin.bet30.local:5173
```

## ?? **Objetivos Específicos**

1. **Conexión API Correcta**: El frontend debe conectarse exitosamente con el backend usando el Host header correcto
2. **Brand Context**: Todas las operaciones deben estar scoped al brand "bet30"
3. **Autenticación**: Login debe funcionar con cookies HttpOnly y JWT
4. **Responsive Design**: Funcional en desktop y móvil
5. **Tema Bet 30**: Usar los colores y branding específicos del casino
6. **Locale ES**: Interfaz en español (es)
7. **Performance**: Carga rápida y navegación fluida
8. **Error Handling**: Manejo elegante de errores de conexión y API

## ?? **Problemas Comunes y Soluciones**

### **Error: No se puede conectar a la API**
- Verificar que el backend esté corriendo en puerto 5000
- Confirmar que el archivo hosts tiene las entradas correctas
- Revisar la configuración de CORS en el backend

### **Error 401: Unauthorized**
- Verificar que `withCredentials: true` esté configurado
- Confirmar que el Host header se esté enviando
- Revisar que el usuario pertenece al brand bet30

### **Error de Brand Resolution**
- Confirmar que el Host header es exactamente "admin.bet30.local"
- Verificar que el brand existe en la base de datos
- Revisar que el domain y adminDomain estén configurados correctamente

---

**¡Desarrolla el backoffice de Bet 30 con esta configuración específica y tendrás una conexión perfecta con tu API!** ???
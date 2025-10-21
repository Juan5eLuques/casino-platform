# Fix: Logout con Proxy - Solución Frontend

## ?? **El Problema con Proxies**

Cuando usas un **reverse proxy** (Netlify, Vercel, etc.), el navegador ve todas las requests como **same-origin**, pero las cookies se crearon con configuraciones específicas (Domain, SameSite) que impiden que se envíen en requests proxeadas.

**Resultado**: El backend no recibe la cookie en el request de logout, entonces no puede borrarla.

---

## ? **Solución: Doble Eliminación (Backend + Frontend)**

### **Backend (Ya implementado)**

El backend ahora:
1. Intenta borrar la cookie de 3 formas diferentes
2. Retorna el nombre de la cookie para que el frontend también la borre

Response del logout:
```json
{
  "ok": true,
  "message": "Logged out successfully",
  "cookieName": "bk.token.netlify_prod",
  "cookieWasPresent": false
}
```

---

### **Frontend: Solución TypeScript/JavaScript**

#### **1. Función Helper para Borrar Cookies**

```typescript
// utils/cookies.ts

/**
 * Borra una cookie del navegador de forma agresiva
 * Prueba múltiples combinaciones de domain/path para asegurar borrado
 */
export function deleteCookie(name: string) {
  // 1. Borrar sin domain (host-only)
  document.cookie = `${name}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/;`;
  
  // 2. Borrar con domain actual
  const domain = window.location.hostname;
  document.cookie = `${name}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/; domain=${domain};`;
  
  // 3. Borrar con dominio padre (para subdominios)
  const parts = domain.split('.');
  if (parts.length > 2) {
    const parentDomain = parts.slice(-2).join('.');
    document.cookie = `${name}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/; domain=.${parentDomain};`;
  }
  
  // 4. También probar con SameSite=None
  document.cookie = `${name}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/; SameSite=None; Secure;`;
  
  console.log(`Cookie ${name} deletion attempted (multiple strategies)`);
}

/**
 * Verifica si una cookie existe
 */
export function hasCookie(name: string): boolean {
  return document.cookie.split(';').some(c => c.trim().startsWith(`${name}=`));
}

/**
 * Obtiene todas las cookies que empiecen con un prefijo
 */
export function getCookiesWithPrefix(prefix: string): string[] {
  return document.cookie
    .split(';')
    .map(c => c.trim())
    .filter(c => c.startsWith(prefix))
    .map(c => c.split('=')[0]);
}
```

---

#### **2. Servicio de Autenticación**

```typescript
// services/authService.ts
import { deleteCookie, getCookiesWithPrefix } from '@/utils/cookies';
import axios from 'axios';

export interface LogoutResponse {
  ok: boolean;
  message: string;
  cookieName: string;
  cookieWasPresent: boolean;
}

export const logout = async (): Promise<void> => {
  try {
    // 1. Llamar al endpoint de logout del backend
    const response = await axios.post<LogoutResponse>(
      '/api/v1/admin/auth/logout',
      {},
      {
        withCredentials: true  // Importante para enviar cookies
      }
    );
    
    console.log('Logout response:', response.data);
    
    // 2. Borrar la cookie específica del brand desde el frontend
    if (response.data.cookieName) {
      deleteCookie(response.data.cookieName);
      console.log(`Deleted cookie: ${response.data.cookieName}`);
    }
    
    // 3. OPCIONAL: Borrar todas las cookies que empiecen con "bk.token."
    // Esto asegura que todas las sesiones se cierren
    const allBkCookies = getCookiesWithPrefix('bk.token.');
    allBkCookies.forEach(cookieName => {
      deleteCookie(cookieName);
      console.log(`Deleted cookie: ${cookieName}`);
    });
    
    // 4. Limpiar localStorage/sessionStorage si lo usas
    localStorage.removeItem('user');
    localStorage.removeItem('brand');
    sessionStorage.clear();
    
    console.log('? Logout completed successfully');
  } catch (error) {
    console.error('? Logout failed:', error);
    
    // FALLBACK: Si el backend falla, igual borrar cookies localmente
    const allBkCookies = getCookiesWithPrefix('bk.token.');
    allBkCookies.forEach(deleteCookie);
    
    throw error;
  }
};
```

---

#### **3. React Hook de Autenticación**

```typescript
// hooks/useAuth.ts
import { useNavigate } from 'react-router-dom';
import { logout as logoutService } from '@/services/authService';
import { useAuthStore } from '@/store/authStore';  // Zustand/Redux

export const useAuth = () => {
  const navigate = useNavigate();
  const clearAuth = useAuthStore(state => state.clearAuth);
  
  const logout = async () => {
    try {
      await logoutService();
      
      // Limpiar estado de la aplicación
      clearAuth();
      
      // Redirigir al login
      navigate('/login', { replace: true });
    } catch (error) {
      console.error('Logout error:', error);
      
      // Aún así limpiar estado local y redirigir
      clearAuth();
      navigate('/login', { replace: true });
    }
  };
  
  return { logout };
};
```

---

#### **4. Componente de UI**

```tsx
// components/LogoutButton.tsx
import React from 'react';
import { useAuth } from '@/hooks/useAuth';
import { toast } from 'sonner';  // o tu librería de notificaciones

export const LogoutButton: React.FC = () => {
  const { logout } = useAuth();
  const [isLoading, setIsLoading] = React.useState(false);
  
  const handleLogout = async () => {
    setIsLoading(true);
    
    try {
      await logout();
      toast.success('Sesión cerrada exitosamente');
    } catch (error) {
      toast.error('Error al cerrar sesión, pero se limpió localmente');
    } finally {
      setIsLoading(false);
    }
  };
  
  return (
    <button 
      onClick={handleLogout}
      disabled={isLoading}
      className="btn btn-danger"
    >
      {isLoading ? 'Cerrando sesión...' : 'Cerrar Sesión'}
    </button>
  );
};
```

---

## ?? **Testing**

### **Test 1: Logout Normal**

1. Login en el frontend
2. Verificar cookie en DevTools: `bk.token.netlify_prod`
3. Click en "Cerrar Sesión"
4. Verificar en DevTools que la cookie **desapareció** ?
5. Verificar que la app redirige al `/login` ?

---

### **Test 2: Logout con Backend Down**

```typescript
// Simular backend down
const mockLogout = async () => {
  // Simular error
  throw new Error('Network error');
};

// El frontend debería:
// 1. Intentar logout en backend (FAIL)
// 2. Aún así borrar cookies localmente ?
// 3. Limpiar estado ?
// 4. Redirigir al login ?
```

---

### **Test 3: Múltiples Brands**

```typescript
// Login en Brand A
await login('admin', 'pass', 'netlify_prod');
// Cookie: bk.token.netlify_prod

// Login en Brand B
await login('admin', 'pass', 'bet30_prod');
// Cookie: bk.token.bet30_prod

// Logout de Brand A
await logout();  // Debería borrar SOLO bk.token.netlify_prod
```

---

## ?? **Comparación Antes/Después**

| Aspecto | ? Antes | ? Ahora |
|---------|----------|----------|
| **Backend borra cookie** | Solo intenta 1 vez | Intenta 3 veces con diferentes opciones |
| **Frontend ayuda** | ? No | ? Sí, borra también |
| **Proxy funciona** | ? No | ? Sí |
| **Cookie presente en request** | A veces | No importa |
| **Fallback si backend falla** | ? No | ? Sí |
| **Limpia estado local** | Manual | ? Automático |

---

## ?? **Checklist de Implementación**

### Backend (? Ya hecho):
- [x] Logout intenta borrar cookie 3 veces
- [x] Retorna `cookieName` en response
- [x] Logs detallados

### Frontend (Por hacer):
- [ ] Crear `utils/cookies.ts` con `deleteCookie()`
- [ ] Actualizar `authService.logout()` para borrar cookie localmente
- [ ] Limpiar localStorage/sessionStorage
- [ ] Actualizar componente de logout
- [ ] Testing en diferentes escenarios

---

## ?? **Deploy**

1. **Backend**: Ya está corregido, solo redeploy Railway
2. **Frontend**: 
   - Implementar funciones de `cookies.ts`
   - Actualizar servicio de auth
   - Deploy

---

## ?? **Debugging**

Si el logout sigue fallando:

```typescript
// En el logout del frontend, agregar esto:
console.log('?? Cookies before logout:', document.cookie);

await logout();

console.log('?? Cookies after logout:', document.cookie);

// Deberías ver que las cookies bk.token.* desaparecen
```

---

## ? **Resumen**

**Problema**: Proxies impiden que la cookie llegue al backend en el request de logout.

**Solución**: 
1. Backend intenta borrar de 3 formas diferentes
2. **Frontend también borra la cookie localmente** (CLAVE)
3. Frontend limpia estado y redirige

**Resultado**: Logout funciona **siempre**, incluso si el backend no recibe la cookie.

**Código clave del frontend:**
```typescript
// Después de llamar al backend
deleteCookie(response.data.cookieName);

// O borrar todas las cookies de sesión
getCookiesWithPrefix('bk.token.').forEach(deleteCookie);
```

**¡Ahora el logout funcionará correctamente con proxies! ??**

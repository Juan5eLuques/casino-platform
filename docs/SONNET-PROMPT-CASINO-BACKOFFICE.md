# Prompt para Claude Sonnet 4.5 - Desarrollo de Backoffice de Casino

## ?? **Objetivo**

Necesito que desarrolles un **Sistema de Backoffice completo** para una plataforma de casino multi-brand usando React + TypeScript. El sistema debe permitir gestionar operadores, brands (sitios de casino), usuarios administrativos, cajeros, jugadores, transacciones y configuraciones.

## ?? **Contexto de la Plataforma**

### **Arquitectura Multi-Brand**
- **Operadores**: Empresas que gestionan múltiples casinos
- **Brands**: Sitios de casino individuales (ej: MiCasino, SuperSlots)
- **Usuarios Backoffice**: Administradores y cajeros con diferentes roles
- **Jugadores**: Usuarios finales que juegan en los sitios
- **Separación por dominios**: admin.mycasino.local (backoffice) vs mycasino.local (sitio público)

### **Sistema de Roles y Permisos**
- **SUPER_ADMIN**: Acceso total, gestiona todos los operadores
- **OPERATOR_ADMIN**: Gestiona solo su operador y sus brands
- **CASHIER**: Gestiona solo jugadores asignados a él

### **Autenticación Híbrida**
- **JWT + Cookies HttpOnly**: Backoffice usa cookie "bk.token" con path "/admin"
- **Separación total**: Tokens de backoffice y players son completamente diferentes
- **Brand Resolution**: El host header determina qué brand se está gestionando

## ??? **Tecnologías y Stack Requerido**

### **Frontend**
- **React 18** con TypeScript
- **Vite** como build tool
- **TanStack Query** (React Query) para estado del servidor
- **React Router v6** para navegación
- **Tailwind CSS** para estilos
- **Headless UI** o **Radix UI** para componentes
- **React Hook Form** + **Zod** para formularios y validación
- **Lucide React** para iconos
- **Recharts** o **Chart.js** para gráficos y dashboards

### **Estado y Datos**
- **Zustand** para estado global
- **TanStack Query** para cache y sincronización de API
- **Axios** para cliente HTTP con interceptors
- **React Hot Toast** para notificaciones

### **Desarrollo**
- **ESLint** + **Prettier** para código limpio
- **TypeScript strict mode**
- **Vite** con HMR para desarrollo rápido

## ?? **API Backend Disponible**

El backend está completamente implementado con estos endpoints principales:

### **Autenticación**
```
POST /api/v1/admin/auth/login     - Login de backoffice
GET  /api/v1/admin/auth/me        - Perfil usuario actual
POST /api/v1/admin/auth/logout    - Cerrar sesión
```

### **Gestión de Operadores** (Solo SUPER_ADMIN)
```
GET  /api/v1/admin/operators      - Listar operadores
POST /api/v1/admin/operators      - Crear operador
PATCH /api/v1/admin/operators/{id} - Actualizar operador
```

### **Gestión de Brands**
```
GET  /api/v1/admin/brands         - Listar brands (scoped por operador)
POST /api/v1/admin/brands         - Crear brand
PATCH /api/v1/admin/brands/{id}   - Actualizar brand
PUT  /api/v1/admin/brands/{id}/providers/{code} - Configurar proveedor
```

### **Gestión de Usuarios Backoffice**
```
GET  /api/v1/admin/users          - Listar usuarios (scoped por operador)
POST /api/v1/admin/users          - Crear usuario
GET  /api/v1/admin/users/{id}     - Obtener usuario
PATCH /api/v1/admin/users/{id}    - Actualizar usuario
DELETE /api/v1/admin/users/{id}   - Eliminar usuario
```

### **Gestión de Jugadores**
```
GET  /api/v1/admin/players        - Listar jugadores (scoped por rol)
POST /api/v1/admin/players        - Crear jugador
GET  /api/v1/admin/players/{id}   - Obtener jugador
PATCH /api/v1/admin/players/{id}/status - Cambiar estado
```

### **Gestión de Billeteras**
```
GET  /api/v1/admin/players/{id}/wallet - Información de billetera
POST /api/v1/admin/players/{id}/wallet/adjust - Ajustar saldo
GET  /api/v1/admin/players/{id}/transactions - Historial transacciones
```

### **Asignaciones Cajero-Jugador**
```
POST /api/v1/admin/cashiers/{cashierId}/players/{playerId} - Asignar
GET  /api/v1/admin/cashiers/{cashierId}/players - Listar asignados
DELETE /api/v1/admin/cashiers/{cashierId}/players/{playerId} - Desasignar
```

### **Gestión de Juegos**
```
GET  /api/v1/admin/games          - Listar juegos
POST /api/v1/admin/games          - Crear juego
GET  /api/v1/admin/brands/{id}/games - Juegos de brand
PUT  /api/v1/admin/brands/{id}/games/{gameId} - Configurar juego para brand
```

### **Passwords**
```
POST /api/v1/admin/users/{id}/password - Cambiar password usuario
POST /api/v1/admin/users/{id}/reset-password - Reset password
POST /api/v1/admin/players/{id}/password - Cambiar password jugador
```

## ?? **Requerimientos de UI/UX**

### **Diseño y Apariencia**
- **Dark Mode Support**: Tema oscuro/claro toggleable
- **Responsive Design**: Funcional en desktop, tablet y móvil
- **Modern Casino Aesthetic**: Colores elegantes (dark blues, golds, greens)
- **Professional Dashboard**: Clean y minimalista pero con toques de casino

### **Layout Principal**
```
???????????????????????????????????????????????????????????
? Header: Brand Selector | User Menu | Dark Mode Toggle   ?
???????????????????????????????????????????????????????????
?             ?                                           ?
?  Sidebar    ?            Main Content Area              ?
?  Navigation ?                                           ?
?             ?                                           ?
? - Dashboard ?  ???????????????????????????????????????  ?
? - Players   ?  ?                                     ?  ?
? - Users     ?  ?         Component Content           ?  ?
? - Brands    ?  ?                                     ?  ?
? - Games     ?  ?                                     ?  ?
? - Reports   ?  ???????????????????????????????????????  ?
? - Settings  ?                                           ?
?             ?                                           ?
???????????????????????????????????????????????????????????
```

### **Navegación y Flujos**
- **Sidebar colapsible** con iconos y etiquetas
- **Breadcrumbs** para navegación contextual
- **Brand Selector** en header para cambiar entre brands (si tiene múltiples)
- **Search Global** para encontrar jugadores/usuarios rápidamente
- **Notifications Panel** para alertas y actividad reciente

## ?? **Páginas y Componentes Principales**

### **1. Dashboard / Resumen**
- **Cards de métricas clave**:
  - Total Players Activos
  - Total Balance de Jugadores
  - Transacciones del día
  - Usuarios online ahora
  - Revenue del mes
- **Gráficos**:
  - Actividad de jugadores (último mes)
  - Transacciones por día
  - Balance total histórico
- **Actividad Reciente**:
  - Últimos registros de jugadores
  - Últimas transacciones importantes
  - Últimos logins de usuarios backoffice

### **2. Gestión de Jugadores**

#### **Lista de Jugadores**
- **Tabla con filtros avanzados**:
  - Por status (ACTIVE, INACTIVE, SUSPENDED, BANNED)
  - Por brand (si el usuario tiene acceso a múltiples)
  - Por cajero asignado
  - Por rango de balance
  - Por fecha de registro
- **Columnas**:
  - Avatar/Inicial
  - Username
  - Email
  - Status (badge colorizado)
  - Balance actual
  - Último login
  - Cajeros asignados
  - Acciones (Ver, Editar, Suspender)

#### **Detalle de Jugador**
- **Información General**:
  - Datos personales
  - Status y fecha de registro
  - External ID para integraciones
- **Billetera**:
  - Balance actual (destacado)
  - Botón "Ajustar Saldo" con modal
  - Historial de transacciones (tabla paginada)
  - Gráfico de evolución del balance
- **Actividad**:
  - Últimas sesiones de juego
  - Juegos más jugados
  - Estadísticas de bet/win
- **Gestión**:
  - Cambiar status con razón
  - Asignar/desasignar cajeros
  - Reset password
  - Notas administrativas

#### **Crear/Editar Jugador**
- **Formulario con validación**:
  - Username (único por brand)
  - Email (único por brand)
  - External ID para integraciones
  - Password inicial
  - Brand (si el usuario tiene acceso a múltiples)
  - Balance inicial
  - Status inicial

### **3. Gestión de Usuarios Backoffice**

#### **Lista de Usuarios**
- **Tabla con filtros**:
  - Por rol (SUPER_ADMIN, OPERATOR_ADMIN, CASHIER)
  - Por status
  - Por operador (si SUPER_ADMIN)
- **Columnas**:
  - Username
  - Rol (badge colorizado)
  - Status
  - Operador
  - Último login
  - Jugadores asignados (solo cajeros)
  - Acciones

#### **Detalle de Usuario**
- **Información General**:
  - Username y rol
  - Operador asignado
  - Status y fechas
- **Permisos y Accesos**:
  - Brands a los que tiene acceso
  - Últimos accesos al sistema
- **Jugadores Asignados** (solo para CASHIER):
  - Lista de jugadores bajo su gestión
  - Botón para asignar/desasignar jugadores
- **Gestión**:
  - Cambiar rol (con validaciones)
  - Cambiar status
  - Reset password
  - Cambiar operador (solo SUPER_ADMIN)

#### **Crear/Editar Usuario**
- **Formulario con validación**:
  - Username (único por operador)
  - Password inicial
  - Rol con descripción de permisos
  - Operador (si SUPER_ADMIN)
  - Status inicial

### **4. Gestión de Brands** (Solo SUPER_ADMIN y OPERATOR_ADMIN)

#### **Lista de Brands**
- **Cards o tabla con información clave**:
  - Logo/nombre del brand
  - Código y dominio
  - Status (badge colorizado)
  - Número de jugadores activos
  - Balance total de jugadores
  - Fecha de creación

#### **Detalle de Brand**
- **Configuración General**:
  - Nombre, código, dominio
  - Dominio de admin
  - Locale y timezone
  - Status del sitio
- **Apariencia**:
  - Editor de tema (colores primarios/secundarios)
  - Logo upload
  - CSS personalizado (opcional)
- **Configuraciones**:
  - Máximo monto de apuesta
  - Moneda
  - CORS origins
  - Configuraciones específicas del casino
- **Juegos**:
  - Lista de juegos habilitados
  - Orden de visualización
  - Tags y categorías
- **Proveedores**:
  - Configuración de proveedores de juegos
  - Secretos HMAC
  - URLs de webhook
- **Estadísticas**:
  - Jugadores registrados
  - Revenue del mes
  - Juegos más populares

#### **Crear/Editar Brand**
- **Formulario por pasos (wizard)**:
  1. Información básica (nombre, código, dominio)
  2. Configuración técnica (CORS, locale, timezone)
  3. Apariencia (tema, colores, logo)
  4. Configuraciones de casino (moneda, límites)
  5. Revisión y confirmación

### **5. Gestión de Juegos**

#### **Lista de Juegos**
- **Tabla con filtros**:
  - Por proveedor
  - Por categoría (SLOT, TABLE, POKER, etc.)
  - Por estado (habilitado/deshabilitado)
  - Por brands que lo usan
- **Columnas**:
  - Imagen/icon del juego
  - Nombre y código
  - Proveedor
  - Categoría
  - Brands activos
  - Estado global
  - Acciones

#### **Configuración de Juego por Brand**
- **Lista de brands con toggles**:
  - Habilitar/deshabilitar por brand
  - Orden de visualización
  - Tags específicos del brand
  - Configuraciones especiales

#### **Crear/Editar Juego**
- **Formulario**:
  - Código único del juego
  - Nombre display
  - Proveedor
  - Categoría
  - Imagen/icon upload
  - Metadatos (RTP, volatilidad, etc.)

### **6. Asignaciones Cajero-Jugador**

#### **Vista de Asignaciones**
- **Layout de dos columnas**:
  - Izquierda: Lista de cajeros con contador de jugadores asignados
  - Derecha: Jugadores del cajero seleccionado
- **Drag & Drop**:
  - Arrastrar jugadores entre cajeros
  - Confirmación antes de mover
- **Búsqueda y filtros**:
  - Buscar cajeros por username
  - Buscar jugadores por username/email
  - Filtrar jugadores sin asignar

#### **Modal de Asignación Múltiple**
- **Selección en batch**:
  - Checkboxes para seleccionar múltiples jugadores
  - Asignar todos a un cajero específico
  - Confirmación con resumen

### **7. Reportes y Analytics**

#### **Dashboard de Reportes**
- **Filtros de tiempo**:
  - Hoy, Esta semana, Este mes, Rango personalizado
  - Comparación con período anterior
- **Métricas de Jugadores**:
  - Nuevos registros
  - Jugadores activos
  - Retención de jugadores
- **Métricas Financieras**:
  - Total deposits/withdrawals
  - GGR (Gross Gaming Revenue)
  - Balance total de jugadores
- **Métricas de Juegos**:
  - Juegos más populares
  - RTP real vs teórico
  - Sessions y duración promedio

#### **Reportes Detallados**
- **Exportación a CSV/Excel**:
  - Lista de transacciones
  - Lista de jugadores
  - Actividad de usuarios backoffice
- **Reportes programados**:
  - Configurar reportes automáticos
  - Envío por email
  - Frecuencia configurable

### **8. Configuraciones del Sistema**

#### **Configuraciones Generales**
- **Información de la empresa**
- **Configuraciones de email**
- **Configuraciones de seguridad**:
  - Políticas de password
  - Timeouts de sesión
  - Intentos de login fallidos

#### **Configuraciones de Brand** (por brand seleccionado)
- **Límites y restricciones**
- **Configuraciones de juegos**
- **Integraciones con proveedores**
- **Configuraciones de payment**

## ?? **Configuración Técnica**

### **Autenticación y Seguridad**
```typescript
// Configuración de Axios con interceptors
const apiClient = axios.create({
  baseURL: 'http://localhost:5000/api/v1',
  withCredentials: true, // Para cookies HttpOnly
  headers: {
    'Host': 'admin.mycasino.local' // Para brand resolution
  }
});

// Interceptor para refresh de token
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Redirect a login
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);
```

### **Estado Global con Zustand**
```typescript
interface AuthState {
  user: BackofficeUser | null;
  isAuthenticated: boolean;
  currentBrand: Brand | null;
  availableBrands: Brand[];
  login: (credentials: LoginCredentials) => Promise<void>;
  logout: () => Promise<void>;
  switchBrand: (brandId: string) => void;
}

interface UIState {
  sidebarCollapsed: boolean;
  darkMode: boolean;
  notifications: Notification[];
  toggleSidebar: () => void;
  toggleDarkMode: () => void;
}
```

### **React Query Setup**
```typescript
// Queries para diferentes entidades
const usePlayersQuery = (filters: PlayerFilters) => 
  useQuery({
    queryKey: ['players', filters],
    queryFn: () => playersApi.getPlayers(filters),
    staleTime: 1000 * 60 * 5, // 5 minutos
  });

const useCreatePlayerMutation = () =>
  useMutation({
    mutationFn: playersApi.createPlayer,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['players'] });
      toast.success('Player created successfully');
    },
    onError: (error) => {
      toast.error(error.message || 'Failed to create player');
    }
  });
```

## ?? **Funcionalidades Específicas del Casino**

### **Gestión de Balances**
- **Visualización en formato de moneda** (centavos ? euros con 2 decimales)
- **Validaciones de saldo** antes de ajustes
- **Razones predefinidas** para ajustes (BONUS, CORRECTION, PROMOTION, etc.)
- **Confirmaciones** para operaciones críticas
- **Audit trail** visible de todas las transacciones

### **Estados de Jugador**
- **ACTIVE**: Verde, puede jugar normalmente
- **INACTIVE**: Gris, cuenta desactivada temporalmente
- **SUSPENDED**: Amarillo, suspendido con razón
- **BANNED**: Rojo, baneado permanentemente

### **Roles y Permisos Visuales**
- **SUPER_ADMIN**: Acceso a todo, badge dorado
- **OPERATOR_ADMIN**: Gestión de su operador, badge azul
- **CASHIER**: Solo jugadores asignados, badge verde

### **Brand Context Awareness**
- **Brand selector** en header si el usuario tiene acceso a múltiples brands
- **Filtros automáticos** basados en el brand seleccionado
- **URLs con brand context** para deep linking
- **Datos scoped** por brand automáticamente

## ?? **Responsividad y Móvil**

### **Breakpoints**
- **Mobile**: < 640px (sidebar como drawer overlay)
- **Tablet**: 640px - 1024px (sidebar colapsado por defecto)
- **Desktop**: > 1024px (sidebar expandido)

### **Componentes Responsivos**
- **Tablas ? Cards** en móvil
- **Sidebar ? Bottom navigation** en móvil
- **Forms ? Pasos separados** en móvil
- **Dashboards ? Scroll horizontal** para cards

## ?? **Guía de Estilos**

### **Colores (Dark Theme)**
```css
:root {
  /* Primary - Casino Gold */
  --primary-50: #fefdf8;
  --primary-500: #f59e0b;
  --primary-900: #78350f;
  
  /* Secondary - Deep Blue */
  --secondary-50: #f8fafc;
  --secondary-500: #1e40af;
  --secondary-900: #1e3a8a;
  
  /* Success - Casino Green */
  --success-500: #10b981;
  
  /* Danger - Alert Red */
  --danger-500: #ef4444;
  
  /* Warning - Casino Orange */
  --warning-500: #f97316;
  
  /* Dark backgrounds */
  --bg-dark: #0f172a;
  --bg-dark-secondary: #1e293b;
  --bg-dark-tertiary: #334155;
}
```

### **Tipografía**
- **Headings**: Inter font family, font-semibold
- **Body**: Inter font family, font-normal
- **Monospace**: JetBrains Mono para números/códigos

## ?? **Funcionalidades Avanzadas**

### **Real-time Updates**
- **WebSocket connection** para updates en vivo
- **Notificaciones** de nuevos jugadores/transacciones
- **Status updates** en tiempo real

### **Exportación de Datos**
- **Botones de export** en todas las tablas
- **Formatos**: CSV, Excel, PDF
- **Filtros aplicados** se mantienen en export

### **Búsqueda Global**
- **Comando + K** para abrir search global
- **Búsqueda cross-entity**: jugadores, usuarios, transacciones
- **Navegación rápida** a cualquier recurso

### **Keyboard Shortcuts**
- **Navegación** entre secciones
- **Acciones rápidas** (crear, editar, search)
- **Modal management** (ESC para cerrar)

### **Audit Trail**
- **Log de todas las acciones** administrativas
- **Detalles de cambios** (before/after)
- **Usuario y timestamp** de cada acción
- **Filtrado y búsqueda** en audit logs

## ?? **Checklist de Entregables**

### **Estructura del Proyecto**
- [ ] Setup inicial con Vite + React + TypeScript
- [ ] Configuración de Tailwind CSS
- [ ] Setup de React Query y Zustand
- [ ] Configuración de React Router v6
- [ ] Configuración de ESLint + Prettier

### **Autenticación**
- [ ] Página de login responsive
- [ ] Protected routes con guards
- [ ] Auto-logout en 401
- [ ] Estado de autenticación global

### **Layout Principal**
- [ ] Header con brand selector y user menu
- [ ] Sidebar con navegación collapsible
- [ ] Main content area responsive
- [ ] Dark/light mode toggle

### **Páginas Principales**
- [ ] Dashboard con métricas y gráficos
- [ ] Gestión de jugadores (CRUD completo)
- [ ] Gestión de usuarios backoffice (CRUD completo)
- [ ] Gestión de brands (CRUD completo)
- [ ] Gestión de juegos y configuración por brand
- [ ] Asignaciones cajero-jugador
- [ ] Configuraciones del sistema

### **Componentes Reutilizables**
- [ ] Table component con sorting, filtering, pagination
- [ ] Form components con validación
- [ ] Modal/Dialog components
- [ ] Card components para métricas
- [ ] Status badges y pills
- [ ] Loading states y skeletons
- [ ] Error boundaries y fallbacks

### **Funcionalidades Específicas**
- [ ] Ajuste de saldo de jugadores con confirmación
- [ ] Cambio de status con razones
- [ ] Asignación drag & drop de jugadores
- [ ] Export de datos a CSV/Excel
- [ ] Búsqueda global con shortcuts
- [ ] Notificaciones toast

### **Optimizaciones**
- [ ] Code splitting por rutas
- [ ] Lazy loading de componentes pesados
- [ ] Optimización de re-renders
- [ ] Caching estratégico con React Query
- [ ] Bundle size analysis

## ?? **Criterios de Aceptación**

1. **Funcionalidad Completa**: Todas las operaciones CRUD funcionando
2. **Responsive Design**: Funcional en desktop, tablet y móvil
3. **Performance**: < 3s carga inicial, < 1s navegación
4. **Accesibilidad**: Keyboard navigation, screen reader friendly
5. **UX Consistente**: Flujos intuitivos y feedback visual claro
6. **Error Handling**: Manejo elegante de errores de API
7. **Security**: Validación client-side, protección de rutas
8. **Code Quality**: TypeScript strict, tests unitarios clave
9. **Casino Aesthetic**: Tema apropiado para ambiente de casino
10. **Multi-tenant**: Soporte para múltiples brands/operadores

## ?? **Soporte y Documentación**

Genera también:
- **README.md** con setup instructions
- **DEPLOYMENT.md** con guía de despliegue
- **COMPONENTS.md** documentando componentes reutilizables
- **API-CLIENT.md** documentando la integración con el backend

---

**¡Desarrolla un backoffice de casino profesional, moderno y completamente funcional!** ???

El objetivo es crear una herramienta que permita a los operadores de casino gestionar eficientemente sus sitios, jugadores y operaciones diarias con una experiencia de usuario excepcional.
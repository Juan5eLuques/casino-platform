# Prompt para Claude Sonnet 4.5 - Desarrollo de Backoffice de Casino

## ?? **Objetivo**

Necesito que desarrolles un **Sistema de Backoffice completo** para una plataforma de casino multi-brand usando React + TypeScript. El sistema debe permitir gestionar operadores, brands (sitios de casino), usuarios administrativos, cajeros, jugadores, transacciones y configuraciones.

## ?? **Contexto de la Plataforma**

### **Arquitectura Multi-Brand**
- **Operadores**: Empresas que gestionan m�ltiples casinos
- **Brands**: Sitios de casino individuales (ej: MiCasino, SuperSlots)
- **Usuarios Backoffice**: Administradores y cajeros con diferentes roles
- **Jugadores**: Usuarios finales que juegan en los sitios
- **Separaci�n por dominios**: admin.mycasino.local (backoffice) vs mycasino.local (sitio p�blico)

### **Sistema de Roles y Permisos**
- **SUPER_ADMIN**: Acceso total, gestiona todos los operadores
- **OPERATOR_ADMIN**: Gestiona solo su operador y sus brands
- **CASHIER**: Gestiona solo jugadores asignados a �l

### **Autenticaci�n H�brida**
- **JWT + Cookies HttpOnly**: Backoffice usa cookie "bk.token" con path "/admin"
- **Separaci�n total**: Tokens de backoffice y players son completamente diferentes
- **Brand Resolution**: El host header determina qu� brand se est� gestionando

## ??? **Tecnolog�as y Stack Requerido**

### **Frontend**
- **React 18** con TypeScript
- **Vite** como build tool
- **TanStack Query** (React Query) para estado del servidor
- **React Router v6** para navegaci�n
- **Tailwind CSS** para estilos
- **Headless UI** o **Radix UI** para componentes
- **React Hook Form** + **Zod** para formularios y validaci�n
- **Lucide React** para iconos
- **Recharts** o **Chart.js** para gr�ficos y dashboards

### **Estado y Datos**
- **Zustand** para estado global
- **TanStack Query** para cache y sincronizaci�n de API
- **Axios** para cliente HTTP con interceptors
- **React Hot Toast** para notificaciones

### **Desarrollo**
- **ESLint** + **Prettier** para c�digo limpio
- **TypeScript strict mode**
- **Vite** con HMR para desarrollo r�pido

## ?? **API Backend Disponible**

El backend est� completamente implementado con estos endpoints principales:

### **Autenticaci�n**
```
POST /api/v1/admin/auth/login     - Login de backoffice
GET  /api/v1/admin/auth/me        - Perfil usuario actual
POST /api/v1/admin/auth/logout    - Cerrar sesi�n
```

### **Gesti�n de Operadores** (Solo SUPER_ADMIN)
```
GET  /api/v1/admin/operators      - Listar operadores
POST /api/v1/admin/operators      - Crear operador
PATCH /api/v1/admin/operators/{id} - Actualizar operador
```

### **Gesti�n de Brands**
```
GET  /api/v1/admin/brands         - Listar brands (scoped por operador)
POST /api/v1/admin/brands         - Crear brand
PATCH /api/v1/admin/brands/{id}   - Actualizar brand
PUT  /api/v1/admin/brands/{id}/providers/{code} - Configurar proveedor
```

### **Gesti�n de Usuarios Backoffice**
```
GET  /api/v1/admin/users          - Listar usuarios (scoped por operador)
POST /api/v1/admin/users          - Crear usuario
GET  /api/v1/admin/users/{id}     - Obtener usuario
PATCH /api/v1/admin/users/{id}    - Actualizar usuario
DELETE /api/v1/admin/users/{id}   - Eliminar usuario
```

### **Gesti�n de Jugadores**
```
GET  /api/v1/admin/players        - Listar jugadores (scoped por rol)
POST /api/v1/admin/players        - Crear jugador
GET  /api/v1/admin/players/{id}   - Obtener jugador
PATCH /api/v1/admin/players/{id}/status - Cambiar estado
```

### **Gesti�n de Billeteras**
```
GET  /api/v1/admin/players/{id}/wallet - Informaci�n de billetera
POST /api/v1/admin/players/{id}/wallet/adjust - Ajustar saldo
GET  /api/v1/admin/players/{id}/transactions - Historial transacciones
```

### **Asignaciones Cajero-Jugador**
```
POST /api/v1/admin/cashiers/{cashierId}/players/{playerId} - Asignar
GET  /api/v1/admin/cashiers/{cashierId}/players - Listar asignados
DELETE /api/v1/admin/cashiers/{cashierId}/players/{playerId} - Desasignar
```

### **Gesti�n de Juegos**
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

### **Dise�o y Apariencia**
- **Dark Mode Support**: Tema oscuro/claro toggleable
- **Responsive Design**: Funcional en desktop, tablet y m�vil
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

### **Navegaci�n y Flujos**
- **Sidebar colapsible** con iconos y etiquetas
- **Breadcrumbs** para navegaci�n contextual
- **Brand Selector** en header para cambiar entre brands (si tiene m�ltiples)
- **Search Global** para encontrar jugadores/usuarios r�pidamente
- **Notifications Panel** para alertas y actividad reciente

## ?? **P�ginas y Componentes Principales**

### **1. Dashboard / Resumen**
- **Cards de m�tricas clave**:
  - Total Players Activos
  - Total Balance de Jugadores
  - Transacciones del d�a
  - Usuarios online ahora
  - Revenue del mes
- **Gr�ficos**:
  - Actividad de jugadores (�ltimo mes)
  - Transacciones por d�a
  - Balance total hist�rico
- **Actividad Reciente**:
  - �ltimos registros de jugadores
  - �ltimas transacciones importantes
  - �ltimos logins de usuarios backoffice

### **2. Gesti�n de Jugadores**

#### **Lista de Jugadores**
- **Tabla con filtros avanzados**:
  - Por status (ACTIVE, INACTIVE, SUSPENDED, BANNED)
  - Por brand (si el usuario tiene acceso a m�ltiples)
  - Por cajero asignado
  - Por rango de balance
  - Por fecha de registro
- **Columnas**:
  - Avatar/Inicial
  - Username
  - Email
  - Status (badge colorizado)
  - Balance actual
  - �ltimo login
  - Cajeros asignados
  - Acciones (Ver, Editar, Suspender)

#### **Detalle de Jugador**
- **Informaci�n General**:
  - Datos personales
  - Status y fecha de registro
  - External ID para integraciones
- **Billetera**:
  - Balance actual (destacado)
  - Bot�n "Ajustar Saldo" con modal
  - Historial de transacciones (tabla paginada)
  - Gr�fico de evoluci�n del balance
- **Actividad**:
  - �ltimas sesiones de juego
  - Juegos m�s jugados
  - Estad�sticas de bet/win
- **Gesti�n**:
  - Cambiar status con raz�n
  - Asignar/desasignar cajeros
  - Reset password
  - Notas administrativas

#### **Crear/Editar Jugador**
- **Formulario con validaci�n**:
  - Username (�nico por brand)
  - Email (�nico por brand)
  - External ID para integraciones
  - Password inicial
  - Brand (si el usuario tiene acceso a m�ltiples)
  - Balance inicial
  - Status inicial

### **3. Gesti�n de Usuarios Backoffice**

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
  - �ltimo login
  - Jugadores asignados (solo cajeros)
  - Acciones

#### **Detalle de Usuario**
- **Informaci�n General**:
  - Username y rol
  - Operador asignado
  - Status y fechas
- **Permisos y Accesos**:
  - Brands a los que tiene acceso
  - �ltimos accesos al sistema
- **Jugadores Asignados** (solo para CASHIER):
  - Lista de jugadores bajo su gesti�n
  - Bot�n para asignar/desasignar jugadores
- **Gesti�n**:
  - Cambiar rol (con validaciones)
  - Cambiar status
  - Reset password
  - Cambiar operador (solo SUPER_ADMIN)

#### **Crear/Editar Usuario**
- **Formulario con validaci�n**:
  - Username (�nico por operador)
  - Password inicial
  - Rol con descripci�n de permisos
  - Operador (si SUPER_ADMIN)
  - Status inicial

### **4. Gesti�n de Brands** (Solo SUPER_ADMIN y OPERATOR_ADMIN)

#### **Lista de Brands**
- **Cards o tabla con informaci�n clave**:
  - Logo/nombre del brand
  - C�digo y dominio
  - Status (badge colorizado)
  - N�mero de jugadores activos
  - Balance total de jugadores
  - Fecha de creaci�n

#### **Detalle de Brand**
- **Configuraci�n General**:
  - Nombre, c�digo, dominio
  - Dominio de admin
  - Locale y timezone
  - Status del sitio
- **Apariencia**:
  - Editor de tema (colores primarios/secundarios)
  - Logo upload
  - CSS personalizado (opcional)
- **Configuraciones**:
  - M�ximo monto de apuesta
  - Moneda
  - CORS origins
  - Configuraciones espec�ficas del casino
- **Juegos**:
  - Lista de juegos habilitados
  - Orden de visualizaci�n
  - Tags y categor�as
- **Proveedores**:
  - Configuraci�n de proveedores de juegos
  - Secretos HMAC
  - URLs de webhook
- **Estad�sticas**:
  - Jugadores registrados
  - Revenue del mes
  - Juegos m�s populares

#### **Crear/Editar Brand**
- **Formulario por pasos (wizard)**:
  1. Informaci�n b�sica (nombre, c�digo, dominio)
  2. Configuraci�n t�cnica (CORS, locale, timezone)
  3. Apariencia (tema, colores, logo)
  4. Configuraciones de casino (moneda, l�mites)
  5. Revisi�n y confirmaci�n

### **5. Gesti�n de Juegos**

#### **Lista de Juegos**
- **Tabla con filtros**:
  - Por proveedor
  - Por categor�a (SLOT, TABLE, POKER, etc.)
  - Por estado (habilitado/deshabilitado)
  - Por brands que lo usan
- **Columnas**:
  - Imagen/icon del juego
  - Nombre y c�digo
  - Proveedor
  - Categor�a
  - Brands activos
  - Estado global
  - Acciones

#### **Configuraci�n de Juego por Brand**
- **Lista de brands con toggles**:
  - Habilitar/deshabilitar por brand
  - Orden de visualizaci�n
  - Tags espec�ficos del brand
  - Configuraciones especiales

#### **Crear/Editar Juego**
- **Formulario**:
  - C�digo �nico del juego
  - Nombre display
  - Proveedor
  - Categor�a
  - Imagen/icon upload
  - Metadatos (RTP, volatilidad, etc.)

### **6. Asignaciones Cajero-Jugador**

#### **Vista de Asignaciones**
- **Layout de dos columnas**:
  - Izquierda: Lista de cajeros con contador de jugadores asignados
  - Derecha: Jugadores del cajero seleccionado
- **Drag & Drop**:
  - Arrastrar jugadores entre cajeros
  - Confirmaci�n antes de mover
- **B�squeda y filtros**:
  - Buscar cajeros por username
  - Buscar jugadores por username/email
  - Filtrar jugadores sin asignar

#### **Modal de Asignaci�n M�ltiple**
- **Selecci�n en batch**:
  - Checkboxes para seleccionar m�ltiples jugadores
  - Asignar todos a un cajero espec�fico
  - Confirmaci�n con resumen

### **7. Reportes y Analytics**

#### **Dashboard de Reportes**
- **Filtros de tiempo**:
  - Hoy, Esta semana, Este mes, Rango personalizado
  - Comparaci�n con per�odo anterior
- **M�tricas de Jugadores**:
  - Nuevos registros
  - Jugadores activos
  - Retenci�n de jugadores
- **M�tricas Financieras**:
  - Total deposits/withdrawals
  - GGR (Gross Gaming Revenue)
  - Balance total de jugadores
- **M�tricas de Juegos**:
  - Juegos m�s populares
  - RTP real vs te�rico
  - Sessions y duraci�n promedio

#### **Reportes Detallados**
- **Exportaci�n a CSV/Excel**:
  - Lista de transacciones
  - Lista de jugadores
  - Actividad de usuarios backoffice
- **Reportes programados**:
  - Configurar reportes autom�ticos
  - Env�o por email
  - Frecuencia configurable

### **8. Configuraciones del Sistema**

#### **Configuraciones Generales**
- **Informaci�n de la empresa**
- **Configuraciones de email**
- **Configuraciones de seguridad**:
  - Pol�ticas de password
  - Timeouts de sesi�n
  - Intentos de login fallidos

#### **Configuraciones de Brand** (por brand seleccionado)
- **L�mites y restricciones**
- **Configuraciones de juegos**
- **Integraciones con proveedores**
- **Configuraciones de payment**

## ?? **Configuraci�n T�cnica**

### **Autenticaci�n y Seguridad**
```typescript
// Configuraci�n de Axios con interceptors
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

## ?? **Funcionalidades Espec�ficas del Casino**

### **Gesti�n de Balances**
- **Visualizaci�n en formato de moneda** (centavos ? euros con 2 decimales)
- **Validaciones de saldo** antes de ajustes
- **Razones predefinidas** para ajustes (BONUS, CORRECTION, PROMOTION, etc.)
- **Confirmaciones** para operaciones cr�ticas
- **Audit trail** visible de todas las transacciones

### **Estados de Jugador**
- **ACTIVE**: Verde, puede jugar normalmente
- **INACTIVE**: Gris, cuenta desactivada temporalmente
- **SUSPENDED**: Amarillo, suspendido con raz�n
- **BANNED**: Rojo, baneado permanentemente

### **Roles y Permisos Visuales**
- **SUPER_ADMIN**: Acceso a todo, badge dorado
- **OPERATOR_ADMIN**: Gesti�n de su operador, badge azul
- **CASHIER**: Solo jugadores asignados, badge verde

### **Brand Context Awareness**
- **Brand selector** en header si el usuario tiene acceso a m�ltiples brands
- **Filtros autom�ticos** basados en el brand seleccionado
- **URLs con brand context** para deep linking
- **Datos scoped** por brand autom�ticamente

## ?? **Responsividad y M�vil**

### **Breakpoints**
- **Mobile**: < 640px (sidebar como drawer overlay)
- **Tablet**: 640px - 1024px (sidebar colapsado por defecto)
- **Desktop**: > 1024px (sidebar expandido)

### **Componentes Responsivos**
- **Tablas ? Cards** en m�vil
- **Sidebar ? Bottom navigation** en m�vil
- **Forms ? Pasos separados** en m�vil
- **Dashboards ? Scroll horizontal** para cards

## ?? **Gu�a de Estilos**

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

### **Tipograf�a**
- **Headings**: Inter font family, font-semibold
- **Body**: Inter font family, font-normal
- **Monospace**: JetBrains Mono para n�meros/c�digos

## ?? **Funcionalidades Avanzadas**

### **Real-time Updates**
- **WebSocket connection** para updates en vivo
- **Notificaciones** de nuevos jugadores/transacciones
- **Status updates** en tiempo real

### **Exportaci�n de Datos**
- **Botones de export** en todas las tablas
- **Formatos**: CSV, Excel, PDF
- **Filtros aplicados** se mantienen en export

### **B�squeda Global**
- **Comando + K** para abrir search global
- **B�squeda cross-entity**: jugadores, usuarios, transacciones
- **Navegaci�n r�pida** a cualquier recurso

### **Keyboard Shortcuts**
- **Navegaci�n** entre secciones
- **Acciones r�pidas** (crear, editar, search)
- **Modal management** (ESC para cerrar)

### **Audit Trail**
- **Log de todas las acciones** administrativas
- **Detalles de cambios** (before/after)
- **Usuario y timestamp** de cada acci�n
- **Filtrado y b�squeda** en audit logs

## ?? **Checklist de Entregables**

### **Estructura del Proyecto**
- [ ] Setup inicial con Vite + React + TypeScript
- [ ] Configuraci�n de Tailwind CSS
- [ ] Setup de React Query y Zustand
- [ ] Configuraci�n de React Router v6
- [ ] Configuraci�n de ESLint + Prettier

### **Autenticaci�n**
- [ ] P�gina de login responsive
- [ ] Protected routes con guards
- [ ] Auto-logout en 401
- [ ] Estado de autenticaci�n global

### **Layout Principal**
- [ ] Header con brand selector y user menu
- [ ] Sidebar con navegaci�n collapsible
- [ ] Main content area responsive
- [ ] Dark/light mode toggle

### **P�ginas Principales**
- [ ] Dashboard con m�tricas y gr�ficos
- [ ] Gesti�n de jugadores (CRUD completo)
- [ ] Gesti�n de usuarios backoffice (CRUD completo)
- [ ] Gesti�n de brands (CRUD completo)
- [ ] Gesti�n de juegos y configuraci�n por brand
- [ ] Asignaciones cajero-jugador
- [ ] Configuraciones del sistema

### **Componentes Reutilizables**
- [ ] Table component con sorting, filtering, pagination
- [ ] Form components con validaci�n
- [ ] Modal/Dialog components
- [ ] Card components para m�tricas
- [ ] Status badges y pills
- [ ] Loading states y skeletons
- [ ] Error boundaries y fallbacks

### **Funcionalidades Espec�ficas**
- [ ] Ajuste de saldo de jugadores con confirmaci�n
- [ ] Cambio de status con razones
- [ ] Asignaci�n drag & drop de jugadores
- [ ] Export de datos a CSV/Excel
- [ ] B�squeda global con shortcuts
- [ ] Notificaciones toast

### **Optimizaciones**
- [ ] Code splitting por rutas
- [ ] Lazy loading de componentes pesados
- [ ] Optimizaci�n de re-renders
- [ ] Caching estrat�gico con React Query
- [ ] Bundle size analysis

## ?? **Criterios de Aceptaci�n**

1. **Funcionalidad Completa**: Todas las operaciones CRUD funcionando
2. **Responsive Design**: Funcional en desktop, tablet y m�vil
3. **Performance**: < 3s carga inicial, < 1s navegaci�n
4. **Accesibilidad**: Keyboard navigation, screen reader friendly
5. **UX Consistente**: Flujos intuitivos y feedback visual claro
6. **Error Handling**: Manejo elegante de errores de API
7. **Security**: Validaci�n client-side, protecci�n de rutas
8. **Code Quality**: TypeScript strict, tests unitarios clave
9. **Casino Aesthetic**: Tema apropiado para ambiente de casino
10. **Multi-tenant**: Soporte para m�ltiples brands/operadores

## ?? **Soporte y Documentaci�n**

Genera tambi�n:
- **README.md** con setup instructions
- **DEPLOYMENT.md** con gu�a de despliegue
- **COMPONENTS.md** documentando componentes reutilizables
- **API-CLIENT.md** documentando la integraci�n con el backend

---

**�Desarrolla un backoffice de casino profesional, moderno y completamente funcional!** ???

El objetivo es crear una herramienta que permita a los operadores de casino gestionar eficientemente sus sitios, jugadores y operaciones diarias con una experiencia de usuario excepcional.
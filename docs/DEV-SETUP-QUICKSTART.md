# Configuración de Desarrollo Multi-Brand

## 🚀 **Opción 1: LOCALHOST_DEV Brand (Recomendada - Más Rápida)**

**TL;DR**: Ejecuta `scripts/setup-localhost-dev-brand.sql` y ya puedes usar `http://localhost:5000` con usuarios pre-configurados.

### ✅ **Ventajas:**
- No requiere editar `/etc/hosts`
- Funciona inmediatamente en `localhost`
- Brand completo con usuarios y jerarquía
- Ideal para desarrollo diario

### 📋 **Configuración:**

**1. Ejecutar script:**
```bash
psql -U postgres -d casino_db -f scripts/setup-localhost-dev-brand.sql
```

**2. Usuarios disponibles:**

| Username | Password | Role | Brand | Balance |
|----------|----------|------|-------|---------|
| `superadmin` | `admin123` | SUPER_ADMIN | Global | $100K |
| `admin_localhost` | `admin123` | BRAND_ADMIN | LOCALHOST_DEV | $50K |
| `cashier_localhost` | `cashier123` | CASHIER | LOCALHOST_DEV | $10K |

**Players:** `player1_localhost`, `player2_localhost`, `player3_localhost`

**3. Test:**
```bash
curl -X POST http://localhost:5000/api/v1/admin/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin_localhost","password":"admin123"}'
```

---

## 🔧 **Opción 2: Multi-Brand con /etc/hosts**

**TL;DR**: Para testing de multi-tenancy y aislamiento completo.

### ✅ **Ventajas:**
- Simula producción exactamente
- Cookies completamente aisladas
- Perfecto para testing final

### ❌ **Desventajas:**
- Requiere editar archivo sistema
- Más configuración inicial

### 📋 **Configuración:**

**1. Editar hosts:**
```
127.0.0.1  sitea.local
127.0.0.1  siteb.local
```

**2. Crear brands y usuarios** (ver script como referencia)

---

## 📊 **¿Cuál Usar?**

| Escenario | Opción Recomendada |
|-----------|-------------------|
| Desarrollo diario | LOCALHOST_DEV |
| Testing rápido | LOCALHOST_DEV |
| Testing multi-tenancy | /etc/hosts |
| Testing pre-producción | /etc/hosts |

**Recomendación:** Empieza con LOCALHOST_DEV, cambia a /etc/hosts solo si necesitas probar aislamiento entre brands.

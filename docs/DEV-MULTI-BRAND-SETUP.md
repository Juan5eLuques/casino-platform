# Configuración de Desarrollo Multi-Brand

## ?? **Problema: Localhost No Funciona**

Si intentas hacer login en `http://localhost:5000` o `http://localhost:5173`, recibirás:

```json
{
  "error": "brand_not_resolved",
  "host": "localhost",
  "message": "No brand found for this host. Please configure the brand domain in the database or use a configured domain.",
  "hint_localhost": "For localhost development, configure /etc/hosts with brand domains like '127.0.0.1 sitea.local'"
}
```

**Esto es INTENCIONAL** por seguridad. Localhost NO usa un brand por defecto para evitar bypass de validaciones.

---

## ? **Solución: Configurar Hosts Locales**

### **Paso 1: Editar archivo hosts**

#### **Windows:**
1. Abrir Notepad como **Administrador**
2. Abrir archivo: `C:\Windows\System32\drivers\etc\hosts`
3. Agregar al final:

```
# Desarrollo Multi-Brand Casino
127.0.0.1  sitea.local
127.0.0.1  siteb.local
127.0.0.1  sitec.local
```

4. Guardar y cerrar

#### **Linux/Mac:**
1. Abrir terminal
2. Editar con sudo:
```bash
sudo nano /etc/hosts
```

3. Agregar al final:
```
# Desarrollo Multi-Brand Casino
127.0.0.1  sitea.local
127.0.0.1  siteb.local
127.0.0.1  sitec.local
```

4. Guardar: `Ctrl+X`, `Y`, `Enter`

---

### **Paso 2: Crear Brands en Base de Datos**

Ejecutar en PostgreSQL:

```sql
-- Brand A (Site A Local)
INSERT INTO "Brands" (
  "Id", "Code", "Name", "Locale", "Domain", "AdminDomain", 
  "CorsOrigins", "Status", "CreatedAt", "UpdatedAt"
)
VALUES (
  gen_random_uuid(),
  'SITEA_LOCAL',
  'Site A Local Development',
  'en-US',
  'sitea.local',
  'sitea.local',
  'http://sitea.local:5173,http://sitea.local:5000',
  'ACTIVE',
  NOW(),
  NOW()
);

-- Brand B (Site B Local)
INSERT INTO "Brands" (
  "Id", "Code", "Name", "Locale", "Domain", "AdminDomain", 
  "CorsOrigins", "Status", "CreatedAt", "UpdatedAt"
)
VALUES (
  gen_random_uuid(),
  'SITEB_LOCAL',
  'Site B Local Development',
  'en-US',
  'siteb.local',
  'siteb.local',
  'http://siteb.local:5173,http://siteb.local:5000',
  'ACTIVE',
  NOW(),
  NOW()
);

-- Brand C (Site C Local)
INSERT INTO "Brands" (
  "Id", "Code", "Name", "Locale", "Domain", "AdminDomain", 
  "CorsOrigins", "Status", "CreatedAt", "UpdatedAt"
)
VALUES (
  gen_random_uuid(),
  'SITEC_LOCAL',
  'Site C Local Development',
  'en-US',
  'sitec.local',
  'sitec.local',
  'http://sitec.local:5173,http://sitec.local:5000',
  'ACTIVE',
  NOW(),
  NOW()
);
```

---

### **Paso 3: Crear Usuarios por Brand**

```sql
-- Obtener IDs de brands
DO $$
DECLARE
  brand_a_id UUID;
  brand_b_id UUID;
  brand_c_id UUID;
BEGIN
  -- Get brand IDs
  SELECT "Id" INTO brand_a_id FROM "Brands" WHERE "Code" = 'SITEA_LOCAL';
  SELECT "Id" INTO brand_b_id FROM "Brands" WHERE "Code" = 'SITEB_LOCAL';
  SELECT "Id" INTO brand_c_id FROM "Brands" WHERE "Code" = 'SITEC_LOCAL';

  -- Admin para Site A
  INSERT INTO "BackofficeUsers" (
    "Id", "Username", "PasswordHash", "Role", "Status", 
    "BrandId", "WalletBalance", "CreatedAt", "UpdatedAt"
  )
  VALUES (
    gen_random_uuid(),
    'admin_sitea',
    '$2a$11$YourHashedPasswordHere',  -- password: "password123"
    'BRAND_ADMIN',
    'ACTIVE',
    brand_a_id,
    1000.00,
    NOW(),
    NOW()
  );

  -- Admin para Site B
  INSERT INTO "BackofficeUsers" (
    "Id", "Username", "PasswordHash", "Role", "Status", 
    "BrandId", "WalletBalance", "CreatedAt", "UpdatedAt"
  )
  VALUES (
    gen_random_uuid(),
    'admin_siteb',
    '$2a$11$YourHashedPasswordHere',  -- password: "password123"
    'BRAND_ADMIN',
    'ACTIVE',
    brand_b_id,
    1000.00,
    NOW(),
    NOW()
  );

  -- SUPER_ADMIN (sin brand, puede acceder a todos)
  INSERT INTO "BackofficeUsers" (
    "Id", "Username", "PasswordHash", "Role", "Status", 
    "BrandId", "WalletBalance", "CreatedAt", "UpdatedAt"
  )
  VALUES (
    gen_random_uuid(),
    'superadmin',
    '$2a$11$YourHashedPasswordHere',  -- password: "password123"
    'SUPER_ADMIN',
    'ACTIVE',
    NULL,  -- Sin brand específico
    10000.00,
    NOW(),
    NOW()
  );
END $$;
```

**Nota**: Reemplaza `$2a$11$YourHashedPasswordHere` con el hash real. Para generar:
```bash
# Usar bcrypt online o en código C#
BCrypt.Net.BCrypt.HashPassword("password123")
```

---

### **Paso 4: Verificar Configuración**

#### **Test 1: Resolver Brand**
```bash
curl http://sitea.local:5000/health
# Respuesta: OK
```

#### **Test 2: Login Admin Site A**
```bash
curl -X POST http://sitea.local:5000/api/v1/admin/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin_sitea","password":"password123"}'

# ? Esperado: {"ok":true,"user":{...},"brand":{"brandCode":"SITEA_LOCAL"}}
```

#### **Test 3: Login Admin Site A en Site B (debe fallar)**
```bash
curl -X POST http://siteb.local:5000/api/v1/admin/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin_sitea","password":"password123"}'

# ? Esperado: 403 Brand Mismatch
{
  "title": "Brand Mismatch",
  "detail": "This user account is not authorized for this brand/site.",
  "status": 403
}
```

#### **Test 4: SUPER_ADMIN en ambos sites**
```bash
# Login en Site A
curl -X POST http://sitea.local:5000/api/v1/admin/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"superadmin","password":"password123"}'
# ? SUCCESS

# Login en Site B
curl -X POST http://siteb.local:5000/api/v1/admin/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"superadmin","password":"password123"}'
# ? SUCCESS (diferentes tokens)
```

---

## ?? **Acceso desde Frontend**

### **Configuración de Vite/React**

Si usas Vite, NO necesitas configuración especial. Solo accede a:

```
http://sitea.local:5173  ?  API: http://sitea.local:5000
http://siteb.local:5173  ?  API: http://siteb.local:5000
```

### **Proxy en Desarrollo (Opcional)**

Si quieres usar proxy en Vite:

```typescript
// vite.config.ts
export default defineConfig({
  server: {
    host: '0.0.0.0',  // Permitir acceso desde cualquier host local
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        // El Origin será el host del frontend (sitea.local:5173)
      }
    }
  }
})
```

---

## ?? **Troubleshooting**

### **Error: "Brand not resolved" en sitea.local**

**Causa**: El dominio no está en la base de datos

**Solución**:
```sql
SELECT "Code", "Domain", "AdminDomain" FROM "Brands";
-- Verificar que 'sitea.local' esté en Domain o AdminDomain
```

### **Error: Login exitoso pero token inválido**

**Causa**: Cookie con domain incorrecto

**Solución**:
1. Abrir DevTools ? Application ? Cookies
2. Verificar que `bk.token` tenga `Domain = sitea.local`
3. Si no, limpiar cookies y volver a hacer login

### **Error: CORS en sitea.local**

**Causa**: `CorsOrigins` no incluye el puerto

**Solución**:
```sql
UPDATE "Brands" 
SET "CorsOrigins" = 'http://sitea.local:5173,http://sitea.local:5000'
WHERE "Code" = 'SITEA_LOCAL';
```

### **Error: "Hash verification failed"**

**Causa**: Password hash incorrecto en base de datos

**Solución**: Generar hash correcto con BCrypt
```csharp
// En C# (usar LINQPad o consola app)
var hash = BCrypt.Net.BCrypt.HashPassword("password123");
Console.WriteLine(hash);
// Copiar el hash a la base de datos
```

---

## ?? **Matriz de Acceso**

| Usuario | Brand | Login en sitea.local | Login en siteb.local |
|---------|-------|---------------------|---------------------|
| admin_sitea | SITEA_LOCAL | ? SUCCESS | ? 403 Brand Mismatch |
| admin_siteb | SITEB_LOCAL | ? 403 Brand Mismatch | ? SUCCESS |
| superadmin | NULL | ? SUCCESS | ? SUCCESS |

---

## ? **Checklist de Configuración**

- [ ] Archivo `/etc/hosts` editado con dominios locales
- [ ] Brands creados en base de datos con dominios `.local`
- [ ] `CorsOrigins` incluye puerto correcto (`:5173`)
- [ ] Usuarios creados con `BrandId` correcto
- [ ] Password hash generado con BCrypt
- [ ] Test de login exitoso en cada brand
- [ ] Test de brand mismatch funciona (403)
- [ ] Cookies tienen `Domain` correcto en DevTools

---

## ?? **Resultado Final**

```
? sitea.local:5173 ? admin_sitea ? Login SUCCESS
? siteb.local:5173 ? admin_siteb ? Login SUCCESS
? sitec.local:5173 ? superadmin ? Login SUCCESS

? sitea.local:5173 ? admin_siteb ? 403 Brand Mismatch
? siteb.local:5173 ? admin_sitea ? 403 Brand Mismatch

? Sesiones completamente aisladas por brand
? No hay bypass de validación
? Seguridad multi-brand garantizada
```

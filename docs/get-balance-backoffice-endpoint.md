# Obtener balance de usuario (Backoffice)

## Endpoint

**GET** `/api/v1/admin/wallet/balance`

### Query Params
- `userId` (string, requerido): GUID del usuario backoffice
- `userType` (string, requerido): Siempre usar el valor `BACKOFFICE` desde el backoffice

### Ejemplo de request
```
GET /api/v1/admin/wallet/balance?userId=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx&userType=BACKOFFICE
```

### Ejemplo de respuesta exitosa (200)
```json
{
  "userId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "userType": "BACKOFFICE",
  "username": "admin1",
  "walletBalance": 1500.00
}
```

### Respuestas de error
- 404: Usuario no encontrado
- 400: Parámetros inválidos

### Notas
- El parámetro `userType` debe ser siempre `BACKOFFICE` para usuarios del backoffice.
- El parámetro `userId` debe ser el GUID del usuario autenticado.
- El endpoint retorna el balance actual del usuario solicitado.

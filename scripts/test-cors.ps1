# Script para probar CORS y diagnosticar problemas
# Ejecutar este script después de aplicar las correcciones

Write-Host "?? Diagnóstico de CORS para Casino Platform" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green

# Configuración
$apiUrl = "https://admin.bet30.local:7182"
$origin = "http://admin.bet30.local:5173"
$loginEndpoint = "$apiUrl/api/v1/admin/auth/login"

Write-Host "`n?? Configuración de prueba:" -ForegroundColor Yellow
Write-Host "API URL: $apiUrl" -ForegroundColor White
Write-Host "Origin:  $origin" -ForegroundColor White
Write-Host "Endpoint: $loginEndpoint" -ForegroundColor White

Write-Host "`n?? PASO 1: Verificando conectividad básica..." -ForegroundColor Yellow

try {
    # Test básico de conectividad
    $response = Invoke-WebRequest -Uri "$apiUrl/health" -Method GET -UseBasicParsing -ErrorAction Stop
    Write-Host "? Health check: $($response.StatusCode)" -ForegroundColor Green
}
catch {
    Write-Host "? Health check falló: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "??  Asegúrate de que la API esté corriendo en $apiUrl" -ForegroundColor Yellow
}

Write-Host "`n?? PASO 2: Probando request OPTIONS (preflight)..." -ForegroundColor Yellow

try {
    # Simular preflight request
    $headers = @{
        'Origin' = $origin
        'Access-Control-Request-Method' = 'POST'
        'Access-Control-Request-Headers' = 'content-type'
    }
    
    $response = Invoke-WebRequest -Uri $loginEndpoint -Method OPTIONS -Headers $headers -UseBasicParsing -ErrorAction Stop
    
    Write-Host "? OPTIONS request: $($response.StatusCode)" -ForegroundColor Green
    
    # Verificar headers CORS
    $corsOrigin = $response.Headers['Access-Control-Allow-Origin']
    $corsCredentials = $response.Headers['Access-Control-Allow-Credentials']
    $corsMethods = $response.Headers['Access-Control-Allow-Methods']
    
    Write-Host "?? Headers CORS recibidos:" -ForegroundColor Cyan
    Write-Host "   Access-Control-Allow-Origin: $corsOrigin" -ForegroundColor White
    Write-Host "   Access-Control-Allow-Credentials: $corsCredentials" -ForegroundColor White
    Write-Host "   Access-Control-Allow-Methods: $corsMethods" -ForegroundColor White
    
    # Verificar que el origin sea específico, no wildcard
    if ($corsOrigin -eq "*") {
        Write-Host "? PROBLEMA: Origin es wildcard (*) pero credentials está habilitado" -ForegroundColor Red
        Write-Host "   Esto causará el error de CORS que estás viendo" -ForegroundColor Red
    }
    elseif ($corsOrigin -eq $origin) {
        Write-Host "? Origin específico correcto: $corsOrigin" -ForegroundColor Green
    }
    else {
        Write-Host "??  Origin no coincide. Esperado: $origin, Recibido: $corsOrigin" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "? OPTIONS request falló: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "   Código de estado: $statusCode" -ForegroundColor Red
    }
}

Write-Host "`n?? PASO 3: Probando request POST real..." -ForegroundColor Yellow

try {
    # Simular request POST con credenciales
    $headers = @{
        'Origin' = $origin
        'Content-Type' = 'application/json'
    }
    
    $body = @{
        username = "superadmin"
        password = "admin123"
    } | ConvertTo-Json
    
    $response = Invoke-WebRequest -Uri $loginEndpoint -Method POST -Headers $headers -Body $body -UseBasicParsing -ErrorAction Stop
    
    Write-Host "? POST request exitoso: $($response.StatusCode)" -ForegroundColor Green
    
    # Verificar cookies
    $cookies = $response.Headers['Set-Cookie']
    if ($cookies -like "*bk.token*") {
        Write-Host "? Cookie de autenticación establecida" -ForegroundColor Green
    }
    else {
        Write-Host "??  No se estableció cookie de autenticación" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "? POST request falló: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "   Código de estado: $statusCode" -ForegroundColor Red
        
        try {
            $errorContent = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($errorContent)
            $errorText = $reader.ReadToEnd()
            Write-Host "   Respuesta: $errorText" -ForegroundColor Red
        }
        catch {
            Write-Host "   No se pudo leer el contenido del error" -ForegroundColor Red
        }
    }
}

Write-Host "`n?? RECOMENDACIONES:" -ForegroundColor Green
Write-Host "1. Si OPTIONS falla: Verifica que la API esté corriendo y accesible" -ForegroundColor White
Write-Host "2. Si Origin es '*': El middleware está usando wildcard incorrectamente" -ForegroundColor White
Write-Host "3. Si Origin no coincide: Verifica la configuración del brand en la BD" -ForegroundColor White
Write-Host "4. Si POST falla: Revisa los logs de la API para más detalles" -ForegroundColor White

Write-Host "`n?? Para revisar logs de la API:" -ForegroundColor Yellow
Write-Host "dotnet watch --project apps/api/Casino.Api" -ForegroundColor Cyan

Write-Host "`n?? Para verificar base de datos:" -ForegroundColor Yellow
Write-Host "psql [connection_string] -f scripts/verify-cors-config.sql" -ForegroundColor Cyan
Write-Host "Starting Casino Platform API..." -ForegroundColor Green
Write-Host "Database connection: Railway PostgreSQL" -ForegroundColor Yellow
Write-Host "Environment: Development" -ForegroundColor Yellow
Write-Host ""
Write-Host "Building project..." -ForegroundColor Cyan
dotnet build apps/api/Casino.Api --configuration Release
Write-Host ""
Write-Host "Starting API server..." -ForegroundColor Green
Write-Host "API will be available at: http://localhost:5000" -ForegroundColor Yellow
Write-Host "Swagger UI at: http://localhost:5000/swagger" -ForegroundColor Yellow
Write-Host ""
Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Red
Write-Host ""
dotnet run --project apps/api/Casino.Api --configuration Release --urls "http://localhost:5000"
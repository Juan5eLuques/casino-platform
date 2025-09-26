#!/bin/bash
echo "Starting Casino Platform API..."
echo "Database connection: Railway PostgreSQL"
echo "Environment: Development"
echo ""
echo "Building project..."
dotnet build apps/api/Casino.Api --configuration Release
echo ""
echo "Starting API server..."
echo "API will be available at: http://localhost:5000"
echo "Swagger UI at: http://localhost:5000/swagger"
echo ""
dotnet run --project apps/api/Casino.Api --configuration Release --urls "http://localhost:5000"
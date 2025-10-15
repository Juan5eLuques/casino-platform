# Consulta https://aka.ms/customizecontainer

# ===== Base (runtime) =====
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
# Railway inyecta $PORT; ASP.NET debe escuchar en 0.0.0.0:$PORT
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
EXPOSE 8080
EXPOSE 8081

# ===== Build =====
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copiamos los .csproj primero para cachear el restore por capas
COPY apps/api/Casino.Api/Casino.Api.csproj apps/api/Casino.Api/
COPY apps/Casino.Application/Casino.Application.csproj apps/Casino.Application/
COPY apps/Casino.Domain/Casino.Domain.csproj apps/Casino.Domain/
COPY apps/Casino.Infrastructure/Casino.Infrastructure.csproj apps/Casino.Infrastructure/

# Restauramos SOLO el proyecto web
RUN dotnet restore "apps/api/Casino.Api/Casino.Api.csproj"

# Copiamos el resto del código
COPY . .

# Compilamos SOLO el proyecto web
WORKDIR /src/apps/api/Casino.Api
RUN dotnet build "Casino.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# ===== Publish =====
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/apps/api/Casino.Api
RUN dotnet publish "Casino.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ===== Final (runtime) =====
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Casino.Api.dll"]

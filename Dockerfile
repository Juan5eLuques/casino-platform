# ===== Base (runtime) =====
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# ===== Build =====
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY apps/api/Casino.Api/Casino.Api.csproj apps/api/Casino.Api/
COPY apps/Casino.Application/Casino.Application.csproj apps/Casino.Application/
COPY apps/Casino.Domain/Casino.Domain.csproj apps/Casino.Domain/
COPY apps/Casino.Infrastructure/Casino.Infrastructure.csproj apps/Casino.Infrastructure/
RUN dotnet restore apps/api/Casino.Api/Casino.Api.csproj
COPY . .
WORKDIR /src/apps/api/Casino.Api
RUN dotnet publish Casino.Api.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ===== Final =====
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
# ⚠️ Forzamos URLS y ASPNETCORE_URLS con el $PORT de Railway
ENTRYPOINT ["sh","-lc","URLS=http://0.0.0.0:${PORT:-8080} ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} exec dotnet Casino.Api.dll"]

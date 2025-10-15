# Etapa de build
using static System.Net.WebRequestMethods;

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
# Restaurar y publicar (ajustá el nombre del .csproj si aplica)
RUN dotnet restore
RUN dotnet publish -c Release -o /app/out

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out ./

# Railway expone un $PORT. ASP.NET debe escuchar en 0.0.0.0:$PORT
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
# Puertos "de cortesía" (no obligatorio, Railway hace port mapping)
EXPOSE 8080
ENTRYPOINT ["dotnet", "TuProyecto.dll"]# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
# Restaurar y publicar (ajustá el nombre del .csproj si aplica)
RUN dotnet restore
RUN dotnet publish -c Release -o /app/out

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out ./

# Railway expone un $PORT. ASP.NET debe escuchar en 0.0.0.0:$PORT
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
# Puertos "de cortesía" (no obligatorio, Railway hace port mapping)
EXPOSE 8080
ENTRYPOINT ["dotnet", "TuProyecto.dll"]
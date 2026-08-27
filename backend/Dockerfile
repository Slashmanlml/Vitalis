# ---------------------------------------------------------------------------
# API de Vitalis (ASP.NET Core 8)
#
# Construccion en dos etapas: la primera compila con el SDK completo (~800 MB),
# la segunda copia solo el resultado sobre la imagen de ejecucion (~200 MB). La
# imagen final no lleva compilador ni codigo fuente.
#
# El contexto de construccion es la carpeta backend/ (ver docker-compose.yml).
# ---------------------------------------------------------------------------

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /origen

# Primero solo los .csproj y la solucion: restaurar dependencias es lo lento, y
# si el codigo cambia pero las dependencias no, Docker reutiliza esta capa.
COPY Vitalis.sln ./
COPY src/Vitalis.Domain/Vitalis.Domain.csproj                 src/Vitalis.Domain/
COPY src/Vitalis.Application/Vitalis.Application.csproj       src/Vitalis.Application/
COPY src/Vitalis.Infrastructure/Vitalis.Infrastructure.csproj src/Vitalis.Infrastructure/
COPY src/Vitalis.Api/Vitalis.Api.csproj                       src/Vitalis.Api/
COPY tests/Vitalis.Tests/Vitalis.Tests.csproj                 tests/Vitalis.Tests/
RUN dotnet restore src/Vitalis.Api/Vitalis.Api.csproj

# Ahora si el codigo
COPY . .
RUN dotnet publish src/Vitalis.Api/Vitalis.Api.csproj -c Release -o /publicado --no-restore

# --- imagen final ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Usuario sin privilegios: si alguien logra ejecutar algo dentro del contenedor,
# no lo hace como root.
RUN adduser --disabled-password --gecos "" vitalis \
 && mkdir -p /app/wwwroot/uploads \
 && chown -R vitalis:vitalis /app

COPY --from=build --chown=vitalis:vitalis /publicado .

USER vitalis
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Vitalis.Api.dll"]

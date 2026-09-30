FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS compilacion
WORKDIR /fuente

COPY Directory.Build.props Directory.Packages.props Shapi.slnx ./
COPY src/ src/
RUN dotnet restore src/Shapi.Trabajador/Shapi.Trabajador.csproj
RUN dotnet publish src/Shapi.Trabajador/Shapi.Trabajador.csproj \
    --configuration Release \
    --no-restore \
    --output /aplicacion \
    /p:UseAppHost=false

# Data Protection vive en Microsoft.AspNetCore.App; el trabajador debe compartir
# exactamente el mismo marco y el mismo volumen de llaves que la API.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
RUN mkdir -p /var/lib/shapi/dpkeys && chown -R app:app /var/lib/shapi
WORKDIR /aplicacion
COPY --from=compilacion /aplicacion .
USER app
ENTRYPOINT ["dotnet", "Shapi.Trabajador.dll"]

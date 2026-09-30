FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS compilacion
WORKDIR /fuente

COPY Directory.Build.props Directory.Packages.props Shapi.slnx ./
COPY src/ src/
RUN dotnet restore src/Shapi.Compuerta/Shapi.Compuerta.csproj
RUN dotnet publish src/Shapi.Compuerta/Shapi.Compuerta.csproj \
    --configuration Release \
    --no-restore \
    --output /aplicacion \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
RUN apk add --no-cache curl
WORKDIR /aplicacion
COPY --from=compilacion /aplicacion .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Shapi.Compuerta.dll"]

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS compilacion
WORKDIR /fuente
COPY Directory.Build.props Directory.Packages.props ./
COPY origenes-demo/agro-precios/AgroPrecios.csproj origenes-demo/agro-precios/
RUN dotnet restore origenes-demo/agro-precios/AgroPrecios.csproj
COPY origenes-demo/agro-precios/ origenes-demo/agro-precios/
RUN dotnet publish origenes-demo/agro-precios/AgroPrecios.csproj --no-restore -c Release -o /aplicacion

FROM mcr.microsoft.com/dotnet/aspnet:10.0
# curl solo para el healthcheck de /salud (infra/compose.yml).
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /aplicacion
COPY --from=compilacion /aplicacion .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "AgroPrecios.dll"]

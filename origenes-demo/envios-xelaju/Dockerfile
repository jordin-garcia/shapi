FROM mcr.microsoft.com/dotnet/sdk:10.0 AS compilacion
WORKDIR /fuente
COPY Directory.Build.props Directory.Packages.props ./
COPY origenes-demo/envios-xelaju/EnviosXelaju.csproj origenes-demo/envios-xelaju/
RUN dotnet restore origenes-demo/envios-xelaju/EnviosXelaju.csproj
COPY origenes-demo/envios-xelaju/ origenes-demo/envios-xelaju/
RUN dotnet publish origenes-demo/envios-xelaju/EnviosXelaju.csproj --no-restore -c Release -o /aplicacion

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /aplicacion
COPY --from=compilacion /aplicacion .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "EnviosXelaju.dll"]

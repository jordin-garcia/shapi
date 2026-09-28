# Manual técnico

## Entorno de desarrollo

### Requisitos

- Docker Engine con Docker Compose v2.
- Node.js 24 o posterior para ejecutar la verificación.
- .NET 10 y pnpm para ejecutar la aplicación completa.

### Configuración inicial

Copie la configuración de ejemplo antes de levantar los servicios:

```powershell
Copy-Item .env.example .env
```

En Linux o macOS, use `cp .env.example .env`. Los valores incluidos son solo para
desarrollo local. No confirme el archivo `.env` ni use estas credenciales en otro
ambiente.

Todos los comandos de Compose pasan `--env-file .env`. Sin esa opción, Compose
busca el `.env` en `infra/`, la carpeta de `compose.yml`, e ignora el de la raíz.
Si falta el `.env`, Compose se detiene con `couldn't find env file`.

Si otro PostgreSQL ya ocupa el puerto 5432 del equipo, cambie
`SHAPI_POSTGRES_PUERTO` (por ejemplo, a `5433`) y ponga el mismo número en
`Port=` de `SHAPI_POSTGRES_CADENA`.

### Levantar y verificar la infraestructura

Desde la raíz del repositorio, ejecute:

```bash
docker compose --env-file .env -f infra/compose.yml up -d
node infra/verificar.mjs
```

El primer comando inicia PostgreSQL, Redis, Mailpit, Caddy y los dos orígenes de
demostración. El segundo valida la configuración, la salud de los seis
contenedores, que solo publiquen puertos en `127.0.0.1`, HTTPS, las cabeceras de
seguridad y la bandeja de correo. Ejecute la verificación antes de iniciar .NET o
Vite: el verificador ocupa temporalmente los puertos 5080, 5090, 5173 y 5174 y
fallará con `EADDRINUSE` si alguno ya está en uso.

Los procesos de Shapi y Vite se ejecutan en el equipo para conservar la recarga
en caliente. .NET no lee el archivo `.env`: sin sus variables, la API no
encuentra la base de datos (`SHAPI_POSTGRES_CADENA`) y no puede aplicar las
migraciones al iniciar. Cargue el `.env` en cada terminal antes de
`dotnet run`. Los valores no van entre comillas y la cadena de conexión lleva
`;`, así que no use `source .env`:

```powershell
# PowerShell
Get-Content .env | ForEach-Object { if ($_ -match '^\s*([A-Z_][A-Z0-9_]*)=(.*)$') { Set-Item "env:$($Matches[1])" $Matches[2] } }
```

```bash
# bash (Linux, macOS o Git Bash)
while IFS= read -r linea || [[ -n $linea ]]; do [[ $linea =~ ^([A-Z_][A-Z0-9_]*)=(.*)$ ]] && export "${BASH_REMATCH[1]}=${BASH_REMATCH[2]%$'\r'}"; done < .env
```

Después, en esas mismas terminales:

```bash
dotnet run --project src/Shapi.Api
dotnet run --project src/Shapi.Compuerta
dotnet run --project src/Shapi.Trabajador
cd frontend && pnpm install && pnpm dev
```

En Linux con Docker Engine nativo, `host.docker.internal` apunta al puente de
Docker y no puede alcanzar procesos que escuchen solo en `localhost`. Inicie los
procesos HTTP en todas las interfaces para que Caddy pueda conectarse:

```bash
dotnet run --project src/Shapi.Api --urls http://0.0.0.0:5080
dotnet run --project src/Shapi.Compuerta --urls http://0.0.0.0:5090
cd frontend && pnpm dev -- --host 0.0.0.0
```

La configuración de Vite usa `server.host: "0.0.0.0"` por la misma razón: en
Linux, Caddy llega a Vite por el puente de Docker. No fija
`server.hmr.clientPort`: el navegador abre la recarga en caliente en el puerto
de la página, 443 detrás de Caddy o 5173 y 5174 sin él. Estos puertos son exclusivamente de desarrollo; no exponga este
entorno en una red no confiable.

### Confiar en la autoridad certificadora local

Caddy guarda su autoridad certificadora en el volumen `caddydata`. Después de
levantar el entorno por primera vez, importe la raíz una sola vez.

En Windows PowerShell:

```powershell
docker compose --env-file .env -f infra/compose.yml cp borde:/data/caddy/pki/authorities/local/root.crt .\caddy-root.crt
Import-Certificate -FilePath .\caddy-root.crt -CertStoreLocation Cert:\CurrentUser\Root
Remove-Item .\caddy-root.crt
```

Chrome y Edge usan el almacén de certificados de Windows. En Firefox, active
`security.enterprise_roots.enabled` en `about:config`.

En Linux:

```bash
docker compose --env-file .env -f infra/compose.yml cp borde:/data/caddy/pki/authorities/local/root.crt ./caddy-root.crt
sudo cp caddy-root.crt /usr/local/share/ca-certificates/caddy-shapi.crt
sudo update-ca-certificates
sudo apt install -y libnss3-tools
certutil -d sql:$HOME/.pki/nssdb -A -t "C,," -n caddy-shapi -i caddy-root.crt
rm caddy-root.crt
```

Firefox permite importar `caddy-root.crt` desde **Ajustes → Privacidad y
seguridad → Certificados → Ver certificados → Autoridades**.

Con la raíz instalada, [https://correo.shapi.localhost](https://correo.shapi.localhost)
abre Mailpit con un certificado confiable.

### Puertos y direcciones

| Servicio | Dirección local | Dirección mediante Caddy |
|---|---|---|
| API de control | `http://localhost:5080` | `https://shapi.localhost/api/*` |
| Compuerta | `http://localhost:5090` | `https://{sub}.api.shapi.localhost` |
| Panel | `http://localhost:5173` | `https://shapi.localhost` |
| Portal | `http://localhost:5174` | `https://{sub}.shapi.localhost` |
| PostgreSQL | `localhost:5432` (o `SHAPI_POSTGRES_PUERTO`) | — |
| Redis | `localhost:6379` | — |
| Mailpit SMTP | `localhost:1025` | — |
| Mailpit web | `http://localhost:8025` | `https://correo.shapi.localhost` |
| Origen Envíos | `http://localhost:5101` | Mediante la compuerta |
| Origen Agro | `http://localhost:5102` | Mediante la compuerta |

Todos los puertos de la tabla que publica Compose se abren solo en `127.0.0.1`:
el entorno no queda expuesto a la red local. Los dominios `*.shapi.localhost`
resuelven siempre a la propia máquina, así que desde otro equipo no se usarían
de todos modos.

### Apagar o reiniciar

Detenga los contenedores sin perder los datos:

```bash
docker compose --env-file .env -f infra/compose.yml down
```

Para reiniciar, ejecute `down` y luego `docker compose --env-file .env -f infra/compose.yml up -d`.
Si necesita borrar también PostgreSQL, Redis y los certificados locales, ejecute:

```bash
docker compose --env-file .env -f infra/compose.yml down -v
```

Después de `down -v` deberá volver a importar la raíz de Caddy.

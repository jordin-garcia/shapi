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

### Levantar y verificar la infraestructura

Desde la raíz del repositorio, ejecute:

```bash
docker compose -f infra/compose.yml up -d
node infra/verificar.mjs
```

El primer comando inicia PostgreSQL, Redis, Mailpit y Caddy. El segundo valida la
configuración, la salud de los cuatro contenedores, HTTPS, las cabeceras de
seguridad y la bandeja de correo.

Los procesos de Shapi y Vite se ejecutan en el equipo para conservar la recarga
en caliente:

```bash
dotnet run --project src/Shapi.Api
dotnet run --project src/Shapi.Compuerta
dotnet run --project src/Shapi.Trabajador
cd frontend && pnpm install && pnpm dev
```

### Confiar en la autoridad certificadora local

Caddy guarda su autoridad certificadora en el volumen `caddydata`. Después de
levantar el entorno por primera vez, importe la raíz una sola vez.

En Windows PowerShell:

```powershell
docker compose -f infra/compose.yml cp borde:/data/caddy/pki/authorities/local/root.crt .\caddy-root.crt
Import-Certificate -FilePath .\caddy-root.crt -CertStoreLocation Cert:\CurrentUser\Root
Remove-Item .\caddy-root.crt
```

Chrome y Edge usan el almacén de certificados de Windows. En Firefox, active
`security.enterprise_roots.enabled` en `about:config`.

En Linux:

```bash
docker compose -f infra/compose.yml cp borde:/data/caddy/pki/authorities/local/root.crt ./caddy-root.crt
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
| PostgreSQL | `localhost:5432` | — |
| Redis | `localhost:6379` | — |
| Mailpit SMTP | `localhost:1025` | — |
| Mailpit web | `http://localhost:8025` | `https://correo.shapi.localhost` |
| Origen Envíos | `http://localhost:5101` | Mediante la compuerta |
| Origen Agro | `http://localhost:5102` | Mediante la compuerta |

### Apagar o reiniciar

Detenga los contenedores sin perder los datos:

```bash
docker compose -f infra/compose.yml down
```

Para reiniciar, ejecute `down` y luego `docker compose -f infra/compose.yml up -d`.
Si necesita borrar también PostgreSQL, Redis y los certificados locales, ejecute:

```bash
docker compose -f infra/compose.yml down -v
```

Después de `down -v` deberá volver a importar la raíz de Caddy.

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

## Ambiente productivo simulado

Este ambiente ejecuta la API de control, la compuerta, el trabajador y los dos
frontends dentro de contenedores en configuración `Release`. Antes de levantarlo,
copie `.env.example` a `.env` y cambie las credenciales de administración y de
PostgreSQL. El archivo `.env` no se confirma en Git.

### Levantar y verificar

Desde la raíz del repositorio, construya las imágenes y levante todo el ambiente:

```bash
docker compose --env-file .env -f infra/compose.yml -f infra/compose.prod.yml up -d --build
node infra/verificar.mjs
```

La verificación espera que API, compuerta, borde, PostgreSQL, Redis, Mailpit y
los dos orígenes estén sanos. El trabajador no expone HTTP y permanece en estado
`running`; JZ-12 agregará su latido `salud:trabajador` en Redis. Solamente los
puertos 80 y 443 del borde quedan publicados, ambos en `127.0.0.1`.

La API aplica las migraciones al iniciar. La API y el trabajador comparten el
volumen `dpkeys`, que conserva las llaves con las que se protegen los secretos de
origen.

El borde comparte con el entorno de desarrollo el volumen `caddydata`, así que usa
la misma autoridad certificadora que ya importó. Caddy corre sin privilegios, con el
usuario `caddy`: al arrancar, el contenedor le da los archivos del volumen, aunque
los haya creado el borde de desarrollo, que corre como root.

### Sembrar la demostración

Cargue los datos de demostración con:

```bash
docker compose --env-file .env -f infra/compose.yml -f infra/compose.prod.yml exec trabajador dotnet Shapi.Trabajador.dll sembrar-demo
```

El comando exige `SHAPI_MODO_DEMO=true`, es idempotente y toma las URL de los
orígenes de `SHAPI_URL_ORIGEN_ENVIOS` y `SHAPI_URL_ORIGEN_AGRO`. Sin esas
variables usa `localhost:5101` y `localhost:5102` en desarrollo. En el ambiente
productivo simulado, `infra/compose.prod.yml` las fija en `origen-envios:8080` y
`origen-agro:8080`, sin tomar las del `.env`, que son las de desarrollo. Ese
ambiente es el de la exposición, así que `SHAPI_MODO_DEMO` vale `true` por defecto
(06 §7.1); en un despliegue real se cambia a `false`. Para recrear
únicamente los datos de demostración, conservando los datos base y la bitácora
append-only, agregue `--reiniciar`. Al terminar resincroniza la configuración de
la compuerta en Redis. Si configura `SHAPI_SECRETO_ORIGEN_ENVIOS` o
`SHAPI_SECRETO_ORIGEN_AGRO`, la siembra cifra esos mismos secretos para que la
compuerta pueda autenticarse ante cada origen.

En desarrollo también se puede ejecutar directamente:

```bash
dotnet run --project src/Shapi.Trabajador -- sembrar-demo
dotnet run --project src/Shapi.Trabajador -- sembrar-demo --reiniciar
```

Todas las cuentas siguientes usan la contraseña `Shapi2026!demo`:

| Área | Nombre | Correo |
|---|---|---|
| Administración | Rodrigo Alvarado | `rodrigo.alvarado@shapi.localhost` |
| Administración | Lucía Ramírez Pineda | `lucia.ramirez@shapi.localhost` |
| Soporte | Sofía Menchú Cojtí | `sofia.menchu@shapi.localhost` |
| Soporte, desactivada | Julio Estrada Ixcot | `julio.estrada@shapi.localhost` |
| Envíos Xelajú | Ana Lucía Morales | `ana.morales@enviosxelaju.com` |
| Envíos Xelajú | Diego Us Pérez | `diego.us@enviosxelaju.com` |
| Envíos Xelajú | Karla Batres | `karla.batres@enviosxelaju.com` |
| Portal de Envíos Xelajú | María José Quiñónez, Mercadito Antigua | `mariajose@mercaditoantigua.com` |
| Portal de Envíos Xelajú | Isabel Herrera, Boutique Cayalá | `isabel@boutiquecayala.com` |
| Portal de Envíos Xelajú | Óscar Méndez, Ferretería Zona 11 | `oscar@ferreteriazona11.com` |
| Portal de Envíos Xelajú | Rosa Choc, Tienda Sololá | `rosa@tiendasolola.com` |
| Agro Precios | Carlos Tzul | `carlos.tzul@agroprecios.com` |
| Portal de Agro Precios | Andrea Xiloj, Distribuidora San Lucas | `andrea@distribuidorasl.com` |
| Cafetalera del Altiplano | Lucía Cotom | `lucia.cotom@cafetaleraaltiplano.com` |
| Transportes Petén | Mario Pop | `mario.pop@transportespeten.com` |
| Datos Chapines | Gabriela Sac | `gabriela.sac@datoschapines.com` |

Claves fijas para llamadas de demostración:

| Portal y consumidor | Entorno | Clave |
|---|---|---|
| Envíos Xelajú · Mercadito Antigua | Producción | `shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e` |
| Envíos Xelajú · Mercadito Antigua | Pruebas | `shp_prueba_Jp5sX1cV8nB3yG7tQe2Kv0a19d` |
| Envíos Xelajú · Boutique Cayalá | Producción | `shp_prod_BoutiqueCayalaDemo00004b80` |
| Envíos Xelajú · Boutique Cayalá | Producción, rotada hace 9 horas y vigente 15 horas más | `shp_prod_BoutiqueCayalaRotada00e513` |
| Envíos Xelajú · Boutique Cayalá | Pruebas | `shp_prueba_BoutiqueCayalaDem0000031c7` |
| Envíos Xelajú · Ferretería Zona 11 | Producción | `shp_prod_FerreteriaZona11Demo0090fb` |
| Envíos Xelajú · Ferretería Zona 11 | Pruebas | `shp_prueba_FerreteriaZona11De00005e28` |
| Envíos Xelajú · Tienda Sololá | Producción | `shp_prod_TiendaSololaDemo000000b3a4` |
| Envíos Xelajú · Tienda Sololá | Pruebas, revocada | `shp_prueba_TiendaSololaDemo000000d6f1` |
| Agro Precios · Distribuidora San Lucas | Producción | `shp_prod_AgroPreciosDemo0000000a7f2` |
| Agro Precios · Distribuidora San Lucas | Pruebas | `shp_prueba_AgroPreciosDemo00000004c8d` |

### Pruebas de extremo a extremo

Las pruebas E2E (Playwright) corren contra el ambiente productivo simulado ya levantado, en `https://shapi.localhost`.
La primera vez, instale sus dependencias y el navegador:

```bash
cd tests/e2e
pnpm install
pnpm exec playwright install chromium
```

Después, con el ambiente levantado, `pnpm test` en `tests/e2e/`. Para una captura de una pantalla, `pnpm captura <url> <archivo.png>`.

### Consultar registros

Muestre los registros estructurados de todos los procesos con:

```bash
docker compose --env-file .env -f infra/compose.yml -f infra/compose.prod.yml logs -f
```

También puede agregar al final el nombre de un servicio, por ejemplo `api`,
`compuerta` o `trabajador`.

### Apagar

Detenga el ambiente sin borrar los datos persistentes:

```bash
docker compose --env-file .env -f infra/compose.yml -f infra/compose.prod.yml down
```

Las imágenes publicadas por la integración continua se llaman
`ghcr.io/jordin-garcia/shapi-{api,compuerta,trabajador,borde}`. Compose usa la
etiqueta `latest`; la opción `--build` permite construir el mismo contenido
localmente.

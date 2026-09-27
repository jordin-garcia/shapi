---
id: JZ-01
titulo: Infraestructura local: Docker Compose, Caddy, TLS y Mailpit
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: 1
prioridad: P1
estado: hecha
depende_de: []
requisitos: [RNF-09, RNF-14]
pantallas: []
---

# JZ-01 · Infraestructura local: Docker Compose, Caddy, TLS y Mailpit

**Responsable:** José Pablo Zúñiga · **Avance:** 1 · **Prioridad:** P1 · **Sin dependencias**

## Objetivo
Levantar con un solo comando PostgreSQL, Redis, Mailpit y el borde Caddy, con HTTPS en `*.shapi.localhost` y el enrutamiento por host de las especificaciones, apuntando a los procesos que corren en el equipo durante el desarrollo.

## Contexto que debes leer
- `docs/specs/06-arquitectura.md` §4 (hosts y enrutamiento), §6 (dominios simulados) y §7 (despliegue)
- `docs/specs/10-identidad-y-seguridad.md` §5 (cabeceras del borde)
- `docs/plan/convenciones.md` §9 (puertos)
- `docs/plan/instalacion.md` §5 y §5.1 (comandos que ya esperan los integrantes: el servicio se llama `borde`)

## Archivos que creas o modificas
- `infra/compose.yml` (crear): `postgres:16-alpine` (volumen `pgdata`, *healthcheck*), `redis:7.4-alpine` con `--appendonly yes --appendfsync everysec` (volumen `redisdata`), `axllent/mailpit` (1025 y 8025) y `borde` con `caddy:2` (volumen `caddydata`, puertos 80 y 443, `extra_hosts: host.docker.internal:host-gateway` para Linux)
- `infra/caddy/Caddyfile.dev` (crear)
- `.env.example` (crear con **todas** las variables `SHAPI_*` y sus valores de desarrollo)
- `docs/manual-tecnico.md` (crear con la sección "Entorno de desarrollo")

## Criterios de aceptación
1. `docker compose --env-file .env -f infra/compose.yml up -d` deja todos los servicios *healthy* en Windows y en Linux.
2. El Caddyfile de desarrollo usa `local_certs` y enruta según 06 §4: `shapi.localhost` → `/api/*` a `host.docker.internal:5080` y el resto a `:5173` (Vite, con websocket); `*.shapi.localhost` → `/api/portal/*` a `:5080` (conservando el `Host`) y el resto a `:5174`; `*.api.shapi.localhost` → `:5090`; `correo.shapi.localhost` → `mailpit:8025`. Además tiene un sitio comodín `https://` con `tls { on_demand }` hacia `:5090`, y en las opciones globales `on_demand_tls { ask http://host.docker.internal:5080/interno/tls/autorizar }`.
3. HTTP redirige a HTTPS, y se agregan `Strict-Transport-Security`, `X-Content-Type-Options: nosniff` y `Referrer-Policy: strict-origin-when-cross-origin`. Caddy no publica `/interno/*`.
4. Después de importar la raíz de Caddy con los pasos de `docs/plan/instalacion.md` §5.1, https://correo.shapi.localhost abre Mailpit con candado. Si algún comando de §5.1 no sirve, corrígelo en ese archivo.
5. `.env.example` tiene: `SHAPI_DOMINIO_BASE=shapi.localhost`, `SHAPI_MODO_DEMO=true`, las credenciales de PostgreSQL y la cadena de conexión, Redis, `SHAPI_SMTP_*` (Mailpit), `SHAPI_DNS_MODO=simulado`, `SHAPI_ORIGENES_PERMITIDOS=localhost:5101,localhost:5102,origen-envios:8080,origen-agro:8080`, `SHAPI_ADMIN_CORREO`, `SHAPI_ADMIN_NOMBRE`, `SHAPI_ADMIN_CONTRASENA`, `SHAPI_PASARELA_FALLA=false`, `SHAPI_DPKEYS_DIR`, `SHAPI_APLICAR_MIGRACIONES`, `SHAPI_URL_ORIGEN_ENVIOS=http://localhost:5101` y `SHAPI_URL_ORIGEN_AGRO=http://localhost:5102`.
6. `docs/manual-tecnico.md` explica cómo levantar el entorno, confiar en el certificado, los puertos y cómo apagarlo o reiniciarlo (`down -v`).

## Pruebas obligatorias
- Script de verificación `infra/verificar.mjs` (crear) que comprueba que los contenedores estén sanos y que `https://correo.shapi.localhost` responda (con `rejectUnauthorized: false`)

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
docker compose --env-file .env -f infra/compose.yml up -d
node infra/verificar.mjs
node scripts/tareas.mjs --validar
```

## Fuera de alcance
- Imágenes de la aplicación y compose de producción (JZ-06)
- Orígenes de demostración (JZ-02)

## Notas
- Si JG-01 todavía no se integró, no hay CI. Verifica localmente e integra con `gh pr merge --squash --delete-branch`.

## Resultado

- Se creó el entorno local con PostgreSQL 16, Redis 7.4 con AOF, Mailpit y Caddy, todos con verificaciones de salud y volúmenes persistentes.
- Caddy enruta el panel, el portal, la API de control, la compuerta y Mailpit mediante HTTPS local; conserva el `Host` del portal, admite WebSocket, autoriza TLS bajo demanda y bloquea `/interno/*`.
- Se agregó `.env.example` con la configuración completa de desarrollo y `docs/manual-tecnico.md` con instalación, certificados, puertos y administración del entorno.
- `infra/verificar.mjs` valida la configuración, espera hasta 90 segundos por la salud, prueba cada destino con orígenes controlados, verifica WebSocket, cabeceras, redirección y Mailpit.
- La raíz de Caddy se importó en el almacén del usuario y Mailpit respondió 200 con validación TLS normal.

### Decisiones

- Las cabeceras de seguridad se aplican de forma diferida para sobrescribir valores del origen y mantener exactamente la política especificada.
- El sitio HTTP comodín redirige también los dominios propios desconocidos hacia HTTPS.
- Los procesos .NET y Vite permanecen en el equipo, mientras Compose administra únicamente la infraestructura de desarrollo.

### Correcciones de la auditoría (2026-09-27)

Paso 8 de `docs/plan/auditoria-2026-09-25.md` (H-68 a H-73):
- **El `.env` de la raíz (H-68).** Con `-f infra/compose.yml`, Compose buscaba el `.env` en `infra/` y no leía el de la raíz, así que se ignoraban las credenciales y los secretos configurados. Ahora todos los comandos pasan `--env-file .env`: `AGENTS.md`, el manual técnico, `instalacion.md`, el calendario y JZ-06. `infra/verificar.mjs` también lo pasa si existe el `.env`. Se descartó `include` con `env_file`, porque el `compose.prod.yml` de JZ-06 no vería el `.env` y el proyecto dejaría de llamarse `shapi`.
- **Puerto de PostgreSQL (H-69).** Se configura con `SHAPI_POSTGRES_PUERTO`, que vale 5432 por defecto, para los equipos que ya tienen un PostgreSQL nativo. `.env.example` suma esa variable, `SHAPI_SECRETO_ORIGEN_ENVIOS` y `SHAPI_SECRETO_ORIGEN_AGRO`.
- **Mailpit `v1.27` (H-70),** la misma versión que en las pruebas, en lugar de `latest`.
- **Solo `127.0.0.1` (H-71).** Todos los puertos, incluidos el 80 y el 443 de Caddy, se publican solo en `127.0.0.1`. Se comprobó que el navegador y `curl` llegan a `https://correo.shapi.localhost` por IPv4.
- **Salud de los orígenes (H-72).** `origen-envios` y `origen-agro` tienen *healthcheck* con `curl`, que se instaló en su imagen, contra `/salud`. Mandan `X-Shapi-Secreto`, porque `/salud` lo exige cuando hay secreto; se comprobó con y sin secreto.
- **`.dockerignore` y `.shapi/` (H-73).** Se agregó `.dockerignore` en la raíz, que es el contexto de construcción: deja fuera `.git`, `bin`, `obj`, `node_modules`, el `.env`, `.shapi/` y los mockups. `.shapi/`, donde `SHAPI_DPKEYS_DIR` guarda las llaves de Data Protection, se agregó al `.gitignore`.
- **`infra/verificar.mjs`** comprueba ahora:
  - la versión fija de Mailpit y que no haya imágenes `:latest`;
  - con un `.env` de prueba, que el puerto de PostgreSQL sale de `SHAPI_POSTGRES_PUERTO`;
  - que todo puerto se publica en `127.0.0.1`, en la configuración y en los contenedores en marcha;
  - que todos los servicios tienen *healthcheck* y los seis están *healthy*;
  - las variables nuevas de `.env.example`, el `--env-file` en los documentos con comandos (manual, instalación, `AGENTS.md`, calendario y tareas), el `.dockerignore` y el `.gitignore`.
- La prueba de JZ-02 `InfraestructuraTests` exige ahora los puertos de los orígenes en `127.0.0.1`.

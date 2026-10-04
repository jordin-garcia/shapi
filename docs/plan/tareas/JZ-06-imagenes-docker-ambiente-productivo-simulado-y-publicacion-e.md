---
id: JZ-06
titulo: Imágenes Docker, ambiente productivo simulado y publicación en GHCR
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: 2
prioridad: P1
estado: hecha
programada: 2026-09-29
depende_de: [JZ-01, JG-02, DC-02]
requisitos: [RNF-14, RNF-09]
pantallas: []
---

# JZ-06 · Imágenes Docker, ambiente productivo simulado y publicación en GHCR

**Responsable:** José Pablo Zúñiga · **Avance:** 2 · **Prioridad:** P1 · **Depende de:** JZ-01, JG-02, DC-02

## Objetivo
Empaquetar el sistema en imágenes y levantar el ambiente productivo simulado con un solo comando, publicando las imágenes en GHCR desde la integración continua.

## Contexto que debes leer
- `docs/specs/06-arquitectura.md` §7 **completo** y §4
- `docs/specs/06-arquitectura.md` §8 (salud)
- `docs/specs/10-identidad-y-seguridad.md` §1 (`X-Forwarded-For`) y §5 (CSP y cabeceras)
- `docs/lineamientos.md` §2 (despliegue en ambientes productivos)

## Archivos que creas o modificas
- `src/Shapi.{Api,Compuerta,Trabajador}/Dockerfile` (crear, *multi-stage*)
- `infra/borde/Dockerfile` (crear: Caddy más los frontends compilados)
- `infra/caddy/Caddyfile.prod` (crear)
- `infra/compose.prod.yml` (crear)
- `.github/workflows/publicar-imagenes.yml` (crear; es de José Pablo según convenciones §3)
- `docs/manual-tecnico.md` (modificar: sección "Ambiente productivo simulado")

## Criterios de aceptación
1. `docker compose --env-file .env -f infra/compose.yml -f infra/compose.prod.yml up -d --build`, desde cero, levanta api, compuerta, trabajador, borde, postgres, redis, mailpit y los dos orígenes, todos *healthy* (salvo el trabajador, que no tiene HTTP y no lleva *healthcheck*: su salud es el latido `salud:trabajador`, 06 §8) y con `restart: unless-stopped`. Solo quedan publicados los puertos 80 y 443. Como `compose.yml` publica en `127.0.0.1` los puertos de desarrollo (PostgreSQL, Redis, Mailpit y los orígenes), `compose.prod.yml` los quita con `ports: !reset []`, porque Compose suma las listas de puertos en vez de reemplazarlas. El 80 y el 443 del borde siguen publicados solo en `127.0.0.1`, como comprueba `infra/verificar.mjs`.
2. El Caddyfile de producción enruta a los nombres de servicio (`api:8080`, `compuerta:8080`) y sirve los frontends compilados con *fallback* a `index.html` para cada aplicación. Tiene la CSP de 10 §5 y el `ask` de on-demand a `http://api:8080/interno/tls/autorizar`.
3. La API aplica las migraciones al iniciar (`SHAPI_APLICAR_MIGRACIONES=true`). Las llaves de Data Protection persisten en el volumen `dpkeys`, compartido entre la API y el trabajador.
4. `publicar-imagenes.yml`, en cada *push* a `main`, construye y publica `ghcr.io/jordin-garcia/shapi-{api,compuerta,trabajador,borde}` con las etiquetas `latest` y el SHA. `compose.prod.yml` usa esas imágenes, y `--build` permite construirlas localmente.
5. El manual técnico explica cómo levantar, sembrar (`docker compose ... exec trabajador ... sembrar-demo`), ver los registros y apagar.
6. El *healthcheck* de la API y de la compuerta consulta `http://localhost:8080/salud` dentro del contenedor. La compuerta solo responde `/salud` con el host `localhost` (`Program.cs`), para no tapar la ruta `/salud` de las APIs de los proveedores.
7. El puerto de la API de control no se publica en el host: solo se llega a ella por el borde (`https://shapi.localhost/api/*`). La API confía en `X-Forwarded-For` y `X-Forwarded-Proto` solo si vienen del borde (10 §1). La red `shapi` ya tiene la subred fija `172.30.0.0/24` (`infra/compose.yml`), que la API acepta por defecto. Si `compose.prod.yml` usa otra red, hay que pasar su subred en `SHAPI_REDES_BORDE`.

## Pruebas obligatorias
- Levantar desde cero en un equipo y comprobar `https://shapi.localhost`, que los contenedores con *healthcheck* estén *healthy* (la API y la compuerta consultan `http://localhost:8080/salud` dentro del contenedor, 06 §8) y `https://envios.api.shapi.localhost` (404, porque todavía no hay siembra)
- Comprobar que el puerto de la API de control no responde desde el host (por ejemplo, `curl http://localhost:8080/salud` falla) y que sí responde a través del borde

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
docker compose --env-file .env -f infra/compose.yml -f infra/compose.prod.yml up -d --build
node infra/verificar.mjs
node scripts/tareas.mjs --validar
```

## Fuera de alcance
- Servidor público (el presupuesto es Q 0)

## Resultado

- Se crearon imágenes multi-stage para la API, la compuerta y el trabajador, y
  una imagen de borde que compila y sirve ambos frontends con Caddy.
- `infra/compose.prod.yml` agrega los procesos de Shapi, elimina los puertos de
  desarrollo, comparte las llaves de Data Protection y configura migraciones,
  salud, persistencia y reinicio automático.
- El Caddyfile productivo enruta por nombres de servicio, sirve las SPA con
  fallback a `index.html`, aplica la CSP y mantiene privados los endpoints
  `/interno/*`.
- El workflow `publicar-imagenes.yml` construye las cuatro imágenes en cada PR,
  verifica el ambiente completo desde cero y, al llegar a `main`, publica las
  etiquetas `latest` y SHA en GHCR.
- `infra/verificar.mjs` valida tanto desarrollo como producción. En producción
  comprueba los servicios, los puertos publicados, HTTPS, CSP, enrutamiento,
  ausencia de siembra y que la API de control no exponga su puerto.
- El manual técnico documenta cómo levantar, sembrar, consultar registros y
  apagar el ambiente productivo simulado.

### Decisiones tomadas

- El montaje del Caddyfile de desarrollo se reemplaza con `!override`, para que
  producción use el archivo incluido en la imagen y conserve únicamente el
  volumen persistente de certificados.
- La capa de frontend aumenta los reintentos de descarga y exige los binarios
  nativos de Alpine antes de compilar, porque pnpm considera opcionales esos
  paquetes y una descarga interrumpida podía producir una imagen incompleta.
- El verificador usa TLS 1.2 en sus solicitudes locales para evitar una
  incompatibilidad de negociación entre Node 25 para Windows y Caddy 2.10; el
  servidor continúa admitiendo TLS 1.3 para los clientes normales.

### Correcciones de la auditoría (2026-10-04)

Paso 6 de `docs/plan/auditoria-2026-10-03.md` (H-43 a H-45), hechas por el coordinador:
- **Acción fijada por SHA (H-43).** `docker/build-push-action` estaba en `@v7`, y corre con las credenciales de GHCR y recibe `GITHUB_TOKEN`. Era una regresión de H-109. Ahora está fijada por SHA (v7.4.0), como `login-action`.
- **Permisos por job (H-44).** `packages: write` estaba a nivel del workflow, así que también lo recibía `verificar-ambiente`, que ejecuta código del PR. Ahora el workflow tiene `contents: read` y solo el job `publicar` tiene `packages: write`.
- **Borde sin privilegios (H-45).** Caddy corre con el usuario `caddy` (UID 1000), y el binario oficial ya trae `cap_net_bind_service` para escuchar en 80 y 443. El volumen `caddydata` lo comparte el borde de desarrollo, que corre como root y deja archivos de root, así que el contenedor arranca como root solo para hacer a `caddy` dueño de `/data` y `/config` y luego ejecuta Caddy con `su-exec`. Así se conserva la misma autoridad certificadora y no hay que borrar nada. `Caddyfile.prod` lleva `skip_install_trust`, porque sin privilegios no puede instalar la raíz en el contenedor (se importa en el navegador). Se probó con una imagen mínima y un volumen con archivos de root.
- **Puerto 8080 (decisión del paso 6).** `infra/verificar.mjs` comprueba, literal, que `http://localhost:8080/salud` no responda en el ambiente productivo.
- `scripts/reglas-repositorio.test.mjs` vigila el SHA y los permisos.

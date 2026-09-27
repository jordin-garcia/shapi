---
id: JZ-06
titulo: Imágenes Docker, ambiente productivo simulado y publicación en GHCR
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: 2
prioridad: P1
estado: pendiente
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
1. `docker compose -f infra/compose.yml -f infra/compose.prod.yml up -d --build`, desde cero, levanta api, compuerta, trabajador, borde, postgres, redis, mailpit y los dos orígenes, todos *healthy* (salvo el trabajador, que no tiene HTTP y no lleva *healthcheck*: su salud es el latido `salud:trabajador`, 06 §8) y con `restart: unless-stopped`. Solo quedan publicados los puertos 80 y 443.
2. El Caddyfile de producción enruta a los nombres de servicio (`api:8080`, `compuerta:8080`) y sirve los frontends compilados con *fallback* a `index.html` para cada aplicación. Tiene la CSP de 10 §5 y el `ask` de on-demand a `http://api:8080/interno/tls/autorizar`.
3. La API aplica las migraciones al iniciar (`SHAPI_APLICAR_MIGRACIONES=true`). Las llaves de Data Protection persisten en el volumen `dpkeys`, compartido entre la API y el trabajador.
4. `publicar-imagenes.yml`, en cada *push* a `main`, construye y publica `ghcr.io/jordin-garcia/shapi-{api,compuerta,trabajador,borde}` con las etiquetas `latest` y el SHA. `compose.prod.yml` usa esas imágenes, y `--build` permite construirlas localmente.
5. El manual técnico explica cómo levantar, sembrar (`docker compose ... exec trabajador ... sembrar-demo`), ver los registros y apagar.
6. El *healthcheck* de la API y de la compuerta consulta `http://localhost:8080/salud` dentro del contenedor. La compuerta solo responde `/salud` con el host `localhost` (`Program.cs`), para no tapar la ruta `/salud` de las APIs de los proveedores.
7. El puerto de la API de control no se publica en el host: solo se llega a ella por el borde (`https://shapi.localhost/api/*`). La API confía en `X-Forwarded-For` solo si viene del borde (10 §1): `compose.prod.yml` fija una subred para la red del compose y pasa esa subred a la API en `SHAPI_REDES_BORDE`.

## Pruebas obligatorias
- Levantar desde cero en un equipo y comprobar `https://shapi.localhost`, que los contenedores con *healthcheck* estén *healthy* (la API y la compuerta consultan `http://localhost:8080/salud` dentro del contenedor, 06 §8) y `https://envios.api.shapi.localhost` (404, porque todavía no hay siembra)
- Comprobar que el puerto de la API de control no responde desde el host (por ejemplo, `curl http://localhost:8080/salud` falla) y que sí responde a través del borde

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
docker compose -f infra/compose.yml -f infra/compose.prod.yml up -d --build
node infra/verificar.mjs
node scripts/tareas.mjs --validar
```

## Fuera de alcance
- Servidor público (el presupuesto es Q 0)

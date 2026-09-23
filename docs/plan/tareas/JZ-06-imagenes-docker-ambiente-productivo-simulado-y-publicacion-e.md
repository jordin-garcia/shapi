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
- `docs/specs/10-identidad-y-seguridad.md` §5 (CSP y cabeceras)
- `docs/lineamientos.md` §2 (despliegue en ambientes productivos)

## Archivos que creas o modificas
- `src/Shapi.{Api,Compuerta,Trabajador}/Dockerfile` (crear, *multi-stage*)
- `infra/borde/Dockerfile` (crear: Caddy más los frontends compilados)
- `infra/caddy/Caddyfile.prod` (crear)
- `infra/compose.prod.yml` (crear)
- `.github/workflows/publicar-imagenes.yml` (crear; es de José Pablo según convenciones §3)
- `docs/manual-tecnico.md` (modificar: sección "Ambiente productivo simulado")

## Criterios de aceptación
1. `docker compose -f infra/compose.yml -f infra/compose.prod.yml up -d --build`, desde cero, levanta api, compuerta, trabajador, borde, postgres, redis, mailpit y los dos orígenes, todos *healthy* y con `restart: unless-stopped`. Solo quedan publicados los puertos 80 y 443.
2. El Caddyfile de producción enruta a los nombres de servicio (`api:8080`, `compuerta:8080`) y sirve los frontends compilados con *fallback* a `index.html` para cada aplicación. Tiene la CSP de 10 §5 y el `ask` de on-demand a `http://api:8080/interno/tls/autorizar`.
3. La API aplica las migraciones al iniciar (`SHAPI_APLICAR_MIGRACIONES=true`). Las llaves de Data Protection persisten en el volumen `dpkeys`, compartido entre la API y el trabajador.
4. `publicar-imagenes.yml`, en cada *push* a `main`, construye y publica `ghcr.io/jordin-garcia/shapi-{api,compuerta,trabajador,borde}` con las etiquetas `latest` y el SHA. `compose.prod.yml` usa esas imágenes, y `--build` permite construirlas localmente.
5. El manual técnico explica cómo levantar, sembrar (`docker compose ... exec trabajador ... sembrar-demo`), ver los registros y apagar.

## Pruebas obligatorias
- Levantar desde cero en un equipo y comprobar `https://shapi.localhost`, `/api/salud` y `https://envios.api.shapi.localhost` (404, porque todavía no hay siembra)

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
docker compose -f infra/compose.yml -f infra/compose.prod.yml up -d --build
node infra/verificar.mjs
node scripts/tareas.mjs --validar
```

## Fuera de alcance
- Servidor público (el presupuesto es Q 0)

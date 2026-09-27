---
id: JZ-02
titulo: Orígenes de demostración (Envíos Xelajú y Agro Precios)
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: 1
prioridad: P1
estado: hecha
depende_de: [JG-01]
requisitos: []
pantallas: []
---

# JZ-02 · Orígenes de demostración (Envíos Xelajú y Agro Precios)

**Responsable:** José Pablo Zúñiga · **Avance:** 1 · **Prioridad:** P1 · **Depende de:** JG-01

## Objetivo
Crear las dos APIs de origen de demostración con su especificación OpenAPI y las respuestas de los mockups, para mostrar Shapi de punta a punta sin depender de terceros.

## Contexto que debes leer
- `docs/specs/12-decisiones.md` ADR-33
- `docs/specs/07-modelo-de-datos.md` §6 (siembra)
- Mockups: `mockups/A3/Especificacion.dc.html` (rutas), `mockups/A5/Documentacion.dc.html` y `mockups/A5/DocumentacionAgro.dc.html` (parámetros y ejemplos), `mockups/A5/InicioAgro.dc.html`

## Archivos que creas o modificas
- `origenes-demo/envios-xelaju/**` (crear: Minimal API .NET 10, `openapi.yaml` y `Dockerfile`)
- `origenes-demo/agro-precios/**` (crear)
- `origenes-demo/OrigenesDemo.Tests/**` (crear)
- `Shapi.slnx` (modificar: agregar los tres proyectos en una carpeta `origenes-demo`; cambio permitido)
- `infra/compose.yml` (modificar: servicios `origen-envios` en 5101 y `origen-agro` en 5102)

## Criterios de aceptación
1. Envíos Xelajú responde `POST /cotizaciones`, `POST /guias`, `GET /tarifas`, `GET /rastreo` y `GET /cobertura`, con datos de ejemplo coherentes con los mockups (por ejemplo, la cotización Quetzaltenango → Antigua Guatemala, Q 38.50, 2 días hábiles).
2. Agro Precios responde `GET /precios`, `GET /productos`, `GET /mercados` y `GET /historial` (por ejemplo, frijol negro en CENMA a Q 510.00 el quintal).
3. Cada uno tiene un `openapi.yaml` 3.0.3 válido, con descripciones, parámetros (tipo y si es obligatorio), ejemplos de petición y de respuesta, igual que A5.1. El de Envíos se llama `cotizacion-envios.yaml`, como en A3.3.
4. Si existe la variable `SECRETO_ORIGEN`, rechazan con 401 las peticiones sin `X-Shapi-Secreto` igual. Si no existe, aceptan todo.
5. Cada uno expone `/salud` y aparece en `infra/compose.yml`.

## Pruebas obligatorias
- Pruebas de cada endpoint y validación de cada `openapi.yaml` con Microsoft.OpenApi

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
docker compose --env-file .env -f infra/compose.yml up -d --build origen-envios origen-agro
curl http://localhost:5101/salud
```

## Fuera de alcance
- Registrar las APIs en Shapi (JZ-05)

## Resultado

- Se crearon las Minimal APIs .NET 10 de Envíos Xelajú y Agro Precios con todos los endpoints, datos y ejemplos de los mockups.
- Ambos orígenes admiten un secreto opcional mediante `SECRETO_ORIGEN` y `X-Shapi-Secreto`, exponen `/salud` y se ejecutan desde `infra/compose.yml` en los puertos 5101 y 5102.
- Se agregaron `cotizacion-envios.yaml` y `openapi.yaml`, validados como OpenAPI 3.0.3 con Microsoft.OpenApi, además de pruebas de integración para rutas, parámetros, ejemplos, seguridad y rangos de fechas.
- Se decidió mantener datos deterministas con fecha de demostración del 10 de septiembre de 2026 para que las respuestas coincidan con los mockups.

### Correcciones de la auditoría (2026-09-27)

Paso 9 de `docs/plan/auditoria-2026-09-25.md` (H-74 a H-77, H-116 y H-117):
- **OpenAPI igual a A3.3 y A5.1 (H-74).** `cotizacion-envios.yaml` y `agro-precios/openapi.yaml` ya no documentan `/salud`, el parámetro `X-Shapi-Secreto` ni las respuestas 401 del secreto.
  - Al cargar `cotizacion-envios.yaml` en A3.3 salen las 5 rutas del mockup.
  - El consumidor no ve en A5.1 un secreto que nunca envía: lo agrega la compuerta.
  - Los textos que muestran A5.1 e InicioAgro se copiaron tal cual: la descripción de `/precios` y los parámetros de `/cotizaciones` y de `/precios`.
  - El origen sigue respondiendo `/salud` para el *healthcheck*.
- **Tilde (H-75).** El error del secreto dice "Secreto de origen inválido.".
- **`/precios` coherente con `/historial` (H-76).**
  - Los dos leen la misma tabla, del 8 al 10 de septiembre.
  - Antes, `/precios` con otra fecha devolvía siempre Q 510.00 y la fecha sin formato.
  - Ahora devuelve el precio de ese día, con la fecha como "8 sep 2026".
  - Una fecha sin precio responde 404 y una fecha mal escrita, 400.
- **Precio para todo el catálogo (H-116).** `/productos` y `/mercados` listan 3 productos y 3 mercados, pero solo frijol negro en CENMA tenía precio. Ahora las 9 combinaciones tienen precio los 3 días, como promete la descripción "Lista los productos que tienen precio publicado". Frijol negro en CENMA conserva los valores de los mockups.
- **Cotización según el peso (H-117).** `POST /cotizaciones` respondía Q 38.50 con cualquier peso. Ahora calcula `precio_base + precio_por_kg × peso_kg` con la tabla de `/tarifas`, y el ejemplo del mockup se mantiene: 2.5 kg normal da Q 38.50.
- **Pruebas (H-77):**
  - cada ejemplo del OpenAPI se compara con la respuesta real (los ejemplos de `/tarifas`, `/rastreo`, `/productos`, `/mercados` y `/historial` ahora están completos);
  - los cuerpos de `/tarifas`, `/rastreo` y `/cobertura`;
  - sin `SECRETO_ORIGEN`, o vacío, se acepta todo;
  - el mensaje del 401;
  - la cotización con distintos pesos;
  - la coherencia de `/precios` con `/historial` para todo el catálogo;
  - los 404 y 400 de `/precios`.
- Se corrigió en DC-05 la ruta de los archivos de ejemplo: el de Envíos es `cotizacion-envios.yaml`.

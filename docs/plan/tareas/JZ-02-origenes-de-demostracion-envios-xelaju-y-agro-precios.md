---
id: JZ-02
titulo: Orígenes de demostración (Envíos Xelajú y Agro Precios)
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: 1
prioridad: P1
estado: pendiente
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
docker compose -f infra/compose.yml up -d --build origen-envios origen-agro
curl http://localhost:5101/salud
```

## Fuera de alcance
- Registrar las APIs en Shapi (JZ-05)

---
id: DC-15
titulo: Sitio público: inicio de Shapi (A0.1)
persona: dominique
responsable: Dominique Contreras
avance: 3
prioridad: P2
estado: hecha
programada: 2026-10-18
depende_de: [DC-02, EM-09]
requisitos: [RF-19]
pantallas: [A0.1]
---

# DC-15 · Sitio público: inicio de Shapi (A0.1)

**Responsable:** Dominique Contreras · **Avance:** 3 · **Prioridad:** P2 · **Depende de:** DC-02, EM-09

## Objetivo
Implementar la página de inicio pública de Shapi, con la sección de planes tomada de la API.

## Contexto que debes leer
- Mockups: `mockups/A0/Main.dc.html` (superficie oscura de la variante 4)
- `docs/specs/11-interfaz.md` §1 (tokens oscuros)
- `contratos/openapi/planes.yaml` (`GET /api/planes-plataforma`)

## Archivos que creas o modificas
- `frontend/apps/panel/src/paginas/A0-1-Inicio.tsx`

## Criterios de aceptación
1. Reproduce todas las secciones del mockup: encabezado, héroe, "Cómo funciona" con los 3 pasos, "Qué recibe el proveedor", planes y pie.
2. Los planes vienen de `GET /api/planes-plataforma`. Escala mensual y Escala anual se muestran agrupados en una sola tarjeta, igual que el mockup.
3. "Crear cuenta" lleva a `/registro` y "Entrar" a `/entrar`.

## Pruebas obligatorias
- Vitest + MSW

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

## Fuera de alcance
- Precios dentro del panel (EM-09)

## Resultado
- Se implementó `frontend/apps/panel/src/paginas/A0-1-Inicio.tsx` con la superficie oscura de la variante 4 (`mockups/A0/Main.dc.html` y tokens oscuros de `11-interfaz.md` §1), reproduciendo el encabezado propio, el héroe, la sección «Cómo funciona» con los 3 pasos e ilustraciones SVG, «Qué recibe el proveedor» con las 4 tarjetas, la sección «Planes» alimentada por `GET /api/planes-plataforma` y el pie académico.
- Los planes «Escala mensual» y «Escala anual» se agrupan en una única tarjeta «Escala» mostrando tanto la tarifa mensual (`Q 1,500.00 · Mensual · vigencia de 30 días`) como la anual (`o Q 15,000.00 anual · 365 días y 120,000,000 peticiones`).
- Los enlaces «Crear cuenta» dirigen a `/registro`, «Entrar» a `/entrar` y «Precios» / «Ver precios» a `#planes`. Se cubren los estados de carga y error recuperable con `EstadoCargando` y `EstadoError`.
- Se agregaron pruebas automatizadas en `frontend/apps/panel/src/tests/Inicio.test.tsx` (Vitest + MSW) y se registró `A0-1` como pantalla implementada en `rutas.test.tsx` y `Estructura.test.tsx`.

### Rediseño de la página de inicio (2026-11-09)
- Dominique rediseñó la presentación de A0.1 con autorización de Jordin (PR #142): animaciones de entrada, tonos violeta, planeta con burbujas de tráfico, franja de claves, insignia «Recomendado» en Producto y un cierre después de los planes con video de fondo (`public/fondo-cierre-inicio.webm`).
- Jordin terminó el PR (protocolo §E4): las claves de la franja usan `shp_prod_` y `shp_prueba_` enmascaradas y la cabecera `X-Api-Key`, y las de pruebas respetan RF-45 (sin cuota, 10 por minuto); el cierre trata al usuario de usted y toma la vigencia de la prueba de la API; con movimiento reducido, el planeta no gira y el video no se reproduce solo; el brillo y la insignia siguen a Producto aunque cambie el orden de los planes. Los precios de las tarjetas siguen viniendo de `GET /api/planes-plataforma`, y los planes del paso 03 se conservan como ejemplo de los planes de un proveedor.
- Pruebas nuevas en `Inicio.test.tsx`: formato de las claves, textos del cierre en usted y movimiento reducido. Precisiones en `11-interfaz.md` §4.

---
id: DC-15
titulo: Sitio público: inicio de Shapi (A0.1)
persona: dominique
responsable: Dominique Contreras
avance: 3
prioridad: P2
estado: pendiente
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

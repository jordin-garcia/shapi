---
id: JZ-07
titulo: Pruebas de extremo a extremo y herramienta de capturas
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: 2
prioridad: P1
estado: pendiente
depende_de: [EM-03, JZ-03, JZ-06]
requisitos: [RNF-15]
pantallas: []
---

# JZ-07 · Pruebas de extremo a extremo y herramienta de capturas

**Responsable:** José Pablo Zúñiga · **Avance:** 2 · **Prioridad:** P1 · **Depende de:** EM-03, JZ-03, JZ-06

## Objetivo
Crear el proyecto de pruebas E2E con Playwright, la herramienta de capturas que usan todos para comparar con los mockups, y la primera prueba de punta a punta.

## Contexto que debes leer
- `docs/plan/protocolo.md` §B8 (cómo se usan las capturas)
- `docs/specs/05-casos-de-uso.md` CU-01 y CU-02
- `docs/plan/convenciones.md` §3 (workflows)

## Archivos que creas o modificas
- `tests/e2e/**` (crear: `package.json` con Playwright, `playwright.config.ts` con `baseURL https://shapi.localhost` e `ignoreHTTPSErrors`, ayudantes para Mailpit)
- `tests/e2e/scripts/captura.ts` (crear)
- `.github/workflows/e2e.yml` (crear; es de José Pablo)

## Criterios de aceptación
1. `pnpm captura <url> <archivo.png>` (en `tests/e2e`) guarda una captura de página completa a 1440 × 900.
2. Un ayudante lee los correos de Mailpit (`/api/v1/messages`) y extrae los enlaces.
3. La prueba `registro-y-acceso.spec.ts` recorre: registro del proveedor → correo de verificación en Mailpit → abrir el enlace → entrar → ver la barra lateral del panel.
4. `e2e.yml` levanta el ambiente productivo simulado en el *runner*, ejecuta las pruebas y adjunta el informe como artefacto. **No** es una verificación obligatoria. Corre en *push* a `main` y a mano (`workflow_dispatch`).

## Pruebas obligatorias
- La propia E2E

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
node scripts/tareas.mjs --validar
cd tests/e2e && pnpm install && pnpm exec playwright install chromium && pnpm test
```

## Fuera de alcance
- Flujos completos (JZ-13)

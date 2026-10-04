---
id: JZ-07
titulo: Pruebas de extremo a extremo y herramienta de capturas
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: 2
prioridad: P1
estado: hecha
programada: 2026-10-01
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

## Resultado

- Se creó el proyecto independiente `tests/e2e` con Playwright, `baseURL` en `https://shapi.localhost`, aceptación del certificado local y un viewport de 1440 × 900.
- `pnpm captura <url> <archivo.png>` guarda capturas de página completa; una prueba comprueba el comando y las dimensiones del PNG.
- El ayudante de Mailpit consulta `/api/v1/messages`, espera por destinatario y extrae enlaces del contenido HTML o de texto. La primera E2E recorre el registro del proveedor, abre el enlace recibido y comprueba la barra lateral del panel.
- `.github/workflows/e2e.yml` levanta el ambiente productivo simulado al llegar a `main` o por ejecución manual, corre Playwright y adjunta el informe como artefacto sin convertirse en una verificación obligatoria del PR.
- Se resolvieron los hosts `*.localhost` a `127.0.0.1` dentro del ayudante, porque el proceso de Node no usa necesariamente la misma resolución especial de nombres que Chromium.

Archivos principales: `tests/e2e/playwright.config.ts`, `tests/e2e/scripts/captura.ts`, `tests/e2e/soporte/mailpit.ts`, `tests/e2e/tests/registro-y-acceso.spec.ts` y `.github/workflows/e2e.yml`.

### Correcciones de la auditoría (2026-10-04)

Paso 6 de `docs/plan/auditoria-2026-10-03.md` (H-46 y H-47), en el PR de JZ-06:
- **`AGENTS.md` (H-46).** Se quitó «Los de `tests/e2e/` todavía no existen: los crea JZ-07».
- **Preparación de las E2E (H-47).** `AGENTS.md`, el README y el manual técnico (nueva sección «Pruebas de extremo a extremo») dicen cómo prepararlas la primera vez: `pnpm install` y `pnpm exec playwright install chromium` en `tests/e2e/`.
- **CU-02 (decisión del paso 6).** La E2E de registro cierra la sesión y vuelve a entrar con el correo y la contraseña en A1.3.

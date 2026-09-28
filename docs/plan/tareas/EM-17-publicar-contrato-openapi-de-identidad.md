---
id: EM-17
titulo: Publicar el contrato OpenAPI de identidad
persona: emilio
responsable: Emilio Méndez
avance: 1
prioridad: P1
estado: hecha
depende_de: [EM-02]
requisitos: [RF-04]
pantallas: []
---

# EM-17 · Publicar el contrato OpenAPI de identidad

**Responsable:** Emilio Méndez · **Avance:** 1 · **Prioridad:** P1 · **Depende de:** EM-02

## Objetivo
Publicar el contrato OpenAPI del módulo de identidad implementado en EM-02 para que el frontend genere sus tipos desde la fuente propiedad de Emilio y pueda eliminar su contrato provisional.

## Contexto que debes leer
- `docs/specs/10-identidad-y-seguridad.md` §1
- `docs/plan/convenciones.md` §2, §5 y §7
- `docs/plan/tareas/EM-02-registro-verificacion-de-correo-e-inicio-de-sesion-del-perso.md`

## Archivos que creas o modificas
- `contratos/openapi/identidad.yaml` (ya existe desde EM-02: modificar solo si hace falta)
- `frontend/packages/api/src/generado/identidad.ts` (ya generado en EM-03 con `pnpm generar:api`: regenerar si cambia el contrato)
- `frontend/packages/api/package.json` (ya exporta `./identidad` desde EM-03; H-83 pide exportar `"./*"`)
- `frontend/apps/panel/src/modulos/sesion/**` (reemplazar el contrato provisional por el generado)

## Criterios de aceptación
1. El contrato describe los endpoints de identidad implementados en EM-02, incluidos `GET /api/auth/sesion` y `POST /api/auth/salir`, con sus cuerpos y códigos HTTP reales.
2. La respuesta de sesión declara `usuario`, `organizacion`, `rol`, `correoVerificado` y `destino` con la misma forma que devuelve el backend.
3. El panel consume los tipos generados y conserva el comportamiento de guardias y cierre de sesión de DC-02.

## Pruebas obligatorias
- Vitest: sesión activa, ausencia de sesión y cierre de sesión usando el contrato generado.

## Verificación
Todos estos comandos deben pasar:
```bash
cd frontend && pnpm generar:api && pnpm lint && pnpm typecheck && pnpm test && pnpm build
node scripts/tareas.mjs --validar
```

## Fuera de alcance
- Cambiar el comportamiento del backend de EM-02.

## Notas
- DC-02 usaba temporalmente `frontend/apps/panel/src/modulos/sesion/contratoSesion.ts` porque el contrato de Identidad pertenece a Emilio. Se eliminó al cerrar esta tarea (paso 11 de la auditoría).

## Resultado

La terminó el coordinador (Jordin) el 2026-09-27, en el paso 11 de `docs/plan/auditoria-2026-09-25.md` (H-83 y H-84), a partir de lo que ya habían dejado EM-02 y EM-03.

- **Contrato (criterios 1 y 2).** No hizo falta cambiar `contratos/openapi/identidad.yaml`: ya describía lo que hace el backend de EM-02. `POST /api/auth/salir` responde 200 aunque no haya sesión, o 403 `csrf`. `GET /api/auth/sesion` responde 200 con el esquema `Sesion` (`usuario`, `organizacion`, `rol`, `correoVerificado` y `destino`) o 401 sin `codigo`. `pnpm generar:api` no cambia `identidad.ts`.
- **Exportación (H-83).** `packages/api/package.json` exporta `"./*": "./src/generado/*.ts"`, en lugar de una entrada por módulo (`./apis` y `./identidad`). Cada contrato generado se importa como `@shapi/api/<modulo>`, sin editar `package.json`. Se precisó en `convenciones.md` §7.
- **Sin contrato provisional (H-84, criterio 3).** Se borró `apps/panel/src/modulos/sesion/contratoSesion.ts`. `useSesion.ts` y `CerrarSesion.tsx` usan `paths` y `components['schemas']['Sesion']` de `@shapi/api/identidad`. `SesionActual.rol` y `SesionActual.destino` toman sus tipos del esquema, así que `destino` ahora es una de las tres rutas del contrato y no cualquier texto. Las guardias y el cierre de sesión de DC-02 no cambiaron: se conserva la tolerancia a un 401 al salir, aunque el contrato no lo declara.
- **Pruebas:**
  - `apps/panel/src/tests/Sesion.test.tsx`: sesión activa, sin sesión (401 del contrato), error del servidor, cierre de sesión (CSRF, `/entrar` y caché borrada) y que no quede un contrato provisional. Las respuestas simuladas tienen el tipo del esquema generado: si el contrato cambia, el *typecheck* falla.
  - `rutas.test.tsx` (guardias de DC-02) y `packages/api/src/index.test.ts` usan los tipos generados en lugar de copias escritas a mano.
  - `packages/api/generar.test.mjs`: el comodín `./*` existe y cada YAML de `contratos/openapi/` se resuelve como `@shapi/api/<modulo>`.
- Archivos principales: `frontend/packages/api/package.json`, `frontend/apps/panel/src/modulos/sesion/` y `frontend/apps/panel/src/tests/Sesion.test.tsx`.

### Correcciones de la auditoría (2026-09-27)

- **H-129:** la prueba de cerrar sesión de `Sesion.test.tsx` decía RF-07, que es la matriz de permisos. Ahora dice RF-04.

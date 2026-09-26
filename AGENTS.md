# AGENTS.md — Instrucciones para agentes de IA en Shapi

Shapi es una plataforma como servicio para publicar APIs, controlar quién las usa y cobrar por ellas. Es el proyecto final de Ingeniería de Software I (URL, 2026). Trabajamos con **desarrollo dirigido por especificaciones**: las especificaciones de `docs/specs/` son la fuente de verdad y las tareas de `docs/plan/tareas/` dicen qué construir.

## Antes de hacer cualquier cosa

1. **Lee `docs/plan/protocolo.md` completo.** Es el procedimiento obligatorio para buscar, implementar e integrar tareas.
2. **Averigua quién es el usuario.** Pregúntaselo o deduce su persona con `git config user.name` o con `gh api user --jq .login`:

| Persona (clave) | Nombre | GitHub | Prefijo de sus tareas |
|---|---|---|---|
| `jordin` | Jordin García (coordinador) | jordin-garcia | JG |
| `emilio` | Emilio Méndez | MiloDou | EM |
| `dominique` | Dominique Contreras | Dom-cs13 | DC |
| `jose-pablo` | José Pablo Zúñiga | PabloZ7-425 | JZ |

3. **Solo implementa tareas de esa persona** y solo las que estén disponibles, es decir, sin dependencias pendientes. Para verlas, ejecuta `node scripts/tareas.mjs --persona <clave>`.
   **Excepción: el coordinador (`jordin`)** está autorizado de forma permanente a corregir, terminar o modificar el trabajo de cualquier persona (código, pruebas, contratos, tareas, bitácoras, documentación y sus PR abiertos). No necesita preguntar antes. Lo hace según `docs/plan/protocolo.md` §E. La excepción cubre solo quién edita qué: el resto de "Preguntar antes" y "Nunca" sigue vigente, incluido no hacer *push* a la rama de otra persona, ni forzado ni normal (protocolo §E4).

## Pedidos habituales del usuario

- **"¿Qué me toca?" / "Revisa el plan"**: ejecuta `node scripts/tareas.mjs --persona <clave>`, lee los archivos de las tareas disponibles y resúmelas: qué hace cada una, su prioridad, su avance y de quién depende lo que está en espera. Incluye los avisos que los demás le dejaron en sus bitácoras (`docs/plan/protocolo.md` §A, paso 5). Recomienda la siguiente. **No implementes nada todavía.**
- **"Implementa <ID>"** o **"continúa"**: sigue la sección B de `docs/plan/protocolo.md` de principio a fin, hasta que el *pull request* quede integrado en `main`, **sin volver a preguntarle al usuario**, salvo en los casos de "Preguntar antes" o si necesitas algo a lo que no puedes acceder.
- **"Audita <ID>"** o **"audita lo integrado"** (solo el coordinador): sigue `docs/plan/protocolo.md` §E2 y detente al presentar los hallazgos. **"Corrige los hallazgos"**, **"continúa la auditoría"** o "continúa" mientras la sesión corrige un plan de auditoría: §E3, un paso a la vez.

## Comandos

Algunos todavía no existen: los crean JG-01, DC-01 y JZ-01.

| Qué | Comando |
|---|---|
| Tareas | `node scripts/tareas.mjs --persona <clave>` · `--ver <ID>` · `--validar` |
| Infraestructura local | `docker compose -f infra/compose.yml up -d` · `docker compose -f infra/compose.yml down` |
| Backend: compilar | `dotnet build Shapi.slnx` |
| Backend: pruebas | `dotnet test Shapi.slnx` (usa Docker por Testcontainers) |
| Backend: formato | `dotnet format Shapi.slnx --verify-no-changes` (para corregir: sin `--verify-no-changes`) |
| Backend: ejecutar | `dotnet run --project src/Shapi.Api` · `src/Shapi.Compuerta` · `src/Shapi.Trabajador` |
| Migraciones | `dotnet ef migrations add <Nombre> -p src/Shapi.Infraestructura -s src/Shapi.Api` |
| Frontend | En `frontend/`: `pnpm install` · `pnpm dev` · `pnpm lint` · `pnpm typecheck` · `pnpm test` · `pnpm build` |
| Contratos → tipos TS | En `frontend/`: `pnpm generar:api` |
| Pruebas E2E | En `tests/e2e/`: `pnpm test` (requiere el entorno levantado) |
| Capturas para comparar con mockups | En `tests/e2e/`: `pnpm captura <url> <archivo.png>` |
| GitHub | `gh pr create` · `gh pr checks --watch` · `gh pr merge --auto --squash --delete-branch` |

## Dónde está cada cosa

- `docs/specs/`: requisitos (RF/RNF), arquitectura, modelo de datos, compuerta, cobros, seguridad, interfaz y ADR. **Si el código las contradice, el código está mal.**
- `docs/plan/`: guía, instalación, protocolo, convenciones, calendario, `tareas/` (un archivo por tarea) y `bitacora/` (una por persona).
- `docs/lineamientos.md`: las reglas del curso.
- `mockups/`: las pantallas aprobadas. La fuente está en `mockups/<Tanda>/*.dc.html`; el catálogo, en `docs/specs/11-interfaz.md`.
- `contratos/openapi/<modulo>.yaml`: contratos HTTP de la API de control, uno por módulo.
- `src/`, `tests/`, `frontend/`, `origenes-demo/`, `infra/`: el código, con la estructura de `docs/specs/06-arquitectura.md` §9.
- La tabla de qué carpeta pertenece a quién está en `docs/plan/convenciones.md` §2.

## Límites

**✅ Siempre**
- Trabaja en una rama `<persona>/<ID>-<descripcion-corta>` creada desde un `main` actualizado. La única excepción es cuando el coordinador termina el PR de otra persona: entonces la rama parte de la de ese PR (protocolo §E4).
- Escribe primero las pruebas de los criterios de aceptación. Nombra cada prueba con el código del requisito que cubre (por ejemplo `// RF-28`).
- Ejecuta todos los comandos de la sección **Verificación** de la tarea y muestra su salida como evidencia.
- Haz la revisión en contexto limpio con `docs/plan/prompts/revision.md` antes de abrir el PR.
- En el mismo PR, actualiza `estado: hecha` en el archivo de la tarea y agrega una entrada en `docs/plan/bitacora/<persona>.md`.
- Escribe los *commits*, el PR y los comentarios en español, con los nombres del glosario (`docs/specs/02-glosario.md`).

**⚠️ Preguntar antes** (detente y pregúntale al usuario)
- Agregar una dependencia (NuGet o npm) que no esté en el stack de `docs/specs/06-arquitectura.md` §9.
- Modificar archivos de otra persona que la tarea no menciona, o cambiar un "archivo caliente" (`docs/plan/convenciones.md` §3) de una forma que no dice la tarea. Si la persona es el coordinador (`jordin`), no hace falta preguntar (protocolo §E1), pero las reglas técnicas de convenciones §3 siguen vigentes.
- Contradecir o cambiar una decisión de `docs/specs/12-decisiones.md`.
- Cualquier cosa que requiera cuentas, credenciales o servicios externos.

**🚫 Nunca**
- Hacer *push* directo a `main`, `git push --force` sobre ramas ajenas, `--no-verify`, ni desactivar u omitir verificaciones de la CI.
- Borrar, debilitar o marcar como omitidas pruebas para que la CI pase.
- Hacer *commit* de secretos, archivos `.env`, contraseñas o tokens. Las credenciales de ejemplo van en `.env.example`.
- Implementar tareas de otra persona, salvo el coordinador (protocolo §E4), o marcar como hecha una tarea sin verificarla.
- Inventar requisitos. Si falta algo en la especificación, sigue `docs/plan/protocolo.md` §C.

## Definición de terminado

Una tarea está terminada cuando se cumplen todas estas condiciones:
1. Todos sus criterios de aceptación tienen una prueba automatizada que pasa.
2. La compilación, el formato, el lint, el *typecheck* y todas las pruebas pasan localmente y en la CI.
3. Si la tarea tiene pantallas, coinciden con su mockup: mismos textos, datos, orden y estados.
4. Si la tarea tiene endpoints, el contrato OpenAPI está actualizado.
5. Si se precisó algún comportamiento, las especificaciones quedaron actualizadas.
6. La revisión en contexto limpio no deja hallazgos de corrección.
7. El PR quedó integrado en `main`.

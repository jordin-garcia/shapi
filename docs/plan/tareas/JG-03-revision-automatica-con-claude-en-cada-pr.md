---
id: JG-03
titulo: Revisión automática con Claude y tablero del plan
persona: jordin
responsable: Jordin García
avance: 2
prioridad: P2
estado: hecha
depende_de: [JG-01]
requisitos: [RNF-15]
pantallas: []
---

# JG-03 · Revisión automática con Claude y tablero del plan

**Responsable:** Jordin García · **Avance:** 2 · **Prioridad:** P2 · **Depende de:** JG-01

## Objetivo
Dos automatizaciones de GitHub para el equipo:

1. **Revisión con Claude.** Cada *pull request* recibe automáticamente una revisión de Claude como comentario, usando el token de la suscripción Pro de Jordin. La revisión **no es obligatoria** para integrar, para que un límite de uso no frene al equipo.
2. **Tablero del plan.** Cada vez que algo se integra en `main`, un issue fijo, "Tablero del plan", muestra el estado de todas las tareas. Cuando una tarea queda disponible, se menciona a su dueño (por ejemplo, "@MiloDou: con JG-02 integrada, tu tarea EM-03 ya está disponible"). Así nadie depende de acordarse de hacer `git pull` para enterarse.

## Contexto que debes leer
- Documentación oficial: https://code.claude.com/docs/en/github-actions (modo de automatización con `prompt`, `claude_args` y el secreto `CLAUDE_CODE_OAUTH_TOKEN`)
- `docs/plan/prompts/revision.md` (las instrucciones de revisión que se aplican)
- `scripts/tareas.mjs` (cómo se calcula si una tarea está disponible, en espera, bloqueada o hecha, y el mapa `PERSONAS` con el usuario de GitHub de cada persona)
- `docs/plan/protocolo.md` §A y §C (tareas bloqueadas)

## Archivos que creas o modificas
- `.github/workflows/revision-claude.yml` (crear)
- `.github/workflows/tablero-plan.yml` (crear)
- `scripts/tareas.mjs` (modificar: opción `--json`)
- `scripts/tablero.mjs` (crear)
- `scripts/tablero.test.mjs` (crear)
- `.github/workflows/ci.yml` (modificar: el *job* `plan` también ejecuta `node --test "scripts/*.test.mjs"`)

## Criterios de aceptación

### Revisión con Claude
1. Se ejecuta `anthropics/claude-code-action@v1` en los eventos `pull_request` (`opened`, `synchronize`, `ready_for_review`, `reopened`), excepto en borradores, con `claude_code_oauth_token: ${{ secrets.CLAUDE_CODE_OAUTH_TOKEN }}`.
2. El `prompt` pide aplicar `docs/plan/prompts/revision.md` al PR (el ID sale del título `[XX-00]`) y publicar el resultado como **un comentario** en el PR. `claude_args` permite solo lo necesario, sin límite de turnos (el tope lo pone `timeout-minutes`): `--allowedTools "Read,Grep,Glob,Bash(git diff:*),Bash(gh pr view:*),Bash(gh pr diff:*),Bash(gh pr comment:*)"`.
3. El workflow también responde a `@claude` en comentarios de issues y PR (modo interactivo, en un *job* aparte) solo para usuarios con permiso de escritura, que es el comportamiento por defecto de la acción.
4. Un `concurrency` por número de PR cancela la revisión anterior cuando llegan *commits* nuevos.
5. El *job* **no** se agrega a las verificaciones obligatorias de la protección de `main`.

### Tablero del plan
6. `.github/workflows/tablero-plan.yml` se ejecuta en tres casos:
   - en cada `push` a `main`;
   - una vez al día, a las 07:00 de Guatemala (`cron: "0 13 * * *"`), para las tareas que se desbloquean por fecha (`no_antes_de`);
   - a mano, con `workflow_dispatch`.

   Usa `GITHUB_TOKEN` con permisos mínimos (`contents: read` e `issues: write`) y un `concurrency` único **sin** `cancel-in-progress`, para que no se pierdan avisos.
7. `node scripts/tareas.mjs --json` imprime un arreglo con todas las tareas. Cada una trae `id`, `titulo`, `persona`, `github`, `avance`, `prioridad`, `estado`, `situacion` (`disponible`, `en_espera`, `bloqueada` o `hecha`), `faltan` (dependencias sin hacer), `no_antes_de` y `bloqueo`. La salida para personas de las demás opciones no cambia.
8. `scripts/tablero.mjs` recibe el cuerpo actual del issue y devuelve dos cosas: el cuerpo nuevo y el texto del comentario de avisos (vacío si no hay novedades).
   - El estado anterior (los IDs disponibles, hechos y bloqueados) se guarda en el propio cuerpo del issue, en un comentario HTML `<!-- estado-tablero: {...} -->`.
   - Una tarea es **recién disponible** si ahora está disponible y en el estado anterior no lo estaba.
   - Si no hay estado anterior, es decir, en la primera ejecución, solo se crea el tablero y no se menciona a nadie.
9. **El issue.**
   - El título es "Tablero del plan" y la etiqueta, `tablero`. El workflow lo busca por la etiqueta y, si no existe, lo crea y lo fija (`gh issue pin`).
   - En cada ejecución se reemplaza el cuerpo, que muestra por persona las tareas disponibles, las que están en espera (con de quién dependen o su fecha), las bloqueadas (con su motivo) y el avance (hechas/total).
   - Al final del cuerpo va el avance por entrega (Avance 1, 2, 3 y final).
10. **Los avisos.** Si hay novedades, el workflow publica **un solo** comentario en el issue, agrupado por persona:
    - Tarea recién disponible por dependencias: `@MiloDou: con JG-02 integrada, tu tarea EM-03 (<título>) ya está disponible.` Se nombran las dependencias que pasaron a `hecha` desde la ejecución anterior.
    - Tarea recién disponible por fecha: `@jordin-garcia: tu tarea JG-08 (<título>) ya está disponible: llegó su fecha (2026-10-08).`
    - Tarea que pasó a `bloqueada`: se menciona al coordinador (`@jordin-garcia`) con el ID, el dueño y el motivo.

    Si no hay novedades, no se comenta nada.
11. El workflow es idempotente: si se ejecuta dos veces seguidas sin cambios en `main`, la segunda vez no repite avisos.

## Pruebas obligatorias
- **Revisión con Claude:** no lleva pruebas unitarias. La prueba es que el propio PR de esta tarea reciba el comentario de revisión.
- **Tablero** (`scripts/tablero.test.mjs`, con `node:test`, sin dependencias nuevas). Cubre estos casos:
  - la primera ejecución no menciona a nadie;
  - una dependencia que pasa a `hecha` genera la mención correcta al dueño;
  - un desbloqueo por fecha genera su mención;
  - dos ejecuciones iguales no repiten avisos;
  - una tarea que pasa a `bloqueada` menciona al coordinador;
  - el cuerpo conserva el estado en el comentario HTML.

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
node scripts/tareas.mjs --validar
node --test "scripts/*.test.mjs"
node scripts/tareas.mjs --json
gh pr view --comments                     # en el PR de esta tarea debe aparecer el comentario de revisión de Claude
gh workflow run tablero-plan.yml          # después de integrar: crea o actualiza el issue "Tablero del plan"
gh issue list --label tablero             # debe existir un solo issue, fijado
```

## ⚠️ Pasos que requieren a una persona
- Jordin ejecuta `claude setup-token` en su terminal. Luego le pasa el token al agente para que lo guarde con `gh secret set CLAUDE_CODE_OAUTH_TOKEN`, o lo guarda él mismo. El agente **no** puede generar el token.
- Solo la revisión con Claude necesita el token. Si todavía no está, implementa y prueba primero el tablero.

## Fuera de alcance
- Hacer que la revisión sea obligatoria
- Revisiones con API key de pago
- Avisos por correo, Slack u otros canales fuera de GitHub
- Un estado "en progreso" para las tareas

## Notas
- El consumo de la revisión se descuenta del plan Pro de Jordin. Si se agota el cupo, la revisión falla sin bloquear nada. La revisión local del protocolo (B9) sigue siendo obligatoria.
- GitHub solo notifica una mención si el usuario tiene acceso al repositorio. Los cuatro integrantes son colaboradores.
- Los *push* hechos con `GITHUB_TOKEN` no disparan otros workflows. Los merges automáticos de los PR sí los disparan, porque cuentan como hechos por quien activó el auto-merge.

## Resultado
- **Revisión con Claude** (`.github/workflows/revision-claude.yml`):
  - El *job* `revision-claude` revisa cada PR que no es borrador con `docs/plan/prompts/revision.md` y deja un solo comentario.
  - Toma el ID del título `[XX-00]` en un paso aparte, a través de `env`, para evitar inyecciones.
  - Usa `concurrency` por número de PR con cancelación.
  - El *job* `claude-interactivo` responde a `@claude` en comentarios de issues y PR.
  - Ninguno de los dos es una verificación obligatoria de `main`.
- **Tablero** (`.github/workflows/tablero-plan.yml` y `scripts/tablero.mjs`):
  - Busca o crea el issue fijo "Tablero del plan", con la etiqueta `tablero`.
  - Publica primero el comentario de avisos y después reemplaza el cuerpo. El cuerpo guarda el estado en `<!-- estado-tablero: ... -->`: si algo falla a la mitad, el aviso se repite en lugar de perderse.
  - `generar(tareas, cuerpoAnterior, fecha)` es una función pura, y `scripts/tablero.test.mjs` la prueba con 11 casos, incluida la forma de `--json`.
- **`scripts/tareas.mjs`:**
  - Tiene la opción `--json`.
  - Exporta `PERSONAS`, `AVANCES`, `cargar`, `clasificar`, `hoy` y `aJson`.
  - Solo se ejecuta como programa cuando es el módulo principal: `import.meta.main`, y en Node < 24.2 una comparación de rutas.
- **Decisiones:**
  - `node --test scripts/` no funciona en Node 24, que trata el directorio como archivo. En la CI y en la Verificación se usa `node --test "scripts/*.test.mjs"`.
  - Modelo fijo `claude-opus-5-5` con `--effort high`, a pedido de Jordin. Sin `--effort`, Opus 5.5 usa `medium`.
  - La acción usa `github_token: ${{ github.token }}` en lugar de la GitHub App de Claude, así que los comentarios aparecen como `github-actions`.
  - El JSON incluye además `depende_de`, que hace falta para nombrar las dependencias integradas.
  - Si el título del PR no tiene ID, la revisión cubre solo los errores, la seguridad y las pruebas.
  - El cuerpo del tablero no usa `@` (las menciones van solo en los avisos).
  - Una tarea que sale de `bloqueada` recibe un aviso genérico ("ya no está bloqueada y está disponible").
  - El workflow usa `TZ=America/Guatemala` para que `no_antes_de` se compare con el día de Guatemala.

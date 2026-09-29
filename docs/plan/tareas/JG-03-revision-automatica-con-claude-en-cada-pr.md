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

1. **Revisión con Claude.** Cada *pull request* recibe automáticamente una revisión de Claude como comentario, usando el token de la suscripción Pro de Jordin. Desde la auditoría del 25 de septiembre, la revisión **es una verificación obligatoria** de `main` (ver "Correcciones de la auditoría" en el Resultado).
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
- Auditoría del 26 de septiembre: `.github/workflows/claude-interactivo.yml`, `scripts/veredicto-revision.mjs` y `scripts/veredicto-revision.test.mjs` (crear)

## Criterios de aceptación

### Revisión con Claude
1. Se ejecuta `anthropics/claude-code-action` v1, fijada por SHA porque recibe el token (H-109), en los eventos `pull_request` (`opened`, `synchronize`, `ready_for_review`, `reopened` y `edited`; una revisión nueva del mismo commit solo se hace si no hay una completa o si cambió la tarea del título); en los borradores no revisa (el check se decide al marcarlos listos), con `claude_code_oauth_token: ${{ secrets.CLAUDE_CODE_OAUTH_TOKEN }}`.
2. El `prompt` pide aplicar `docs/plan/prompts/revision.md` al PR (el ID sale del título `[XX-00]`) y publicar el resultado como **un comentario** en el PR. `claude_args` permite solo lo necesario, sin límite de turnos (el tope lo pone `timeout-minutes`): `--allowedTools "Read,Grep,Glob,Bash(git diff:*),Bash(gh pr view:*),Bash(gh pr diff:*),Bash(gh pr comment:*)"`.
3. Otro workflow, `claude-interactivo.yml`, responde a `@claude` en comentarios de issues y PR, solo para usuarios con permiso de escritura, que es el comportamiento por defecto de la acción.
4. Un `concurrency` por número de PR cancela la revisión anterior cuando llegan *commits* nuevos.
5. El *job* `revision-claude` es una verificación obligatoria de la protección de `main`:
   - pasa solo si la revisión publicada para el commit dice `VEREDICTO: LISTO` sin hallazgos de corrección;
   - falla si pide corregir o si no se completó (cuota, caída o tiempo);
   - solo el coordinador integra un falso positivo, con `--admin`.

   Antes decía que no era obligatoria; se cambió en la auditoría del 25 de septiembre.

### Tablero del plan
6. `.github/workflows/tablero-plan.yml` se ejecuta en tres casos:
   - en cada `push` a `main`;
   - una vez al día, a las 07:00 de Guatemala (`cron: "0 13 * * *"`), para las tareas que se desbloquean por fecha (`no_antes_de`) y, desde el 28 sep, para el recordatorio del día (criterio 14);
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
- **Revisión con Claude:** la prueba es que el propio PR de esta tarea reciba el comentario de revisión. Desde la auditoría, la decisión del check (revisar o no, y el veredicto) se prueba en `scripts/veredicto-revision.test.mjs`.
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
- Revisiones con API key de pago
- Avisos por correo, Slack u otros canales fuera de GitHub
- Un estado "en progreso" para las tareas

## Notas
- El consumo de la revisión se descuenta del plan Pro de Jordin. Si se agota el cupo, el check falla y el PR espera hasta que la revisión se complete (decisión de Jordin del 26 de septiembre). La revisión local del protocolo (B9) sigue siendo obligatoria.
- GitHub solo notifica una mención si el usuario tiene acceso al repositorio. Los cuatro integrantes son colaboradores.
- Los *push* hechos con `GITHUB_TOKEN` no disparan otros workflows. Los merges automáticos de los PR sí los disparan, porque cuentan como hechos por quien activó el auto-merge.

## Resultado
- **Revisión con Claude** (`.github/workflows/revision-claude.yml`):
  - El *job* `revision-claude` revisa cada PR que no es borrador con `docs/plan/prompts/revision.md` y deja un solo comentario.
  - Toma el ID del título `[XX-00]` en un paso aparte, a través de `env`, para evitar inyecciones.
  - Usa `concurrency` por número de PR con cancelación.
  - El *job* `claude-interactivo` responde a `@claude` en comentarios de issues y PR.
  - Ninguno de los dos es una verificación obligatoria de `main`. (Lo cambió la auditoría: ver "Correcciones de la auditoría" más abajo.)
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

### Correcciones de la auditoría (2026-09-26)
- **La revisión con Claude es una verificación obligatoria de `main`** (H-09 y H-10). EM-02 (#10) se había integrado con 13 hallazgos obligatorios que la revisión sí había marcado.
- **Cómo se decide el check.** Un último paso, `Veredicto de la revisión`, lee los comentarios del PR con `scripts/veredicto-revision.mjs`:
  - toma solo la revisión más reciente de `github-actions[bot]` para el commit actual;
  - pasa con `VEREDICTO: LISTO` sin hallazgos de corrección;
  - falla con `CORREGIR`, o si la revisión dice `LISTO` pero enumera correcciones;
  - si no hay revisión de ese commit (cuota, caída o tiempo), falla con un mensaje para reintentarla.

  El veredicto es el primero que aparece después de la sección de corrección, así que las notas posteriores no cuentan. Las palabras clave se reconocen aunque vengan con formato Markdown.
- **Una revisión completa no se repite.** El paso `tarea` usa `--decidir`: un commit que ya tiene una revisión completa no se revisa otra vez, ni con `gh run rerun`, ni reabriendo el PR, ni marcándolo listo de nuevo, salvo que cambie la tarea del título. El veredicto solo cuenta si la revisión es de la tarea que dice ahora el título: si la revisión nueva no se completa, el check falla.
- **Pruebas:** 30 en `scripts/veredicto-revision.test.mjs`. El script se comprobó con los comentarios reales de los PR #14, #16 y #22.
- **El formato del comentario queda fijo** en el prompt: la primera línea es la marca, la segunda `Commit revisado: <sha>` y el comentario termina con el veredicto.
- **El check no se puede saltar con eventos que no revisan.** Un check "omitido" cuenta como aprobado y pisaría el fallo del mismo commit.
  - `@claude` pasó a `.github/workflows/claude-interactivo.yml`.
  - El job no tiene `if`: un job omitido cuenta como aprobado, y `gh run rerun` de un run viejo (por ejemplo, de cuando el PR era borrador) lo repetiría. Borrador, estado y título se leen en vivo con `gh pr view`. En un borrador el check falla con un aviso y se decide al marcarlo listo.
  - Los eventos que no necesitan una revisión nueva no se omiten: vuelven a leer el veredicto ya publicado.
  - `concurrency` cancela la revisión anterior con cada *commit* nuevo; las ediciones del PR esperan a que termine. El paso del veredicto no corre si el run se canceló.
  - El título se lee en vivo, así que `gh run rerun` usa el título corregido.
- **Decisiones de Jordin (26 de septiembre):**
  - Si la revisión no se completa, el PR se bloquea hasta reintentarla.
  - Un falso positivo solo lo desbloquea Jordin, con `gh pr merge <n> --admin --squash`. Por eso `enforce_admins` queda desactivado, lo que resuelve la decisión pendiente de H-110.
- **Una revisión editada no cuenta.** Quien tiene permiso de escritura puede editar comentarios ajenos. Por eso, si el comentario de la revisión se editó después de publicarse (`updated_at` distinto de `created_at`), el check falla, no se revisa otra vez y solo Jordin puede integrar con `--admin`. El commit del comentario se toma solo de la línea `Commit revisado`.
- **Limitaciones conocidas:**
  - Como en todo check con `pull_request`, un PR ejecuta el workflow y el script de su propia rama. Cambiar `.github/workflows/` o `scripts/veredicto-revision.mjs` es tocar archivos de Jordin: la revisión lo marca como problema de alcance y la auditoría del coordinador (protocolo §E) lo revisa.
  - Hay formas de pedir otra revisión del mismo código: borrar el comentario de la revisión, hacer un *commit* nuevo (aunque sea vacío o con `gh pr update-branch`) o cambiar la tarea del título y volver a ponerla. Todas quedan registradas en el historial del PR, y la auditoría del coordinador lo revisa.
  - Los comentarios de `@claude` (`claude-interactivo.yml`) salen con el mismo usuario del bot. En modo interactivo la acción edita su comentario de seguimiento, y un comentario editado no cuenta para el check. Aun así, pedirle a `@claude` que publique una revisión falsa queda en el historial y lo revisa la auditoría.
- **Corrección del 26 de septiembre (falso positivo en el #24).** La revisión del #24 decía `CORRECCIÓN: Ninguno` y `VEREDICTO: LISTO`, pero el check falló por dos errores de `veredicto-revision.mjs`:
  - buscaba "CORRECCIÓN" sin distinguir mayúsculas, así que tomó como encabezado una línea del texto libre ("Corrección de auditoría…");
  - el prefijo de los encabezados aceptaba saltos de línea, así que la propia línea "CORRECCIÓN (…)" contaba como un hallazgo.

  Ahora los encabezados se buscan en MAYÚSCULAS (con "(", ":", un guion o el fin de línea después) y el prefijo solo acepta espacios y marcas de Markdown. Un hallazgo en la misma línea del encabezado también cuenta, y el veredicto se acepta como `VEREDICTO:` o `Veredicto:`. También se aceptan los encabezados con mayúscula inicial ("Corrección (…):"), y sin una sección de corrección reconocida la revisión cuenta como incompleta: nunca se aprueba y se puede reintentar. La sección que cuenta es el primer encabezado en MAYÚSCULAS antes del veredicto (o, si no hay ninguno, el primero con mayúscula inicial): un "Corrección (…)" en el texto libre no la reemplaza y un segundo encabezado no oculta hallazgos. "Opcional" con mayúscula inicial solo cierra la sección si ocupa toda la línea. Hay una prueba con la estructura real de esa revisión, y se volvieron a comprobar los comentarios de #14, #16, #22, #23 y #24.

### Correcciones de la auditoría (2026-09-27)
- **Falso positivo en el #30 (H-118).** La revisión decía `CORRECCIÓN: Ninguno` y `VEREDICTO: LISTO`, pero agregó una sección "Comprobado:" con viñetas entre CORRECCIÓN y OPCIONAL. `veredicto-revision.mjs` toma como sección de corrección todo lo que hay hasta OPCIONAL, así que contó esas viñetas como hallazgos. Jordin integró el #30 con `--admin`.
- **La regla nueva es conservadora.**
  - Si la sección de corrección empieza con "Ninguno", termina en la siguiente sección que agregue la revisión: un título de Markdown (`### Lo que comprobé`) o una línea corta que no es un elemento de lista y termina en ":".
  - Con cualquier otro comienzo, todo sigue contando como hallazgo, así que un hallazgo nunca queda oculto bajo un título.
  - Un "Ninguno" seguido de un hallazgo numerado también falla.
  - Una línea que menciona la corrección ("### CORRECCIÓN…", "Corrección adicional:") nunca cierra la sección, así que un segundo encabezado de corrección no oculta sus hallazgos.
  - Las viñetas (`-`, `*`, `+`) y las enumeraciones (`1.`, `a)`) que terminan en ":" siguen siendo hallazgos, no títulos.
  - Una línea separadora (`---`) no cuenta como hallazgo.
- **`revision.md`** pide no agregar secciones entre CORRECCIÓN y OPCIONAL, y poner lo comprobado antes de CORRECCIÓN.
- **Pruebas:** 2 nuevas en `scripts/veredicto-revision.test.mjs`.
  - Una usa la estructura real del #30, con variantes del título.
  - La otra comprueba que una sección posterior no oculta hallazgos: ni un hallazgo antes del título, ni un segundo encabezado de corrección, ni una viñeta o una enumeración con letra que termine en ":".
- **Comprobación con datos reales:** se recalculó el veredicto de todas las revisiones reales de los PR #16 a #30 con el script anterior y con el nuevo. Solo cambia el #30.

**Auditoría final (paso 17):**
- **H-135:** el criterio 1 dice que la acción v1 está fijada por SHA (H-109).
- **H-137:** una prueba de `reglas-repositorio.test.mjs` comprueba el cableado del check obligatorio `revision-claude`:
  - el job no tiene `if`;
  - la acción tiene `continue-on-error: true`;
  - el veredicto se decide con `if: ${{ !cancelled() }}`.

### Correcciones de la auditoría (2026-09-28)

- **H-151:** `.claude/settings.json` niega `gh pr merge *--admin*` a los agentes, así que solo Jordin, desde su terminal, integra un falso positivo del check (criterio 5 y B11). El modo automático no deja que un agente edite sus propios permisos, así que Jordin agregó la regla a mano el 28 sep. Una prueba de `reglas-repositorio.test.mjs` comprueba que `--admin` está negado y que el *auto-merge* normal sigue permitido.

### Calendario diario (2026-09-28)

Jordin pidió que los agentes y el equipo sigan un calendario por día para cumplir las entregas. Sus decisiones del 28 sep: el campo se llama `programada`, el aviso diario menciona solo a quien tiene algo ese día o algo atrasado, el cambio va como `[JG-03]` y las fechas son las del calendario que acordó ese día (JG-04, EM-04 y DC-03 el lunes 28).

**Criterios de aceptación:**

12. **Campo `programada: AAAA-MM-DD`** en cada tarea no hecha: el día en que el calendario espera que se integre. Es una meta, no una restricción: `depende_de` y `no_antes_de` siguen decidiendo si se puede empezar. `--validar` rechaza:
    - una tarea no hecha sin `programada`, o con otro formato;
    - una fecha anterior a `no_antes_de`;
    - una fecha igual o anterior a la de una dependencia que todavía no está hecha (se empieza, como pronto, el día siguiente);
    - un `docs/plan/calendario.md` cuya tabla por día no coincida con las fechas de los archivos.
13. **`scripts/tareas.mjs` sigue el calendario:**
    - ordena por `programada` (las tareas sin fecha van al final), así que `--persona`, `--siguiente` y "continúa" toman primero lo atrasado y luego lo más cercano;
    - marca como **atrasada** (⏰) toda tarea no hecha cuya fecha ya pasó;
    - `--hoy [persona]` muestra, por persona, lo programado para hoy, lo atrasado, quién de las otras personas lo espera y la tarea siguiente;
    - `--calendario` imprime la tabla por día (una columna por persona) y `--calendario --escribir` la reescribe entre las marcas de `docs/plan/calendario.md`;
    - `--json` agrega `programada`.
14. **Recordatorio diario en el tablero.** En la primera ejecución del día a partir de las 07:00 de Guatemala (la del cron), el comentario del tablero empieza con "Tareas del día":
    - menciona solo a quien tiene una tarea no hecha programada para hoy o atrasada; primero las atrasadas;
    - dice si la tarea todavía espera a alguien o está bloqueada, y quién de las otras personas la espera;
    - sale una sola vez al día: el día se guarda en el estado del issue (`recordatorio`). Si nadie tiene nada, no se comenta;
    - va en el mismo comentario que las novedades, antes de ellas;
    - como las novedades, no sale en la primera ejecución, sin estado anterior (criterio 8), sino en la siguiente.
15. **El cuerpo del tablero** muestra una sección "Hoy" con lo de cada persona y la fecha programada de cada tarea (⏰ si está atrasada), sin menciones.
16. `AGENTS.md`, `protocolo.md` (§A, §B, §C y B12), `calendario.md`, `README.md` del plan, la plantilla y `prompts/convergencia.md` explican el calendario y cómo reprogramarlo.

**Pruebas:** 8 en `scripts/tareas.test.mjs` (validación de `programada`, orden, atrasadas, `fechaCorta`, la tabla, el reemplazo entre marcas con LF y CRLF y el plan real) y 8 en `scripts/tablero.test.mjs` (a quién menciona el recordatorio, qué dice si la tarea espera o está bloqueada, una vez al día desde las 07:00, nada antes de esa hora, nada en la primera ejecución, sin comentario si nadie tiene nada, un solo comentario con las novedades y la sección "Hoy" del cuerpo). Se ajustaron 2 pruebas existentes por el campo nuevo del estado y del JSON.

**Resultado:**
- 52 tareas con `programada`, del 28 sep al 29 oct. Adelantan tareas de un avance posterior para repartir la carga: JG-09, EM-09, EM-12, DC-09, JZ-10, JZ-11 y JZ-14.
- `generar(tareas, cuerpoAnterior, fecha, hora)` recibe la hora para poder probarla; el workflow no cambió: ya corría a las 07:00 con `TZ=America/Guatemala`.
- **Decisiones:**
  - El recordatorio espera a las 07:00 para no marcar como atrasada, pasada la medianoche, una tarea que alguien está por integrar.
  - "La esperan" nombra solo a otras personas; las dependencias propias ya se ven en la tarea siguiente.
  - Las tareas hechas conservan su `programada` y siguen en la tabla por día, para que la tabla no cambie cada vez que se integra algo.

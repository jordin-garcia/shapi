# Protocolo de trabajo para agentes de IA

Este es el procedimiento **obligatorio** para cualquier agente (Claude Code, Codex, Antigravity, Copilot, Cursor, Gemini u otro) que trabaje en Shapi. Su objetivo es que el agente complete una tarea de principio a fin **sin intervención humana**, salvo cuando necesite algo a lo que no puede acceder.

> Para las personas: no necesitan memorizar esto. Basta con pedirle a su agente "¿qué me toca?" y luego "implementa <ID>". La guía rápida está en `docs/plan/README.md`.

---

## A. Buscar tareas ("¿qué me toca?")

1. **Identifica a la persona** con la tabla de `AGENTS.md`. Si no puedes deducirla con seguridad, pregúntale: "¿Eres Jordin, Emilio, Dominique o José Pablo?".
2. **Actualiza tu copia**:
   ```
   git switch main
   git pull --ff-only
   ```
   Si hay cambios locales sin guardar, **no los descartes**: díselo a la persona y pregúntale qué hacer.
3. **Lista sus tareas**: `node scripts/tareas.mjs --persona <clave>`.
4. **Lee el archivo** de cada tarea **disponible** y las últimas 3 entradas de `docs/plan/bitacora/<persona>.md`.
5. **Busca los avisos que le dejaron los demás.** En las bitácoras de las otras personas (`docs/plan/bitacora/*.md`), revisa las líneas de "Pendiente o aviso para otros" dirigidas a esta persona. Se reconocen porque empiezan en negrita con:
   - un ID de sus tareas (`**EM-03:**`, o también `**JG-04 y EM-03:**`);
   - su nombre (`**Emilio:**`);
   - `**Todos:**`.

   Puedes usar la herramienta de búsqueda de tu harness, o `grep -n "\*\*.*\(EM-\|Emilio\|Todos\)" docs/plan/bitacora/*.md` cambiando el prefijo y el nombre. Quédate con los avisos de tareas que todavía no están hechas.
6. **Responde** con:
   - las tareas disponibles, en el orden que da el script: ID, título y un resumen de 1 o 2 líneas de lo que hará;
   - las tareas en espera y de quién dependen, por si la persona quiere avisarle a un compañero;
   - los avisos que le dejaron los demás (paso 5), indicando quién lo dejó y en qué tarea;
   - la siguiente tarea recomendada.
7. **No implementes nada** hasta que te lo pidan.

---

## B. Implementar una tarea ("implementa <ID>" o "continúa")

Si la persona dice "continúa" o "la siguiente", toma la primera disponible con `node scripts/tareas.mjs --siguiente <clave>`. Excepción: si el coordinador dice "continúa la auditoría" o "siguiente paso de la auditoría", o si la sesión ya está corrigiendo un plan de auditoría, se sigue §E3.

Desde aquí trabaja **sin pedir confirmación** en cada paso. Solo te detienes en los casos de §C.

### B1. Comprobar que se puede empezar
- Ejecuta `node scripts/tareas.mjs --ver <ID>`. Si no está disponible, explica por qué y detente.
- Comprueba que la tarea es de esta persona. Si no lo es, detente.
- Excepción: el coordinador puede corregir tareas ya hechas o terminar tareas de otras personas (§E3 y §E4).

### B2. Preparar la rama
```
git switch main && git pull --ff-only
git switch -c <persona>/<ID>-<descripcion-corta>      # ejemplo: emilio/EM-02-registro-proveedor
```
Si el coordinador termina el PR abierto de otra persona (§E4), la rama parte de la rama de ese PR y no de `main`.

### B3. Entender la tarea
- Lee el archivo de la tarea **completo**.
- Lee **todas** las secciones de especificación que enumera en "Contexto que debes leer". No leas todas las especificaciones enteras: lee lo que se indica.
- Si la tarea tiene pantallas, abre el `.dc.html` de cada una en `mockups/` y fíjate en los textos, datos, orden, estados y colores. El catálogo está en `docs/specs/11-interfaz.md`.
- Lee `docs/plan/convenciones.md` si todavía no lo has hecho en esta sesión.
- Busca en las bitácoras de los demás los avisos dirigidos a **esta tarea** (`**<ID>:**`) o a tu persona, como en §A paso 5, y tenlos en cuenta al implementar. Si un aviso contradice la tarea o la especificación, aplica §C.
- Si algo es ambiguo o contradictorio, aplica §C antes de escribir código.

### B4. Contrato primero (si la tarea tiene endpoints)
- Define o actualiza los endpoints en `contratos/openapi/<modulo>.yaml`, siguiendo las convenciones de §5 de `docs/plan/convenciones.md`.
- Si la tarea incluye frontend, ejecuta `pnpm generar:api` en `frontend/`.

### B5. Pruebas primero
- Convierte **cada criterio de aceptación** en al menos una prueba automatizada. Pon el código del requisito en el nombre o en un comentario (por ejemplo `// RF-28`).
- Ejecuta las pruebas y confirma que **fallan por la razón esperada** (fase roja).
- El tipo de prueba depende de lo que se prueba:
  - reglas del dominio: prueba unitaria en `tests/Shapi.Dominio.Tests`;
  - endpoints y persistencia: prueba de integración con `WebApplicationFactory` y Testcontainers en `tests/Shapi.Api.Tests`;
  - compuerta: `tests/Shapi.Compuerta.Tests`;
  - frontend: Vitest y Testing Library, con MSW para simular la API.

### B6. Implementar
- Escribe el código mínimo que haga pasar las pruebas, respetando la arquitectura de `docs/specs/06-arquitectura.md` y la tabla de propiedad de `docs/plan/convenciones.md` §2.
- **Solo toca los archivos que la tarea indica** en "Archivos que creas o modificas", más las pruebas, el contrato, la especificación relacionada y tu bitácora. Si necesitas tocar otro archivo, aplica §C. La excepción es el coordinador, que puede tocar cualquier archivo (§E1).
- Haz *commits* pequeños y frecuentes, con mensajes en español: `feat(identidad): registro del proveedor (EM-02)`.

### B7. Verificar
- Ejecuta **todos** los comandos de la sección **Verificación** de la tarea, además de estos:
  - `node scripts/tareas.mjs --validar`
  - `dotnet build Shapi.slnx`, `dotnet test Shapi.slnx` y `dotnet format Shapi.slnx --verify-no-changes`, si tocaste el backend
  - `pnpm lint`, `pnpm typecheck`, `pnpm test` y `pnpm build`, en `frontend/`, si tocaste el frontend
- Si algo falla, **corrige la causa**. Nunca debilites las pruebas.
- Guarda la salida resumida de cada comando: es la evidencia del PR.

### B8. Verificar pantallas (si la tarea tiene pantallas)
- Levanta el entorno. La forma de hacerlo está en `docs/plan/instalacion.md` §5.
- Toma una captura de cada estado de la pantalla, con `pnpm captura <url> <archivo>` en `tests/e2e/` o con la herramienta de navegador de tu harness.
- **Compárala con el mockup**, abriendo el `.dc.html` en el navegador. Si tu harness no puede ver imágenes, compara textos y datos contra el HTML del mockup.
- Corrige cualquier diferencia de textos, datos, orden o estados. No se exige que coincida pixel por pixel, pero sí el contenido y la estructura.

### B9. Revisión en contexto limpio
- Pide una revisión **a un agente que no escribió el código**, con las instrucciones de `docs/plan/prompts/revision.md`:
  - **Claude Code:** el subagente `revisor`.
  - **Codex:** una sesión nueva, o `/review` si está disponible, con ese archivo como instrucción.
  - **Antigravity, Cursor, Copilot u otro:** una conversación o agente nuevo con ese archivo como instrucción.
- Corrige **todos los hallazgos de corrección**: requisitos, errores, seguridad, pruebas que faltan. Los hallazgos de estilo u opcionales se anotan en el PR y no son obligatorios.
- Si corregiste algo, vuelve a B7.

### B10. Cerrar la tarea en el mismo PR
- **Especificaciones:** si precisaste un comportamiento que la especificación no decía, agrégalo en la sección correspondiente de `docs/specs/`.
- **Archivo de la tarea:** cambia `estado: pendiente` por `estado: hecha` y agrega al final una sección `## Resultado` con lo que se hizo, las decisiones tomadas y los archivos principales.
- **Bitácora:** agrega una entrada al **final** de `docs/plan/bitacora/<persona>.md` con este formato:
  ```
  ## AAAA-MM-DD · <ID> · <título>
  - Hecho: ...
  - Decisiones: ...
  - Pendiente o aviso para otros: ...
  ```
  Cada aviso para otra persona va en su propia línea y empieza en negrita con **a quién va dirigido**: los IDs de las tareas afectadas (`**JZ-01:**`, `**JG-04 y JG-07:**`), el nombre de la persona (`**José Pablo:**`) o `**Todos:**`. Así el agente de esa persona lo encuentra en §A paso 5 y en B3. Si el aviso cambia lo que una tarea debe hacer, además sigue §C: el aviso no reemplaza a la tarea ni a la especificación.

### B11. Abrir el PR e integrarlo
```
git push -u origin <rama>
gh pr create --title "[<ID>] <título>" --body-file <archivo con la plantilla completada>
gh pr merge --auto --squash --delete-branch
gh pr checks --watch
```
- **El título es obligatorio en el formato `[<ID>] <título>`**, con el ID de una tarea que existe; por ejemplo, `[EM-03] Pantallas de registro, verificación y acceso`. El job `plan` de la CI rechaza cualquier otro, porque sin el ID la revisión automática no revisa criterios ni alcance. Una corrección posterior de una tarea ya hecha usa el ID de esa tarea. Si el título está mal, edítalo con `gh pr edit --title "[<ID>] <título>"`: la CI y la revisión se vuelven a ejecutar solas.
- El cuerpo del PR sigue `.github/pull_request_template.md`, con la evidencia de B7 y B8 y el resultado de B9.
- **Si falla una verificación de la CI:** lee el registro (`gh run view --log-failed`), corrige, haz *commit* y *push*, y vuelve a esperar. Tienes como máximo 5 intentos; después aplica §C.
- **Si GitHub dice que la rama está desactualizada:** ejecuta `gh pr update-branch`, o haz `git pull --rebase origin main`, resuelve los conflictos según `docs/plan/convenciones.md` §3 y haz `git push --force-with-lease` **sobre tu rama**. Luego espera la CI otra vez. Si tu rama parte de la de otra persona (§E4), ponla al día con `gh pr update-branch` sobre **tu** PR o con un *merge* de `main` en tu rama, sin *rebase* ni *push* forzado. Nunca hagas *push* a la rama del dueño.
- El PR se integra solo cuando todas las verificaciones obligatorias pasan (auto-merge): `plan`, `backend`, `frontend` y `revision-claude`. **No hace falta que ningún humano lo apruebe.**
- **`revision-claude`** falla si la revisión automática del último *commit* tiene hallazgos de corrección: corrígelos, haz *push* y espera la revisión nueva. También falla si la revisión no se completó (cuota agotada, caída del servicio o tiempo agotado): vuelve a ejecutarla con `gh run rerun <id del run> --failed`. Un *commit* que ya tiene una revisión completa no se revisa otra vez, ni con `rerun` ni reabriendo el PR, salvo que cambie la tarea del título. Si crees que un hallazgo es un falso positivo, explícalo en el PR y avísale al coordinador. Solo él puede integrar con `gh pr merge <n> --admin --squash`.
- **Antes de JG-01** todavía no hay CI ni auto-merge: si `gh pr merge --auto` falla por eso, integra con `gh pr merge --squash --delete-branch` después de tu verificación local.

### B12. Terminar
- Ejecuta `git switch main && git pull --ff-only`.
- Dale a la persona un resumen breve: qué se hizo, el enlace al PR, la evidencia y las decisiones tomadas.
- Muéstrale sus próximas tareas disponibles con `node scripts/tareas.mjs --persona <clave>`.
- Si pidió que continuaras con varias tareas, sigue con la siguiente.

---

## C. Cuándo detenerse y qué hacer

| Situación | Qué hacer |
|---|---|
| La especificación no dice algo, la decisión es **local y reversible** (un nombre interno, un orden de columnas, un texto de error) y es coherente con el resto | Decide lo más simple, sigue adelante y anótalo en "Decisiones tomadas" del PR. Si afecta el comportamiento visible, agrégalo a la especificación |
| La especificación **se contradice** o contradice a un mockup | Manda la especificación, en este orden: `docs/specs/12-decisiones.md` > la especificación del tema > `11-interfaz.md` > el mockup. Documenta la contradicción en el PR y corrige el documento de menor rango |
| La decisión es **de alto impacto**: dependencia nueva, cambio de esquema fuera de la tarea, un cambio a un ADR, seguridad, o modificar archivos de otra persona | **Detente y pregúntale a la persona**, con las opciones y tu recomendación. Si la persona es el coordinador (`jordin`), modificar archivos de otra persona **no** requiere preguntar: sigue §E. Lo demás de esta fila sí |
| Necesitas **información o acceso externo**: credenciales, secretos, cuentas, una decisión que solo el equipo puede tomar | Detente y explica exactamente qué necesitas y por qué. Si la persona no puede darlo ahora, marca la tarea como bloqueada (ver abajo) |
| Una **dependencia** no funciona como dice su tarea, porque un compañero dejó un error | No lo arregles en su código. Crea una tarea nueva para esa persona (ver abajo), avísale a tu persona y, si puedes avanzar con un mock, sigue. **Excepción:** si la persona es el coordinador (`jordin`), puede corregirlo directamente en el código de su compañero, siguiendo §E |
| 5 intentos fallidos con la CI, o sigues atascado en el mismo error después de 3 enfoques distintos | Detente y explícale a la persona el problema, lo que intentaste y las opciones |

**Marcar una tarea como bloqueada.** Cambia `estado: bloqueada` y agrega la línea `bloqueo: <qué falta y quién puede resolverlo>` en los metadatos. Haz un PR que **solo** cambie ese archivo y la bitácora, con el título `[<ID>] Bloqueada: <motivo>`, e intégralo igual que en B11.

**Crear una tarea nueva**, por un error ajeno o por algo que falta:
- Copia `docs/plan/tareas/_plantilla.md` a `docs/plan/tareas/<PREFIJO>-<siguiente número libre de esa persona>-<descripcion>.md`.
- Complétala con la persona responsable según la tabla de propiedad, `avance` y `prioridad`, y agrégala en tu PR.
- En la bitácora, anota que la creaste.

---

## D. Reglas para no pisarse

- Cada persona trabaja **solo** en sus tareas y en los archivos que le pertenecen (`docs/plan/convenciones.md` §2). El coordinador es la excepción (§E).
- Los **archivos calientes** tienen reglas especiales para resolver conflictos (`docs/plan/convenciones.md` §3).
- Integra seguido: una tarea por PR, y cada PR se integra el mismo día en que se termina.
- Antes de empezar cualquier tarea, actualiza `main`.
- Si vas a usar código que dejó una tarea de otra persona, lee su sección `## Resultado`: ahí está lo que hizo y cómo usarlo.

---

## E. Auditoría del coordinador después de cada integración

Las pruebas, la CI y la revisión automática no bastan para detectar todo lo que incumple una tarea o una especificación. Por eso el coordinador, **Jordin García** (persona `jordin`, usuario `jordin-garcia` en GitHub), audita cada tarea que se integra en `main` y corrige lo que encuentra.

### E1. Autorización permanente

En cualquier PR suyo, sea de auditoría o no, el coordinador puede:
- modificar el código, las pruebas, los contratos, las migraciones, los archivos calientes, los archivos de tarea, las bitácoras y la documentación de **cualquier persona**;
- terminar o corregir tareas de otros, incluidos sus PR abiertos;
- borrar archivos que sobran.

No necesita preguntar antes ni crear una tarea nueva para la persona dueña. Esta autorización prevalece sobre las reglas de **propiedad** que digan lo contrario: B1, B2 (solo en §E4), B6, §C, §D, `AGENTS.md` (paso 3, "Siempre" en cuanto a la rama de §E4, "Preguntar antes" y "Nunca"), `docs/plan/README.md` y `docs/plan/convenciones.md` §2 y §3.

La autorización cubre **solo quién puede editar qué**. Todo lo demás sigue vigente, en particular:
- **Git y CI:** nada de *push* directo a `main`, nada de `--no-verify`, y la CI sigue siendo obligatoria.
- **Rama de otra persona:** nunca se hace *push* a la rama de otra persona, ni forzado ni normal. Para terminar su trabajo se sigue §E4.
- **Preguntar antes:** para agregar una dependencia, cambiar un ADR o usar cuentas o credenciales.
- **Pruebas:** no se debilitan ni se omiten.
- **Reglas técnicas de convenciones §3:** no editar el snapshot a mano, regenerar el lockfile, no editar `Program.cs`, etc.

Cada vez que un PR del coordinador toca archivos de otra persona, su entrada en `docs/plan/bitacora/jordin.md` lleva un aviso en negrita para esa persona (`**Emilio:**`), con los archivos que cambiaron y la indicación de actualizar su rama desde `main`.

### E2. Cómo se audita ("audita <ID>" o "audita lo integrado")

1. **Qué se audita.** Las tareas integradas en `main` después de la fecha del `docs/plan/auditoria-*.md` más reciente (`gh pr list --state merged --search "merged:>=AAAA-MM-DD"` y `git log main`, descartando las que ese plan ya auditó). Si Jordin nombra una tarea, solo esa.
2. **Revisión de cada tarea**, preferiblemente con subagentes o sesiones en contexto limpio, una por tarea o área:
   - Leer completos el archivo de la tarea (incluido `## Resultado`), las especificaciones de "Contexto que debes leer" y el código actual. No basta con el diff.
   - Por cada criterio de aceptación y cada prueba obligatoria, comprobar que hay código que lo cumple **y** una prueba que lo verifica de verdad.
   - Contrastar con la especificación: tablas, códigos de error, nombres del glosario, estados, esquema y contratos.
   - Si la tarea tiene pantallas, contrastar con su mockup.
   - Revisar la seguridad y los errores del punto 4 y del punto 5 de `docs/plan/prompts/revision.md`.
   - Revisar el cierre según B10: `## Resultado`, bitácora y contrato.
3. **Verificación completa.** Ejecutar los comandos de B7. Si una prueba falla, repetirla sola para descartar fallos del entorno, por ejemplo de Docker.
4. **Registro.** Escribir `docs/plan/auditoria-<AAAA-MM-DD>.md`:
   - hallazgos numerados `H-01`, `H-02`… con su severidad y `archivo:línea`;
   - agrupados en pasos, uno o más por tarea original;
   - con cada duda que decide Jordin marcada **❓ Decisión pendiente**.
5. **Detenerse.** Presentarle a Jordin el resumen y el plan, y **no corregir nada** hasta que lo indique.
6. **Integrar el plan.** El plan entra a `main` en el primer PR de E3, junto con su primera corrección. Si Jordin prefiere integrarlo solo, se usa la rama `jordin/JG-01-auditoria-plan-<AAAA-MM-DD>` y el PR `[JG-01] Plan de la auditoría <AAAA-MM-DD>`.

### E3. Cómo se corrige una tarea ya integrada

El agente de Jordin sigue la sección B con estas diferencias:
1. **B1:** la tarea ya está `hecha` y puede ser de otra persona. No es motivo para detenerse.
2. **Decisiones pendientes:** al empezar cada paso del plan, se le preguntan a Jordin las **❓ Decisión pendiente** de ese paso, antes de escribir código.
3. **Rama:** `jordin/<ID>-auditoria-<tema>`, donde `<ID>` es la tarea original.
4. **Título del PR:** `[<ID>] Correcciones de la auditoría: <tema>`. Cada PR lleva el ID de la tarea original, aunque un paso del plan necesite varios PR.
5. **Hallazgos:** cada hallazgo corregido lleva una prueba que lo habría detectado, salvo que sea solo de documentación. Su casilla se marca en el plan de la auditoría, en el mismo PR.
6. **Archivo de la tarea:** dentro de `## Resultado`, se agrega o se completa la subsección `### Correcciones de la auditoría (AAAA-MM-DD)`, con la fecha del PR, qué se corrigió y por qué. El estado de la tarea no cambia.
7. **Bitácora:** la entrada va en `docs/plan/bitacora/jordin.md`, con el aviso de E1.
8. **Ritmo:** salvo que Jordin pida otra cosa, se avanza un paso del plan a la vez. Al terminar cada paso se le informa y se espera su indicación.

### E4. Cómo se termina una tarea de otra persona que no está integrada

Por ejemplo, un PR abierto que quedó con la CI en rojo o con hallazgos de la revisión.
1. **Aviso previo:** preguntarle a Jordin si ya le avisó al dueño, para que no siga trabajando en su PR al mismo tiempo.
2. **Rama nueva:** `jordin/<ID>-<descripcion-corta>`, creada a partir de la rama del PR del dueño (`git fetch origin <rama>` y `git switch --no-track -c jordin/<ID>-… origin/<rama>`), para conservar sus *commits*. Nunca se hace *push* a la rama del dueño. Para ponerla al día se hace un *merge* de `main` en la rama nueva.
3. **PR nuevo:** su autor es el coordinador, y así la revisión aplica E5. Al abrirlo, se cierra el PR original con un comentario que enlaza al nuevo (`gh pr close <n> --comment "Lo continúa #<nuevo> (protocolo §E4)"`), sin borrar la rama del dueño.
4. **Cierre:** B10 y B11 normales. El título es `[<ID>] <título de la tarea>`, la tarea queda con `estado: hecha` y lleva un `## Resultado` completo, donde se menciona que la terminó el coordinador a partir del trabajo de su dueño.
5. **Bitácora:** la entrada va en `docs/plan/bitacora/jordin.md`, con el aviso de E1.

### E5. Revisión en contexto limpio (B9) de los PR del coordinador

Si el autor del PR es el coordinador, el revisor no marca como problema de alcance que se toquen archivos de otra persona. En cambio, revisa estos tres puntos:
- cada cambio corresponde a un hallazgo de la auditoría, al título del PR o a la tarea del PR;
- no se debilitó ninguna prueba;
- la bitácora tiene el aviso para cada persona dueña de un archivo modificado.

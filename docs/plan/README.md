# Plan de desarrollo de Shapi: guía rápida

> **Si es la primera vez que trabajas así, lee esta página completa (10 minutos).** Todo lo demás lo lee tu agente de IA.

## ¿Cómo trabajamos?

Usamos **desarrollo dirigido por especificaciones** (*spec-driven development*). La idea es simple:

1. **Primero se escribe qué hay que construir.** Eso ya está hecho: `docs/specs/` describe todo el sistema (requisitos, pantallas, base de datos, reglas) y `mockups/` muestra cómo se ve cada pantalla.
2. **El trabajo está dividido en tareas pequeñas.** Cada una está en un archivo de `docs/plan/tareas/`, con su responsable, qué hacer, qué leer, qué archivos tocar, cómo verificar que funciona y de qué otras tareas depende.
3. **Tu agente de IA hace el trabajo.** Busca tus tareas, las implementa con pruebas, las verifica, abre el *pull request* y lo integra a `main` cuando la integración continua (CI) pasa. Tú solo le das la instrucción y le respondes si necesita algo que no puede conseguir solo.

Todo agente (Claude Code, Codex, Antigravity, Copilot, Cursor, Gemini…) lee el archivo `AGENTS.md` de la raíz del proyecto. Ahí están las reglas y el procedimiento, así que no tienes que explicarle nada.

## Antes de empezar (una sola vez)

Sigue [`instalacion.md`](instalacion.md): herramientas, cuenta de GitHub, clonar el repositorio y configurar tu agente.

## Tu rutina en cada sesión de trabajo

Abre tu agente **dentro de la carpeta `shapi`** y usa estos tres mensajes.

**1. Ver qué te toca:**
> Soy **<tu nombre>**. Revisa el plan y dime qué tareas me tocan.

Te va a mostrar las tareas **disponibles** (las que ya puedes hacer), las que están **en espera** (y de quién dependen) y cuál te recomienda.

**2. Mandarla a hacer:**
> Implementa la tarea **<ID>** siguiendo el protocolo. No te detengas hasta integrarla, salvo que necesites algo que no puedas obtener.

Mientras tanto puedes hacer otra cosa. El agente escribe las pruebas y el código, verifica, abre el PR, espera la CI y lo integra.

**3. Seguir con la próxima (opcional):**
> Continúa con mi siguiente tarea disponible.

**Solo en Claude Code**, para que trabaje solo hasta terminar, ponlo en modo auto y usa:
> `/goal La tarea <ID> está integrada en main: su PR está mergeado, el archivo de la tarea dice "estado: hecha" y todos los comandos de su sección Verificación pasaron`

## ¿Cuándo te va a preguntar algo tu agente?

Solo en estos casos:
- Necesita una **credencial, una cuenta o un acceso** que no tiene.
- Encontró una **decisión importante** que la especificación no resuelve: una librería nueva, un cambio en archivos de otro compañero o algo de seguridad. Te dará opciones y una recomendación. Si no estás seguro, pregúntale a Jordin.
- Algo **falla una y otra vez**: lleva 5 intentos con la CI o 3 enfoques distintos para el mismo error.

Si no puedes responder en ese momento, pídele: "Marca la tarea como bloqueada con este motivo: …". Así el resto del equipo lo ve.

## Qué hay en esta carpeta

| Archivo | Para qué sirve |
|---|---|
| [`instalacion.md`](instalacion.md) | Qué instalar y configurar, por sistema operativo y por agente |
| [`calendario.md`](calendario.md) | Fechas, qué tareas van en cada avance y el guion de cada demostración |
| [`protocolo.md`](protocolo.md) | El procedimiento que sigue el agente, paso a paso (no necesitas memorizarlo) |
| [`convenciones.md`](convenciones.md) | Estructura del código, qué carpeta es de quién y reglas técnicas |
| [`tareas/`](tareas/) | **Una tarea por archivo.** El nombre empieza con el prefijo de su responsable |
| [`bitacora/`](bitacora/) | Un diario por persona, donde el agente anota qué hizo en cada tarea |
| [`prompts/`](prompts/) | Instrucciones para la revisión automática y para la convergencia |

## Quién hace qué

| Persona | Prefijo | Responsabilidad |
|---|---|---|
| **Jordin García** | JG | Coordinación, CI, compuerta de tráfico, claves, medición del consumo y sus pantallas, convergencias y PDF de requisitos y diseño |
| **Emilio Méndez** | EM | Base de datos, identidad y sesiones (personal y consumidores), organizaciones y miembros, planes, suscripciones, pasarela simulada, pagos y renovaciones |
| **Dominique Contreras** | DC | Sistema de diseño, estructura del panel y del portal, publicación de APIs (A3), portal de marca blanca (A5) y cuenta del consumidor (B2) |
| **José Pablo Zúñiga** | JZ | Infraestructura (Docker, Caddy), orígenes de demostración, siembra, correo, bitácora, administración y soporte (A6, A7, B3), E2E, pruebas de carga y manuales |

Estado del plan en cualquier momento: `node scripts/tareas.mjs` (resumen del equipo) o `node scripts/tareas.mjs --persona <jordin|emilio|dominique|jose-pablo>`.

## Reglas de oro

1. **Nunca trabajes directo en `main`.** El agente siempre crea una rama y un PR.
2. **No edites a mano las tareas de otro compañero.** Si algo suyo falla, el agente crea una tarea nueva para esa persona. La excepción es Jordin, el coordinador: audita cada tarea que se integra en `main` y puede corregir directamente el trabajo de cualquiera. Cuando lo hace, te deja un aviso en su bitácora ([`protocolo.md`](protocolo.md) §E).
3. **Antes de cada sesión, el agente actualiza `main`.** Si te dice que tienes cambios sin guardar, no los descartes sin saber qué son.
4. **Si la CI está en rojo en `main`**, avisa al grupo: tiene prioridad sobre todo lo demás.
5. **Cumple tus horas cada semana** (unas 3). Las fechas de entrega no se mueven.

## Preguntas frecuentes

**¿Tengo que revisar el código que escribe el agente?** No es obligatorio: la CI, las pruebas y la revisión automática lo verifican. Pero conviene que leas el resumen del PR para poder explicar tu parte en la exposición.

**¿Qué pasa si mi tarea depende de la de otra persona que no ha terminado?** El script la muestra "en espera" y dice de quién depende. Avísale a esa persona o haz otra de tus tareas disponibles.

**¿Qué hago si el agente se equivocó o dejó algo mal?** Pídele que lo corrija en una tarea nueva: "Crea una tarea para corregir …". Nunca reviertas cambios de otros a mano. Si Jordin corrigió algo tuyo en su auditoría, el aviso está en `bitacora/jordin.md`: actualiza tu rama desde `main` antes de seguir.

**¿Puedo usar un agente distinto al de mis compañeros?** Sí. El plan funciona con cualquiera que lea `AGENTS.md`.

**¿Dónde veo cómo debe verse una pantalla?** Abre el paquete de su tanda en `mockups/` (por ejemplo `mockups/a3-publicacion-api.html`) con doble clic en el navegador.

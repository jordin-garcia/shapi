# Instrucciones para la revisión de convergencia

Úsalas en las tareas de convergencia de Jordin (JG-08, JG-14, JG-17). El objetivo es comparar **lo que dicen las especificaciones** con **lo que hay en `main`** y convertir cada brecha en una tarea. Es el equivalente de `/speckit.converge`.

## Pasos
1. `git switch main && git pull --ff-only`. Luego ejecuta `node scripts/tareas.mjs` para ver el estado general.
2. Levanta el entorno completo (`docs/plan/instalacion.md` §5) y la siembra de demostración, si ya existe.
3. Por cada requisito de `docs/specs/03-requisitos.md` que corresponde a este avance (según `docs/plan/calendario.md`):
   - busca el código que lo implementa y la prueba que lo verifica, buscando el código `RF-xx` en `tests/`;
   - clasifícalo como ✅ cumplido, ⚠️ parcial (falta prueba, algún criterio o alguna pantalla) o ❌ ausente.
4. Recorre el guion de demostración del avance (`docs/plan/calendario.md`) en el entorno levantado y anota lo que falla.
5. **Por cada ⚠️ o ❌ que no tenga ya una tarea pendiente**, crea una tarea nueva:
   - usa `docs/plan/tareas/_plantilla.md`;
   - asígnala al dueño según `docs/plan/convenciones.md` §2;
   - ponle un ID con el siguiente número libre de esa persona;
   - dale la prioridad que corresponda: P1 si el guion de demostración no funciona sin ella.
6. Si una persona tiene más tareas pendientes de las que puede hacer antes de la siguiente entrega (unas 4 por semana), baja a P3 las de menor valor y anótalo.
7. Escribe un informe en `docs/plan/convergencia/<AAAA-MM-DD>.md` con la tabla de requisitos (✅/⚠️/❌), el resultado del guion y las tareas creadas.
8. Cierra tu tarea de convergencia con el protocolo normal. El PR incluye el informe y las tareas nuevas.

## Reglas
- No implementes nada: solo evalúa y crea tareas.
- No cambies las decisiones de las especificaciones. Si encuentras una contradicción, crea una tarea para Jordin (`docs`) que la resuelva.

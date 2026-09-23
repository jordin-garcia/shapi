---
id: JG-03
titulo: Revisión automática con Claude en cada PR
persona: jordin
responsable: Jordin García
avance: 2
prioridad: P2
estado: pendiente
depende_de: [JG-01]
requisitos: [RNF-15]
pantallas: []
---

# JG-03 · Revisión automática con Claude en cada PR

**Responsable:** Jordin García · **Avance:** 2 · **Prioridad:** P2 · **Depende de:** JG-01

## Objetivo
Que cada *pull request* reciba automáticamente una revisión de Claude como comentario, usando el token de la suscripción Pro de Jordin. La revisión **no es obligatoria** para integrar, para que un límite de uso no frene al equipo.

## Contexto que debes leer
- Documentación oficial: https://code.claude.com/docs/en/github-actions (modo de automatización con `prompt`, `claude_args` y el secreto `CLAUDE_CODE_OAUTH_TOKEN`)
- `docs/plan/prompts/revision.md` (las instrucciones de revisión que se aplican)

## Archivos que creas o modificas
- `.github/workflows/revision-claude.yml` (crear)

## Criterios de aceptación
1. Se ejecuta `anthropics/claude-code-action@v1` en los eventos `pull_request` (`opened`, `synchronize`, `ready_for_review`, `reopened`), excepto en borradores, con `claude_code_oauth_token: ${{ secrets.CLAUDE_CODE_OAUTH_TOKEN }}`.
2. El `prompt` pide aplicar `docs/plan/prompts/revision.md` al PR (el ID sale del título `[XX-00]`) y publicar el resultado como **un comentario** en el PR. `claude_args` permite solo lo necesario: `--max-turns 15 --allowedTools "Read,Grep,Glob,Bash(git diff:*),Bash(gh pr view:*),Bash(gh pr diff:*),Bash(gh pr comment:*)"`.
3. El workflow también responde a `@claude` en comentarios de issues y PR (modo interactivo, en un *job* aparte) solo para usuarios con permiso de escritura, que es el comportamiento por defecto de la acción.
4. Un `concurrency` por número de PR cancela la revisión anterior cuando llegan *commits* nuevos.
5. El *job* **no** se agrega a las verificaciones obligatorias de la protección de `main`.

## Pruebas obligatorias
- Esta tarea no lleva pruebas unitarias. La prueba es que el propio PR de esta tarea reciba el comentario de revisión

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
node scripts/tareas.mjs --validar
gh pr view --comments   # en el PR de esta tarea debe aparecer el comentario de revisión de Claude
```

## ⚠️ Pasos que requieren a una persona
- Jordin ejecuta `claude setup-token` en su terminal. Luego le pasa el token al agente para que lo guarde con `gh secret set CLAUDE_CODE_OAUTH_TOKEN`, o lo guarda él mismo. El agente **no** puede generar el token.

## Fuera de alcance
- Hacer que la revisión sea obligatoria
- Revisiones con API key de pago

## Notas
- El consumo se descuenta del plan Pro de Jordin. Si se agota el cupo, la revisión falla sin bloquear nada. La revisión local del protocolo (B9) sigue siendo obligatoria.

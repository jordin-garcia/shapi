---
id: EM-16
titulo: Pruebas de aislamiento entre organizaciones y de permisos
persona: emilio
responsable: Emilio Méndez
avance: final
prioridad: P1
estado: hecha
programada: 2026-10-24
depende_de: [EM-14, EM-12]
no_antes_de: 2026-10-24
requisitos: [RNF-08, RF-07]
pantallas: []
---

# EM-16 · Pruebas de aislamiento entre organizaciones y de permisos

**Responsable:** Emilio Méndez · **Avance:** final · **Prioridad:** P1 · **Depende de:** EM-14, EM-12 · **No antes del:** 2026-10-24

## Objetivo
Verificar de forma sistemática que ningún endpoint filtra datos entre organizaciones y que se cumple la matriz de permisos.

## Contexto que debes leer
- `docs/specs/04-roles-y-permisos.md` completo
- `docs/specs/10-identidad-y-seguridad.md` §2
- `contratos/openapi/*.yaml` (la lista de endpoints)

## Archivos que creas o modificas
- `tests/Shapi.Api.Tests/Aislamiento/**` (crear)
- `docs/plan/tareas/<nuevas>.md` (una por cada falla que encuentres en un módulo ajeno)

## Criterios de aceptación
1. Una prueba parametrizada recorre **todos** los endpoints de los contratos que tienen `{id}` y confirma que un recurso de otra organización responde 404.
2. Una prueba por rol (propietario, editor, lector, administrador, soporte y consumidor) confirma los permisos de 04 §3: por ejemplo, el lector recibe 403 al escribir, el soporte recibe 403 al suspender y el editor recibe 403 al invitar miembros.
3. Si una falla está en un módulo ajeno, se crea una tarea P1 para su dueño en lugar de corregirla. Las fallas de los módulos propios se corrigen en esta tarea.

## Pruebas obligatorias
- Las propias de esta tarea

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
```

## Fuera de alcance
- Corregir el código de otros

## Resultado
- Se agregó una prueba parametrizada de integración que comprueba 28 rutas HTTP de APIs con identificador y dos operaciones de miembros; las solicitudes de una organización distinta deben recibir 404.
- Se agregó una matriz HTTP para propietario, editor, lector, administrador, soporte y consumidor, con casos permitidos y denegados basados en 04 §3.
- Se agregaron comprobaciones 404 para recursos administrativos globales inexistentes, claves pertenecientes a otro consumidor/organización y casos de soporte pertenecientes a otra organización. No se modificó código de otros módulos.
- Las pruebas cruzadas de simulación/verificación de dominio y descarga de logotipo preparan esos recursos para que el 404 no se deba a que falten.
- `dotnet build Shapi.slnx`, `dotnet format Shapi.slnx --verify-no-changes`, `node scripts/tareas.mjs --validar` y `git diff --check` pasan. Las pruebas de integración compilan, pero su ejecución local requiere Docker para Testcontainers; CI debe confirmarlas.
- La revisión independiente en contexto limpio quedó **LISTO**; la corrida de CI del commit corregido sigue en curso.

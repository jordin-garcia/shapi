---
id: EM-16
titulo: Pruebas de aislamiento entre organizaciones y de permisos
persona: emilio
responsable: Emilio Méndez
avance: final
prioridad: P1
estado: pendiente
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

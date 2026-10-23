---
id: JG-28
titulo: Pendientes de la auditoría del 3 de octubre (identidad, planes, contratación y compuerta)
persona: jordin
responsable: Jordin García
avance: final
prioridad: P3
estado: pendiente
programada: 2026-10-28
depende_de: [JG-23]
requisitos: [RF-03, RF-04, RF-18, RF-20, RF-30]
pantallas: []
---

# JG-28 · Pendientes de la auditoría del 3 de octubre

**Responsable:** Jordin García · **Avance:** final · **Prioridad:** P3 · **Depende de:** JG-23 (que también toca la contratación)

## Objetivo
Cerrar los hallazgos y las dudas de la auditoría del 3 oct que quedaron «para la próxima auditoría» (`docs/plan/auditoria-2026-10-03.md`, paso 13). Ya no habrá otra auditoría completa: el 23 oct Jordin decidió corregir todos los hallazgos encontrados hasta ahora. Esta tarea toma los de backend y documentación. Los de pantallas van en JG-26 (H-108, H-111, H-113, H-114 y H-115); los de infraestructura, en JG-27 (H-116, H-117 y H-118); y H-104, en JG-25. Los archivos son de EM-04, EM-05, EM-07, EM-08, JG-05, JG-06 y DC-05 (protocolo §E1).

Es P3: si no cabe antes del jueves 29, lo que falte queda fuera y se anota en la convergencia final (JG-17).

## Hallazgos que corrige
El texto completo de cada uno, con su archivo y línea, está en el paso 13 de `docs/plan/auditoria-2026-10-03.md`:
- **H-98:** el límite de 3 recuperaciones por hora no se serializa (`ServicioRecuperacion.cs:76-87`).
- **H-99:** faltan pruebas del portal para `recuperar` y `restablecer`, y para que un cambio de contraseña en Mi perfil reinicie los intentos.
- **H-100:** referencias viejas a «11 §Precisiones» en DC-08 y EM-04.
- **H-101:** la prueba de H-01 depende del DNS real y solo cubre el 502 (`LimitesYCuotasTests.cs:494`).
- **H-102:** JG-06 dice que todo 502 devuelve la cuota; desde #65, solo el 502 sin conexión.
- **H-103:** el 400 de un vencimiento de tarjeta mal escrito casi nunca se alcanza, y apunta a un campo que no existe (`ContratacionApi.cs:115-120`).
- **H-105:** editar o desactivar un plan puede cruzarse con una contratación en curso.
- **H-106:** los 404 de la contratación no traen `codigo`.
- **H-107:** dos pruebas de `PlanesTests.cs` (:245 y :270) sin el código del requisito.
- **H-112:** el `FOR UPDATE` de `RepositorioApis.cs:56` (la auditoría citaba `:39`) puede interbloquearse con la consolidación de JG-09. Se cambia por `FOR NO KEY UPDATE`.

## ❓ Decisiones pendientes (preguntarle a Jordin antes de escribir código)
Son las dudas del 3 oct que siguen abiertas. Cada una tiene su recomendación en `auditoria-2026-10-03.md`, paso 13:
- 1 (tiempo de respuesta y COMMIT);
- 2 (límite de recuperación por cuenta);
- 3 (medición y 502);
- 4 (el consumidor corta mientras se abre la conexión);
- 5 (formato del vencimiento, H-103);
- 7 y 8 (carreras de `EliminarHost` y `PublicarClave` en 07 §4);
- 10 (insignias de Agro);
- 11 (versión de OpenAPI en `GET /rutas`);
- 13 (el reinicio borra membresías);
- 15 (numeración de los casos).

La duda 6 la resuelve JG-25, la 9 la resuelve JG-26 y la 14 la resuelve JG-27. La 12 ya está resuelta, porque JZ-17 siembra el ambiente productivo en la CI.

Para avanzar rápido, se le pueden presentar a Jordin todas juntas, con la recomendación de cada una, y aplicar las que acepte.

## Contexto que debes leer
- `docs/plan/auditoria-2026-10-03.md`, paso 13 completo
- Las secciones de especificación que cita cada hallazgo (08 §3, 07 §4, 10 §8 y convenciones §5)

## Archivos que creas o modificas
- Los que cita cada hallazgo (modificar)
- `docs/plan/auditoria-2026-10-03.md` (marcar las casillas y anotar «Decidido (AAAA-MM-DD)» en cada duda)
- Las especificaciones que precise cada decisión
- Pruebas (crear o modificar)

## Criterios de aceptación
1. Cada hallazgo está corregido como lo describe su corrección en `auditoria-2026-10-03.md` y tiene su casilla marcada.
2. Cada duda tiene su decisión anotada, y la especificación correspondiente queda precisada.

## Pruebas obligatorias
- Cada hallazgo tiene una prueba que falla sin la corrección, salvo H-100, H-102 y H-107, que son de documentación o de nombres.

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
node scripts/tareas.mjs --validar
dotnet build Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
dotnet test tests/Shapi.Api.Tests
dotnet test tests/Shapi.Compuerta.Tests
```

## Fuera de alcance
- Funcionalidades nuevas.

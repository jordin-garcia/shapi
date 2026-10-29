---
id: JG-28
titulo: Pendientes de la auditoría del 3 de octubre (identidad, planes, contratación y compuerta)
persona: jordin
responsable: Jordin García
avance: final
prioridad: P3
estado: hecha
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

## Resultado
Se corrigieron los diez hallazgos (H-98 a H-103, H-105 a H-107 y H-112) y se decidieron las diez dudas (29 oct). Todas las casillas del paso 13 de `auditoria-2026-10-03.md` quedaron marcadas.

**Hallazgos**
- **H-98:** `ServicioRecuperacion.Solicitar` abre la transacción antes de contar y bloquea la fila de la cuenta (`usuario` o `consumidor`; `Guid.Empty` si no existe, con los mismos comandos).
- **H-99:** pruebas del portal para el mismo trabajo exista o no la cuenta, el límite de 3 por hora y `restablecer`; y la de Mi perfil, donde un cambio correcto reinicia los intentos.
- **H-100 y H-102:** referencias corregidas en DC-08, EM-04 y JG-06.
- **H-101:** la prueba de la tilde en `X-Shapi-Plan` usa un origen real en modo demostración y espera 200.
- **H-103:** `ErroresVencimiento` valida el formato antes de tokenizar en la contratación, la reactivación y el cambio de plan. Los errores van en `tarjeta.mesVencimiento` y `tarjeta.anioVencimiento`.
- **H-105:** `EditarPlan` y `DesactivarPlan` bloquean el plan con `FOR UPDATE`; la contratación y el cambio de plan lo bloquean con `FOR SHARE` y lo vuelven a leer.
- **H-106:** los 404 de la contratación traen `api_no_encontrada`, `plan_no_encontrado` o `suscripcion_no_encontrada`.
- **H-107:** dos pruebas de `PlanesTests` nombran RF-18.
- **H-112:** `BloquearApi` usa `FOR NO KEY UPDATE`. **Precisión:** como `FOR KEY SHARE` no choca con `FOR NO KEY UPDATE`, la consolidación de JG-09 pasó a `FOR SHARE`, para seguir esperando a una recarga de la especificación en curso. Lo cubre una prueba.

**Dudas:** 1, 2, 5, 7, 8, 10 y 15, como se recomendó. La 3 ya estaba resuelta por JG-09 (`LlamadasDescontadas` vuelve a 0). La 13 ya la cubre JG-27 (el reinicio borra todas las organizaciones proveedoras). La 11 se deja como está. **La 4 cambió:** la compuerta no puede saber si la petición llegó al origen cuando el consumidor corta, así que Jordin decidió que la cuota se descuenta.

**Especificaciones:** 07 §4 (ventanas de Redis y bloqueos de fila), 08 §3 (corte del consumidor y medición de la reserva devuelta), 09 §2 (formato del vencimiento) y 10 §1 y §8. Contrato `suscripciones.yaml` y tipos generados.

**Archivos principales:** `ServicioRecuperacion.cs`, `ContratacionApi.cs`, `GestionarPlanes.cs`, `RepositorioApis.cs`, `RepositorioConsolidacion.cs`; pruebas en `Identidad`, `Suscripciones`, `Planes`, `Consumo` y `Tuberia`, con el ayudante `Persistencia/EsperaDeBloqueos.cs`. También se actualizaron las líneas citadas en `docs/video/elementos/emilio.md`, que se movieron (JG-29, criterio 7).

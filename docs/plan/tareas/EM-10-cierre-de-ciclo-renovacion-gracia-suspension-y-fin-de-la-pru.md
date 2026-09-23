---
id: EM-10
titulo: Cierre de ciclo: renovación, gracia, suspensión y fin de la Prueba
persona: emilio
responsable: Emilio Méndez
avance: 3
prioridad: P1
estado: pendiente
depende_de: [EM-09, EM-08]
requisitos: [RF-22, RF-23, RF-44]
pantallas: []
---

# EM-10 · Cierre de ciclo: renovación, gracia, suspensión y fin de la Prueba

**Responsable:** Emilio Méndez · **Avance:** 3 · **Prioridad:** P1 · **Depende de:** EM-09, EM-08

## Objetivo
Automatizar en el trabajador la máquina de estados de las suscripciones de los dos niveles, y agregar el reloj ajustable del modo demostración.

## Contexto que debes leer
- `docs/specs/06-arquitectura.md` §5.5 (secuencia)
- `docs/specs/09-cobros-y-suscripciones.md` §3, §4 y §9
- `docs/specs/05-casos-de-uso.md` CU-16
- `docs/specs/10-identidad-y-seguridad.md` §6 (plantillas) y §7

## Archivos que creas o modificas
- `src/Shapi.Dominio/Suscripciones/**` (modificar: transiciones)
- `src/Shapi.Trabajador/CierreCiclo/**` (crear)
- `src/Shapi.Infraestructura/Comun/RelojSistema.cs` (modificar: desplazamiento desde `demo:reloj:desplazamiento` en Redis, solo con `SHAPI_MODO_DEMO=true`)
- `src/Shapi.Api/Suscripciones/**` (modificar: `POST /api/admin/demo/reloj` y `POST /api/portal/suscripcion/pagar`)
- `contratos/openapi/suscripciones.yaml` (modificar)
- `tests/*/Suscripciones/**`

## Criterios de aceptación
1. Cada minuto, el trabajador toma las suscripciones `activa` con `fin <= ahora` (`FOR UPDATE SKIP LOCKED`) y aplica el algoritmo de 06 §5.5 a las de plataforma y a las de API: primero aplica `plan_siguiente_id`; si el plan es de pago, cobra con el token (`esRenovacion=true`); si se autoriza, abre un ciclo nuevo desde el `fin` anterior; si se rechaza, pasa a `en_gracia` con `graciaHasta = fin + 7 días`; si el plan es gratuito, renueva sin cobrar; si es Prueba, pasa a `en_gracia`.
2. Las suscripciones `en_gracia` con `graciaHasta <= ahora` pasan a `suspendida`. Se publica en Redis (la organización o la suscripción) y la compuerta responde 403. Queda en la bitácora `suscripcion.suspendida` con el actor `sistema`.
3. Correos: `pago_rechazado`, `suscripcion_en_gracia`, `suscripcion_suspendida` y `prueba_por_vencer` (7 días antes del fin de la Prueba, una sola vez).
4. `POST /api/portal/suscripcion/pagar` `{tarjeta}` reactiva la suscripción del consumidor en gracia o suspendida, con un ciclo nuevo desde hoy.
5. `POST /api/admin/demo/reloj` `{adelantarDias}` (solo el administrador y solo con `SHAPI_MODO_DEMO=true`; si no, 404) mueve el `IReloj` de la API y del trabajador y dispara el cierre de ciclo.
6. Con la tarjeta 4000 0000 0000 0341: la contratación se aprueba; al adelantar 30 días entra en gracia; al adelantar 7 más, se suspende; y al pagar con 4242…, vuelve a estar activa.

## Pruebas obligatorias
- Integración del trabajo con `IReloj` falso para cada transición y cada nivel
- El escenario completo del criterio 6
- Idempotencia: ejecutar el trabajo dos veces no cobra dos veces

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
```

## Fuera de alcance
- Pantallas de gracia (B1.4 en EM-09, B2.3 en DC-11)

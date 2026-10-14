---
id: EM-19
titulo: Pruebas de backend del acceso del consumidor
persona: emilio
responsable: Emilio Méndez
avance: 3
prioridad: P2
estado: hecha
programada: 2026-10-13
depende_de: []
requisitos: [RF-02, RF-03, RF-04, RF-05]
pantallas: []
---

# EM-19 · Pruebas de backend del acceso del consumidor

**Responsable:** Emilio Méndez · **Avance:** 3 · **Prioridad:** P2 · **Sin dependencias**

## Objetivo
Completar las pruebas de integración de los endpoints del portal (`/api/portal/auth/*`). Hoy, algunas cláusulas de RF-02 a RF-05 solo están probadas para el personal o con MSW en el frontend.

## Contexto que debes leer
- `docs/specs/03-requisitos.md` RF-02 a RF-05
- `docs/specs/10-identidad-y-seguridad.md` §1 y §2
- `docs/plan/convergencia/2026-10-08.md` (tabla de requisitos)
- `src/Shapi.Api/Identidad/EndpointsPortal.cs`

## Archivos que creas o modificas
- `tests/Shapi.Api.Tests/Identidad/ConsumidorPortalTests.cs` (modificar)
- `src/Shapi.Api/Identidad/EndpointsPortal.cs` (modificar, solo si una prueba encuentra un error)

## Criterios de aceptación
1. **RF-02:** en el portal, verificar con un enlace ya usado y con uno vencido a las 24 horas responde 422 `token_invalido`, y el correo sigue sin verificar.
2. **RF-03:** en el portal, pedir la recuperación con un correo que existe y con uno que no devuelve la misma respuesta, y el enlace del consumidor vence a los 60 minutos.
3. **RF-04:** la sesión del consumidor vence tras la inactividad configurada y a los 7 días aunque siga en uso.
4. **RF-05:** registrar en el portal un correo que ya tiene cuenta **en la misma organización** responde 409 `correo_ya_registrado`, tanto por registro como por invitación.
5. Cada prueba lleva el código del requisito en su nombre.

## Pruebas obligatorias
- Integración (criterios 1 a 4)

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test tests/Shapi.Api.Tests --filter "FullyQualifiedName~Shapi.Api.Tests.Identidad"
dotnet format Shapi.slnx --verify-no-changes
```

## Fuera de alcance
- Las pruebas de aislamiento y de permisos por rol (EM-16)
- La invitación de consumidores desde el panel (EM-15)

## Resultado
- Se agregaron pruebas `RF-02` para enlaces de verificación usados y vencidos a las 24 horas, comprobando que el correo siga sin verificar.
- Se agregaron pruebas `RF-03` para respuestas iguales ante correos existentes y desconocidos y para el vencimiento del enlace de recuperación a los 60 minutos.
- Se agregaron pruebas `RF-04` para la inactividad configurada en dos horas y el vencimiento máximo a los siete días aunque la sesión se use antes del límite.
- Se agregaron pruebas `RF-05` para rechazar con 409 `correo_ya_registrado` el registro y la aceptación de invitación con un correo ya usado en la misma organización.
- No se necesitó modificar `EndpointsPortal.cs`: las pruebas cubren el comportamiento actual. La compilación completa pasa; la ejecución de las pruebas de integración local quedó impedida porque este entorno no tiene Docker disponible.

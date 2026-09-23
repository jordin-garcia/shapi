---
id: EM-13
titulo: Límites del plan de plataforma y cambio de plan del consumidor
persona: emilio
responsable: Emilio Méndez
avance: 3
prioridad: P2
estado: pendiente
depende_de: [EM-10, DC-04]
requisitos: [RF-43, RF-25]
pantallas: []
---

# EM-13 · Límites del plan de plataforma y cambio de plan del consumidor

**Responsable:** Emilio Méndez · **Avance:** 3 · **Prioridad:** P2 · **Depende de:** EM-10, DC-04

## Objetivo
Aplicar los límites del plan de plataforma (APIs, miembros, dominio propio y avisos de cuota) y permitir que el consumidor cambie de plan con las mismas reglas de prorrateo.

## Contexto que debes leer
- `docs/specs/09-cobros-y-suscripciones.md` §5 y §6
- `docs/specs/03-requisitos.md` RF-25 y RF-43
- Sección Resultado de DC-04 (caso de uso de registro de una API)

## Archivos que creas o modificas
- `src/Shapi.Aplicacion/Suscripciones/VerificadorLimitesPlan.cs` (crear `IVerificadorLimitesPlan`)
- `src/Shapi.Aplicacion/Apis/RegistrarApi*.cs` (modificar **solo** para llamar al verificador; es de Dominique, cambio acordado)
- `src/*/Suscripciones/**`
- `src/Shapi.Trabajador/AvisosCuota/**` (crear)
- `contratos/openapi/suscripciones.yaml`
- `tests/*/Suscripciones/**`

## Criterios de aceptación
1. Registrar una API cuando ya se llegó a `max_apis` → 422 `limite_del_plan` con `limite: apis`. El verificador también expone `PermiteDominioPropio(organizacionId)` para DC-14.
2. `GET /api/suscripcion/uso` devuelve las peticiones del ciclo (leídas de `cuota:org:*`), la cuota y el %. B1.4 muestra un aviso al llegar al 80 % y otro al 100 % (modifica tu página B1-4).
3. El trabajador envía `aviso_cuota_plataforma` al propietario la primera vez que se cruza el 80 % y el 100 % en cada ciclo.
4. `POST /api/portal/suscripcion/cambiar` `{planId, tarjeta?}` aplica al consumidor las mismas reglas de subida y bajada de 09 §5 (reutiliza `Suscripcion.CambiarPlan`) y publica en Redis.

## Pruebas obligatorias
- Integración de cada límite y del cambio de plan del consumidor

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
```

## Fuera de alcance
- Pantalla B2.7 (DC-13)

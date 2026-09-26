---
id: EM-05
titulo: Identidad del consumidor (backend del portal)
persona: emilio
responsable: Emilio Méndez
avance: 2
prioridad: P1
estado: pendiente
depende_de: [EM-04, DC-03]
requisitos: [RF-02, RF-03, RF-04, RF-05]
pantallas: []
---

# EM-05 · Identidad del consumidor (backend del portal)

**Responsable:** Emilio Méndez · **Avance:** 2 · **Prioridad:** P1 · **Depende de:** EM-04, DC-03

## Objetivo
Implementar la autenticación del ámbito `consumidor`: registro en el portal de una API, verificación, inicio y cierre de sesión, recuperación y aceptación de invitaciones, con la cuenta aislada por organización.

## Contexto que debes leer
- `docs/specs/12-decisiones.md` ADR-03 y ADR-04
- `docs/specs/04-roles-y-permisos.md` §1
- `docs/specs/10-identidad-y-seguridad.md` §1, §2 y §6
- `docs/specs/05-casos-de-uso.md` CU-11
- `IResolutorPortal` de DC-03 (lee su sección Resultado)

## Archivos que creas o modificas
- `src/*/Identidad/**` (modificar)
- `contratos/openapi/identidad.yaml` (modificar: `/api/portal/auth/*`)
- `tests/*/Identidad/**`

## Criterios de aceptación
1. `POST /api/portal/auth/registro` crea el consumidor dentro de la organización de la API que corresponde al host (`IResolutorPortal`), con correo único **dentro de esa organización**: el mismo correo en otra organización sí se permite. Encola `verificacion_correo` con los datos de marca del portal. Los correos del portal (`verificacion_correo` y `recuperacion`) llevan en sus datos `nombrePortal` y `hostPortal`, el host del portal que atendió la petición, para que el enlace lleve a A5.8 o A5.10 y no al panel (10 §6).
2. `/api/portal/auth/{verificar-correo,reenviar-verificacion,entrar,salir,sesion,recuperar,restablecer}` funcionan como sus equivalentes del personal, con la cookie `portal_sesion` limitada al host del portal. Una sesión de un portal no sirve en otro host ni en el panel, y una sesión del panel no sirve en el portal.
3. `GET /api/portal/auth/sesion` devuelve `correoVerificado`, para que el portal bloquee la contratación hasta que se confirme el correo (RF-02).
4. `GET /api/portal/auth/invitacion/{token}` devuelve el correo invitado, y `POST .../aceptar` `{nombre, nombreEmpresa, contrasena}` crea la cuenta con el correo ya verificado. El token es de tipo `invitacion_consumidor` y vence a los 7 días.
5. Bloqueo tras intentos fallidos y límite por IP, igual que en el personal.

## Pruebas obligatorias
- Integración: aislamiento entre organizaciones (el mismo correo en dos portales, con contraseñas distintas)
- La sesión de un portal rechazada en otro host
- Invitación aceptada (el token se crea directamente en la prueba)

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
```

## Fuera de alcance
- Pantallas del portal (DC-08)
- Envío de invitaciones desde el panel (EM-15)

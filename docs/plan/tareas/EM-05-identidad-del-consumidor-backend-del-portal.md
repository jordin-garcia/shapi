---
id: EM-05
titulo: Identidad del consumidor (backend del portal)
persona: emilio
responsable: Emilio Méndez
avance: 2
prioridad: P1
estado: hecha
programada: 2026-09-30
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
- `frontend/packages/api/src/generado/identidad.ts` (regenerado desde el contrato; reemplaza los tipos provisionales de DC-03)
- `tests/*/Identidad/**`

## Criterios de aceptación
1. `POST /api/portal/auth/registro` crea el consumidor dentro de la organización de la API que corresponde al host (`IResolutorPortal`), con correo único **dentro de esa organización**: el mismo correo en otra organización sí se permite. Encola `verificacion_correo` con los datos de marca del portal. Los correos del portal (`verificacion_correo` y `recuperacion`) llevan en sus datos `nombrePortal` y `hostPortal` = `{sub}.{dominio_base}`, armado con el subdominio de la API que resolvió `IResolutorPortal` (no con la cabecera `Host` ni con el dominio propio), para que el enlace lleve a A5.8 o A5.10 y no al panel (10 §1).
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

## Resultado
- Se implementó autenticación del consumidor por portal: registro aislado por organización, verificación, reenvío, inicio y cierre de sesión, consulta de sesión, recuperación de contraseña y aceptación de invitaciones.
- Las cookies `portal_sesion` quedan limitadas por host; la autenticación valida ámbito, organización y host. Se agregaron bloqueo tras cinco fallos y límite de diez peticiones por minuto por IP.
- Se amplió el servicio de recuperación para distinguir tokens personales y de consumidores, se actualizó el contrato OpenAPI y se regeneraron sus tipos TypeScript.
- Decisiones: las rutas de invitación reciben el mismo limitador que registro y acceso porque aceptan tokens; restablecer conserva el alcance de límite del flujo personal. La marca del correo usa `PortalResuelto` y el host canónico que resuelve `IResolutorPortal`.
- Archivos principales: `EndpointsPortal.cs`, `ConsumidorAutenticacionHandler.cs`, `ServicioRecuperacion.cs`, `identidad.yaml` y `ConsumidorPortalTests.cs`.

### Correcciones de la auditoría (2026-10-03)

Paso 2 de `docs/plan/auditoria-2026-10-03.md` (H-05 a H-13), hechas por el coordinador:
- **Ámbitos de sesión (H-05).** El esquema por defecto elegía el del consumidor si llegaba la cookie `portal_sesion`, y la política por defecto solo pedía estar autenticado. Por eso una sesión del portal entraba en `/api/auth/sesion` (500), `PUT /api/perfil` (200 sin hacer nada) y `POST /api/perfil/contrasena` (500), y en el panel desplazaba a una `shapi_sesion` válida (401). Esto contradecía 04: «una sesión de un ámbito nunca da acceso a las rutas del otro». Ahora `IdentidadModulo` elige el esquema por la ruta: `/api/portal/*` usa el del consumidor y todo lo demás el del personal. Además, la política por defecto y la de respaldo de `PoliticasAutorizacion` exigen el ámbito `Personal`. Se precisó en 10 §1.
- **Pruebas (H-06 a H-10):**
  - el mismo correo en dos portales, con la contraseña cruzada en los dos sentidos;
  - `salir` revoca la sesión del consumidor y borra la cookie;
  - los atributos de `portal_sesion`, sin `Domain`;
  - el límite por IP en los siete endpoints públicos del portal (una teoría);
  - `hostPortal` canónico con un `Host` en mayúsculas y con punto final, y el color y el logotipo del portal en el correo;
  - la sesión del portal rechazada en las rutas del panel, y las dos cookies juntas.
- **Nombres (H-11).** Las pruebas llevan RF-03 o RF-04, según el requisito.
- **Contrato (H-12).** `GET /api/portal/auth/sesion` ya no declara un 404 inalcanzable. Los 422 del portal declaran `Problema` con `token_invalido`. Se regeneraron los tipos.
- **Cierre (H-13).** 10 §1 nombra el límite por IP de `/api/portal/auth/*`. El cambio de este PR en `Modulos/IdentidadModulo.cs` queda descrito arriba.

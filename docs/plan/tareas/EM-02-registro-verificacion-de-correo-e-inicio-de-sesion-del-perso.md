---
id: EM-02
titulo: Registro, verificación de correo e inicio de sesión del personal (backend)
persona: emilio
responsable: Emilio Méndez
avance: 1
prioridad: P1
estado: hecha
depende_de: [EM-01]
requisitos: [RF-01, RF-02, RF-04, RNF-07]
pantallas: []
---

# EM-02 · Registro, verificación de correo e inicio de sesión del personal (backend)

**Responsable:** Emilio Méndez · **Avance:** 1 · **Prioridad:** P1 · **Depende de:** EM-01

## Objetivo
Implementar la autenticación del ámbito `personal`: registro del proveedor con su organización en el plan Prueba, verificación de correo, inicio y cierre de sesión con sesiones del lado del servidor, y la base de autorización que usarán todos los módulos.

## Contexto que debes leer
- `docs/specs/03-requisitos.md` RF-01, RF-02, RF-04
- `docs/specs/10-identidad-y-seguridad.md` §1, §2 y §3
- `docs/specs/04-roles-y-permisos.md` §1, §3 y §4
- `docs/specs/05-casos-de-uso.md` CU-01, CU-02
- `docs/specs/07-modelo-de-datos.md` §3.1
- `docs/specs/09-cobros-y-suscripciones.md` §4 (ciclo de la Prueba)
- `docs/plan/convenciones.md` §5

## Archivos que creas o modificas
- `src/Shapi.{Dominio,Aplicacion,Infraestructura,Api}/Identidad/**` (crear)
- `src/Shapi.Api/Modulos/IdentidadModulo.cs` (modificar)
- `contratos/openapi/identidad.yaml` (crear)
- `tests/*/Identidad/**` (crear)

## Criterios de aceptación
1. `POST /api/auth/registro` crea: el usuario (contraseña con `PasswordHasher` de Identity), la organización `proveedor`, la membresía `propietario` y la suscripción de plataforma Prueba `activa` por 30 días (ciclo de 09 §4). Además encola `verificacion_correo` con un token de 24 h, del que solo se guarda el hash. Si el correo ya existe → 409 `correo_ya_registrado`. Si la contraseña tiene menos de 10 caracteres o es igual al correo → 400 con `errores`.
2. `POST /api/auth/verificar-correo` `{token}` marca `correo_verificado_en`, marca el token como usado e inicia sesión. Un token vencido o ya usado → 422 `token_invalido`. `POST /api/auth/reenviar-verificacion` genera un token nuevo.
3. `POST /api/auth/entrar` crea la sesión (tabla `sesion` con el hash del identificador) y la cookie `shapi_sesion` (`HttpOnly`, `Secure`, `SameSite=Lax`). La sesión vence tras 8 h de inactividad o a los 7 días. `POST /api/auth/salir` la revoca.
4. Tras 5 intentos fallidos seguidos, la cuenta se bloquea 15 minutos (`cuenta_bloqueada`). Los mensajes de credenciales incorrectas son genéricos (`credenciales_invalidas`).
5. Todo método distinto de GET sin `X-Requested-With: shapi` → 403 `csrf`. `/api/auth/*` tiene un límite de 10 peticiones por minuto por IP (429).
6. `GET /api/auth/sesion` devuelve el usuario, la organización, el rol, `correoVerificado` y el `destino` según el rol (10 §1).
7. Quedan registrados el esquema de autenticación por cookie y **todas las políticas de permisos** de 04 §3 como constantes (`Permisos.*`), para que los demás módulos usen `RequireAuthorization(Permisos.X)`.

## Pruebas obligatorias
- Integración de cada endpoint (WebApplicationFactory + Testcontainers)
- Bloqueo tras 5 intentos
- CSRF
- Vencimiento de la sesión con `IReloj` falso

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
```

## Fuera de alcance
- Recuperación y perfil (EM-04)
- Consumidores (EM-05)
- Pantallas (EM-03)
- Envío del correo (JZ-03)

## Resultado

La implementación original es de Emilio (PR #10). Se integró sin la revisión completa, y el 25 de septiembre de 2026 Jordin la corrigió (PR de corrección) para que cumpla los criterios. Lo que queda:

- **Endpoints** (`src/Shapi.Api/Modulos/IdentidadModulo.cs`): `POST /api/auth/registro`, `verificar-correo`, `reenviar-verificacion`, `entrar` y `salir`, y `GET /api/auth/sesion`. Todos los errores son ProblemDetails con `codigo` (`Problemas.Crear`), y el 400 trae `errores` por campo.
- **Registro:** `RegistroProveedor` y `ValidadorRegistroProveedor` (FluentValidation) en `Shapi.Aplicacion/Identidad`. Crea el usuario, la organización, la membresía de propietario y la Prueba con el ciclo de 09 §4 (`SuscripcionPlataforma.IniciarPrueba`), y encola `verificacion_correo` con `IColaCorreo` en la misma transacción. Los datos del correo son `{ nombre, token }`, donde `token` es el valor del enlace; JZ-03 arma el enlace.
- **Sesión:** `PersonalAutenticacionHandler` (`Shapi.Api/Identidad`) lee la sesión, el usuario y la membresía sin el filtro global y pone los claims `OrganizacionId`, el rol y `Ambito`. `ContextoOrganizacionHttp` le da la organización al filtro global de los demás módulos.
- **Dominio:** constructores y métodos en `Usuario` (bloqueo), `Token`, `Sesion` (vigencia y último uso), `Organizacion`, `Membresia` y `SuscripcionPlataforma`. Las propiedades siguen con `private set`.
- **Tokens:** `SeguridadTokens` genera 32 bytes en Base64Url y guarda el SHA-256 en hex minúsculas.
- **Seguridad:** CSRF (`CsrfMiddleware`), límite de 10 por minuto por IP en los endpoints con credenciales, con `X-Forwarded-For` desde el borde, y tiempo de respuesta igual exista o no la cuenta.
- **Permisos:** `Permisos.*` con valores `Permiso.X` para las 28 políticas de 04 §3, incluidas `EditarPerfil` y las tres del consumidor, que llegan con EM-05. Se registran en `PoliticasAutorizacion.AgregarPoliticasShapi()`.
- **Contrato:** `contratos/openapi/identidad.yaml`, con los códigos reales.
- **Pruebas:** `tests/Shapi.Api.Tests/Identidad/` (integración con Testcontainers, un contenedor por clase y una base por prueba, y la matriz de permisos) y `tests/Shapi.Dominio.Tests/Identidad/`.

### Decisiones
- `423 cuenta_bloqueada`, `403 cuenta_desactivada` (solo con la contraseña correcta) y `429 demasiadas_peticiones`. Se agregaron a `CodigosError` y a 10 §1.
- `GET /api/auth/sesion` y `POST /api/auth/salir` no llevan el límite por IP. El panel consulta la sesión en cada carga y esos endpoints no sirven para adivinar credenciales. Quedó en 10 §1.
- Los endpoints todavía hacen las consultas a la base. Llevarlas a casos de uso de `Shapi.Aplicacion` (convenciones §6) necesita una abstracción del acceso a datos que ningún módulo tiene aún, y se decide en la convergencia (JG-08).


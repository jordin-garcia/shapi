---
id: JZ-03
titulo: Envío de correos desde la bandeja de salida
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: 1
prioridad: P1
estado: hecha
depende_de: [EM-01]
requisitos: [RF-46]
pantallas: []
---

# JZ-03 · Envío de correos desde la bandeja de salida

**Responsable:** José Pablo Zúñiga · **Avance:** 1 · **Prioridad:** P1 · **Depende de:** EM-01

## Objetivo
Implementar en el trabajador el envío de los correos encolados en `correo_saliente`, por SMTP (Mailpit en el entorno simulado), con reintentos, y las primeras plantillas: verificación y recuperación.

## Contexto que debes leer
- `docs/specs/10-identidad-y-seguridad.md` §6
- `docs/specs/03-requisitos.md` RF-46
- `docs/specs/07-modelo-de-datos.md` §3.6 (`correo_saliente`)
- `docs/specs/06-arquitectura.md` §3 (trabajador)

## Archivos que creas o modificas
- `src/Shapi.Trabajador/Correo/**` (crear)
- `src/Shapi.Infraestructura/Correo/**` (crear: `EnviadorSmtp` con MailKit y el motor de plantillas)
- `src/Shapi.Infraestructura/Correo/Plantillas/verificacion_correo.{html,txt}` y `recuperacion.{html,txt}` (crear)
- `tests/**/Correo/**`

## Criterios de aceptación
1. Cada 5 s, el trabajador toma los correos `pendiente` con `proximo_intento_en <= ahora`, arma el correo con la plantilla y los `datos` (reemplazo de `{{campo}}`, con los valores escapados en HTML), lo envía y lo marca `enviado` con `enviado_en`.
2. Si falla, incrementa `intentos`, guarda `ultimo_error` y calcula el próximo intento (5 s, 30 s, 2 min, 10 min y 1 h). Si falla el quinto reintento (el sexto intento), queda `fallido` (RF-46: "se reintentan hasta 5 veces").
3. El remitente es `no-responder@{dominio_base}`. Si los `datos` traen `nombrePortal`, ese es el nombre visible del remitente, y si traen `hostPortal`, los enlaces llevan a ese host (correos de un portal, 10 §1).
4. Las plantillas `verificacion_correo` y `recuperacion` están en español, tratan al usuario de usted y llevan el enlace con el token.
5. La configuración viene de `SHAPI_SMTP_HOST`, `_PUERTO`, `_USUARIO`, `_CONTRASENA` y `_TLS`.

## Pruebas obligatorias
- Integración con Mailpit en Testcontainers (contenedor genérico `axllent/mailpit`, verificado con su API `/api/v1/messages`)
- Reintentos con un servidor SMTP caído

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
```

## Fuera de alcance
- Resto de las plantillas (JZ-11)
- Encolar los correos (ya lo hacen los módulos con `IColaCorreo`)

## Resultado

- El trabajador procesa cada 5 segundos los correos pendientes cuyo próximo intento ya venció, los entrega por SMTP y persiste el resultado después de cada correo.
- Se implementaron los cinco intervalos de reintento (5 s, 30 s, 2 min, 10 min y 1 h). (Corregido el 25 de septiembre: el correo queda `fallido` al fallar el sexto intento, ver más abajo.)
- MailKit lee `SHAPI_SMTP_HOST`, `SHAPI_SMTP_PUERTO`, `SHAPI_SMTP_USUARIO`, `SHAPI_SMTP_CONTRASENA` y `SHAPI_SMTP_TLS`. El remitente usa `no-responder@{dominio_base}` y `nombrePortal` como nombre visible cuando está presente.
- Las plantillas HTML y texto de verificación y recuperación están incrustadas en `Shapi.Infraestructura`, tratan al usuario de usted, escapan los valores HTML y generan los enlaces definidos en `10-identidad-y-seguridad.md`.
- Se agregaron pruebas de dominio, renderizado y una integración con PostgreSQL y Mailpit que verifica la entrega mediante `/api/v1/messages`, además del servidor SMTP caído y la programación futura.
- Archivos principales: `src/Shapi.Trabajador/Correo/`, `src/Shapi.Infraestructura/Correo/` y `tests/Shapi.Api.Tests/Correo/`.

### Corrección posterior (2026-09-25, Jordin)

- Los enlaces de los correos de un consumidor llevan al host del portal cuando los `datos` traen `hostPortal`; antes siempre llevaban al dominio base, es decir, al panel del personal, donde el token del consumidor no sirve. `hostPortal` debe ser `{sub}.{dominio_base}`, con una sola etiqueta ASCII, para que el token no pueda terminar en otro dominio.
- El criterio 2 contradecía RF-46 ("se reintentan hasta 5 veces"): con "al quinto fallo, `fallido`" la espera de 1 h nunca se usaba. Ahora hay 5 reintentos con las 5 esperas, y el correo queda `fallido` al fallar el sexto intento, con `proximo_intento_en` vacío.
- Especificación precisada en `10-identidad-y-seguridad.md` §6.

### Correcciones de la auditoría (2026-09-26)

Paso 7 de `docs/plan/auditoria-2026-09-25.md` (H-61 a H-67):
- **Dos trabajadores no envían el mismo correo (H-61).** Antes, cada trabajador leía un lote de 50 sin bloquearlo, así que dos trabajadores enviaban los mismos correos. Ahora cada correo se toma en su propia transacción con `FOR UPDATE SKIP LOCKED`. Mientras un trabajador lo envía, el otro lo salta y toma el siguiente.
- **El token no se queda en la base (H-62).** Al quedar `enviado` o `fallido`, el `token` se borra de `correo_saliente.datos` (10 §3 y §8). Mientras el correo está pendiente sigue en claro, porque hace falta para el enlace.
- **Sin reenvíos después de entregar (H-63).** El resultado se guardaba con el token de cancelación del trabajador: si se detenía justo después de enviar, el correo seguía `pendiente` y se reenviaba. Ahora se guarda con `CancellationToken.None` y el correo queda `enviado`. En cuanto al `QUIT`, MailKit 4.18 ya ignora sus errores y cancelaciones. Aun así, se agregó un `try/catch` explícito, por si eso cambia, con una prueba de regresión.
- **SMTPS implícito (H-64).** Con `SHAPI_SMTP_TLS=true`, se usa SMTPS implícito en el puerto 465 y STARTTLS obligatorio en los demás. No se usa `SecureSocketOptions.Auto`, porque enviaría en claro si el servidor no ofrece STARTTLS (decisión de Jordin).
- **Subdominios reservados (H-65).** `hostPortal` rechaza los subdominios reservados de 06 §4 (`api`, `correo`, `interno`…). La lista está en `Shapi.Dominio/Apis/SubdominiosReservados.cs`, que también usará DC-04.
- **Índice (H-66).** `correo_saliente` tiene un índice por `(estado, proximo_intento_en)` (migración `IndiceCorreoSalientePendientes`). `creado_en` ya se había agregado con H-37.
- **Pruebas (H-67):**
  - dos procesadores a la vez;
  - los 6 intentos hasta `fallido` en el procesador;
  - un `hostPortal` inválido cuenta como intento y no se envía;
  - el cuerpo entregado (enlace, HTML escapado y parte de texto) leído de Mailpit;
  - un Mailpit que exige STARTTLS y usuario, con credenciales correctas, incorrectas y sin TLS;
  - el trabajador detenido después de enviar;
  - el error en el `QUIT` (regresión);
  - la elección del cifrado según el puerto.
- Especificación precisada en `07-modelo-de-datos.md` §3.6 y `10-identidad-y-seguridad.md` §3 y §6.

### Correcciones de la auditoría (2026-09-27)

- **H-144:** DC-04 dice que tiene que usar `SubdominiosReservados.Contiene`, la única definición de la lista.
- **H-146:** los correos del personal salen con «Shapi» como nombre visible del remitente, y los de un portal, con `nombrePortal` (10 §6). Tiene prueba.
- **H-147:** JZ-11 precisa dos cosas:
  - que también se agrega la marca a `verificacion_correo` y `recuperacion`;
  - qué `datos` pasa quien encola un correo de consumidor (`nombrePortal`, `hostPortal`, `colorPortal` y `logoPortal`).

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
3. El remitente es `no-responder@{dominio_base}`. Si los `datos` traen `nombrePortal`, ese es el nombre visible del remitente, y si traen `hostPortal`, los enlaces llevan a ese host (correos de un portal, 10 §6).
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
- Se implementaron los cinco intervalos de reintento (5 s, 30 s, 2 min, 10 min y 1 h); el quinto fallo deja el correo en estado `fallido`.
- MailKit lee `SHAPI_SMTP_HOST`, `SHAPI_SMTP_PUERTO`, `SHAPI_SMTP_USUARIO`, `SHAPI_SMTP_CONTRASENA` y `SHAPI_SMTP_TLS`. El remitente usa `no-responder@{dominio_base}` y `nombrePortal` como nombre visible cuando está presente.
- Las plantillas HTML y texto de verificación y recuperación están incrustadas en `Shapi.Infraestructura`, tratan al usuario de usted, escapan los valores HTML y generan los enlaces definidos en `10-identidad-y-seguridad.md`.
- Se agregaron pruebas de dominio, renderizado y una integración con PostgreSQL y Mailpit que verifica la entrega mediante `/api/v1/messages`, además del servidor SMTP caído y la programación futura.
- Archivos principales: `src/Shapi.Trabajador/Correo/`, `src/Shapi.Infraestructura/Correo/` y `tests/Shapi.Api.Tests/Correo/`.

### Corrección posterior (2026-09-25, Jordin)

- Los enlaces de los correos de un consumidor llevan al host del portal cuando los `datos` traen `hostPortal`; antes siempre llevaban al dominio base, es decir, al panel del personal, donde el token del consumidor no sirve. `hostPortal` debe ser `{sub}.{dominio_base}`, con una sola etiqueta ASCII, para que el token no pueda terminar en otro dominio.
- El criterio 2 contradecía RF-46 ("se reintentan hasta 5 veces"): con "al quinto fallo, `fallido`" la espera de 1 h nunca se usaba. Ahora hay 5 reintentos con las 5 esperas, y el correo queda `fallido` al fallar el sexto intento, con `proximo_intento_en` vacío.
- Especificación precisada en `10-identidad-y-seguridad.md` §6.

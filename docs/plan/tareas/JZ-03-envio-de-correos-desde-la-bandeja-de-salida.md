---
id: JZ-03
titulo: Envío de correos desde la bandeja de salida
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: 1
prioridad: P1
estado: pendiente
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
2. Si falla, incrementa `intentos`, guarda `ultimo_error` y calcula el próximo intento (5 s, 30 s, 2 min, 10 min y 1 h). Al quinto fallo, queda `fallido`.
3. El remitente es `no-responder@{dominio_base}`. Si los `datos` traen `nombrePortal`, ese es el nombre visible del remitente (correos de un portal).
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

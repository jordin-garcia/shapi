# Bitácora de José Pablo Zúñiga

Cada tarea terminada agrega una entrada **al final** de este archivo (protocolo, paso B10):

```
## AAAA-MM-DD · <ID> · <título>
- Hecho: ...
- Decisiones: ...
- Pendiente o aviso para otros: ...
```

---

## 2026-09-23 · JZ-01 · Infraestructura local: Docker Compose, Caddy, TLS y Mailpit
- Hecho: se creó Compose para PostgreSQL, Redis, Mailpit y Caddy; se configuraron HTTPS, enrutamiento, cabeceras, TLS bajo demanda, variables de entorno, documentación y verificación automatizada.
- Decisiones: Caddy sobrescribe las cabeceras del origen de forma diferida; el verificador usa orígenes locales controlados y espera hasta 90 segundos por la salud de todos los servicios.
- **JZ-02 y JZ-06:** JZ-02 puede agregar `origen-envios` y `origen-agro` a `infra/compose.yml`; JZ-06 puede extender este entorno con las imágenes de la aplicación.
- **Jordin y DC-01:** en Linux nativo, Kestrel debe escuchar en `0.0.0.0` y Vite debe usar `server.host: "0.0.0.0"` y `server.hmr.clientPort: 443` para que Caddy alcance los procesos del equipo.

## 2026-09-23 · JZ-02 · Orígenes de demostración (Envíos Xelajú y Agro Precios)
- Hecho: se crearon ambas Minimal APIs .NET 10, sus contratos OpenAPI 3.0.3, Dockerfiles, servicios de Compose y pruebas automatizadas de rutas, ejemplos, seguridad y salud.
- Decisiones: los datos son deterministas y reproducen los mockups; el secreto del origen es opcional y se compara en tiempo constante cuando está configurado.
- Pendiente o aviso para otros:
  - **JZ-05:** puede registrar Envíos Xelajú en `http://origen-envios:8080` y Agro Precios en `http://origen-agro:8080` dentro de Compose.
  - **JZ-06:** los Dockerfiles de ambos orígenes ya están listos para el ambiente productivo simulado.

## 2026-09-25 · JZ-03 · Envío de correos desde la bandeja de salida
- Hecho: se implementó el procesamiento cada 5 segundos, envío SMTP con MailKit, remitente configurable, plantillas HTML/texto de verificación y recuperación, escape HTML y cinco intentos con espera progresiva. La integración entrega el correo a Mailpit y lo comprueba con su API.
- Decisiones: los enlaces canónicos son `/verificar-correo?token=` y `/restablecer?token=` sobre `SHAPI_DOMINIO_BASE`; el quinto fallo conserva el último intervalo calculado de 1 hora aunque el estado ya sea `fallido` (corregido el 25 de septiembre: queda `fallido` al sexto intento, sin `proximo_intento_en`); cada correo guarda su resultado antes de procesar el siguiente.
- Pendiente o aviso para otros:
  - **EM-03:** `verificacion_correo` recibe `{ nombre, token }` y el enlace enviado apunta a `/verificar-correo?token=`; la pantalla ya puede consumir ese token.
  - **EM-04:** `recuperacion` recibe `{ nombre, token }` y el enlace enviado apunta a `/restablecer?token=`. Puede incluir `nombrePortal` para usarlo como remitente visible.
  - **JZ-11:** para agregar plantillas, cree el par `.html`/`.txt` en `Shapi.Infraestructura/Correo/Plantillas`; los marcadores `{{campo}}` se toman de `datos` y se escapan en HTML.

## 2026-09-28 · JZ-04 · Bitácora de acciones sensibles (B3.2)
- Hecho: se implementaron la consulta administrativa paginada, el contrato OpenAPI y B3.2 con estados poblado, vacío, carga, error y selector de fechas; se cubrieron permisos, zona horaria, orden y límites con pruebas automatizadas.
- Decisiones: los días se interpretan en `America/Guatemala` con extremos inclusivos; el periodo predeterminado es el día actual y los seis anteriores; la página predeterminada contiene 20 entradas y admite hasta 100.
- Pendiente o aviso para otros:
  - **JZ-08:** las acciones `organizacion.suspendida` y `organizacion.reactivada` que registre mediante `IBitacora` aparecerán en `GET /api/admin/bitacora`; use las descripciones legibles del catálogo de `docs/specs/10-identidad-y-seguridad.md` §7.

## 2026-09-29 · JZ-06 · Imágenes Docker, ambiente productivo simulado y publicación en GHCR
- Hecho: se crearon imágenes multi-stage para API, compuerta, trabajador y borde; Compose productivo con migraciones, salud, persistencia, llaves compartidas y puertos internos cerrados; Caddy productivo con frontends compilados, CSP y enrutamiento interno; publicación de `latest` y SHA en GHCR; verificación automatizada y documentación operativa.
- Decisiones: el borde reemplaza los volúmenes de desarrollo con `!override`; la instalación del frontend comprueba los binarios nativos de Alpine; el workflow construye en PR, verifica Compose desde cero y publica únicamente al llegar a `main`.
- Pendiente o aviso para otros:
  - **JZ-05:** el trabajador y el ambiente productivo ya están listos para agregar y ejecutar `sembrar-demo`; conserve el volumen compartido `dpkeys` y el comando documentado en `docs/manual-tecnico.md`.
  - **JZ-07:** puede usar `infra/compose.prod.yml` y `node infra/verificar.mjs` como base del entorno E2E y de la herramienta de capturas.
  - **JZ-12:** el trabajador queda sin healthcheck HTTP, como exige 06 §8; agregue su latido `salud:trabajador` sin exponer un puerto nuevo.

## 2026-10-01 · JZ-07 · Pruebas de extremo a extremo y herramienta de capturas
- Hecho: se creó el proyecto Playwright, la herramienta de capturas a 1440 × 900, el ayudante de Mailpit, la E2E de registro y acceso, y el workflow manual y posterior a cada integración en `main` con su informe como artefacto.
- Decisiones: el ayudante resuelve los hosts locales en `127.0.0.1` para que Node llegue a Caddy de la misma forma que Chromium; cada ejecución usa un correo único y conserva rastros solo cuando hay fallos.
- Pendiente o aviso para otros:
  - **JZ-13:** reutilice `playwright.config.ts` y `soporte/mailpit.ts` para agregar los flujos E2E principales.
  - **Todos:** para comparar una pantalla con su mockup, ejecute `pnpm captura <url> <archivo.png>` desde `tests/e2e`.

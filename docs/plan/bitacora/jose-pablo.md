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

## 2026-10-03 · JZ-05 · Siembra de demostración
- Hecho: se implementó `sembrar-demo [--reiniciar]` con datos completos y relativos de A3–A6 y B1–B3, claves fijas, consumo, pagos, casos, bitácora, configuración de orígenes y resincronización de Redis; se documentaron las credenciales y se agregaron pruebas de integración.
- Decisiones: el reinicio conserva la bitácora inmutable y agrega un conjunto nuevo con fechas relativas; la clave rotada de Boutique Cayalá conserva una ventana total de 24 horas y permanece visible durante la demostración.
- Pendiente o aviso para otros:
  - **JZ-13:** el entorno E2E puede ejecutar `dotnet run --project src/Shapi.Trabajador -- sembrar-demo --reiniciar` antes de los flujos para reconstruir datos deterministas.
  - **Todos:** las cuentas y claves fijas están en `docs/manual-tecnico.md`; en producción simulada los orígenes predeterminados son `http://origen-envios:8080` y `http://origen-agro:8080`.

## 2026-10-05 · JZ-11 · Plantillas de correo completas
- Hecho: se completaron las 12 plantillas de RF-46 en HTML y texto, con asuntos específicos, marca de Shapi para el personal, marca blanca para consumidores, logotipo opcional y pruebas unitarias para cada plantilla y variante.
- Decisiones: la marca se agrega de forma centralizada en `MotorPlantillasCorreo`; el portal exige `nombrePortal`, `hostPortal` y `colorPortal` válido, y usa el logotipo solo con `logoPortal: "true"`. Los marcadores requeridos quedaron documentados en 10 §6.
- Pendiente o aviso para otros:
  - **EM-10 y EM-13:** para `pago_rechazado`, `suscripcion_en_gracia`, `suscripcion_suspendida`, `prueba_por_vencer` y `aviso_cuota_plataforma`, use los marcadores documentados en 10 §6.
  - **EM-12 y EM-15:** `invitacion_miembro` recibe `nombre`, `nombreOrganizacion` y `enlace`; `invitacion_consumidor` recibe `nombre`, `nombreApi` y `enlace`, además de la marca del portal.
  - **JZ-08, JZ-09 y JZ-10:** `organizacion_suspendida`, `definir_contrasena` y `respuesta_caso` ya están listas; sus marcadores están en 10 §6.
  - **Todos:** todo correo de consumidor debe incluir `nombrePortal`, `hostPortal`, `colorPortal` y, si corresponde, `logoPortal: "true"`; el motor rechaza una marca incompleta o un color distinto de `#RRGGBB`.

## 2026-10-07 · JZ-10 · Casos de soporte (A6.4, A6.4b, A7.1 y A7.2)
- Hecho: se implementaron los casos paginados de proveedor y administración, apertura, asignación, conversación, cierre, resumen de organización de solo lectura, correos `respuesta_caso`, bitácora y las cuatro pantallas aprobadas.
- Decisiones: las acciones compuestas son transaccionales; responder y cerrar bloquean la fila del caso para serializar la transición; una respuesta de plataforma se notifica al proveedor que abrió el caso y, si soporte abre el caso, al propietario de la organización.
- Pendiente o aviso para otros:
  - **JZ-13:** los endpoints y las pantallas de soporte ya están listos para incorporar un flujo E2E de apertura, respuesta y cierre.

## 2026-10-09 · JZ-08 · Administración de organizaciones (A6.2 y A6.2b)
- Hecho: se implementaron la consulta administrativa de organizaciones, la suspensión y reactivación con estado efectivo inmediato en Redis, 403 en la compuerta, correo al propietario, bitácora, contrato OpenAPI y las pantallas poblada, vacía y de confirmación.
- Decisiones: los casos de uso viven en Aplicación y el acceso a EF queda detrás de un repositorio; la reactivación solo elimina la suspensión administrativa; A6.2b captura el motivo administrativo requerido por RF-38 y la precisión quedó documentada en la especificación de interfaz.
- Pendiente o aviso para otros:
  - **EM-10:** la reactivación administrativa conserva el estado efectivo `suspendida` mientras la suscripción de plataforma continúe suspendida por falta de pago.
  - **JZ-13:** puede agregar al flujo E2E la suspensión desde A6.2b y comprobar el 403 `api_no_disponible` de la compuerta.

## 2026-10-09 · JZ-18 · Levantar el ambiente con la siembra en un solo comando
- Hecho: se agregó el perfil `demo` con el servicio de una sola ejecución `siembra-demo`, que espera las migraciones y comparte imagen, configuración y llaves con el trabajador. Se actualizaron el verificador, el manual técnico, arquitectura §7 y el paso 1 del guion 2.
- Decisiones: el arranque sin perfil conserva la garantía de no sembrar. `up -d --build` devuelve el control antes de terminar la siembra; se espera la salida 0 de `siembra-demo`. `--sembrar` prueba ambos modos y la idempotencia del comando; `--demo` verifica un ambiente ya sembrado.
- Verificación: fase roja confirmada; el verificador pasó en desarrollo, en producción sin siembra, con `--sembrar` (dos arranques sin duplicar datos) y con `--demo`. Las pruebas productivas usaron volúmenes nuevos del proyecto `shapi-jz18-prueba`, conservando los datos locales existentes. Plan válido (75 tareas), sintaxis y diff correctos. Revisión independiente sin hallazgos.
- Pendiente o aviso para otros:
  - **JZ-13:** para levantar todo con datos use `docker compose --env-file .env -f infra/compose.yml -f infra/compose.prod.yml --profile demo up -d --build` y espere la salida 0 de `siembra-demo`; luego puede ejecutar `node infra/verificar.mjs --demo`. El reinicio explícito de datos con `sembrar-demo --reiniciar` sigue disponible.

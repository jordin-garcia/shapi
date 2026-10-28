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

## 2026-10-12 · JZ-09 · Cuentas de administración y soporte
- Hecho: se implementaron cuentas de plataforma, creación con correo, activación/desactivación, enlace inicial de siete días, verificación del correo y sesión. A6.5 reproduce las variantes poblada/vacía y A6.5b permite definir la contraseña desde el enlace público. Se actualizaron el contrato y los tipos generados.
- Decisiones: la desactivación revoca sesiones; no se pueden modificar usuarios de proveedores ni desactivar la cuenta propia. Los roles existentes son de solo lectura. La cola guarda `token`, y el motor construye el enlace solo para enviarlo; el secreto se elimina al terminar. Bitácora con el nombre real del actor. El usuario autorizó las ampliaciones de alcance.
- Verificación: compilación y formato correctos; 152 pruebas del backend afectado, 58 de dominio y 361 del frontend correctas; lint, TypeScript y build correctos. Chromium comprobó creación, desactivación y definición de contraseña con sesión de soporte en una base aislada; cuatro estados comparados con sus mockups. Revisión independiente: corregidos los dos hallazgos y cubiertos con pruebas. Segunda revisión LISTO, sin hallazgos obligatorios ni opcionales.
- Pendiente o aviso para otros:
  - **Emilio:** `Usuario.cs` agrega `Activar`/`Desactivar` y `Token.cs` agrega `DefinirContrasena`, válido por siete días. Actualice su rama desde `main`.
  - **Dominique:** se agregó `/definir-contrasena` en `rutas.tsx` y su carga diferida en `paginasDiferidas.tsx`. Actualice su rama desde `main`.
  - **JZ-13:** el flujo de cuentas está listo para E2E; `definir_contrasena` recibe `{ nombre, token }`, ya no una URL persistida. El enlace lleva a `/definir-contrasena?token=` y al guardarlo se inicia sesión.

## 2026-10-13 · JZ-12 · Estado de los componentes
- Hecho: consulta de salud para administrador y soporte, siete componentes, caché concurrente de 30 s, p95 global de la última hora, contrato y tipos, y B3.1 normal/degradado/soporte con actualización automática. La API productiva comprueba SMTP con la dirección de Mailpit.
- Decisiones: se reutiliza el latido de JG-09 y se valida por TTL; sin métricas identificadas ni lotes pendientes no se exige una consolidación reciente. La ventana del p95 usa segundos del reloj real, independiente de la demostración; las fechas de negocio usan `IReloj`. Los fallos recientes de correo abarcan dos minutos. José Pablo autorizó las ampliaciones de alcance.
- Verificación: fase roja confirmada; build y formato completos correctos; 33 pruebas dirigidas del backend, 285 de compuerta y 380 del frontend correctas; lint, TypeScript y build correctos. El frontend se verificó con un trabajador para evitar saturación, sin cambiar pruebas ni plazos. Ambiente productivo aislado construido y sembrado; API real, caché y acceso de ambos roles comprobados en Chromium; tres variantes comparadas visualmente y por textos con sus mockups. Revisión independiente LISTO, sin hallazgos.
- Pendiente o aviso para otros:
  - **Jordin:** se modificaron `src/Shapi.Compuerta/Medicion/MedicionMiddleware.cs` y `src/Shapi.Contratos/Redis/LlavesRedis.cs`, con autorización de José Pablo, para medir la última hora que no podía reconstruirse desde los histogramas diarios. El mismo pipeline agrega `salud:latencia-compuerta:{minuto_unix}` con campos `segundo_unix:rango` y TTL de 65 min. No se añade espera de Redis ni se cambia la consolidación. Actualice su rama desde `main`.
  - **JZ-13:** B3.1 y `GET /api/admin/estado` ya permiten verificar salud para administrador y soporte. La API productiva recibe ahora la configuración SMTP para comprobar el correo sin enviar mensajes.
- Revisión automática del PR #98: se corrigió `RedisTimeoutException` para que una demora de Redis no derribe toda la consulta. Dos regresiones confirmaron fase roja en `PING` y en lectura de histogramas. Se cubrió directamente el umbral de 15 ms y se documentaron las sugerencias opcionales. Nueva verificación: 35 pruebas dirigidas correctas, build y formato completos correctos y plan válido (76 tareas tras actualizar desde `main`).

## 2026-10-18 · JZ-14 · Pruebas de carga (RNF-01 y RNF-03)
- Hecho: escenarios k6 en Docker, preparación de datos en el ambiente productivo aislado `shapi-jz14`, comparación automática y reporte del equipo y las mediciones. El origen completó 60 000 peticiones y la compuerta 60 001 en la carga de 200/s por cinco minutos; ambos sin errores ni omisiones. p95 de origen 2.003 ms y compuerta 10.585 ms, diferencia 8.583 ms. RNF-01 y RNF-03 cumplidos en este equipo.
- Decisiones: los límites de plan y ruta se elevan a 24 000/min, la cuota del consumidor a 1 000 000 y la de plataforma a 10 000 000 solo en la base aislada; se genera una clave aleatoria, se almacena solo su hash en PostgreSQL y se resincroniza con el trabajador. Se admite una iteración adicional en el límite temporal observado de k6 sin reducir el mínimo de 60 000 ni permitir omisiones. José Pablo autorizó la referencia adicional en el manual técnico.
- Verificación: fase roja confirmada y nueve pruebas Node correctas, humo Docker correcto, escenarios completos y comparador con salida 0. Revisión independiente sin hallazgos de corrección. Resultados completos en `docs/pruebas-de-carga.md`.
- Pendiente o aviso para otros:
  - **JZ-15:** el manual técnico ya enlaza `docs/pruebas-de-carga.md`, con la tabla y la conclusión de RNF-01 y RNF-03; preserve esa evidencia al completar el manual.

## 2026-10-23 · JZ-13 · Pruebas E2E de los flujos principales
- Hecho: dos escenarios automatizan los guiones 2 y 3 con usuarios y recursos propios: publicación, contratación con 4242, claves, consola, recuperación, límites, consumo y facturación, soporte, administración y el ciclo completo de 0341 con prorrateo, gracia, suspensión y reactivación. Se detectó y corrigió EM-23 con autorización expresa de José Pablo.
- Decisiones: la prueba del reloj se ejecuta después de las demás por su efecto global; el consumidor de ese escenario tiene vigencia anual para aislar la suspensión de plataforma. Los ayudantes respetan `Retry-After` en autenticación y no alteran los límites reales. La preparación y las comprobaciones usan la API, PostgreSQL y Redis reales, sin respuestas de negocio simuladas.
- Verificación: fase roja de EM-23 confirmada por contexto Redis nulo y 403 en la compuerta; compilación completa con cero errores y advertencias; 268 pruebas de Identidad correctas; TypeScript estricto correcto; ambiente productivo verificado. Las 14 E2E pasaron en 3 min 51 s, sin fallos, reintentos ni omisiones. Dos revisiones independientes en contexto limpio: LISTO, sin hallazgos.
- Pendiente o aviso para otros:
  - **Emilio:** con autorización de José Pablo se corrigió `src/Shapi.Api/Identidad/Endpoints.cs` para publicar la organización después del registro; se agregó `tests/Shapi.Api.Tests/Identidad/RegistroCacheTests.cs` y se aisló `IPublicadorCache` en las pruebas existentes `AutenticacionTests.cs` y `RecuperacionTests.cs`. EM-23 documenta y verifica la falla. Actualice su rama desde `main` al integrarse.
  - **JG-17 y JZ-16:** las E2E principales ya están automatizadas. `pnpm test`, en `tests/e2e`, ejecuta el reloj al final; adelanta el ambiente de demostración 37 días. La CI usa un ambiente efímero nuevo por ejecución.
- Cierre: formato completo correcto, sin cambios; plan válido con 89 tareas.

## 2026-10-27 · JZ-19 · Elemento de código para el video del Avance 3
- Hecho: se preparó `docs/video/elementos/jose-pablo.md` con el patrón de bandeja de salida transaccional de soporte y correo, dos características de Clean Code, un fragmento literal de 20 líneas, pruebas de RF-46, narración de 337 palabras y orden de pantalla para 2 a 3 minutos. La tabla de funcionalidades distingue las pantallas compartidas y las continuaciones realizadas por Jordin.
- Decisiones: explicar la coordinación de la transacción de negocio y el envío posterior sin prometer entrega exactamente una vez. Se eligió un patrón distinto del elemento de Emilio y del asignado a Jordin; el de Dominique aún no estaba creado. Solo se modificó documentación.
- Verificación: plan válido (89 tareas); cinco pruebas de `CorreoSalienteTests` correctas, cero fallidas y cero omitidas; fragmento y 15 referencias completas comparados con `main` en `30b14fee9d6f2aba0302e61b0f168e88a09c9570`; narración y duración estimada comprobadas.
- Revisión independiente en contexto limpio según el protocolo B9: LISTO, sin hallazgos de corrección ni opcionales.
- Pendiente o aviso para otros:
  - **JG-29:** el elemento ya está en `docs/video/elementos/jose-pablo.md`; vuelva a comprobar las líneas después de JG-25 y JG-27 y mida la narración durante el ensayo. La estimación es de 2 min 45 s.
  - **DC-18:** José Pablo eligió bandeja de salida transaccional para evitar repetir el patrón entre los cuatro elementos.

# Plan de correcciones de la auditoría del 3 de octubre de 2026

El 3 de octubre de 2026 se auditaron las tareas integradas en `main` después de la auditoría del 25 de septiembre (`auditoria-2026-09-25.md`, cerrada con los PR #40 y #41):

| PR | Tarea | Dueño |
|---|---|---|
| #42 | JZ-04 Bitácora de acciones sensibles (B3.2) | José Pablo |
| #43 | JG-03 Calendario diario y recordatorio en el tablero (ampliación) | Jordin |
| #44 | JG-04 Publicador de configuración en Redis y resincronización | Jordin |
| #45 | DC-03 Estructura del portal de marca blanca | Dominique |
| #46 | EM-04 Recuperación de contraseña y Mi perfil | Emilio |
| #47 | JZ-06 Imágenes Docker, ambiente productivo simulado y GHCR | José Pablo |
| #48 | DC-04 Registrar una API y lista de APIs | Dominique |
| #49 | JG-07 Servicio de claves (backend) | Jordin |
| #50 | EM-05 Identidad del consumidor (backend del portal) | Emilio |
| #51 | JG-05 Compuerta: organización, suscripción, ruta, secreto, SSRF y CORS | Jordin |
| #52 | JZ-07 Pruebas de extremo a extremo y herramienta de capturas | José Pablo |
| #53 | DC-05 Especificación OpenAPI y rutas expuestas | Dominique |
| #54 | EM-06 Prueba intermitente de tokenización | Jordin |
| #55 | EM-07 Planes de API | Emilio |
| #57 | JG-06 Compuerta: límites por minuto, cuotas y cabeceras (Lua) | Jordin |
| #58, #59 | JG-18 Optimización de las pruebas y la CI | Jordin |
| #60 | EM-18 Destino de la sesión del consumidor | Jordin |
| #61 | DC-08 Pantallas de acceso del consumidor (terminada por Jordin, §E4) | Dominique |
| #62 | JZ-05 Siembra de demostración | José Pablo |
| #63 | EM-08 Contratación de un plan de API (backend) | Emilio |

Cada tarea se revisó en contexto limpio (10 subagentes, en solo lectura) contra su archivo de tarea, las especificaciones, los contratos y los mockups. Los hallazgos de severidad alta los comprobó además el agente coordinador en el código (y, en el caso de YARP, en su código fuente de la versión 2.3.0).

**Estado de partida (verificación completa del 3 oct).** Todo pasa:
- `--validar`: 68 tareas;
- `scripts/`: 98 pruebas;
- compilación sin errores, formato y migraciones al día;
- backend, `dotnet test Shapi.slnx -m:1`: 949 pruebas (Api 610, Compuerta 243, OrígenesDemo 48 y Dominio 48);
- tipos generados al día, lint, *typecheck*, 259 pruebas del frontend y su *build*.

Como en la auditoría anterior, los hallazgos son incumplimientos de las especificaciones o de los criterios que las pruebas no cubrían. No reapareció ningún hallazgo de H-01 a H-151 de la auditoría anterior, salvo una regresión de H-109 (H-43) y dos casos de la misma clase que H-51 y H-136 en código nuevo (H-15 y H-39).

**Resumen.** 97 hallazgos: 7 de severidad alta, 35 media y 55 baja, más 33 decisiones pendientes.

Los 7 de severidad alta:
- **H-01:** un plan con tilde («Básico», que está en la siembra) hace fallar cada respuesta de la compuerta.
- **H-02:** si el origen no acepta la conexión en 10 s, se responde 504 en vez de 502 y no se devuelve la cuota.
- **H-05:** una cookie del portal se acepta en `/api/auth/sesion` y `/api/perfil`.
- **H-15:** recuperar la contraseña no tarda lo mismo exista o no la cuenta.
- **H-27:** en el ambiente productivo, los orígenes de la demostración apuntan a `localhost` y no responden.
- **H-28:** `sembrar-demo --reiniciar` falla después de un pago rechazado.
- **H-36:** la CI omite el backend y el frontend si el PR mueve un archivo.

**Urgencia.** H-01, H-27 y H-28 rompen la demostración en el ambiente productivo, y JG-08 (convergencia del Avance 2) es el jueves 8 de octubre. Por eso los pasos 1 y 4 van primero.

**Cómo se trabaja.** Igual que en `auditoria-2026-09-25.md`:
- un paso a la vez, informando a Jordin al terminar cada uno;
- un PR por tarea original, titulado `[<ID>] Correcciones de la auditoría: <tema>`, en la rama `jordin/<ID>-auditoria-<tema>` (protocolo §E3);
- pruebas primero, verificación completa y revisión con el subagente `revisor`;
- en el mismo PR: la casilla del hallazgo en este plan, la subsección `### Correcciones de la auditoría (AAAA-MM-DD)` en el `## Resultado` de la tarea, y la entrada en `docs/plan/bitacora/jordin.md` con el aviso en negrita para la persona dueña;
- las **❓ Decisión pendiente** se le preguntan a Jordin al empezar cada paso, antes de escribir código.

**Decidido por Jordin (3 oct):**
- **Quién corrige:** Jordin corrige todo, por §E3, paso a paso y sin detenerse entre pasos.
- **Decisiones pendientes:** como Jordin pidió no detenerse, cada **❓** se resolvió con la recomendación del agente. Cada una queda anotada como **Decidido (3 oct)** en su paso, para que Jordin pueda revisarla después.
- **Avisos a los dueños:** van en la entrada de cada PR en `docs/plan/bitacora/jordin.md`, con los archivos que cambiaron.

---

## Paso 1 · [JG-06] y [JG-05] Compuerta

- [x] **H-01 (alta, JG-06)** `src/Shapi.Compuerta/Filtros/FiltroLimitesYCuotas.cs:200`: `X-Shapi-Plan` lleva el nombre del plan tal cual. Kestrel rechaza las cabeceras de respuesta que no son ASCII (`InvalidOperationException: Invalid non-ASCII or control character in header`) y la compuerta no configura `ResponseHeaderEncodingSelector`. La siembra crea el plan «Básico» (`SiembraDemo.cs:182`), así que las claves de Boutique Cayalá y Tienda Sololá fallarían en cada petición. Las pruebas no lo detectan porque solo siembran «Comercio» y `TestServer` no valida las cabeceras. Incumple el criterio 7 y RF-32. Corrección: codificar el valor y probarlo con un plan con tilde, contra Kestrel real o con una prueba unitaria de la codificación.
  - **Decidido (3 oct):** (a), por porcentajes en UTF-8 (`B%C3%A1sico`) con `Uri.EscapeDataString`, precisado en 08 §5, con aviso a DC-10 para que use `decodeURIComponent`. Se descartaron (b) `ResponseHeaderEncodingSelector` en Latin-1 y (c) quitar las tildes.
- [x] **H-02 (alta, JG-05 y JG-06)** `src/Shapi.Compuerta/Reenvio/ReenvioOrigen.cs:46-48`: si el origen no acepta la conexión en los 10 s de `ConnectTimeout`, la compuerta responde 504 `origen_sin_respuesta` y descuenta la cuota. Debería responder 502 `origen_inaccesible` y devolverla (08 §1, §3 y §4; criterio 6 de JG-05 y criterio 5 de JG-06). La causa es que YARP 2.3.0 reporta cualquier `OperationCanceledException` que no venga del token enlazado como `RequestTimedOut` con 504, incluido el vencimiento de `ConnectTimeout` (`HttpForwarder.cs`, `HandleRequestFailureAsync`). El comentario de `:83` («YARP responde 502») es incorrecto. Las pruebas de 502 usan un puerto cerrado, que se rechaza al instante. Corrección: si `tiempoTotal` no se canceló y la excepción de `IForwarderErrorFeature` lleva un `TimeoutException` interno, tratarlo como `Inaccesible`; otra opción es aplicar los 10 s dentro de `ConexionOrigen`. Agregar una prueba con un `ConnectCallback` lento (con `TiemposOrigen.Conexion` reducido) que espere 502 y la cuota en 0.
- [x] **H-03 (media, JG-06)** `tests/Shapi.Compuerta.Tests/Tuberia/LimitesYCuotasTests.cs:150-181`: solo se prueba el TTL de `cuota:susc`. Faltan el `EXPIRE 120` de `rl:s`, `rl:r` y `rl:p`, y los `EXPIREAT` de `cuota:org` (`ciclo_fin + 8 d`) y de `dia:p` (07 §4). Si se perdiera uno, las llaves por minuto no vencerían nunca.
- [x] **H-04 (baja, JG-05)** `tests/Shapi.Compuerta.Tests/Tuberia/ValidacionesCompuertaTests.cs:189`: ninguna prueba nombra RF-47, que JG-05 declara. La del secreto de origen se llama `RF_31_…`.
- [x] **Decidido (3 oct, JG-05):** hoy YARP también responde 502 cuando el origen aceptó la conexión y recibió la petición, pero la cortó antes de responder. La compuerta lo traduce a `origen_inaccesible` y devuelve la cuota, aunque la petición sí llegó. Se elige (b): devolver la cuota solo si el error es de conexión (`HttpRequestError.ConnectionError`, `SocketException` o el vencimiento de la conexión). Se descartó (a), dejarlo así.
- [x] **Decidido (3 oct, JG-06):** en modo demostración, `inicio` y `fin` de `susc:` van en la hora adelantada de `IReloj` y la compuerta usa la hora real, así que el `Retry-After` de `cuota_agotada` sale más largo. La cuota es correcta. Solo se anotó en 08 §3, sin cambiar el código.

## Paso 2 · [EM-05] y [EM-18] Ámbitos de sesión del consumidor

- [x] **H-05 (alta, EM-05)** `src/Shapi.Api/Modulos/IdentidadModulo.cs:39-43`: el esquema por defecto elige `Consumidor` si llega la cookie `portal_sesion`, y la política por defecto (`PoliticasAutorizacion.cs:24`) solo exige estar autenticado. Por eso una sesión del portal se acepta en `/api/auth/sesion` (responde 500 por `Enum.Parse<Rol>(null)`, `Endpoints.cs:302`), en `PUT /api/perfil` (200 sin hacer nada) y en `POST /api/perfil/contrasena` (500). Además, en el panel, cualquier `portal_sesion` desplaza a una `shapi_sesion` válida y responde 401. Contradice 04 (línea 12: «Una sesión de un ámbito nunca da acceso a las rutas del otro») y el criterio 2 de EM-05. Hoy no se llega desde fuera porque Caddy solo pasa `/api/portal/*` en los hosts de portal, pero cualquier endpoint futuro que solo pida estar autenticado quedaría abierto a consumidores. Ninguna prueba lo cubre.
  - **Decidido (3 oct):** (a). La política por defecto y la de respaldo exigen el ámbito `Personal`, y el esquema se elige **por la ruta** (`/api/portal/*` → consumidor; lo demás → personal) en vez de por el host, porque resolver el host necesita la base de datos y el selector de esquema es síncrono, y la ruta es justo lo que dice 04. Se descartó (b), restringir solo dos endpoints. Pruebas: `portal_sesion` en `/api/auth/sesion` y `/api/perfil`, en el host del portal y en `shapi.localhost` → 401; `shapi_sesion` + `portal_sesion` en el panel → 200.
- [x] **H-06 (media, EM-05)** `tests/Shapi.Api.Tests/Identidad/ConsumidorPortalTests.cs:52-79`: la prueba del mismo correo en dos portales solo comprueba que en «dos» se entra con su contraseña. Falta comprobar que la contraseña de «uno» falla en «dos» y al revés.
- [x] **H-07 (media, EM-05)** `ConsumidorPortalTests.cs:169-190`: no hay prueba de que `salir` en el portal revoque la sesión del consumidor y borre `portal_sesion` (criterio 2, RF-04). Solo está la negativa.
- [x] **H-08 (media, EM-05)** No hay prueba de los atributos de `portal_sesion`: sin `Domain`, con `HttpOnly`, `Secure`, `SameSite=Lax` y `Path=/` (criterio 2 y 10 §1). El código es correcto (`EndpointsPortal.cs:381`).
- [x] **H-09 (baja, EM-05)** `ConsumidorPortalTests.cs:192-206`: el límite por IP del portal solo se prueba en `entrar`. Convertirlo en una teoría con todos los endpoints, como H-127 en el personal.
- [x] **H-10 (baja, EM-05)** Las pruebas de `hostPortal` usan un `Host` idéntico al canónico, así que no distinguirían si se copia la cabecera (criterio 1). Tampoco se comprueban `colorPortal` ni `logoPortal` en `correo_saliente.datos`.
- [x] **H-11 (baja, EM-05)** Pruebas con el requisito equivocado: `RF_05_CincoIntentosFallidos…`, `RF_05_Entrar_LimitaDiez…`, `RF_05_SalirEnPortal…` y `RF_05_SesionDePortal…` son RF-04; `RF_05_RecuperarConCorreoCompartido…` es RF-03.
- [x] **H-12 (baja, EM-05 y EM-18)** `contratos/openapi/identidad.yaml:353`: `GET /api/portal/auth/sesion` declara un 404 al que nunca se llega. Además, los 422 del portal (`:285`, `:384`, `:403` y `:427`) no declaran el contenido `Problema` con `token_invalido`, a diferencia de los del personal.
- [x] **H-13 (baja, EM-05)** 10 §1 no menciona el límite por IP de `/api/portal/auth/*`, y el `## Resultado` no menciona el cambio a `Modulos/IdentidadModulo.cs`.
- [x] **H-14 (baja, EM-18)** `src/Shapi.Api/Identidad/EndpointsPortal.cs:230`: usa `IgnoreQueryFilters()` en un endpoint con sesión. 10 §2 lo reserva a la administración, al trabajador y a la compuerta, y aquí el filtro global funciona.

## Paso 3 · [EM-04] Recuperación de contraseña y Mi perfil

- [ ] **H-15 (alta, EM-04 y EM-05)** `src/Shapi.Api/Identidad/ServicioRecuperacion.cs:54-89`: si la cuenta no existe, `Solicitar` termina después de un SELECT; si existe, crea el token, encola el correo y hace dos `SaveChanges`. 10 §8 («Enumeración de cuentas») exige el mismo tiempo de respuesta, exista o no la cuenta, al recuperar la contraseña. Es la misma clase de fallo que H-51, ahora en `/api/auth/recuperar` y en `/api/portal/auth/recuperar`, que usan el mismo servicio.
  - **❓ Decisión pendiente:** (a) igualar el trabajo en la base en las dos ramas, como se hizo en H-51, con una prueba RF-03; (b) precisar 10 §8 para que el mismo tiempo se exija solo al iniciar sesión. Recomiendo (a), porque la spec lo pide de forma explícita y es poco código.
- [ ] **H-16 (media, EM-04)** `src/Shapi.Api/Identidad/Endpoints.cs:39-40` y `EndpointsPortal.cs:35-36`: el código limita `recuperar`, que no está en la lista de 10 §1, y no limita `restablecer`, que recibe un token y una contraseña. Corrección: agregar los dos a 10 §1, `RequireRateLimiting` en los dos `restablecer` y los dos endpoints en la teoría de `AutenticacionTests.cs:447`.
- [ ] **H-17 (media, EM-04)** `frontend/apps/panel/src/paginas/A8-1-Perfil.tsx:87,107,114,136,165`: `border-separador` y `border-borde-fuerte` no existen en `packages/ui/src/style.css`. En Tailwind 4 el borde queda en `currentColor`, casi negro, en vez de `#EBEFF5` y `#E4E9F1` del mockup. Corrección: `border-borde-fila` y `border-borde-inactivo`.
- [ ] **H-18 (media, EM-04)** `A8-1-Perfil.tsx:89-98,137-146`: la confirmación es un aviso en línea de 3 s, y 11 §4 pide un aviso breve de 4 s arriba a la derecha (`Toast`). El error de red no ofrece «Reintentar», y los colores son hexadecimales escritos a mano en vez de tokens o de `Aviso`.
- [ ] **H-19 (media, EM-04)** `Endpoints.cs:480-483`: con la contraseña actual incorrecta, la API responde 401 `credenciales_invalidas` («El correo o la contraseña no son correctos.»). A8.1 lo muestra como aviso general, con un correo que el usuario no escribió, en vez de debajo del campo (11 §4). Corrección: 400 `datos_invalidos` con `errores.contrasenaActual`, y actualizar el contrato y la prueba.
- [ ] **H-20 (media, EM-04)** `tests/Shapi.Api.Tests/Identidad/RecuperacionTests.cs:60-89`: la prueba obligatoria «respuesta idéntica exista o no el correo» solo comprueba el 200 de cada caso por separado. Debe comparar el cuerpo y las cabeceras de las dos respuestas.
- [ ] **H-21 (media, EM-04)** `frontend/apps/panel/src/tests/Perfil.test.tsx:43-69`: solo cubre el camino feliz. Faltan los errores por campo, la contraseña actual incorrecta, la confirmación, el error con reintento, Organización y Rol, «Cerrar sesión» y `/admin/perfil` (criterio 5).
- [ ] **H-22 (baja, EM-04)** `Endpoints.cs:46`: `/api/perfil` usa `.RequireAuthorization()` genérico y no `Permisos.EditarPerfil` (04 §3.1), que existe y no se usa. Se resuelve junto con H-05.
- [ ] **H-23 (baja, EM-04)** Faltan pruebas de recuperación:
  - un token aleatorio da 422 (criterio 2);
  - un token de consumidor se rechaza en `/api/auth/restablecer`;
  - el 401 o 400 de la contraseña actual deja la contraseña igual;
  - después de cambiarla, la nueva funciona y la anterior no.
- [ ] **H-24 (baja, EM-04)** `docs/specs/11-interfaz.md:232-246`: EM-04 creó una sección suelta `## §Precisiones`, aparte de §4. Además, quedaron sin documentar los textos del enlace inválido de A1.4b («El enlace ya no sirve»), que difieren de los de A1.2, y los de confirmación de A8.1.
- [ ] **H-25 (baja, EM-04)** `frontend/apps/panel/src/modulos/identidad/useIdentidad.ts:63-67`: `consultarPerfil` es código muerto, y el mock de `Perfil.test.tsx:34-36` devuelve `correo`, que no está en el esquema `Perfil`.
- [ ] **H-26 (baja, EM-04)** Cierre: el criterio 3 dice `{actual, nueva}` y el código usa `contrasenaActual` y `contrasenaNueva`, sin anotarlo como precisión. La bitácora dice «Todo ajustado a los mockups» (ver H-17), y el `## Resultado` atribuye el criterio 4 a pruebas que están en EM-05.
- **❓ Decisión pendiente:** hoy se pueden encolar 10 correos de recuperación por minuto por IP para la misma cuenta. (a) Dejarlo; (b) 3 por hora por cuenta, respondiendo igual, como la verificación (H-53). Recomiendo (b), precisado en 10 §1.
- **❓ Decisión pendiente:** al restablecer o cambiar la contraseña, hoy no se invalidan los demás enlaces de recuperación pendientes ni se limpian `intentos_fallidos` y `bloqueado_hasta`. (a) Dejarlo; (b) marcar como usados los demás tokens `recuperacion` de la cuenta y reiniciar el bloqueo. Recomiendo (b).
- **❓ Decisión pendiente:** hoy los intentos con la contraseña actual incorrecta en Mi perfil no cuentan para el bloqueo, así que una sesión robada puede probar contraseñas sin límite. (a) Contarlos con `RegistrarIntentoFallido`; (b) aplicar el límite por IP; (c) dejarlo. Recomiendo (a).

## Paso 4 · [JZ-05] Siembra de demostración

- [ ] **H-27 (alta, JZ-05)** `infra/compose.prod.yml:100-101`: `SHAPI_URL_ORIGEN_ENVIOS: ${SHAPI_URL_ORIGEN_ENVIOS:-http://origen-envios:8080}` toma el valor del `.env`, y `.env.example:42-43` lo define como `http://localhost:5101` y `:5102`, porque es lo que necesita `dotnet run` en desarrollo. Como el manual, la CI y `e2e.yml` copian `.env.example` a `.env`, en el ambiente productivo `url_origen` queda en `localhost`. La compuerta no llega a los orígenes y todas las llamadas de la demostración fallan. Incumple el criterio 5 («en producción, los nombres de servicio»). La prueba `RNF_14_UsaUrlsLocales…` solo busca el texto en el compose. Corrección: fijar `http://origen-envios:8080` y `http://origen-agro:8080` en `compose.prod.yml` sin interpolar, como ya se hace con `SHAPI_POSTGRES_CADENA`, y comprobar en `infra/verificar.mjs` el valor resuelto (`compose config`).
- [ ] **H-28 (alta, JZ-05)** `src/Shapi.Infraestructura/Siembra/Demo/SiembraDemo.cs:686-696`: `--reiniciar` falla si durante la demo hubo un pago rechazado de contratación. EM-08 agregó `pago.consumidor_id` y `pago.api_id`, y un rechazo no tiene suscripción. El borrado solo quita pagos por `suscripcion_api_id` o `suscripcion_plataforma_id`, así que `DELETE FROM consumidor` y `DELETE FROM api` violan la FK. La transacción revierte todo, pero el comando deja de funcionar (criterio 1). Corrección: borrar también los pagos por `consumidor_id` o `api_id`, con una prueba que siembre un `Pago.ContratacionRechazada` y reinicie.
- [ ] **H-29 (media, JZ-05)** `SiembraDemo.cs:697-702`: el reinicio también falla si el personal o un propietario de la demo dejó referencias en una organización que no es de la demo, por ejemplo Sofía responde un caso (`caso_mensaje.autor_id`), Rodrigo revierte un pago (`pago.revertido_por`) o Ana Lucía tiene otra membresía. Es plausible si en la exposición se registra un proveedor en vivo. Corrección: no borrar a esos usuarios, solo restablecerlos, o reasignar las referencias, con prueba.
- [ ] **H-30 (media, JZ-05)** `SiembraDemo.cs:141-145, 243-249`: los ciclos de plataforma y los periodos de los pagos no coinciden con A6.2, B1.4 y B1.5.
  - Ciclos: el mockup da Envíos −20, Petén −32, Datos −39 y Cafetalera −16, y la siembra usa −21, −33, −40 y −8.
  - Periodos: cada pago cubre el ciclo anterior y no el que se pagó; por ejemplo, Lanzamiento de Envíos cubre −34…−4 y B1.5 muestra «24 ago – 22 sep».
  - Corrección: tomar los inicios del mockup, `periodoInicio` igual al inicio del ciclo pagado, y una prueba de fechas contra A6.2 y B1.5.
- [ ] **H-31 (media, JZ-05)** `SiembraDemo.cs:149-157`: A6.2 muestra 1, 2 y 1 APIs para Cafetalera, Petén y Datos Chapines (y CAS-102 dice «Sus APIs dejaron de responder»), pero la siembra no les crea ninguna. Corrección: sembrarlas, aunque sea sin publicar, y probar los conteos.
- [ ] **H-32 (media, JZ-05)** `SiembraDemo.cs:296-298`: nada prueba que el siguiente caso que crea la aplicación después de sembrar sea CAS-105; el `setval` es lo único que evita el choque con la secuencia de H-30 de la auditoría anterior. Tampoco se prueba la siembra cuando ya existe un CAS-100.
- [ ] **H-33 (baja, JZ-05)** `src/Shapi.Trabajador/Program.cs:16-34`: no hay prueba del comando: que exija `SHAPI_MODO_DEMO`, que lea `--reiniciar` y que llame a `ResincronizarCache` (criterio 6).
- [ ] **H-34 (baja, JZ-05)** `SiembraDemo.cs:356-374, 413-461, 706-718`: crea suscripciones, claves, pagos, medios de pago y casos por reflexión (`Crear<T>` y `Poner`), sin las fábricas del dominio que ya existen. Va contra el criterio de H-125, y se rompería en silencio si cambian las propiedades.
- [ ] **H-35 (baja, JZ-05)** El `## Resultado` no recoge las decisiones que solo están en la bitácora (la bitácora duplicada al reiniciar y la clave rotada hace 9 h). El aviso a JZ-13 propone `dotnet run … sembrar-demo --reiniciar`, pero el entorno E2E es el productivo y debe usar `docker compose … exec trabajador …`.
- **❓ Decisión pendiente:** `--reiniciar` vuelve a agregar las 11 entradas de B3.2, y después de cada reinicio la bitácora muestra entradas repetidas que apuntan a actores borrados. (a) Dejarlo así, porque la bitácora es solo de inserción; (b) al reiniciar, no insertar las que ya existen (por descripción), como la siembra normal; (c) un rol de base de datos para la demo con permiso de borrar la bitácora. Recomiendo (b).
- **❓ Decisión pendiente:** los mockups usan dos «hoy»: A6, B1.1 y los casos usan el 13 sep, y B1.3 y B2 el 22 sep. La siembra mezcla los dos. Recomiendo documentar en 07 §6 qué referencia usa cada grupo de datos.
- **❓ Decisión pendiente:** `SHAPI_MODO_DEMO` vale `true` por defecto en `compose.prod.yml:38,72,97`, y 06 §7.1 dice «solo para la exposición». Recomiendo dejarlo, porque ese ambiente es el de la exposición, y anotarlo en el manual técnico.

## Paso 5 · [JG-18] y [JG-03] CI y plan

- [ ] **H-36 (alta, JG-18)** `scripts/tareas.mjs:339` (`archivosDelPr`, usado por `scripts/cambios-ci.mjs`): `git diff --name-only` detecta los renombres, así que un archivo movido solo aparece con su ruta nueva. Si un PR mueve `docs/specs/08-compuerta.md` (lo leen `CodigosErrorTests` y `AccionesBitacoraTests`) o `docs/specs/11-interfaz.md` (lo lee `rutas.test.tsx`) a una ruta libre, se omiten el backend y el frontend, los checks obligatorios quedan en verde y se integra con pruebas rotas. Pasa lo mismo al mover `src/…` o `frontend/…`. Incumple RNF-15 y la garantía del `## Resultado` de JG-18. El auditor lo reprodujo en un repositorio temporal. Corrección: `--no-renames`, con una prueba que lo exija y un caso en `cambios-ci.test.mjs` con la ruta de origen y la de destino.
- [ ] **H-37 (media, JG-18)** `docs/specs/06-arquitectura.md:374` (§7.3) sigue diciendo que en cada PR se compila y se prueban el backend y el frontend. Desde #59 depende del área que toca el PR. Solo se actualizó `convenciones.md:85`, y la spec manda (ADR-31). Corrección: precisar 06 §7.3 con la regla de `cambios-ci.mjs`.
- [ ] **H-38 (baja, JG-18)** `docs/plan/protocolo.md:88`, `AGENTS.md:37,84`, `.github/pull_request_template.md:14` y `docs/plan/instalacion.md:52` dicen que el check `backend` corre la suite completa en cada *push*. En un PR sin cambios de backend, ese check sale verde sin pruebas. Corrección: «salvo que `scripts/cambios-ci.mjs` lo omita; en `main` siempre corre completa».
- [ ] **H-39 (baja, JG-18)** `docs/specs/06-arquitectura.md:407` (§9): la lista de `scripts/` no incluye `cambios-ci.mjs` (la misma clase de hueco que H-136).
- [ ] **H-40 (baja, JG-18)** `scripts/tareas.mjs:311, 338-343`: en un PR, si el diff no se puede leer, `archivos = null`, la bitácora no se revisa y el check obligatorio `titulo` aprueba. Corrección: en `--validar-cierre`, si el evento es `pull_request` y no hay diff, fallar, con prueba.
- [ ] **H-41 (baja, JG-03)** `scripts/tareas.mjs:204-222` (`mostrarHoy`): ninguna prueba cubre la salida de `--hoy` (criterio 13: hoy, atrasadas, quién lo espera y la siguiente). Corrección: extraer una función pura y probarla.
- [ ] **H-42 (baja, JG-03)** `docs/plan/tareas/JG-03-…md:225`: el `## Resultado` dice que «el workflow no cambió», pero el PR #43 cambió los comentarios de `tablero-plan.yml`.
- **❓ Decisión pendiente:** el job `ambiente-productivo` (E2E en el PR) no es check obligatorio, así que una E2E rota no bloquea la integración. (a) Hacerlo obligatorio (tarda unos 2.5 min y su `if` es por evento, así que no se omite en un PR); (b) dejarlo informativo. Recomiendo (a).
- **❓ Decisión pendiente:** un PR puede editar `cambios-ci.mjs` para que omita todo. Es el mismo modelo de confianza que editar `ci.yml`, y lo cubren `revision-claude` y la CI de `main`. (a) Aceptarlo y documentarlo; (b) leer el decidor desde la base (`git show HEAD^1:scripts/cambios-ci.mjs`). Recomiendo (a).

## Paso 6 · [JZ-06] y [JZ-07] Infraestructura y pruebas E2E

- [ ] **H-43 (media, JZ-06, regresión de H-109)** `.github/workflows/publicar-imagenes.yml:39`: `docker/build-push-action@v7` no está fijada por SHA. Corre después de `docker/login-action`, con las credenciales de GHCR ya guardadas, y recibe `github.token`. Corrección: fijarla por SHA con un comentario de la versión.
- [ ] **H-44 (media, JZ-06)** `publicar-imagenes.yml:8-10`: `packages: write` está a nivel del workflow, así que también lo recibe el job `verificar-ambiente` de los PR, que ejecuta código del PR (`pnpm install`, Playwright y `infra/verificar.mjs`). Corrección: `contents: read` global y `packages: write` solo en el job `publicar`.
- [ ] **H-45 (baja, JZ-06)** `infra/borde/Dockerfile:18-21`: la imagen del borde corre como root; las demás usan `USER app`. Corrección: un usuario no root con `/data` y `/config` propios (el binario de Caddy ya tiene `cap_net_bind_service`).
- [ ] **H-46 (baja, JZ-07)** `AGENTS.md:29` todavía dice «Los de `tests/e2e/` todavía no existen: los crea JZ-07» (la misma clase de texto atrasado que H-23).
- [ ] **H-47 (baja, JZ-07)** Ni el manual técnico, ni `instalacion.md`, ni el README explican cómo preparar las E2E la primera vez (`pnpm install` y `pnpm exec playwright install chromium` en `tests/e2e`).
- **❓ Decisión pendiente (JZ-06):** la prueba obligatoria pide comprobar que `curl http://localhost:8080/salud` falle. `verificar.mjs:553` prueba el 5080, y lo demás lo cubre `compose config`. Recomiendo agregar el 8080 literal (una línea).
- **❓ Decisión pendiente (JZ-07):** la E2E de registro llega al panel porque la verificación inicia la sesión, pero no pasa por A1.3 (CU-02). Recomiendo agregar «Salir» y luego «Entrar» en la misma prueba.

## Paso 7 · [DC-05] y [DC-04] APIs

- [ ] **H-48 (media, DC-05)** `src/Shapi.Api/Apis/Endpoints.cs:21`: `GET /api/apis/{id}/rutas` exige `ConfigurarApis`, así que el lector recibe 403. 04 (línea 33, «Ver APIs, rutas, planes…») le da lectura. La prueba `ApisTests.cs:587-597` fija el comportamiento incorrecto. En la práctica, un lector que abre una API desde A3.1 ve «No se pudo cargar la API.». El criterio 4 de la tarea dice «(propietario o editor)», pero manda la spec. Corrección: `VerApis` en el GET, la prueba con 200 para el lector y otra de 403 para el lector en los dos PUT.
- [ ] **H-49 (media, DC-05)** `frontend/apps/panel/src/paginas/A3-3-Especificacion.tsx:24-32,116-124`: la tarjeta del archivo cargado del mockup (nombre, «OpenAPI 3.0.3 · 18 KB» y «Cargado») solo aparece justo después de subirlo. Al volver a A3.3, que es el destino de A3.1, no se ve que ya hay una especificación. `GET /rutas` no devuelve título, versión ni fecha de carga, aunque `api` los guarda. Corrección: devolverlos y mostrar la tarjeta.
- [ ] **H-50 (baja, DC-05)** `src/Shapi.Aplicacion/Apis/GestionarEspecificacion.cs:43-81`: dos cargas simultáneas de la misma API violan el UNIQUE `(api_id, metodo, patron)` y responden 500. Corrección: `SELECT … FOR UPDATE` sobre `api` al empezar.
- [ ] **H-51 (baja, DC-05)** `CambioExposicionRuta`: un elemento sin `expuesta` se toma como `false` y oculta la ruta sin aviso, y un elemento `null` da 500 (`GestionarEspecificacion.cs:175`). Corrección: validar el lote con 400 `datos_invalidos`.
- [ ] **H-52 (baja, DC-05)** `contratos/openapi/apis.yaml:95-113,195-196`: no documenta el 400 cuando falta la parte `archivo`, ni el 404 de una `rutaId` que no es de la API.
- [ ] **H-53 (baja, DC-05)** `frontend/apps/panel/src/paginas/A3-4-Rutas.tsx:79-90`: los radios son los nativos. El mockup usa radios de 16 px con borde `#C9D2E1` y punto `#3B6FF0`, separados 28 px.
- [ ] **H-54 (baja, DC-05)** Cierre: quedaron sin precisar en 07 que, al retirar una ruta, su `consumo_diario` se consolida con `ruta_id` nulo (07 §3.5 solo habla de rutas no identificadas) y la forma de `ruta.definicion` (07 §3.2), que consumen DC-07 y DC-10. Además, falta avisar a JG-09 de que, tras una recarga, pueden llegar eventos con una `ruta_id` ya borrada.
- [ ] **H-55 (media, DC-04)** `frontend/apps/panel/src/paginas/A3-2-Registro.tsx:52`: ante un 422 `origen_inaccesible` u `origen_no_permitido` solo se muestra el título y se pierde `detalle.motivo`. El criterio 6 pide mostrar el resultado de la prueba de conexión. Corrección: mostrar el motivo y agregar la prueba del 422.
- [ ] **H-56 (baja, DC-04)** `src/Shapi.Aplicacion/Apis/RegistrarApi.cs:20-25,58`: el subdominio se valida sin recortar y se usa recortado. En .NET, `$` acepta un `\n` final, así que `"admin\n"` pasa el patrón y la lista de reservados, y luego la entidad lanza una excepción: 500 en vez de 400. No permite saltarse la lista. Corrección: recortar antes de validar (o `\z`) y probar ese caso.
- [ ] **H-57 (baja, DC-04)** El `## Resultado` no explica que se modificó `A3-2-Registro.tsx` y no `A3-2-RegistrarApi.tsx`, como dice la tarea.
- **❓ Decisión pendiente (DC-04):** «DNS fallido → 422 `origen_inaccesible`» y «subdominio reservado → 400 `datos_invalidos`» no están en ninguna spec. Recomiendo precisarlo en 06 §5.3.
- **❓ Decisión pendiente (DC-05):** la prueba de especificación inválida solo usa un YAML mal formado. Recomiendo agregar un documento semánticamente inválido (Swagger 2.0, sin `info.version`) y una bomba de alias YAML que espere 422. Microsoft.OpenApi 2.12 ya se defiende, pero ninguna prueba lo fija.
- **❓ Decisión pendiente (DC-05):** los mensajes de validación de Microsoft.OpenApi y SharpYaml llegan en inglés a la pantalla (RNF-12). (a) Anteponer un texto en español y dejar el original como detalle; (b) aceptarlo. Recomiendo (a).

## Paso 8 · [EM-07] y [EM-08] Planes de API y contratación

- [ ] **H-58 (media, EM-07)** `src/Shapi.Api/Planes/GestionarPlanes.cs:31`: `ListarPlanes` no tiene `ORDER BY`. A4.1 y A5.4 muestran los planes por precio (Básico, Comercio y Volumen), y al editar un plan su fila suele pasar al final. Corrección: ordenar por precio y nombre, con prueba.
- [ ] **H-59 (media, EM-07)** `GestionarPlanes.cs:86-89,190-193`: cualquier `DbUpdateException` se traduce a 409 `plan_duplicado`; por ejemplo, un precio de 1e11 desborda `numeric(12,2)` y responde «Ya existe un plan con ese nombre». `ValidarPlan` tampoco limita el precio a 2 decimales (convenciones §5), así que la base redondea sin avisar. Corrección: validar decimales y máximo con 400 y atrapar solo el 23505.
- [ ] **H-60 (media, EM-07 y EM-08)** `GestionarPlanes.cs:105-115` acepta un plan de pago con precio 0. Al contratarlo, el cobro se autoriza, `ck_pago_monto` (`monto > 0`) hace fallar el `SaveChanges` y la respuesta es 500, sin reembolso.
  - **❓ Decisión pendiente:** (a) exigir `precio > 0` si no es gratuito (400) y agregar `CHECK (es_gratuito OR precio > 0)` en 07 §3.3; (b) tratar el precio 0 como gratuito al contratar. Recomiendo (a).
- [ ] **H-61 (media, EM-07)** `tests/Shapi.Api.Tests/Planes/PlanesTests.cs:59-68,152-185`: la prueba obligatoria «con la publicación en Redis» usa un publicador falso y solo comprueba que se llamó. Debe leer `susc:{id}` en Redis real (colección `RedisCache`) y comprobar `cuota_llamadas` y `limite_minuto` (criterio 2).
- [ ] **H-62 (baja, EM-07)** `frontend/apps/panel/src/paginas/A4-1-PlanesApi.tsx:82,112`: al editar, el precio sale «149» y el mockup muestra «149.00»; un plan gratuito sale «Gratis» y RF-18 y los mockups usan «Q 0.00».
- [ ] **H-63 (baja, EM-07)** `GestionarPlanes.cs:75,179,234`: la bitácora queda «…de la API API de Cotización de Envíos», y usa textos fijos en vez de `AccionesBitacora.PlanApi*`. Corrección: reutilizar `EnLaApi` de `ServicioClaves.cs:336` y las constantes.
- [ ] **H-64 (baja, EM-07 y EM-08)** `src/Shapi.Api/Planes/Endpoints.cs:97-118` y `ContratacionApi.cs:88`: los 400 `datos_invalidos` no traen `errores` por campo (convenciones §5).
- [ ] **H-65 (baja, EM-07)** `contratos/openapi/planes.yaml:184-196`: `/api/portal/planes` no documenta su 404, la respuesta incluye `creadoEn` y `actualizadoEn`, que no están en el contrato, y no lleva `moneda: "GTQ"` (convenciones §5).
- [ ] **H-66 (baja, EM-07)** `PlanesTests.cs:150,187,316` dicen RF-19 y son RF-18, y `:207` y `:224` no llevan requisito. La prueba de edición en Vitest no comprueba el cuerpo del PUT, y ninguna afirma «peticiones» (criterio 5) ni el 409.
- [ ] **H-67 (media, EM-08)** `src/Shapi.Api/Suscripciones/ContratacionApi.cs:129-157`: el cobro se autoriza antes del `SaveChanges` y del `Commit`. Si estos fallan, el cobro queda sin pago registrado y sin `ReembolsarAsync`. Corrección: ante una excepción después del cobro, reembolsar, con prueba.
- [ ] **H-68 (media, EM-08)** `tests/Shapi.Api.Tests/Suscripciones/ContratacionTests.cs:57-74`: la prueba del cobro aprobado no comprueba lo que exige el criterio 3: `susc:{id}` en Redis, el ciclo (`inicio` a la medianoche de America/Guatemala y `fin = inicio + vigencia`, 09 §4), el monto y el periodo del pago, y la marca, los últimos 4 y el vencimiento del medio de pago. Agregar esas aserciones con un reloj a media tarde de Guatemala.
- [ ] **H-69 (baja, EM-08)** `ContratacionTests.cs:89-100`: el rechazo no comprueba que no queden `medio_pago` ni `clave` (09 §2), y no hay prueba de dos contrataciones simultáneas.
- [ ] **H-70 (baja, EM-08)** `ContratacionApi.cs:122-126`: un vencimiento o un titular inválidos responden 422 `datos_invalidos`, y convenciones §5 le asigna 400.
- [ ] **H-71 (baja, EM-08)** `ContratacionApi.cs:130-142`: si `CobrarAsync` devuelve `pasarela_no_disponible`, se registra un pago `rechazado` y se responde 402, mientras que en la tokenización el mismo error responde 503. Corrección: tratarlo igual, sin registrar el pago.
- [ ] **H-72 (baja, EM-08)** `ContratacionTests.cs:147` dice RF-21 y es RF-20. En 07, el diagrama ER (`PAGO`) y la línea de índices de §3.4 no incluyen `consumidor_id`, `api_id`, sus FK ni sus índices.
- **❓ Decisión pendiente (EM-07):** el mockup `NuevoPlan.dc.html` empieza con «Plan gratuito» marcado y el formulario empieza sin marcar. Recomiendo seguir el mockup.
- **❓ Decisión pendiente (EM-07):** A4.1 no tiene botón para desactivar planes (el mockup solo muestra «Editar») y la vigencia solo ofrece 30 y 365 días, aunque la spec permite de 1 a 366. Recomiendo aceptarlo y anotarlo en el `## Resultado`.
- **❓ Decisión pendiente (EM-07):** pasar un plan con suscripciones de gratuito a pago (o al revés) deja renovaciones sin tarjeta, y la spec no lo cubre. Recomiendo impedirlo con 422 mientras haya suscripciones vigentes y precisarlo en 09.
- **❓ Decisión pendiente (EM-07 y EM-08):** la lógica vive en `Shapi.Api/Planes` y `Shapi.Api/Suscripciones/ContratacionApi.cs`, no en `Shapi.Aplicacion` con `Resultado<T>` (convenciones §6), lo que dificulta que EM-13 la reutilice. (a) Moverla ahora; (b) dejarla y que EM-13 la mueva si la necesita. Recomiendo (b), con un aviso en EM-13.
- **❓ Decisión pendiente (EM-08):** el PR #63 agregó `PrepararClavesParaSuscripcion` a `IServicioClaves` (módulo de Jordin). Es aditivo y coherente con 06 §5.2. Recomiendo aceptarlo.
- **❓ Decisión pendiente (EM-08):** un plan inactivo responde 422 `plan_no_encontrado`. Recomiendo dejarlo y documentarlo en `suscripciones.yaml`.
- **❓ Decisión pendiente (EM-08):** con una suscripción `suspendida`, el consumidor recibe 409 `suscripcion_existente`, y 09 §3 dice «suspendida → finalizada: el consumidor contrata otro plan». Recomiendo confirmar en EM-13 que ese caso le corresponde.

## Paso 9 · [DC-03] y [DC-08] Portal de marca blanca

- [ ] **H-73 (media, DC-03)** `frontend/apps/portal/src/layouts/LayoutPublico.tsx:21-22`: el pie muestra el nombre del portal sin insignia. Los mockups (`A5/Main.dc.html` e `InicioAgro.dc.html:161`) muestran la insignia de iniciales de 24 px y el nombre de la organización. `ConfiguracionPortal` no expone ese nombre, aunque `PortalResuelto.NombreOrganizacion` lo tiene. Corrección: agregar `nombreOrganizacion` al contrato `portal.yaml` y al endpoint, y pintarlo en el pie, con pruebas.
- [ ] **H-74 (media, DC-03)** `frontend/apps/portal/src/App.tsx:25`: la raíz solo define `--marca-principal`, pero `Boton`, `Campo` y el anillo de foco leen `--principal`, `--principal-hover` y `--anillo-foco`, que siguen en el azul de Shapi. Solo `MarcoAcceso` los redefine (`FormulariosAcceso.tsx:9-13`). 11 §1 dice que en el portal el color principal es la marca, y el criterio 3 dice que no aparece la de Shapi. Corrección: mover `COLORES_MARCA` a la raíz de `App`, quitarlo de `MarcoAcceso` y probarlo en la raíz.
- [ ] **H-75 (media, DC-03)** `frontend/apps/portal/src/layouts/LayoutCuenta.tsx:50`: el enlace activo usa `bg-fondo` y `text-tinta`. El mockup `B2/Suscripcion.dc.html` usa un fondo teñido con la marca (`color-mix(in srgb, var(--marca-principal) 8%, #FFFFFF)`) y el texto `#2B3547`.
- [ ] **H-76 (media, DC-03)** `frontend/apps/portal/src/modulos/sesion/CerrarSesionConsumidor.tsx:139-147`: al cerrar sesión no se limpia la caché de consultas (el panel sí lo hace en `useCerrarSesion.ts:21-25`) y no hay aviso con «Reintentar» si falla (11 §4). Cuando DC-11 y DC-13 guarden claves o pagos en caché, otro consumidor en el mismo navegador vería primero los datos del anterior.
- [ ] **H-77 (baja, DC-03)** `frontend/apps/portal/src/rutas.tsx:70-103`: ninguna ruta tiene `errorElement`, así que un error de render muestra la pantalla de React Router en inglés (la misma clase que H-95).
- [ ] **H-78 (baja, DC-03)** `rutas.tsx:89-99`: `/cuenta` no tiene ruta índice y muestra el layout vacío (la misma clase que H-96).
- [ ] **H-79 (baja, DC-03)** `frontend/apps/portal/src/tests/Estructura.test.tsx`: faltan pruebas de la configuración con 5xx (debe mostrar `EstadoError` y no «API no disponible»), de la sesión con 5xx en `/cuenta/*` y de la ruta comodín 404.
- [ ] **H-80 (media, DC-08)** `frontend/apps/portal/src/paginas/A5-3b-Invitacion.tsx:84-91`: si aceptar la invitación responde 409 `correo_ya_registrado`, la pantalla solo muestra un aviso general, sin «Entrar» ni «Recuperar la contraseña», como pide CU-11 2a (`05-casos-de-uso.md:205`). Tampoco hay prueba.
- [ ] **H-81 (baja, DC-08)** `A5-9-Recuperacion.tsx:35-37,57-59`: después de enviar, la frase «Si el correo tiene una cuenta en este portal, recibirá el enlace.» sale dos veces.
- [ ] **H-82 (baja, DC-08)** `A5-7-Acceso.tsx:140-150`: «Contraseña» es un `<span>` y el campo solo tiene `aria-label`, así que hacer clic en el rótulo no enfoca el campo (H-88).
- [ ] **H-83 (baja, DC-08)** `FormulariosAcceso.tsx:40-47`: «Reintentar» es texto subrayado y no el botón secundario de 11 §4 (H-89). Es la misma decisión que el `AvisoError` de A1, aplazado a DC-16. Corrección: que DC-16 incluya el portal.
- [ ] **H-84 (baja, DC-08)** Faltan en 11 §4 las precisiones de las pantallas de acceso del portal: los estados de A5.3b, A5.8 y A5.10 con el enlace inválido, el estado de A5.9 después de enviar y el encabezado solo con la marca.
- **❓ Decisión pendiente (DC-03):** `LayoutPublico.tsx:14,24` y `LayoutCuenta.tsx:67` enlazan a `/documentacion/inicio`, que no existe, y `/documentacion` sin parámetro da 404. (a) Agregar una ruta índice que redirija a la primera ruta expuesta; (b) dejarlo para DC-07. Recomiendo (a), con aviso a DC-07.
- **❓ Decisión pendiente (DC-08):** el mockup de A5.10 dice «Está definiendo la contraseña de {correo} en el portal de Envíos Xelajú.». La precisión de 11 solo quita el correo, pero el código también quita el nombre del portal. (a) «Defina una contraseña nueva para su cuenta en el portal de {nombrePortal}.»; (b) dejar el texto fijo de A1.4b. Recomiendo (a).

## Paso 10 · [JZ-04] Bitácora de acciones sensibles

- [ ] **H-85 (media, JZ-04)** `src/Shapi.Api/Bitacora/Endpoints.cs:83-87,114` y `src/Shapi.Infraestructura/Siembra/Base/SiembraBase.cs:19`: la organización de plataforma se llama «Shapi», así que B3.2 muestra «Administrador · Shapi», y el mockup dice «Administrador · Plataforma Shapi» (criterio 2). Las pruebas no lo detectan porque el Vitest simula el texto a mano y la de integración compara con `plataforma.Nombre`.
  - **❓ Decisión pendiente:** (a) renombrar la organización de plataforma en la siembra base; (b) que el endpoint muestre «Plataforma Shapi» cuando la organización es de tipo `plataforma`, precisado en 10 §7; (c) corregir el mockup. Recomiendo (b), y que la prueba compare con el texto literal.
- [ ] **H-86 (media, JZ-04)** `frontend/apps/panel/src/paginas/B3-2-Bitacora.tsx:80`: la pantalla siempre pide `pagina: 1, tamano: 20` y no usa `total`, así que con más de 20 acciones en el periodo, las anteriores no se pueden ver y no hay aviso (10 §7 dice que los resultados «se paginan»).
  - **❓ Decisión pendiente:** (a) un paginador mínimo que solo aparece si hay más de una página; (b) pedir 100 y avisar si hay más; (c) aceptar el límite de 20 y documentarlo. Recomiendo (a).
- [ ] **H-87 (media, JZ-04)** `tests/Shapi.Api.Tests/Bitacora/BitacoraTests.cs`: solo se prueban actores de tipo usuario del personal de plataforma. Faltan el consumidor (rol `consumidor` con la organización del proveedor), el sistema (`rol: sistema`, `organizacion: null`) y un miembro de un proveedor (criterio 1).
- [ ] **H-88 (baja, JZ-04)** `Endpoints.cs:155-160`: el 400 `datos_invalidos` no trae `errores` por campo (convenciones §5).
- [ ] **H-89 (baja, JZ-04)** `contratos/openapi/sistema.yaml:107-109`: `ActorBitacora.rol` es texto libre. Corrección: un `enum` con los valores posibles, incluido el `usuario` que devuelve el código para un usuario sin membresía (`Endpoints.cs:122`).
- [ ] **H-90 (baja, JZ-04)** 10 §7 no recoge que el rol y la organización del actor salen de su membresía actual; solo está en el `## Resultado`.
- [ ] **H-91 (baja, JZ-04)** `frontend/apps/panel/src/tests/Bitacora.test.tsx`: faltan el estado de error y que la primera consulta lleve el periodo por defecto (hoy y los seis días anteriores, en Guatemala).

## Paso 11 · [JG-04] y [JG-07] Publicador de Redis y servicio de claves

- [ ] **H-92 (media, JG-04)** `src/Shapi.Aplicacion/Comun/IPublicadorCache.cs:8-33` y `src/Shapi.Infraestructura/Cache/EscritorCacheRedis.cs:38-41`: `EscribirApiAsync` solo escribe los hosts actuales y no hay forma de quitar uno, así que un dominio propio quitado o sin verificar sigue enrutando a la API hasta que corre la resincronización (5 min). 07 §4 (línea 730) presenta ese borrado como respaldo cuando Redis falla, no como el camino normal.
  - **❓ Decisión pendiente:** (a) agregar `EliminarHost` (o que `PublicarApi` reciba los hosts que se quitan) y exigirlo en DC-14; (b) aceptar los 5 minutos y precisarlo en 07 §4 y DC-14. Recomiendo (a).
- [ ] **H-93 (baja, JG-04)** `src/Shapi.Trabajador/Resincronizacion/ResincronizarCache.cs:69-78`: el paso 4, que corrige las claves revocadas o rotadas durante la resincronización (RF-28), no tiene una prueba propia con una revocación entre la lectura y la escritura.
- [ ] **H-94 (baja, JG-04)** Los avisos de `jordin.md:502-503` no llegaron a las tareas: DC-14 (criterio 3) no dice que `DELETE .../dominio` debe publicar y borrar `api:host:{dominio}`, y EM-09 (criterios 2 y 3) no dice que contratar o subir de plan debe publicar `org:{id}` y la suscripción.
- [ ] **H-95 (baja, JG-07)** `contratos/openapi/claves.yaml:72,194`: las descripciones mencionan `revocadaPor`, un campo que no existe en el esquema `Clave` ni en la respuesta. Corrección: redactarlo como «guarda `revocada_por = proveedor`» y regenerar los tipos.
- [ ] **H-96 (baja, JG-07)** `src/Shapi.Infraestructura/Claves/ServicioClaves.cs:251-252`: `Bloquear` hace `SELECT … FOR UPDATE` sin el filtro por organización, antes de comprobar el dueño. No expone datos (luego responde 404), pero permite bloquear un instante la fila de otra organización. Corrección: unir con `suscripcion_api` y `api` en el mismo SQL.
- [ ] **H-97 (baja, JG-07)** `tests/Shapi.Api.Tests/Claves/ClavesTests.cs:31-35,801-805`: los comentarios dicen que la sesión del consumidor todavía no existe (EM-05 ya está integrada). Agregar al menos una rotación o revocación con la cookie `portal_sesion` real.

## Paso 12 · [JG-01] Auditoría final

- [ ] Ejecutar la verificación completa: build, formato, migraciones, pruebas (`-m:1`), tipos generados, lint, *typecheck*, pruebas y *build* del frontend, `scripts/` y `--validar`.
- [ ] Volver a auditar en contexto limpio las tareas corregidas.
- [ ] Anotar el resultado final en este documento y en la bitácora de Jordin.

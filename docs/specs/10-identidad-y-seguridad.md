# 10 · Identidad y seguridad

## 1. Autenticación

| Aspecto | Decisión |
|---|---|
| Mecanismo | **Sesiones del lado del servidor** con una cookie, sin JWT ([ADR-06](12-decisiones.md)). La cookie guarda un valor aleatorio de 32 bytes, y la tabla `sesion` guarda su SHA-256 |
| Cookie | Personal: `shapi_sesion`, en el host `shapi.localhost`. Consumidor: `portal_sesion`, en el host de cada portal. Las dos son `HttpOnly`, `Secure`, `SameSite=Lax` y `Path=/` |
| Vencimiento | A las 8 h de inactividad (`Sesion:InactividadHoras`) o a los 7 días desde el inicio de sesión. Cada petición actualiza `ultimo_uso_en`, como máximo una vez por minuto |
| CSRF | `SameSite=Lax`, más la cabecera `X-Requested-With: shapi` obligatoria en todo método que no sea GET, más la comprobación de `Origin`. Los frontends llaman a la API **en su mismo host**, así que no hace falta CORS entre el panel y la API |
| Contraseñas | Tienen entre 10 y 128 caracteres y no pueden ser iguales al correo. Se guardan con `PasswordHasher<T>` de ASP.NET Core Identity (PBKDF2-HMAC-SHA512, 100,000 iteraciones o más) |
| Bloqueo | Tras 5 intentos fallidos seguidos, la cuenta se bloquea 15 minutos (`bloqueado_hasta`). Los mensajes de error son genéricos: `401 credenciales_invalidas` si la cuenta no existe o la contraseña no coincide, con el mismo tiempo de respuesta en los dos casos. Durante el bloqueo responde `423 cuenta_bloqueada`. Una cuenta desactivada responde `403 cuenta_desactivada` (CU-02 2b), solo si la contraseña es correcta |
| Recuperación | Siempre se responde lo mismo, exista o no la cuenta. El enlace es de un solo uso y vence a los 60 minutos. Al usarlo **se revocan todas las sesiones** de la cuenta ([RF-03](03-requisitos.md#rf-03)) |
| Verificación de correo | El enlace vence a las 24 horas. Mientras no se confirme, el proveedor no puede **publicar** APIs y el consumidor no puede **contratar** planes ([RF-02](03-requisitos.md#rf-02)) |
| Cuentas de plataforma | Las crea el administrador y reciben un enlace `definir_contrasena` que vence a los 7 días ([RF-42](03-requisitos.md#rf-42)) |
| Limitación de peticiones | La API de control limita a 10 peticiones por minuto por IP (`RateLimiter` de ASP.NET Core) los endpoints de `/api/auth/*` que reciben credenciales o tokens: `registro`, `verificar-correo`, `reenviar-verificacion` y `entrar`. Al pasarse responde `429 demasiadas_peticiones` con `Retry-After`. `GET /api/auth/sesion` y `POST /api/auth/salir` no se limitan, porque no sirven para adivinar credenciales y el panel consulta la sesión en cada carga. La IP del cliente se toma de `X-Forwarded-For` solo si la conexión viene del borde (la máquina o una red privada, como la de Docker) |

Los enlaces enviados por correo llevan a la pantalla del ámbito de la cuenta. Para el personal, el host es el dominio base configurado: la verificación lleva a `https://{dominio_base}/verificar-correo?token={token}` (A1.2) y la recuperación a `https://{dominio_base}/restablecer?token={token}` (A1.4b). Para un consumidor, quien encola el correo agrega `hostPortal` a los datos, con el host del portal de la API, `{sub}.{dominio_base}` (06 §4). Se arma con el subdominio de la API que resolvió `IResolutorPortal`, nunca copiando la cabecera `Host`, y el enlace usa ese host: `https://{hostPortal}/verificar-correo?token={token}` (A5.8) y `https://{hostPortal}/restablecer?token={token}` (A5.10). El dominio propio de una API (RF-12) no sirve, porque apunta a la compuerta y no al portal. Si `hostPortal` no es una sola etiqueta ASCII seguida del dominio base, el correo no se arma y cuenta como un intento fallido. El token se codifica como componente de la URL.

### Enrutamiento después de iniciar sesión

| Rol | Destino |
|---|---|
| administrador | `/admin/organizaciones` |
| soporte | `/admin/casos` |
| propietario, editor, lector | `/panel/apis` |
| consumidor | `/cuenta/suscripcion` o, si no tiene suscripción, `/planes` |

## 2. Resolución de la organización (multi-tenancy)

- **Personal:** el `organizacion_id` sale de la membresía del usuario de la sesión.
- **Portal:** la API de control identifica la API con el `Host` de la petición (`{sub}.shapi.localhost` → `api.subdominio`), y de ahí saca el `organizacion_id`. Para cada petición, la sesión del consumidor debe pertenecer a esa misma organización y a ese mismo host.
- `ShapiDbContext` aplica un **filtro global** (`HasQueryFilter`) por `organizacion_id` a todas las entidades que pertenecen a una organización. Solo los servicios de administración, del trabajador y de la compuerta lo desactivan, y lo hacen de forma explícita (`IgnoreQueryFilters`), con una revisión obligatoria en el código.
- Pedir un recurso de otra organización devuelve **404** ([04 §4](04-roles-y-permisos.md#4-reglas-adicionales)).

## 3. Secretos y datos sensibles

| Dato | Cómo se guarda | Se muestra |
|---|---|---|
| Contraseña | PBKDF2 (Identity v3) | Nunca |
| Clave de API | SHA-256 en hex + prefijo + últimos 4 | Completa **una sola vez**. Después, `shp_prod_••••7c2e` |
| Token de correo y sesión | SHA-256 | Nunca (solo viaja en el enlace o en la cookie) |
| Secreto de origen | Cifrado con **ASP.NET Data Protection**. El anillo de llaves persiste en el volumen `dpkeys`, compartido por la API y el trabajador. En Redis va en claro, porque Redis solo es accesible desde la red interna | Completo **una sola vez**, al generarlo o regenerarlo |
| Tarjeta | Solo el token de la pasarela, la marca, los últimos 4, el titular y el vencimiento | La marca y los últimos 4 |
| Credenciales de infraestructura | Variables de entorno en `.env`, que no se versiona. En el repositorio va un `.env.example` | — |

## 4. Protección del origen (SSRF)

Cuando se registra o se edita una API ([RF-08](03-requisitos.md#rf-08)), y **también en cada conexión** que abre la compuerta (`ConnectCallback`):

1. Se exige el esquema `http` o `https`, que no haya credenciales en la URL y que el puerto sea cualquiera entre 1 y 65535.
2. Se resuelve el host y se **rechaza** si **alguna** de las direcciones cae en: `127.0.0.0/8`, `::1`, `10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`, `169.254.0.0/16`, `fe80::/10`, `fc00::/7`, `100.64.0.0/10`, `0.0.0.0/8`, multidifusión o las redes de Docker.
3. **Excepción del modo demostración:** con `SHAPI_MODO_DEMO=true`, la lista `SHAPI_ORIGENES_PERMITIDOS`, con entradas `host:puerto` (por defecto `localhost:5101,localhost:5102,origen-envios:8080,origen-agro:8080`), permite los orígenes de demostración en desarrollo y en el ambiente productivo simulado.
4. En la compuerta, la conexión se abre **contra la dirección ya validada**, para evitar ataques de *DNS rebinding*.
5. No se siguen redirecciones del origen: la respuesta 3xx se devuelve tal cual al consumidor.

## 5. Encabezados y protección de los frontends

- La **política de seguridad de contenido** (CSP) del panel y del portal es `default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src https://fonts.gstatic.com; connect-src 'self' https://*.api.shapi.localhost; frame-ancestors 'none'`.
- Los logotipos SVG se sirven con `Content-Type: image/svg+xml`, `Content-Security-Policy: sandbox` y `X-Content-Type-Options: nosniff`. El portal los muestra **solo** con `<img>`, nunca incrustados en el HTML.
- Todos los textos que escribe el proveedor (nombre, bienvenida y descripciones de la especificación) se muestran escapados. Las descripciones en Markdown de la especificación se convierten a HTML con una lista de etiquetas permitidas.
- Caddy agrega `Strict-Transport-Security`, `X-Content-Type-Options: nosniff` y `Referrer-Policy: strict-origin-when-cross-origin`.

## 6. Correos (RF-46)

Se envían desde `no-responder@{dominio_base}`. Los correos de un portal usan como remitente visible el nombre de ese portal. Las plantillas están en `Shapi.Infraestructura/Correo/Plantillas`:

| Plantilla | Destinatario | Disparador |
|---|---|---|
| `verificacion_correo` | Proveedor o consumidor | Registro, o pedir de nuevo el enlace |
| `recuperacion` | Cualquiera | Solicitud de recuperación |
| `invitacion_miembro` | Persona invitada | CU-04 |
| `invitacion_consumidor` | Consumidor invitado | CU-23 |
| `definir_contrasena` | Cuenta de plataforma nueva | CU-21 |
| `pago_rechazado` | Propietario o consumidor | Un cobro de renovación rechazado |
| `suscripcion_en_gracia` | Propietario o consumidor | Entrada en gracia |
| `suscripcion_suspendida` | Propietario o consumidor | Suspensión |
| `organizacion_suspendida` | Propietario | Suspensión administrativa |
| `prueba_por_vencer` | Propietario | 7 días antes de que termine la Prueba |
| `aviso_cuota_plataforma` | Propietario | Al cruzar el 80 % y el 100 % de la cuota |
| `respuesta_caso` | La otra parte del caso | Mensaje nuevo en un caso |

**Reintentos:** el trabajador revisa `correo_saliente` cada 5 segundos. Si un envío falla, reintenta hasta 5 veces, con esperas de 5 s, 30 s, 2 min, 10 min y 1 h antes de cada reintento. Si falla el quinto reintento (el sexto intento), el correo queda `fallido`, con `intentos = 6` y `ultimo_error`.

Los correos de los consumidores llevan la marca del portal: el nombre, el color y el logotipo como enlace. Ningún correo lleva la marca de Shapi en el cuerpo.

## 7. Bitácora

La bitácora ([RF-41](03-requisitos.md#rf-41)) registra las acciones que cambian **el acceso, el cobro o el estado** de una organización. Cada entrada guarda el actor, la organización, la acción, el objetivo, una descripción que se puede leer (la que muestra B3.2) y la IP.

| Acción (`accion`) | Descripción de ejemplo |
|---|---|
| `organizacion.suspendida` / `organizacion.reactivada` | Suspendió la organización Datos Chapines, S.A. |
| `pago.revertido` | Revirtió el cobro de Q 199.00 de Datos Chapines, S.A. |
| `plan_plataforma.creado` / `.editado` / `.desactivado` | Editó el plan de plataforma Lanzamiento |
| `cuenta_plataforma.creada` / `.desactivada` / `.activada` | Creó la cuenta de plataforma de Lucía Ramírez Pineda con el rol de administrador |
| `suscripcion_plataforma.contratada` / `.cambiada` | Cambió su suscripción de plataforma de Lanzamiento a Producto |
| `miembro.invitado` / `.rol_cambiado` / `.quitado` | Invitó a karla.batres@enviosxelaju.com con el rol de lector |
| `api.registrada` / `.publicada` / `.despublicada` | Publicó la API de Cotización de Envíos |
| `ruta.expuesta` / `ruta.ocultada` | Ocultó la ruta GET /tarifas de la API de Cotización de Envíos |
| `dominio.conectado` / `.verificado` | Verificó el dominio api.enviosxelaju.localhost |
| `secreto_origen.regenerado` | Regeneró el secreto de origen de la API de Cotización de Envíos |
| `plan_api.creado` / `.editado` / `.desactivado` | Editó el plan Básico de la API de Cotización de Envíos |
| `clave.revocada_por_proveedor` | Revocó la clave de pruebas de Tienda Sololá en la API de Cotización de Envíos |
| `clave.rotada` / `clave.revocada_por_consumidor` | Boutique Cayalá rotó su clave de producción en la API de Cotización de Envíos |
| `caso.abierto` / `caso.cerrado` | Abrió el caso CAS-104 de Envíos Xelajú, S.A. |
| `suscripcion.suspendida` (actor: sistema) | Suspendió por falta de pago la suscripción de Transportes Petén, S.A. |

## 8. Amenazas consideradas

| Amenaza | Mitigación |
|---|---|
| Robo de la base de datos | Las claves, los tokens y las contraseñas solo están como hash; el secreto de origen está cifrado; no hay datos de tarjetas |
| Uso de una clave filtrada | El consumidor la rota o la revoca, y la revocación se aplica en menos de 10 s |
| Saltarse la compuerta llamando directo al origen | La URL de origen nunca aparece en el portal. El proveedor puede validar `X-Shapi-Secreto` o limitar las IP que acepta su origen. Es una limitación documentada |
| SSRF desde la URL de origen | [§4](#4-proteccion-del-origen-ssrf) |
| Fuerza bruta en el inicio de sesión | Bloqueo tras 5 intentos y limitación por IP |
| Enumeración de cuentas | Mensajes genéricos al iniciar sesión y al recuperar la contraseña, con el mismo tiempo de respuesta exista o no la cuenta. Se acepta que el registro (`409 correo_ya_registrado`, CU-01 2a) y el bloqueo (`423 cuenta_bloqueada`) revelen que una cuenta existe: la limitación por IP frena la enumeración masiva |
| XSS a través de la marca o de la especificación | Escape de textos, sanitización del Markdown, SVG solo como `<img>` y CSP |
| Acceso a datos de otra organización | Filtro global, 404 y pruebas automatizadas de aislamiento |
| Abuso de peticiones | Límites por minuto y cuotas en la compuerta, y limitación en `/api/auth/*` |

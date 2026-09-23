# 03 · Requisitos

Los códigos RF-01 a RF-42 **conservan su número** respecto al documento del 11 de septiembre, para que sigan coincidiendo con los mockups. RF-43 a RF-47 son nuevos. Al final hay una tabla con cada cambio.

Cada requisito indica el actor, el criterio de aceptación verificable y las pantallas correspondientes. Las pantallas nuevas están marcadas con ★.

## 1. Requisitos funcionales

### 1.1 Gestión de usuarios y acceso

| Código | Requisito | Actor | Criterio de aceptación | Pantallas |
|---|---|---|---|---|
| <a id="rf-01"></a>RF-01 | El sistema permitirá registrar un proveedor con nombre, correo, nombre de la organización y contraseña. Creará la organización, dejará al usuario como **propietario** y pondrá a la organización en el plan **Prueba** sin pedirle tarjeta. | Proveedor | La contraseña tiene al menos 10 caracteres. El correo es único entre el personal. La cuenta queda con el correo sin verificar y la suscripción Prueba queda activa por 30 días. | A1.1 |
| <a id="rf-02"></a>RF-02 | El sistema enviará un enlace de verificación al correo del proveedor o del consumidor. El enlace vence a las 24 horas y puede pedirse de nuevo. Mientras no se confirme el correo, **el proveedor no puede publicar APIs y el consumidor no puede contratar planes**. | Proveedor, Consumidor | Si el correo no está verificado, intentar publicar o contratar devuelve el error `correo_no_verificado`. Un enlace ya usado o vencido muestra un mensaje y la opción de reenviarlo. | A1.2, A5.8 ★ |
| <a id="rf-03"></a>RF-03 | El sistema permitirá recuperar la contraseña con un enlace de un solo uso que vence a los 60 minutos. **Al usarlo se cierran todas las demás sesiones** de esa cuenta. | Todos | La respuesta es idéntica exista o no el correo, para no revelar qué cuentas existen. El enlace solo sirve una vez. Las sesiones anteriores quedan revocadas. | A1.4a, A1.4b, A5.9 ★, A5.10 ★ |
| <a id="rf-04"></a>RF-04 | El sistema autenticará a los usuarios con correo y contraseña y mantendrá una sesión que vence tras un tiempo de inactividad configurable (8 h por defecto) o a los 7 días como máximo. El personal puede cerrar sesión, cambiar su nombre y cambiar su contraseña. | Todos | Tras 5 intentos fallidos, la cuenta se bloquea 15 minutos. La cookie es `HttpOnly`, `Secure` y `SameSite=Lax`. Al cerrar sesión, esa sesión queda revocada en el servidor. | A1.3, A5.7, A8.1 ★ |
| <a id="rf-05"></a>RF-05 | El sistema permitirá registrar consumidores desde el portal de una API o **por invitación que el proveedor envía desde su panel**. La invitación vence a los 7 días. | Consumidor, Proveedor | Una cuenta creada por invitación queda con el correo ya verificado. El correo del consumidor es único dentro de su organización. | A5.3, A5.3b, B1.5 ★ |
| <a id="rf-06"></a>RF-06 | El sistema manejará estos roles: `administrador` y `soporte` en la organización de plataforma; `propietario`, `editor` y `lector` en las organizaciones proveedoras; y **consumidor** como identidad aparte. El propietario puede invitar miembros, cambiar su rol y quitarlos. | Administrador, Propietario | Hay un solo propietario por organización. No se puede invitar un correo que ya pertenece a otra organización (error `correo_en_otra_organizacion`). Aceptar una invitación crea la cuenta y la membresía. | A4.2, A8.2 ★ |
| <a id="rf-07"></a>RF-07 | El sistema restringirá cada operación según el rol y la organización del usuario, conforme a la [matriz de permisos](04-roles-y-permisos.md). | Sistema | Toda operación que no esté permitida devuelve 403, y hay pruebas automatizadas por rol. | — |

### 1.2 Publicación y configuración de APIs

| Código | Requisito | Actor | Criterio de aceptación | Pantallas |
|---|---|---|---|---|
| <a id="rf-08"></a>RF-08 | El sistema permitirá registrar una API con nombre, URL del servidor de origen y **subdominio**. Antes de guardar, validará la URL y **probará la conexión** con el origen. | Proveedor (propietario, editor) | La URL es `http` o `https` y **no apunta a direcciones internas** ([RNF-10]). La prueba de conexión se aprueba si el origen devuelve cualquier respuesta HTTP en menos de 5 segundos. El subdominio cumple el formato, es único y no está reservado ([02](02-glosario.md)). Se respeta el límite de APIs del plan ([RF-43]). La API queda en estado `borrador`. | A3.2 |
| <a id="rf-09"></a>RF-09 | El sistema permitirá cargar una especificación OpenAPI 3.0 o 3.1, en JSON o YAML y de hasta 2 MB, y extraerá sus rutas. **Si se vuelve a cargar**, las rutas que ya existían conservan su configuración, las nuevas quedan **ocultas** y las que ya no aparecen se eliminan. | Proveedor (propietario, editor) | Un archivo que no es válido se rechaza, indicando la línea o la sección del error. Se muestran las rutas encontradas. | A3.3 |
| <a id="rf-10"></a>RF-10 | El sistema permitirá marcar cada ruta como expuesta u oculta. | Proveedor (propietario, editor) | Solo las rutas expuestas pasan por la compuerta y aparecen en el portal. | A3.4 |
| <a id="rf-11"></a>RF-11 | El sistema servirá cada API en dos hosts: `{sub}.api.{dominio_base}` para la API y `{sub}.{dominio_base}` para el portal. Los dos tendrán TLS **emitido y renovado automáticamente por el borde**. | Sistema | Al publicar la API, los dos hosts responden en menos de 10 segundos, sin ningún paso manual. | A3.6 |
| <a id="rf-12"></a>RF-12 | El sistema permitirá conectar un **dominio propio a la API** si el plan de plataforma lo incluye. La propiedad del dominio se verifica con un registro CNAME que apunta a `{sub}.api.{dominio_base}`. En el entorno simulado, el registro se crea en el **DNS simulado** y el dominio debe terminar en `.localhost`. | Proveedor (propietario, editor) | Solo se verifica el dominio si el CNAME existe y apunta al destino correcto. El certificado se emite con la primera petición. Si el plan no incluye dominio propio, se responde `plan_sin_dominio_propio`. | A3.6 |
| <a id="rf-13"></a>RF-13 | El sistema permitirá configurar en cada ruta el **límite por minuto** (en peticiones; si se deja vacío, la ruta no tiene límite propio), la **caché** (en segundos, solo para GET; 0 la desactiva) y el **peso en llamadas** (1 o más). | Proveedor (propietario, editor) | Los cambios llegan a la compuerta en menos de 10 segundos. | A3.5 |
| <a id="rf-14"></a>RF-14 | El sistema permitirá publicar y despublicar una API sin eliminar sus datos. | Proveedor (propietario, editor) | Para publicar se necesita el correo verificado, al menos una ruta expuesta y al menos un plan activo. Una API despublicada responde 404 `api_no_encontrada` y su portal muestra "API no disponible". | A3.1 |
| <a id="rf-15"></a>RF-15 | El sistema permitirá personalizar el portal **de cada API** con logotipo (PNG o SVG de hasta 512 KB), color principal, nombre del portal y texto de bienvenida. | Proveedor (propietario, editor) | La vista previa muestra los cambios antes de guardarlos. El portal no muestra la marca de Shapi. | A3.7 |
| <a id="rf-16"></a>RF-16 | El sistema generará el portal automáticamente: inicio, documentación por ruta (descripción, parámetros y ejemplos sacados de la especificación, más el peso en llamadas) y consola de pruebas. **En la consola, el consumidor pega su clave de pruebas**, que solo se recuerda mientras la pestaña esté abierta. | Sistema, Consumidor | La consola envía la petición real a `{sub}.api.{dominio_base}` y muestra el código y el cuerpo de la respuesta. | A5.0, A5.1, A5.2 |

### 1.3 Suscripciones y pagos

El único medio de pago es la tarjeta de crédito o débito, procesada por la **pasarela simulada** ([09](09-cobros-y-suscripciones.md#2-pasarela-simulada)). Shapi guarda el **token** que entrega la pasarela, la marca, los últimos 4 dígitos y el vencimiento, y **nunca** el número completo ni el CVV.

| Código | Requisito | Actor | Criterio de aceptación | Pantallas |
|---|---|---|---|---|
| <a id="rf-17"></a>RF-17 | El sistema permitirá al administrador crear y editar planes de plataforma con nombre, descripción, precio, vigencia en días, máximo de APIs, máximo de miembros, cuota de peticiones y si incluyen dominio propio. | Administrador | Un plan con suscripciones no se elimina: se desactiva. Los cambios de precio se aplican a partir de la siguiente renovación. | A6.1 |
| <a id="rf-18"></a>RF-18 | El sistema permitirá al proveedor crear y editar planes de API con nombre, descripción, precio, vigencia, cuota de llamadas por ciclo y límite de peticiones por minuto, y marcarlos como gratuitos. | Proveedor (propietario, editor) | Un plan gratuito tiene precio Q 0.00. Los cambios de cuota y de límite se aplican de inmediato a las suscripciones vigentes; los de precio, a partir de la siguiente renovación. Los planes con suscripciones se desactivan en vez de eliminarse. | A4.1 |
| <a id="rf-19"></a>RF-19 | El sistema mostrará los planes disponibles al proveedor y al consumidor antes de contratar. | Todos | Solo se muestran los planes activos. | A0, A2.1, A5.4 |
| <a id="rf-20"></a>RF-20 | El sistema permitirá contratar un plan con tarjeta y activará la suscripción **en el momento en que la pasarela autorice el cobro**. Los planes gratuitos se activan sin pedir tarjeta. Si el cobro se rechaza, no se crea la suscripción y la suscripción vigente no cambia. | Proveedor (propietario), Consumidor | El pago queda registrado como `autorizado` o `rechazado`. Al activarse una suscripción de API se emiten sus claves ([RF-26]). | A2.2, A2.3, A2.4, A5.6, A5.4b |
| <a id="rf-21"></a>RF-21 | El sistema registrará el historial de pagos de cada organización y de cada consumidor. | Proveedor (propietario, lector), Consumidor | Cada pago muestra fecha, concepto, periodo, monto, medio de pago y estado. | B1.3, B2.2 |
| <a id="rf-22"></a>RF-22 | El sistema renovará automáticamente las suscripciones al cerrar cada ciclo, cobrando al medio de pago registrado. | Sistema | La renovación ocurre en el primer minuto después de `fin`. Si se autoriza, empieza un ciclo nuevo y la cuota se reinicia. Si se rechaza, se aplica [RF-23]. | B1.4, B2.3 |
| <a id="rf-23"></a>RF-23 | El sistema aplicará un **periodo de gracia de 7 días** cuando se rechace el cobro de una renovación y **suspenderá** la suscripción si no se paga en ese plazo. Mientras dure la gracia, el servicio sigue funcionando. Si se suspende la plataforma de una organización, **todas sus APIs responden 403**; si se suspende la suscripción de un consumidor, **sus claves responden 403**. Un cobro autorizado reactiva la suscripción con un ciclo nuevo desde ese día. | Sistema, Proveedor, Consumidor | Las pantallas muestran los días que quedan de gracia. La reactivación llega a la compuerta en menos de 10 segundos. | B1.4, B2.3, A6.2 |
| <a id="rf-24"></a>RF-24 | El sistema permitirá al administrador **revisar los pagos de plataforma y revertir** un cobro autorizado. | Administrador | Un pago revertido queda en estado `revertido`. Si ese pago cubría el ciclo vigente, la suscripción entra en gracia. La acción queda en la bitácora. | A6.3 |
| <a id="rf-25"></a>RF-25 | El sistema permitirá cambiar de plan. **Subir** de plan se cobra prorrateado y se aplica de inmediato. **Bajar** de plan se aplica en la siguiente renovación, sin reembolso. **No se puede bajar** a un plan si la organización excede sus límites (por ejemplo, si tiene más APIs publicadas de las que el plan permite). | Proveedor (propietario), Consumidor | El monto del prorrateo sigue la fórmula de [09 §5](09-cobros-y-suscripciones.md#5-cambio-de-plan). Un cambio a un plan más barato queda programado en `plan_siguiente_id` y la pantalla lo indica. | A2.5, B2.7 ★ |

### 1.4 Compuerta de tráfico

| Código | Requisito | Actor | Criterio de aceptación | Pantallas |
|---|---|---|---|---|
| <a id="rf-26"></a>RF-26 | El sistema emitirá una clave de producción y una de pruebas al activarse una suscripción de API y **las mostrará completas una sola vez**. | Sistema | En la base de datos solo se guardan el prefijo, los últimos 4 caracteres y el hash SHA-256. Después de mostrarlas, solo se ven enmascaradas (`shp_prod_••••7c2e`). | A5.4b, B2.3 |
| <a id="rf-27"></a>RF-27 | El sistema permitirá **al consumidor** rotar una de sus claves. La clave nueva se muestra una sola vez y la anterior sigue funcionando 24 horas. | Consumidor | Una clave ya rotada no se puede volver a rotar. Durante esas 24 horas coexisten, como máximo, dos claves del mismo tipo. | B2.3, B2.4 ★, B2.5 ★ |
| <a id="rf-28"></a>RF-28 | El sistema permitirá revocar una clave, al **consumidor** si es suya y al **proveedor** si es de uno de sus consumidores. La revocación se hace efectiva en **menos de 10 segundos**. Después de revocar una clave, el consumidor puede emitir una nueva del mismo tipo. | Consumidor, Proveedor (propietario, editor) | Una petición con una clave revocada recibe 401 `clave_invalida`. Las revocaciones que hace el proveedor quedan en la bitácora. | A4.3, A4.3b, B2.6 ★ |
| <a id="rf-29"></a>RF-29 | El sistema validará en cada petición que la API esté publicada, la clave, el estado de la organización, el estado de la suscripción y que la ruta esté expuesta. Rechazará con **404, 401 o 403** según corresponda. | Sistema | Los códigos y el formato del error son los de [08 §4](08-compuerta.md#4-contrato-de-errores). | — |
| <a id="rf-30"></a>RF-30 | El sistema rechazará con **429** las peticiones que superen el límite por minuto (del plan o de la ruta), las que agoten la cuota del consumidor y las que agoten la cuota de plataforma del proveedor. | Sistema | Cada caso tiene su propio código de error y la cabecera `Retry-After`. La cuota nunca se excede, gracias a la reserva atómica ([08 §3](08-compuerta.md#3-tuberia-de-filtros)). | — |
| <a id="rf-31"></a>RF-31 | El sistema reenviará al origen solo las peticiones que pasen todos los filtros. Antes de reenviarlas quitará `X-Api-Key` y agregará `X-Shapi-Consumidor`, `X-Shapi-Entorno` y, si existe, `X-Shapi-Secreto`. | Sistema | El origen nunca recibe la clave del consumidor. | — |
| <a id="rf-32"></a>RF-32 | El sistema devolverá al consumidor cabeceras con el plan vigente, el límite por minuto y la cuota restante. | Sistema | Las cabeceras son las de [08 §5](08-compuerta.md#5-cabeceras). | — |
| <a id="rf-33"></a>RF-33 | El sistema registrará de cada petición la API, la ruta, la suscripción, el entorno, el código de respuesta, la latencia total, la latencia de la compuerta y los bytes de entrada y de salida. | Sistema | Esto se hace **después** de la respuesta y sin tocar PostgreSQL ([RNF-02]). | — |
| <a id="rf-34"></a>RF-34 | El sistema consolidará cada 10 segundos las métricas acumuladas en Redis en la tabla `consumo_diario`, sin perder datos y sin contarlos dos veces. | Sistema (trabajador) | Si se interrumpe la consolidación en cualquier punto, al reiniciarse no pierde ni duplica datos ([08 §7](08-compuerta.md#7-medicion-y-consolidacion)). | — |

### 1.5 Consulta, administración y soporte

| Código | Requisito | Actor | Criterio de aceptación | Pantallas |
|---|---|---|---|---|
| <a id="rf-35"></a>RF-35 | El sistema mostrará al proveedor, por cada API y periodo: peticiones, llamadas descontadas, **latencia total p95 y latencia de la compuerta p95** (sacadas de histogramas), errores por código (401/403/429 de la compuerta y 4xx/5xx/fallas de conexión del origen), y **bytes transferidos**. | Proveedor | Las cifras salen de `consumo_diario`. El p95 se calcula por interpolación dentro de cada rango del histograma ([08 §7](08-compuerta.md#7-medicion-y-consolidacion)). | B1.1 |
| <a id="rf-36"></a>RF-36 | El sistema mostrará al proveedor el consumo del ciclo y la facturación de cada consumidor. | Proveedor | El consumo es igual a la suma de llamadas del ciclo vigente de cada suscripción. | B1.2 |
| <a id="rf-37"></a>RF-37 | El sistema mostrará al consumidor su consumo del ciclo, desglosado por ruta: peticiones, peso y llamadas descontadas. | Consumidor | Los totales coinciden con la cuota restante que informan las cabeceras, con un desfase máximo de 10 segundos. | B2.1 |
| <a id="rf-38"></a>RF-38 | El sistema permitirá al administrador listar organizaciones y suspenderlas o reactivarlas por motivos administrativos. | Administrador | Una suspensión administrativa hace que la compuerta responda 403 en menos de 10 segundos. Queda en la bitácora. | A6.2, A6.2b |
| <a id="rf-39"></a>RF-39 | El sistema mostrará al administrador y al soporte el estado de cada componente y la latencia p95 que añade la compuerta. | Administrador, Soporte | Los estados son `en_servicio`, `degradado` o `fuera_de_servicio`, y se calculan con verificaciones de salud cada 30 segundos ([06 §8](06-arquitectura.md#8-observabilidad-y-estado-de-componentes)). | B3.1 |
| <a id="rf-40"></a>RF-40 | El sistema permitirá **al proveedor abrir casos de soporte y conversar en ellos**, y **al soporte y al administrador** registrar casos a nombre de una organización, responder, asignarse casos y cerrarlos, con acceso de **solo lectura** a los datos de la organización. | Proveedor, Soporte, Administrador | Cada respuesta notifica por correo a la otra parte. Un caso cerrado no admite más mensajes. | A7.1 ★, A7.2 ★, A6.4 |
| <a id="rf-41"></a>RF-41 | El sistema registrará en una bitácora las acciones sensibles, indicando actor, organización, acción, objetivo, fecha e IP. | Sistema | La lista de acciones está en [10 §7](10-identidad-y-seguridad.md#7-bitacora). La bitácora no admite UPDATE ni DELETE. | B3.2 |
| <a id="rf-42"></a>RF-42 | El sistema permitirá al administrador crear, desactivar y reactivar cuentas de administración y de soporte. Estas cuentas no tienen registro público. | Administrador | La cuenta nueva recibe un enlace para definir su contraseña, que vence a los 7 días. Un administrador no puede desactivar su propia cuenta. | A6.5 |
| <a id="rf-43"></a>RF-43 | El sistema aplicará los **límites del plan de plataforma**: máximo de APIs registradas, máximo de miembros (contando las invitaciones pendientes), dominio propio y cuota de peticiones. Avisará en el panel cuando se llegue al 80 % y al 100 % de la cuota. | Sistema | Pasar un límite devuelve `limite_del_plan` con el nombre del límite. La compuerta aplica la cuota de peticiones ([RF-30]). | A3.1, A4.2 |
| <a id="rf-44"></a>RF-44 | El plan **Prueba** vence a los 30 días y no se renueva. Si en ese plazo la organización no contrata un plan de pago, su suscripción pasa a gracia y después a suspensión, como en [RF-23]. | Sistema | El panel avisa 7 días antes del vencimiento. | B1.4 |
| <a id="rf-45"></a>RF-45 | La **clave de pruebas** llega al mismo origen con `X-Shapi-Entorno: pruebas`, **no descuenta cuota ni se factura**, y tiene un límite fijo de 10 peticiones por minuto y 1,000 por día. | Sistema | Las métricas se guardan con `entorno = pruebas`. | A5.2 |
| <a id="rf-46"></a>RF-46 | El sistema enviará correos de verificación, recuperación, invitación (a miembros, a consumidores y a cuentas de plataforma), pago rechazado, entrada en gracia, suspensión, aviso de vencimiento de la Prueba y respuesta a un caso. | Sistema (trabajador) | Los correos pasan por la tabla `correo_saliente` y se reintentan hasta 5 veces. En el entorno simulado llegan a Mailpit. | — |
| <a id="rf-47"></a>RF-47 | El sistema generará para cada API un **secreto de origen**, que se muestra una sola vez y puede regenerarse, y lo enviará en `X-Shapi-Secreto` en cada petición reenviada. | Proveedor (propietario, editor) | Se guarda cifrado con ASP.NET Data Protection. Al regenerarlo, el anterior deja de enviarse en menos de 10 segundos. | A3.6 |

## 2. Requisitos no funcionales

Están ordenados según ISO/IEC 25010 y cada uno tiene un criterio que se puede medir.

| Código | Característica | Requisito y criterio de verificación |
|---|---|---|
| <a id="rnf-01"></a>RNF-01 | Eficiencia | La compuerta no añadirá más de **15 ms en el percentil 95** al tiempo de respuesta del origen. Se mide con una prueba de carga contra un origen de demostración. |
| <a id="rnf-02"></a>RNF-02 | Eficiencia | Validar una petición **no consultará PostgreSQL**; todo se resuelve contra Redis. Se verifica con una prueba que apaga PostgreSQL. |
| <a id="rnf-03"></a>RNF-03 | Eficiencia | El sistema soportará al menos **200 peticiones por segundo** sostenidas durante 5 minutos, en el equipo de despliegue, con menos del 1 % de errores de la compuerta. |
| <a id="rnf-04"></a>RNF-04 | Fiabilidad | La compuerta seguirá enrutando aunque la API de control, el trabajador o PostgreSQL estén fuera de servicio, siempre que Redis esté disponible. |
| <a id="rnf-05"></a>RNF-05 | Fiabilidad | Una caída del trabajador no hará perder datos de consumo ni de facturación. Redis se configura con persistencia AOF (`appendfsync everysec`) y la consolidación es idempotente. Si Redis pierde sus datos, la caché se reconstruye desde PostgreSQL. |
| <a id="rnf-06"></a>RNF-06 | Fiabilidad | La disponibilidad mensual objetivo del enrutamiento es del **99.5 %** en el ambiente de despliegue. |
| <a id="rnf-07"></a>RNF-07 | Seguridad | Las contraseñas se guardan con **PBKDF2** (el hasher de ASP.NET Core Identity, v3). Las claves de API se guardan con **hash SHA-256** y se muestran una sola vez. Los tokens de correo y los identificadores de sesión se guardan con hash. El secreto de origen se guarda **cifrado** con Data Protection. Nunca se guardan datos completos de tarjetas. |
| <a id="rnf-08"></a>RNF-08 | Seguridad | Ninguna consulta devolverá datos de una organización distinta a la del solicitante. Se aplica con un filtro global de EF Core por organización y se verifica con pruebas automatizadas de aislamiento. |
| <a id="rnf-09"></a>RNF-09 | Seguridad | Todo el tráfico se servirá por **HTTPS**, con certificados que el borde emite y renueva automáticamente antes de su vencimiento (en el entorno simulado, con la autoridad certificadora interna de Caddy). HTTP redirige a HTTPS. |
| <a id="rnf-10"></a>RNF-10 | Seguridad | La plataforma **no ejecutará código de los clientes**; solo enruta tráfico. Las URL de origen no pueden resolver a direcciones de loopback, privadas, link-local ni de la red interna de Docker. Esto se valida **al guardar la URL y al abrir cada conexión**, como protección contra SSRF. |
| <a id="rnf-11"></a>RNF-11 | Usabilidad | Publicar una API, desde el registro hasta la primera respuesta válida, tomará **menos de 5 minutos**. |
| <a id="rnf-12"></a>RNF-12 | Usabilidad | Las interfaces estarán en español y funcionarán en la versión actual de Chrome, Edge y Firefox de escritorio, desde 1280 px de ancho. |
| <a id="rnf-13"></a>RNF-13 | Mantenibilidad | Agregar una regla de validación a la compuerta consistirá en **agregar un filtro** a la tubería sin modificar los demás. La cuota de plataforma ([RF-30]) sirve de ejemplo. |
| <a id="rnf-14"></a>RNF-14 | Portabilidad | Todo el sistema se levantará en cualquier equipo con **un solo comando** (`docker compose up`), con los datos de siembra de los mockups. |
| <a id="rnf-15"></a>RNF-15 | Mantenibilidad | Las reglas de negocio del dominio, cada filtro de la compuerta y el aislamiento entre organizaciones tendrán pruebas automatizadas que se ejecutan en la integración continua. No se integra a `main` nada que tenga pruebas fallidas. |

## 3. Trazabilidad con los lineamientos

| Lineamiento | Requisitos |
|---|---|
| Registro, validación de correo, recuperación de contraseña | RF-01, RF-02, RF-03, RF-05 |
| Autenticación segura y manejo de sesiones | RF-04, RNF-07 |
| Roles (Administrador y Cliente como mínimo; Soporte es opcional) | RF-06, RF-07, RF-42 |
| Servicios con nombre, descripción, precio y vigencia | RF-17, RF-18 |
| Visualización de planes, contratación, historial de pagos y renovación | RF-19, RF-20, RF-21, RF-22, RF-23, RF-25 |
| Crear entornos web | RF-08 a RF-11, RF-14 |
| Gestionar configuraciones | RF-12, RF-13, RF-15, RF-47 |
| Monitorear consumo de recursos | RF-35, RF-36, RF-37, RF-39 |
| Despliegue en ambientes productivos | RNF-09, RNF-14, [06 §7](06-arquitectura.md#7-despliegue) |

## 4. Cambios respecto al documento del 11 de septiembre

| Requisito | Cambio | Decisión |
|---|---|---|
| RF-02, RF-03 | Se amplían a los consumidores; la recuperación cierra las demás sesiones | ADR-04, ADR-06 |
| RF-04 | Se agregan bloqueo tras intentos fallidos, cierre de sesión y perfil | ADR-06 |
| RF-05 | Se agrega que el proveedor envía la invitación desde su panel | D29 |
| RF-06 | Un solo propietario por organización y una sola organización por usuario | ADR-05 |
| RF-08 | El subdominio lo elige el proveedor; se agregan la protección contra SSRF y la prueba de conexión | ADR-07, ADR-11 |
| RF-11 | Hosts separados para el portal y la API; certificados emitidos por el borde | ADR-08, ADR-09 |
| RF-12 | El dominio propio es solo para la API; se verifica con DNS simulado | ADR-10 |
| RF-13 | El límite por minuto se cuenta en peticiones; el "peso en llamadas" reemplaza a la "unidad de medición" | ADR-18 |
| RF-15 | La personalización es por API | ADR-07 |
| RF-24 | Se elimina "rechazar un pago": los rechazos los decide la pasarela | ADR-12 |
| RF-25 | Se definen las reglas para subir y bajar de plan | ADR-17 |
| RF-26, RF-27, RF-28 | Las claves se guardan con hash y se muestran una vez; rota el consumidor; revocan el consumidor y el proveedor; soporte no toca claves | ADR-01, ADR-02 |
| RF-29, RF-30 | Se agregan el 404 por API despublicada y el 429 por cuota de plataforma | ADR-20 |
| RF-35 | Se agregan histogramas, bytes y errores por código | ADR-23 |
| RF-40 | El proveedor abre y conversa en sus casos | D29 |
| RF-43 a RF-47 | Requisitos nuevos | ADR-16, ADR-19, ADR-24, ADR-27, ADR-11 |
| RNF-04, RNF-05, RNF-07, RNF-09, RNF-10 | Se precisan | ADR-01, ADR-09, ADR-11, ADR-22, ADR-27 |
| RNF-15 | Requisito nuevo | ADR-31 |

[RNF-02]: #rnf-02
[RNF-10]: #rnf-10
[RF-23]: #rf-23
[RF-26]: #rf-26
[RF-30]: #rf-30
[RF-43]: #rf-43

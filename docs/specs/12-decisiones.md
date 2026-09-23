# 12 · Registro de decisiones de arquitectura (ADR)

Estas decisiones salieron de la auditoría del 22 de septiembre de 2026, que encontró contradicciones entre la propuesta, el documento de requisitos y diseño y los mockups. El coordinador las aprobó el mismo día. El número de cada ADR coincide con el de la decisión (D1 a D34) de esa revisión.

Formato: **Contexto** → **Decisión** → **Alternativas descartadas** → **Consecuencias**.

---

### ADR-01 · Las claves de API se guardan con hash y se muestran una sola vez
- **Contexto:** el RNF-07 decía "cifradas", el MER decía "hash", una secuencia decía "se muestra una vez" y los mockups las mostraban siempre.
- **Decisión:** se guarda el **SHA-256** de la clave, su prefijo y sus últimos 4 caracteres. La clave completa se muestra solo al emitirla o rotarla. En la consola, el consumidor pega su clave de pruebas.
- **Alternativas descartadas:** el cifrado reversible, porque obliga a guardar además un hash para buscar la clave y a administrar llaves de cifrado, y es menos seguro. Tampoco se usa bcrypt o PBKDF2, porque la clave ya tiene unos 154 bits aleatorios y un hash lento castigaría la latencia de la compuerta.
- **Consecuencias:** si el consumidor pierde una clave, la **rota**. Cambian A5.4b, B2.3 y A5.2. Es la práctica de Stripe, GitHub y AWS.

### ADR-02 · El consumidor rota; el consumidor y el proveedor revocan; el soporte no toca claves
- **Contexto:** el RF-27 decía que rotaba el consumidor, los mockups decían que el proveedor y el RF-28 incluía a soporte.
- **Decisión:** solo el consumidor rota (la clave anterior vive 24 h más). El consumidor revoca sus claves y el proveedor revoca las de sus consumidores. El soporte es de solo lectura.
- **Alternativas descartadas:** que rote el proveedor. Con ADR-01 nadie vería la clave nueva, y el dueño de la credencial es el consumidor.
- **Consecuencias:** A4.3 pierde "Rotar". B2.3 gana "Rotar" y "Revocar". Nuevas pantallas B2.4, B2.5 y B2.6. Bloquear a un consumidor completo queda fuera del alcance.

### ADR-03 · El consumidor es una identidad separada, aislada por organización
- **Contexto:** el MER relacionaba al consumidor con un `usuario` global, pero todos los textos decían que "su cuenta pertenece al portal".
- **Decisión:** tabla `consumidor` con credenciales propias y `UNIQUE(organizacion_id, correo)`. Hay dos ámbitos de sesión: `personal` y `consumidor`.
- **Alternativas descartadas:** un usuario global compartido. Si se filtrara un portal afectaría a los demás, el mismo correo tendría la misma contraseña en todos los portales, y contradice el modelo de marca blanca.
- **Consecuencias:** hay dos flujos de autenticación que comparten el mismo servicio, con un parámetro para el ámbito. La cuenta sirve en todos los portales de esa organización.

### ADR-04 · El consumidor también verifica su correo y recupera su contraseña
- **Contexto:** los lineamientos lo exigen, el RF-03 decía "Todos" y no había pantallas.
- **Decisión:** el consumidor también verifica su correo (y no puede contratar hasta hacerlo) y recupera su contraseña. Pantallas nuevas A5.8, A5.9 y A5.10.
- **Consecuencias:** se evitan cuentas falsas y se cumple el punto 4.1 de los lineamientos para todos los usuarios.

### ADR-05 · Matriz de permisos explícita y una sola organización por usuario
- **Decisión:** la matriz de [04](04-roles-y-permisos.md), con denegación por defecto. Un solo propietario por organización. Un usuario pertenece a **una sola** organización.
- **Alternativas descartadas:** usuarios en varias organizaciones, porque exigiría un selector de organización y más pruebas de aislamiento, sin que ningún requisito lo pida.
- **Consecuencias:** no se puede invitar a alguien que ya pertenece a otra organización (`correo_en_otra_organizacion`).

### ADR-06 · Sesiones del lado del servidor con cookie, PBKDF2 y sin verificación en dos pasos
- **Decisión:** la sesión se guarda en la tabla `sesion` y la cookie es `HttpOnly` con `SameSite=Lax`. El hash de las contraseñas es el PBKDF2 de Identity. Se agregan bloqueo tras intentos fallidos y cierre de las demás sesiones al recuperar la contraseña. **Se quita la verificación en dos pasos** que mencionaba la propuesta.
- **Alternativas descartadas:** JWT, porque no permite revocar sesiones al recuperar la contraseña sin montar una lista de bloqueo, y en el navegador suma riesgo de XSS.
- **Consecuencias:** como el frontend y la API comparten host, no hace falta CORS entre ellos.

### ADR-07 · El subdominio, el portal y la marca son de cada API
- **Contexto:** la propuesta y A3 los ponían por API, pero A6.2 y el MER los tenían por organización.
- **Decisión:** cada API tiene su subdominio (elegido por el proveedor), su dominio propio, su portal y su marca.
- **Alternativas descartadas:** un portal por organización con varias APIs, porque necesita un selector de API en el portal y rediseñar A5 y B2.
- **Consecuencias:** cada API es un "entorno web", lo que **refuerza el argumento de la categoría** del enunciado. Salen de `organizacion` los campos `subdominio`, `color_marca` y `logo_url`. En A3.2 se agrega el campo Subdominio. En A6.2 se muestra el propietario.

### ADR-08 · Hosts distintos para el portal y la API
- **Contexto:** `envios.shapi.com` servía el portal y también `envios.shapi.com/cotizaciones`, así que las rutas chocaban.
- **Decisión:** `{sub}.shapi.localhost` para el portal, `{sub}.api.shapi.localhost` para la API y `shapi.localhost` para el sitio, el panel y la administración.
- **Alternativas descartadas:** un prefijo de ruta (`/api/…`), porque cambia las URL del proveedor. Un segundo dominio, porque cuesta dinero.
- **Consecuencias:** el borde enruta solo por nombre de host. Hay que manejar CORS en la compuerta para la consola de pruebas ([08 §6](08-compuerta.md#6-cors)).

### ADR-09 · Caddy en el borde y dominios simulados bajo `*.localhost` (plan B, costo cero)
- **Contexto:** Let's Encrypt con un certificado por subdominio tiene límites de emisión, y Nginx necesita un cliente ACME y scripts. **El equipo decidió no gastar dinero**: no hay dominio ni servidor público.
- **Decisión:** **Caddy**, con su autoridad certificadora interna (`local_certs`). Emite al instante los comodines `*.shapi.localhost` y `*.api.shapi.localhost`, y los certificados *on-demand* de los dominios propios verificados (pregunta a `/interno/tls/autorizar`). Los navegadores resuelven `*.localhost` a 127.0.0.1 sin configuración (RFC 6761).
- **Alternativas descartadas:**
  - Nginx con Certbot, que requiere un dominio real y es más complejo.
  - `nip.io` o `sslip.io`, que dependen de un servicio externo y producen nombres difíciles de leer.
  - Editar el archivo `hosts`, que no admite comodines.
- **Consecuencias:** cada equipo confía una vez en la raíz de Caddy. El dominio base se configura con `SHAPI_DOMINIO_BASE`, así que el código no cambia si algún día hay un dominio real. Encaja con el requisito de "gestión de dominios simulados" del enunciado.

### ADR-10 · El dominio propio es solo para la API y se verifica con DNS simulado
- **Decisión:** el proveedor conecta su dominio a `{sub}.api.shapi.localhost` con un CNAME. `IResolutorDns` tiene dos implementaciones: `simulado` (tabla `registro_dns_simulado`, con el botón "Simular la creación del registro" en A3.6) y `real` (DnsClient.NET). En el entorno simulado, los dominios deben terminar en `.localhost`.
- **Por qué solo la API:** la URL de la API es la que el consumidor escribe en su código. Si es del proveedor, puede irse de Shapi sin romperle nada a sus clientes. El portal ya es de marca blanca en su subdominio.
- **Consecuencias:** requiere un plan con dominio propio (desde Producto). El certificado se emite con la primera petición.

### ADR-11 · Protección contra SSRF y secreto de origen
- **Decisión:** la URL de origen no puede resolver a direcciones internas. Se valida al guardar **y** en cada conexión (`ConnectCallback`), para cubrir el *DNS rebinding*. Cada API tiene un **secreto de origen**, guardado cifrado, que se muestra una vez y viaja en `X-Shapi-Secreto`. En el modo demostración se permiten los orígenes de demostración, que están en la red de Docker.
- **Consecuencias:** sin esto, un proveedor podría usar la compuerta para llegar a Redis o a PostgreSQL. El proveedor puede validar el secreto (es opcional), así que "sin modificar el servidor" sigue siendo cierto.

### ADR-12 · Pasarela de pagos simulada detrás de una interfaz
- **Decisión:** `IPasarelaPagos` tiene una implementación simulada, con validación de Luhn, detección de la marca, tarjetas de prueba (incluida una que falla solo en las renovaciones) y autorización inmediata. Se quitan "Confirmar pagos" y la rama "pendiente de confirmación". El RF-24 queda como "revisar y revertir".
- **Consecuencias:** la propuesta de valor ("conectar una pasarela local") queda como un punto de extensión real de la arquitectura.

### ADR-13 · Se guarda el token de la tarjeta, no la tarjeta
- **Contexto:** los textos decían que "no se almacena la tarjeta", pero la renovación se cobraba "a la misma tarjeta".
- **Decisión:** se guardan el token de la pasarela, la marca, los últimos 4 dígitos, el titular y el vencimiento. Nunca el número completo ni el CVV.
- **Consecuencias:** las renovaciones automáticas funcionan como en una pasarela real (PCI). Cambian los textos de A2.2 y A5.6.

### ADR-14 · Historia de negocio y cobro por cuenta del proveedor
- **Decisión:** Shapi cobra a los consumidores por cuenta del proveedor y sin comisión. **Liquidarle ese dinero al proveedor queda fuera del alcance.** Se corrige la justificación de la propuesta, incluida la frase sin terminar.

### ADR-15 · Una sola máquina de estados para las suscripciones, con 7 días de gracia
- **Decisión:** `activa → en_gracia (7 días) → suspendida → activa` (al pagar, con un ciclo nuevo desde ese día), más `finalizada`. Es la misma para la plataforma y para las APIs. La compuerta solo mira el estado, nunca las fechas.
- **Consecuencias:** hay un solo conjunto de reglas y de pruebas. Los 7 días que ya mostraban los mockups pasan a ser un requisito.

### ADR-16 · El plan Prueba vence
- **Decisión:** la Prueba dura 30 días y **no se renueva**. Al terminar entra en gracia y después se suspende. Se avisa 7 días antes.

### ADR-17 · Reglas para cambiar de plan
- **Decisión:** subir de plan se cobra prorrateado y se aplica de inmediato. Bajar se aplica en la siguiente renovación, sin reembolso, y se bloquea si la organización excede los límites del plan de destino. La comparación se hace por **precio diario**.
- **Consecuencias:** no hay saldos a favor ni cobros negativos. Nueva pantalla B2.7 para el consumidor.

### ADR-18 · Petición y llamada son unidades distintas
- **Decisión:**
  - *Petición* = una solicitud HTTP.
  - *Llamada* = la unidad de la cuota del consumidor, según el peso de cada ruta.
  - Los límites por minuto y la **cuota de plataforma** se cuentan en peticiones.
  - La **cuota del consumidor**, en llamadas.
- **Por qué:** si la cuota de plataforma se contara en llamadas, el proveedor podría evitarla poniéndoles peso 0 a sus rutas.
- **Consecuencias:** se corrigen los textos de A0, A2, A6, A3, A4, A5 y B1.

### ADR-19 · Datos concretos de los planes y una suscripción por API
- **Decisión:**
  - Miembros por plan: Prueba 1, Lanzamiento 3, Producto 10 y Escala sin límite.
  - "Escala: 30 días o anual" se divide en **Escala mensual** (Q 1,500 y 10,000,000 peticiones cada 30 días) y **Escala anual** (Q 15,000 y 120,000,000 peticiones cada 365 días).
  - Cada consumidor tiene **una sola suscripción vigente por API**; si quiere otra, cambia de plan.

### ADR-20 · Orden definitivo de la tubería de filtros
- **Decisión:** CORS → API → clave → organización → suscripción → ruta → límites y cuotas (incluida la **cuota de plataforma**, que es un filtro nuevo) → caché → reenvío → medición, esta última **después** de responder. El detalle está en [08 §3](08-compuerta.md#3-tuberia-de-filtros).
- **Consecuencias:** el diagrama de arquitectura y la secuencia coinciden. La cuota de plataforma sirve de ejemplo del RNF-13.

### ADR-21 · Reserva atómica de la cuota con Lua en Redis
- **Decisión:** un script Lua hace `INCR` y `INCRBY`, y revierte los contadores si se pasa algún límite. Si el origen no se pudo conectar (502), la cuota se devuelve.
- **Alternativas descartadas:** contar en memoria y consolidar después. Así lo planteaba la propuesta, con el costo de que un consumidor podía pasarse de su cuota.
- **Consecuencias:** **la cuota nunca se excede**, y se elimina el párrafo del "margen mínimo".

### ADR-22 · La compuerta sin datos en memoria, Redis persistente y resincronización
- **Decisión:** la compuerta consulta Redis en cada petición. Solo guarda en memoria, 5 segundos como máximo, las rutas de cada API, junto con su versión. Redis se configura con AOF `everysec`. El trabajador reconstruye las llaves de configuración desde PostgreSQL al arrancar y cada 5 minutos.
- **Consecuencias:** las revocaciones y las suspensiones se aplican al instante. Se cumple RNF-05 y existe un camino para recuperarse si Redis pierde sus datos.

### ADR-23 · Histogramas de latencia, errores por código y bytes
- **Contexto:** con solo el promedio no se puede calcular el p95 que muestra B1.1.
- **Decisión:** `consumo_diario` guarda histogramas de 10 rangos (para la latencia total y la de la compuerta), contadores por código y bytes de entrada y de salida. La consolidación es por lotes y es idempotente (`lote_consolidado`).
- **Consecuencias:** el p95 se puede sumar entre días y rutas. Los bytes refuerzan el requisito de "monitorear consumo de recursos".

### ADR-24 · Qué hace la clave de pruebas
- **Decisión:** llega al mismo origen con `X-Shapi-Entorno: pruebas`, no descuenta cuota ni se factura, y tiene un límite de 10 peticiones por minuto y 1,000 por día.

### ADR-25 · Cabeceras de la compuerta
- **Decisión:** la clave va en `X-Api-Key` (nunca en la query string) y el origen no la recibe. Al origen se le envían `X-Shapi-Consumidor`, `X-Shapi-Entorno` y `X-Shapi-Secreto`. Al consumidor se le devuelven `X-Shapi-Plan`, `X-RateLimit-*` y `X-Cuota-*` ([08 §5](08-compuerta.md#5-cabeceras)).

### ADR-26 · OpenAPI y caché de respuestas
- **Decisión:**
  - Se aceptan OpenAPI 3.0 y 3.1, en JSON o YAML y de hasta 2 MB.
  - Si se vuelve a cargar la especificación, las rutas que ya existían conservan su configuración y las nuevas quedan **ocultas**.
  - La caché es opcional por ruta (solo GET), compartida entre consumidores, y un acierto sí descuenta cuota.
  - Es la funcionalidad de menor prioridad.

### ADR-27 · Procesos y estilo arquitectónico
- **Decisión:**
  - **Procesos:** borde (Caddy), compuerta (YARP), API de control (tres capas), **trabajador aparte** (consolidación, cierre de ciclos, correo, dominios y resincronización), dos frontends React (panel y portal), Redis, PostgreSQL y Mailpit.
  - **Correo:** pasa por una bandeja de salida en la base de datos (`correo_saliente`).
- **Alternativas descartadas:**
  - **Un monolito**, porque un despliegue o una caída del panel cortaría el tráfico (incumple RNF-04).
  - **Microservicios por dominio**, porque su operación cuesta demasiado para 4 personas y 6 semanas.
  - **Kong o Tyk como compuerta**, porque el curso pide desarrollar la solución y la compuerta es el núcleo del producto.
- **Consecuencias:** si se cae la API de control, la medición sigue. El correo tiene su propio indicador de salud (B3.1).

### ADR-28 · Modelo de datos completo
- **Decisión:** el MER de [07](07-modelo-de-datos.md). Se agregan `consumidor` (con credenciales), `token`, `sesion`, `dominio_propio`, `registro_dns_simulado`, `medio_pago`, `caso_mensaje`, `bitacora`, `correo_saliente` y `lote_consolidado`. Se completan columnas que faltaban: descripciones, límites, peso, estados, concepto del pago y otras. Hay un diseño físico con restricciones e índices, y una estructura documentada de las llaves de Redis.

### ADR-29 · Mockups corregidos y pantallas nuevas
- **Decisión:** se corrigen las pantallas que se contradecían con estas decisiones y se agregan las que faltaban: A5.8 a A5.10, A7.1, A7.2, A8.1, A8.2, B1.5 y B2.4 a B2.7. La lista está en [11 §3](11-interfaz.md#3-catalogo-de-pantallas).

### ADR-30 · Ciclos exactos de 30 días y cómo se muestran las fechas
- **Contexto:** los mockups mezclaban ciclos de 30 y de 31 días (por ejemplo "24 ago – 23 sep" con renovación el 24 de septiembre), aunque la vigencia decía "30 días".
- **Decisión:** `fin = inicio + vigencia_dias`. En pantalla se muestra el último día con servicio (`fin − 1 día`) y la renovación en `fin`. Por ejemplo, **24 ago 2026 – 22 sep 2026**, con renovación el **23 de septiembre de 2026**. El periodo de gracia se muestra igual (por ejemplo, 23 sep – 29 sep, con suspensión el 30 de septiembre).
- **Consecuencias:** se corrigieron las fechas de A2, A6 y B1. El prorrateo de A2.5 (13 días) no cambia.

### ADR-31 · Las especificaciones viven en el repositorio y se verifican automáticamente
- **Decisión:** `docs/specs/*.md` es la **fuente de verdad**. Los diagramas están en Mermaid, que GitHub muestra directamente. Los documentos Word y PDF que se entregan se generan desde ahí. Nada se integra a `main` sin CI en verde (RNF-15).
- **Consecuencias:** los agentes de IA trabajan contra texto que pueden versionar y revisar. Desaparece la marca "Text is not SVG" de los diagramas exportados.

### ADR-32 · La variante 4 es el sistema visual oficial
- **Decisión:** la variante 4, "Plano azul" (Sora, IBM Plex Sans y `#3B6FF0`), que es la que ya usaban todas las pantallas de la aplicación. Las demás quedan en `mockups/_descartadas/`.

### ADR-33 · Orígenes de demostración y siembra igual a los mockups
- **Decisión:** el repositorio incluye dos APIs de origen de demostración (Envíos Xelajú y Agro Precios, cada una con su OpenAPI) y una siembra con los mismos datos que los mockups.
- **Consecuencias:** el "sistema funcionando" se puede demostrar de punta a punta sin depender de terceros.

### ADR-34 · La categoría ya está aprobada
- **Decisión:** la docente aprobó la propuesta en la categoría *Servidores Web como Servicio*. La correspondencia con cada requisito del enunciado está en [01 §2](01-vision-y-alcance.md#2-categoria-del-enunciado).

### ADR-35 · Versiones del stack
- **Decisión:**
  - **Backend:** **.NET 10 (LTS)** en vez de .NET 8, cuyo soporte termina en noviembre de 2026. Sigue siendo ASP.NET Core, así que no cambia nada respecto a lo aprobado.
  - **Frontend:** React 19, TypeScript, Vite y Tailwind 4, en un *workspace* pnpm con dos aplicaciones y un paquete de UI.
  - **Datos:** PostgreSQL 16 (por `NULLS NOT DISTINCT`) y Redis 7.4.
- **Consecuencias:** cada integrante instala el SDK de .NET 10, Node 22 o posterior, pnpm y Docker Desktop.

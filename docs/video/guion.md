# Guion del video del Avance 3

Video de **22 min 45 s** como máximo planeado (límite del curso: 25 min, con 2 min de margen). Se graba el **viernes 30 de octubre** y se entrega el **sábado 31 a las 23:59**. Narran las cuatro personas. Muestra el proyecto terminado: la parte 1 recorre toda la funcionalidad por la interfaz y la parte 2 explica cuatro elementos del código, uno por persona.

Cada bloque se graba por separado: se puede pausar entre bloques y unirlos al editar. Si algo sale mal, se repite solo ese bloque (ver «Repetir una toma»). La pantalla la maneja una persona y narra quien indica el bloque. Si es más cómodo, cada persona graba su audio y se monta sobre la toma.

## Resumen

| # | Bloque | Narra | Duración | Acumulado |
|---|---|---|---|---|
| | **Parte 1 · Funcionalidad** | | **13:30** | |
| 1 | Apertura y sitio público | Jordin | 0:30 | 0:30 |
| 2 | El ambiente con un solo comando | José Pablo | 0:30 | 1:00 |
| 3 | Una proveedora nueva se registra y contrata su plan | Emilio | 1:45 | 2:45 |
| 4 | Ana publica una API | Dominique | 1:45 | 4:30 |
| 5 | Portal de marca blanca, dominio propio y una consumidora nueva | Dominique | 2:00 | 6:30 |
| 6 | La compuerta desde la consola: 200, 401 y 429 | Jordin | 1:00 | 7:30 |
| 7 | Una consumidora con historia: recuperación, consumo, claves y cambio de plan | Emilio | 1:15 | 8:45 |
| 8 | El proveedor: equipo, claves, consumo y facturación | Jordin | 1:00 | 9:45 |
| 9 | Soporte | José Pablo | 1:00 | 10:45 |
| 10 | Administración, estado y bitácora | José Pablo | 1:15 | 12:00 |
| 11 | Reloj de demostración: renovación, gracia, suspensión (403) y reactivación | Emilio | 1:30 | 13:30 |
| | **Parte 2 · Código** | | **9:00** | |
| 12 | Puertos y adaptadores en los pagos | Emilio | 2:15 | 15:45 |
| 13 | Método de fábrica al validar el origen (SSRF) | Dominique | 2:15 | 18:00 |
| 14 | Bandeja de salida transaccional del correo | José Pablo | 2:15 | 20:15 |
| 15 | Cadena de responsabilidad en la compuerta | Jordin | 2:15 | 22:30 |
| 16 | Cierre | Jordin | 0:15 | **22:45** |

**Participación:** Jordin 5:00 (bloques 1, 6, 8, 15 y 16) · Emilio 6:45 (3, 7, 11 y 12) · Dominique 6:00 (4, 5 y 13) · José Pablo 5:00 (2, 9, 10 y 14).

Si el video pasa de 24 minutos al editar, se acorta en este orden: el paso opcional del bloque 5 (portal de Agro Precios), la parte de B1.5 del bloque 8 y la de A6.1 del bloque 10.

---

## Antes de grabar

Hacerlo en este orden la mañana de la grabación. Los pasos 1 a 5 los puede hacer el agente de Jordin, salvo el borrado de volúmenes, que hace Jordin con `!` porque el modo automático no lo permite.

1. **Ambiente desde cero con la siembra (RNF-14).** Con `main` actualizado, en la raíz del repositorio:
   ```powershell
   docker compose --env-file .env -f infra/compose.yml -f infra/compose.prod.yml --profile demo down -v
   docker compose --env-file .env -f infra/compose.yml -f infra/compose.prod.yml --profile demo up -d --build
   docker compose --env-file .env -f infra/compose.yml -f infra/compose.prod.yml --profile demo ps -a siembra-demo
   ```
   `down -v` borra la base, Redis, el reloj de demostración y la autoridad certificadora: así no quedan datos de ensayos (Carmen, Lucía) ni el reloj adelantado. `siembra-demo` debe terminar con `Exited (0)`. **Grabe la terminal del bloque 2 en este momento** (el segundo comando y el `ps`); al editar se corta la espera.
2. **Verificar y calentar la compuerta:** `node infra/verificar.mjs --demo`. Tarda unos dos minutos. Luego espere 30 segundos, para que B3.1 muestre la compuerta sana.
3. **Importar el certificado.** `down -v` crea una autoridad certificadora nueva. En Windows PowerShell (manual técnico, «Confiar en la autoridad certificadora local»):
   ```powershell
   docker compose --env-file .env -f infra/compose.yml cp borde:/data/caddy/pki/authorities/local/root.crt .\caddy-root.crt
   Import-Certificate -FilePath .\caddy-root.crt -CertStoreLocation Cert:\CurrentUser\Root
   Remove-Item .\caddy-root.crt
   ```
   Cierre y vuelva a abrir el navegador. https://shapi.localhost debe abrir con candado y sin aviso.
4. **Pantalla:** resolución de 1920 × 1080 (o 1366 × 768 si no hay más). Navegador maximizado, zoom al 100 %, sin marcadores ni otras pestañas. Para el código: el editor con fuente de 16 pt o más y el tema claro, y la vista previa de Markdown para el diagrama de 08 §3.
5. **Notificaciones:** active el Asistente de concentración de Windows (solo alarmas). Cierre Teams, WhatsApp, el correo y los avisos del antivirus.
6. **No haga nada del guion antes de grabar:** crearía a Carmen o a Lucía y la toma daría «ya existe».
7. **Prueba de audio** de 10 segundos de cada persona.

### Ventanas y orden de las cuentas

El panel, el portal y la administración tienen cookies distintas por host, pero Ana y Carmen comparten el host del panel, y Rodrigo y Sofía el de la administración. Por eso van en ventanas separadas y se entra en este orden:

| Ventana | Pestañas | Cuentas, en orden |
|---|---|---|
| Normal | https://shapi.localhost · https://envios.shapi.localhost · https://correo.shapi.localhost | Panel: Carmen (bloque 3) → Ana (bloques 4, 5, 8 y 9). Portal: Lucía (bloques 5 y 6) → María José (bloque 7) → Lucía (bloque 11) |
| Incógnito | https://shapi.localhost/entrar | Sofía (bloque 9) → Rodrigo (bloques 10 y 11) |

### Hoja de datos

Las cuentas de la siembra usan la contraseña **`Shapi2026!demo`**.

| Quién | Correo | Contraseña | Papel |
|---|---|---|---|
| Carmen Ixchop (nueva) | carmen@cafedelvolcan.com | `CafeVolcan2026` | Propietaria de «Café del Volcán» |
| Ana Lucía Morales (siembra) | ana.morales@enviosxelaju.com | `Shapi2026!demo` | Propietaria de «Envíos Xelajú», plan Producto |
| Lucía Pérez (nueva) | lucia@tiendazunil.com | `TiendaZunil2026` | Consumidora de «Tienda Zunil» |
| María José Quiñónez (siembra) | mariajose@mercaditoantigua.com | `Shapi2026!demo` → en el bloque 7, `Mercadito2026` | Consumidora de «Mercadito Antigua», plan Comercio |
| Sofía Menchú Cojtí (siembra) | sofia.menchu@shapi.localhost | `Shapi2026!demo` | Soporte de Shapi |
| Rodrigo Alvarado (siembra) | rodrigo.alvarado@shapi.localhost | `Shapi2026!demo` | Administrador de Shapi |

| Tarjeta (pasarela simulada) | Resultado |
|---|---|
| 4242 4242 4242 4242 · 12/28 · CVV 123 | Aprobada |
| 4000 0000 0000 0002 · 12/28 · CVV 123 | Rechazada por fondos insuficientes |
| 4000 0000 0000 0341 · 12/28 · CVV 123 | Aprobada al contratar; rechazada al renovar |

- Especificación OpenAPI para el bloque 4: `origenes-demo/envios-xelaju/cotizacion-envios.yaml`, del repositorio (5 rutas).
- Consola, `POST /cotizaciones`: origen `0901`, destino `0301`, peso_kg `2.5`.
- Clave de pruebas de respaldo para la consola (Mercadito Antigua): `shp_prueba_Jp5sX1cV8nB3yG7tQe2Kv0a19d`.
- Cerrar sesión: en el panel, el ícono junto al nombre, abajo en la barra lateral; en el portal, «Cerrar sesión» en la barra lateral.

---

## Parte 1 · Funcionalidad

### Bloque 1 · Apertura y sitio público · Jordin · 0:30
**Pantalla:** https://shapi.localhost (A0.1). **Datos:** los cuatro planes de plataforma de la siembra base.
1. Baje despacio por «Tres pasos para publicar y cobrar por su API» hasta «Los planes de Shapi».
- *Qué decir:* «Shapi es una plataforma como servicio para publicar APIs, controlar quién las usa y cobrar por ellas. Somos Jordin, Emilio, Dominique y José Pablo; les mostramos el proyecto terminado y cuatro decisiones de diseño del código.» Señale los planes: «estos planes, con su precio y su vigencia, se leen de la base de datos».

### Bloque 2 · El ambiente con un solo comando · José Pablo · 0:30
**Pantalla:** la terminal grabada en el paso 1 de «Antes de grabar».
1. El comando `docker compose … --profile demo up -d --build`, los contenedores en `Started` y el `ps` con `siembra-demo` en `Exited (0)`.
- *Qué decir:* «Todo el ambiente productivo simulado se levanta con un solo comando: PostgreSQL, Redis, la API de control, la compuerta, el trabajador, los dos portales, el correo y los orígenes de demostración, detrás de un borde con HTTPS. La siembra de demostración termina con código cero y deja las cuentas y los datos que van a ver.»

### Bloque 3 · Una proveedora nueva se registra y contrata su plan · Emilio · 1:45
**Pantallas:** A1.1, A1.2, Mailpit, B1.4, A2.1, A2.2, A2.4, A2.3. **Datos:** Carmen (nueva), tarjetas 0002 y 4242.
1. **Crear cuenta** en el inicio → **Registro (A1.1):** Nombre «Carmen Ixchop», carmen@cafedelvolcan.com, organización «Café del Volcán», `CafeVolcan2026` → **Crear cuenta** → «Revise su correo» (A1.2).
2. Pestaña del correo → «Verifique su correo» → **Verificar mi correo** → entra al panel. *Qué decir:* «el correo se valida con un enlace de un solo uso que vence en 24 horas; la cuenta empieza en el plan Prueba, sin costo».
3. **Suscripción de plataforma (B1.4)** → **Cambiar de plan** → **Planes (A2.1)** → **Contratar** en **Lanzamiento** (Q 199).
4. **Contratación (A2.2):** tarjeta **4000 0000 0000 0002**, `12`, `28`, `123`, titular «Carmen Ixchop» → **Pagar Q 199.00 y activar el plan** → **Tarjeta rechazada (A2.4)**. *Qué decir:* «la pasarela es simulada; esta tarjeta simula fondos insuficientes y el plan no cambia».
5. **Usar otra tarjeta** → **4242 4242 4242 4242** → **Plan activado (A2.3)** → **Suscripción de plataforma**: Lanzamiento, Visa •••• 4242 y la fecha de renovación. *Qué decir:* «Shapi no guarda el número: solo un token, la marca y los últimos cuatro dígitos. La renovación se cobra sola cada 30 días».
6. **Cerrar sesión.**

### Bloque 4 · Ana publica una API · Dominique · 1:45
**Pantallas:** A1.3, A3.1, A3.2, A3.3, A3.4, A3.5, A4.1. **Datos:** Ana y su organización «Envíos Xelajú» de la siembra; origen `http://origen-envios:8080`.
1. **Entrar (A1.3)** con Ana → **APIs (A3.1):** «API de Cotización de Envíos» (publicada) y «API de Recolecciones». *Qué decir:* «Ana ya usa Shapi; su historia viene de la siembra».
2. **Registrar una API (A3.2):** `Rastreo de Paquetes`, origen `http://origen-envios:8080`, subdominio `rastreo` → **Registrar y continuar** → «Conexión probada» y el **secreto de origen**, visible una sola vez. *Qué decir:* «Shapi prueba la conexión y no acepta direcciones de la red interna».
3. **Especificación (A3.3):** elija `origenes-demo/envios-xelaju/cotizacion-envios.yaml` → «Rutas encontradas 5» → **Continuar a la selección de rutas**.
4. **Rutas expuestas (A3.4):** marque `GET /rastreo` y `GET /cobertura` → **Guardar rutas expuestas**. *Qué decir:* «una ruta oculta no aparece en el portal y la compuerta la rechaza con 403».
5. **Configuración por ruta (A3.5):** `GET /rastreo` con límite **5** por minuto y caché de **1 minuto** → **Guardar configuración**.
6. **Planes (A4.1)** → **Crear un plan**: «Gratis», «Para probar la API sin costo», cuota 1000, límite 5; **Plan gratuito** ya viene marcado (no lo toque) → **Crear plan**.
7. **APIs** → **Publicar** en «Rastreo de Paquetes». *Qué decir:* «al publicar, la configuración se copia a Redis y la compuerta ya la enruta, sin consultar la base de datos».

### Bloque 5 · Portal de marca blanca, dominio propio y una consumidora nueva · Dominique · 2:00
**Pantallas:** A3.7, A3.6, A5.0, A5.1, A5.3, A5.8, A5.4, A5.6, A5.4b. **Datos:** «API de Cotización de Envíos» de la siembra; Lucía (nueva) con la tarjeta 0341.
1. Panel de Ana → selector de API: **API de Cotización de Envíos** → **Portal (A3.7):** cambie el color principal y muestre la **vista previa en vivo** con el logotipo y los textos. Deje el color de la siembra antes de salir.
2. **Dominios (A3.6):** las direcciones asignadas y el dominio `api.enviosxelaju.localhost` **pendiente de verificación** con su registro CNAME → **Simular la creación del registro** → espere «El registro DNS simulado se creó» → **Verificar registro DNS** → **Verificado**. *Qué decir:* «el proveedor apunta su dominio a Shapi con un CNAME; aquí el DNS es simulado».
3. Pestaña https://envios.shapi.localhost → **Inicio del portal (A5.0):** la marca de Envíos Xelajú, sin la de Shapi → **Documentación (A5.1)** → `POST /cotizaciones`, con la dirección del dominio propio.
4. Inicio → **Crear cuenta (A5.3):** Lucía Pérez, lucia@tiendazunil.com, «Tienda Zunil», `TiendaZunil2026` → **Crear cuenta** → en el correo, «Verifique su correo» **de Envíos Xelajú** → el enlace (A5.8) abre **Planes (A5.4)**.
5. **Contratar** en **Básico** (Q 149) → **Pago (A5.6):** **4000 0000 0000 0341**, «Lucía Pérez», `12/28`, `123` → **Pagar Q 149.00 y contratar el plan** → **Plan contratado (A5.4b):** sus dos claves, que no se vuelven a mostrar. **Copiar** la de **Pruebas** y péguela, fuera de cámara, en el Bloc de notas: se usa en los bloques 6 y 11. *Qué decir:* «esta tarjeta se aprueba ahora y la vamos a ver fallar al renovar».
6. *(Opcional, si sobra tiempo)* https://agro.shapi.localhost (A5.5): el mismo portal con otra marca.

### Bloque 6 · La compuerta desde la consola: 200, 401 y 429 · Jordin · 1:00
**Pantalla:** A5.2. **Datos:** la clave de pruebas de Lucía (la de respaldo de Mercadito Antigua, si se perdió).
1. **Consola de pruebas (A5.2):** `POST /cotizaciones`, origen `0901`, destino `0301`, peso_kg `2.5`, pegue la clave de pruebas → **Enviar petición** → **200**, el JSON del origen y **Límites y cuota** (`X-RateLimit-*` y `X-Cuota-*`). *Qué decir:* «es una petición real: pasa por la compuerta hasta el servidor de Envíos Xelajú. La clave de pruebas no descuenta cuota».
2. Cambie el último carácter de la clave → **Enviar petición** → **401** `clave_invalida`. Restaure la clave.
3. Pulse **Enviar petición** unas 10 veces seguidas, hasta el **429** `limite_por_minuto`, con `Retry-After` (en el ensayo llegó en el décimo envío después del 200). *Qué decir:* «la clave de pruebas admite 10 peticiones por minuto; el límite y la cuota se evalúan en Redis con un script atómico. El 403 lo veremos al suspender la suscripción».
4. **Cerrar sesión.**

### Bloque 7 · Una consumidora con historia: recuperación, consumo, claves y cambio de plan · Emilio · 1:15
**Pantallas:** A5.7, A5.9, A5.10, B2.1, B2.2, B2.3, B2.4, B2.5, B2.7. **Datos:** María José, plan Comercio, Mastercard •••• 3057, consumo del ciclo de la siembra.
1. **Entrar (A5.7)** → **¿Olvidó su contraseña?** (A5.9) → mariajose@mercaditoantigua.com → **Enviar el enlace** → en el correo, «Recupere su contraseña» → **Contraseña nueva (A5.10)** `Mercadito2026` → entra a su cuenta. *Qué decir:* «la respuesta es la misma aunque el correo no exista, y el enlace es de un solo uso».
2. **Consumo (B2.1):** el ciclo y el consumo por ruta; `POST /guias` descuenta 5 llamadas por petición. **Pagos (B2.2):** su historial.
3. **Suscripción y claves (B2.3)** → **Rotar** en la clave de pruebas → la confirmación (B2.4) dice hasta cuándo funciona la anterior → **Rotar clave** → la clave nueva, una sola vez (B2.5) → **Ya la guardé**.
4. **Cambiar de plan (B2.7)** → **Elegir Volumen**: crédito por los días de Comercio, cargo por los de Volumen y **A pagar hoy** → **Pagar … y cambiar de plan**. *Qué decir:* «al subir de plan se cobra solo la diferencia prorrateada; al bajar, el cambio se programa para la renovación».
5. **Cerrar sesión.**

### Bloque 8 · El proveedor: equipo, claves, consumo y facturación · Jordin · 1:00
**Pantallas:** A4.2, A4.3, B1.1, B1.2, B1.5, B1.3. **Datos:** Envíos Xelajú de la siembra: Diego (editor), Karla (lectora), cuatro consumidores.
1. Panel de Ana → **Miembros y roles (A4.2):** Ana propietaria, Diego editor y Karla lectora, y el formulario **Invitar miembro**. *Qué decir:* «el editor configura APIs y planes; el lector solo consulta; el cobro y los miembros son del propietario».
2. API de Cotización de Envíos → **Claves (A4.3):** solo los últimos 4 caracteres, una **rotada** y una **revocada**. *Qué decir:* «Shapi solo guarda el hash; revocar tarda menos de 10 segundos en la compuerta».
3. **Consumo (B1.1):** peticiones, latencia p95, errores por código y consumo por ruta. **Consumidores (B1.2):** consumo y facturación por consumidor, con **Invitar consumidores** (B1.5).
4. **Historial de pagos (B1.3):** el cambio de Lanzamiento a Producto con la diferencia prorrateada.

### Bloque 9 · Soporte · José Pablo · 1:00
**Pantallas:** A7.1, A6.4, A6.4b, Mailpit, A7.2. **Datos:** casos CAS-100 y CAS-104 de la siembra; Sofía.
1. Panel de Ana → **Casos de soporte (A7.1)** → «Abrir un caso»: «¿Cómo cambio el color de mi portal?», API «Rastreo de Paquetes» y una descripción → **Abrir caso**: aparece en la lista.
2. Ventana de incógnito → **Entrar** con Sofía → **Casos (A6.4)** → **Abrir** el caso nuevo (A6.4b): la conversación y los datos de la organización en solo lectura → **Asignarme el caso** → una respuesta → **Enviar respuesta**.
3. Pestaña del correo: el aviso «Nuevo caso» para Sofía y «Nueva respuesta en su caso» para Ana. Ventana normal: recargue el caso; Ana ve la respuesta (A7.2). *Qué decir:* «el correo se guarda junto con la respuesta y el trabajador lo envía después; lo explico en mi parte del código».
4. Incógnito: **Cerrar sesión** de Sofía.

### Bloque 10 · Administración, estado y bitácora · José Pablo · 1:15
**Pantallas:** A6.2, A6.2b, A6.1, A6.3, A6.3b, A6.5, B3.1, B3.2. **Datos:** organizaciones de la siembra (Transportes Petén en gracia, Datos Chapines suspendida), Agro Precios, el pago de Carmen.
1. Incógnito → **Entrar** con Rodrigo → **Organizaciones (A6.2):** plan, ciclo y estado → **Suspender** en **Agro Precios** (A6.2b): APIs y consumidores afectados, un motivo → **Suspender organización** → **Reactivar**. *Qué decir:* «mientras está suspendida, la compuerta responde 403 a todas sus APIs».
2. **Planes de plataforma (A6.1)**, los mismos del sitio público. **Pagos (A6.3):** el rechazado y el aprobado de Carmen → **Revertir** (A6.3b): la advertencia → **Cancelar**.
3. **Cuentas (A6.5):** administradores y soporte; Julio Estrada, desactivado.
4. **Estado de los componentes (B3.1):** los siete componentes y el p95 de la compuerta. **Bitácora (B3.2):** las acciones de hoy: la reactivación y la suspensión de Agro Precios, el caso, el dominio, la API publicada y los planes contratados.

### Bloque 11 · Reloj de demostración: renovación, gracia, suspensión (403) y reactivación · Emilio · 1:30
**Pantallas:** B3.1 (tarjeta «Modo demostración»), B2.3, A5.2, B1.4. **Datos:** la suscripción de Lucía con la tarjeta 0341. **Va al final** porque el reloj no se puede retroceder.
1. Incógnito (Rodrigo) → **Estado de los componentes** → tarjeta **Modo demostración**: la fecha de la demostración → **Adelantar 30 días**.
2. Ventana normal, portal → **Entrar** con Lucía → **Suscripción y claves (B2.3):** **en gracia**, «Quedan 7 días antes de que sus claves dejen de responder»: la renovación con 0341 fue rechazada. Si todavía no aparece, recargue unos segundos después. *Qué decir:* «el trabajador cerró el ciclo: intentó cobrar, falló y dio 7 días de gracia; durante la gracia la API sigue funcionando».
3. Incógnito → **Adelantar 7 días**. Portal → recargue B2.3: **Suspendida**. **Consola (A5.2)** con su clave de pruebas → **403** `suscripcion_inactiva`.
4. B2.3 → **Pagar con otra tarjeta** → 4242 4242 4242 4242, «Lucía Pérez», `12/28`, `123` → **Pagar Q 149.00 y reactivar** → espere a que termine «Procesando…» y la suscripción diga **Activa** (en el ensayo, 1,5 s) → Consola → **200**. Si sale de la página mientras dice «Procesando…», el pago puede no completarse; en el ensayo pasó así y hubo que pagar otra vez.
5. *(Si el tiempo alcanza)* Panel de Ana → **Suscripción de plataforma (B1.4):** se renovó sola, con la fecha nueva.

---

## Parte 2 · Código

En cada bloque se muestra el editor con los archivos en el orden de su elemento y, al final, la terminal con las pruebas. El texto para narrar y el orden de la pantalla están en cada archivo; las líneas citadas se comprobaron contra `main` el 30 de octubre.

| # | Narra | Elemento | Archivo | Duración |
|---|---|---|---|---|
| 12 | Emilio | Puertos y adaptadores: `IPasarelaPagos` y `PasarelaSimulada` (ADR-12 y ADR-13) | [elementos/emilio.md](elementos/emilio.md) | 2:15 |
| 13 | Dominique | Método de fábrica con objeto resultado en `ValidadorDireccionOrigen` (RNF-10) | [elementos/dominique.md](elementos/dominique.md) | 2:15 |
| 14 | José Pablo | Bandeja de salida transaccional del correo (RF-46, ADR-27) | [elementos/jose-pablo.md](elementos/jose-pablo.md) | 2:15 |
| 15 | Jordin | Cadena de responsabilidad en la tubería de la compuerta (RNF-13) | [elementos/jordin.md](elementos/jordin.md) | 2:15 |

Cada narración tiene entre 285 y 340 palabras, unos 2 minutos a ritmo de presentación. Si una toma pasa de 2:30, se acorta su penúltimo párrafo.

### Bloque 16 · Cierre · Jordin · 0:15
**Pantalla:** https://shapi.localhost.
- *Qué decir:* «Eso es Shapi: publicar una API, protegerla y cobrar por ella, todo funcionando en el ambiente productivo simulado y con pruebas automáticas en cada cambio. Gracias.»

---

## Cobertura de la funcionalidad

| Lineamiento o requisito | Bloque |
|---|---|
| Lineamientos 4.1 · Registro y validación de correo (RF-01, RF-02, RF-05) | 3 (proveedor), 5 (consumidor) |
| Lineamientos 4.1 · Recuperación de contraseña (RF-03) | 7 |
| Lineamientos 4.1 · Autenticación segura y sesiones (RF-04) | 3, 4, 7, 9 y 10 (entrar y cerrar sesión con cookies por host) |
| Lineamientos 4.1 · Roles: administrador, cliente y soporte (RF-06, 04 §2) | 8 (propietario, editor y lector), 9 (soporte), 10 (administración y cuentas) |
| Lineamientos 4.2 · Planes con nombre, descripción, precio y vigencia; visualización de planes (RF-17, RF-18, RF-19) | 1 (A0.1), 3 (A2.1), 4 (A4.1), 5 (A5.4), 10 (A6.1) |
| Lineamientos 4.2 · Contratación de servicios (RF-20) | 3 (plataforma), 5 (API) |
| Lineamientos 4.2 · Historial de pagos (RF-21, RF-24) | 7 (B2.2), 8 (B1.3), 10 (A6.3) |
| Lineamientos 4.2 · Renovación, gracia, suspensión y reactivación (RF-22, RF-23) | 11 |
| Cambio de plan con prorrateo (RF-25) | 7 (B2.7), 8 (B1.3) |
| Lineamientos 4.3 (opción C) · Crear entornos: publicar una API con su especificación, rutas y configuración (RF-08, RF-09, RF-10, RF-13, RF-14) | 4 |
| Lineamientos 4.3 · Gestión de dominios simulados (RF-11, RF-12) | 5 (A3.6) |
| Personalización del portal de marca blanca (RF-15, RF-16) | 5 (A3.7, A5.0, A5.1, A5.5) |
| Claves: emisión, rotación y revocación (RF-26, RF-27, RF-28) | 5 (A5.4b), 7 (B2.4 y B2.5), 8 (A4.3) |
| Compuerta: 200, 401, 403 y 429 (RF-29, RF-30, RF-31, RF-45) | 6 (200, 401 y 429), 11 (403) |
| Lineamientos 4.3 · Monitorear consumo: consumo y facturación (RF-35, RF-36, RF-37) | 7 (B2.1), 8 (B1.1 y B1.2) |
| Invitar consumidores (RF-05) | 8 (B1.5) |
| Administración de organizaciones (RF-38) | 10 (A6.2 y A6.2b) |
| Estado de los componentes (RF-39) | 10 (B3.1) |
| Soporte (RF-40) y correo con reintentos (RF-46) | 9, 14 |
| Bitácora de acciones sensibles (RF-41) | 10 (B3.2) |
| Cuentas de administración y soporte (RF-42) | 10 (A6.5) |
| Protección del origen contra SSRF (RNF-10) | 4 (A3.2), 13 |
| Agregar una regla a la compuerta sin tocar las demás (RNF-13) | 15 |
| Lineamientos 6 y 8 · Despliegue: el ambiente productivo con un solo comando (RNF-14) | 2 |
| Lineamientos (Avance 3) · 4 elementos de código con Clean Code y patrones | 12 a 15 |

## Qué evitar

- **Repetir un bloque después del bloque 11:** el reloj ya está adelantado y no se puede retroceder.
- **Registrar a Carmen o a Lucía dos veces:** la segunda vez da «ya existe». Use otro correo (agregue un 2) o vuelva a sembrar.
- **Entrar con una contraseña incorrecta cinco veces:** bloquea la cuenta 15 minutos.
- **Más de 10 inicios de sesión o registros por minuto desde el mismo equipo:** el límite de las pantallas de acceso responde «demasiados intentos»; espere un minuto.
- **Señalar el encabezado del portal en la consola o en el inicio:** con la sesión abierta, sigue mostrando «Entrar» y «Crear cuenta» (la barra lateral sí muestra a la consumidora). Señale la barra lateral.

## Si algo falla

- **Una página da error:** pause la grabación, revise y repita el bloque.
- **No llega un correo:** espere unos 5 segundos (el trabajador revisa la bandeja cada 5 s) y recargue el buzón.
- **Se cerró una sesión:** vuelva a entrar; lo hecho se conserva.
- **B3.1 marca la compuerta como degradada:** el umbral es un p95 de 15 ms en la última hora (06 §8). En el ensayo, después de siete corridas seguidas en esta PC, llegó a 38 ms. Ejecute otra vez `node infra/verificar.mjs --demo` y espere 30 segundos. Si sigue degradada, no lo oculte: José Pablo lo narra como lo que es, «B3.1 detecta que la compuerta pasa de 15 ms en este equipo».

### Repetir una toma

- **Bloques 1 a 10:** vuelva a sembrar sin reconstruir:
  ```powershell
  docker compose --env-file .env -f infra/compose.yml -f infra/compose.prod.yml exec trabajador dotnet Shapi.Trabajador.dll sembrar-demo --reiniciar
  ```
  Borra las organizaciones y los consumidores que no son de la siembra (Café del Volcán, Lucía) y deja los datos sembrados como al principio. Conserva el personal y la bitácora, que es solo de inserción. Después, `node infra/verificar.mjs --demo`.
- **Bloque 11 o si ya se adelantó el reloj:** hay que empezar desde cero, con el paso 1 de «Antes de grabar» (`down -v` y `up`), e importar otra vez el certificado.

---

## Ensayo del 30 de octubre

Se recorrió el guion completo, solo por la interfaz, en un ambiente productivo simulado recién levantado y sembrado (proyecto `shapi-jg29`, imágenes construidas desde `main` en `8dc94e0`, `node infra/verificar.mjs --demo` en verde). Lo recorrió un navegador automatizado (Playwright, 1366 × 768) que seguía los pasos de cada bloque. Por eso el tiempo medido es el de la interfaz: lo que tardan las pantallas, los correos y los cobros, sin escribir a mano ni narrar. La narración se estimó a 150 palabras por minuto con los textos de «Qué decir». **La primera toma real se cronometra y, si un bloque pasa de lo planeado, se aplica el orden de recortes del resumen.**

| # | Planeado | Interfaz medida | Narración estimada | Resultado |
|---|---|---|---|---|
| 1 | 0:30 | 2 s | 0:24 | Correcto: los cuatro planes |
| 2 | 0:30 | — | 0:22 | Arranque desde cero: `siembra-demo` en `Exited (0)` y el verificador en verde (unos 2 min, que se cortan) |
| 3 | 1:45 | 16 s | 0:25 | Correcto: rechazo con 0002, activación con 4242 y Visa •••• 4242 en B1.4 |
| 4 | 1:45 | 9 s | 0:23 | Correcto. «Plan gratuito» viene marcado; la pantalla dice «Rutas encontradas 5» |
| 5 | 2:00 | 17 s | 0:11 | Correcto: la siembra trae el dominio pendiente; A5.1 muestra `https://api.enviosxelaju.localhost/cotizaciones`; contratación con 0341 |
| 6 | 1:00 | 9 s | 0:20 | 200 con `X-RateLimit-*` y `X-Cuota-*`; 401 `clave_invalida`; 429 `limite_por_minuto` en el décimo envío, con `Retry-After` |
| 7 | 1:15 | 15 s | 0:15 | Correcto: recuperación, Mastercard •••• 3057, rotación y «Pagar Q 225.00 y cambiar de plan» |
| 8 | 1:00 | 8 s | 0:13 | Correcto: propietario, editor y lector; claves rotada y revocada; Lanzamiento → Producto Q 173.34 |
| 9 | 1:00 | 15 s | 0:11 | Correcto: el caso aparece sin recargar; llegan «Nuevo caso» y «Nueva respuesta en su caso» |
| 10 | 1:15 | 14 s | 0:05 | Correcto. B3.1 marcó la compuerta degradada (p95 de 38 ms tras siete corridas; ver «Si algo falla») |
| 11 | 1:30 | 25 s | 0:08 | Gracia visible 2,5 s después de +30 días; suspensión 0,6 s después de +7; 403 `suscripcion_inactiva`; pago 200 en 1,5 s y consola 200; B1.4 de Ana renovada |
| 12 a 15 | 9:00 | — | 1:54, 2:15, 2:15 y 2:08 | Narraciones de 286, 338, 337 y unas 320 palabras |

También se comprobó «Repetir una toma»: `sembrar-demo --reiniciar` tardó 16 s, borró las organizaciones de Carmen, a Lucía y las APIs «rastreo», y conservó a María José.

Lo que el ensayo corrigió en este guion: «Cambiar de plan» en B1.4, «Plan gratuito» ya marcado, «Rutas encontradas 5», la caché «1 minuto», esperar el aviso del DNS simulado antes de verificar, guardar la clave de pruebas para el bloque 11, el nombre de los correos de soporte y esperar a que termine el pago antes de salir de B2.3.

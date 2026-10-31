# Guion del video del Avance 3

Video de **22 min 35 s** como máximo planeado (límite del curso: 25 min; quedan 2:25 de margen). Se entrega el **sábado 31 de octubre a las 23:59**. Muestra el proyecto terminado y lo narran las cuatro personas. **Solo se muestran la interfaz gráfica y el código en el editor; nada de la consola o la terminal.**

**Cada integrante graba su parte por su cuenta y después se unen los cuatro videos en este orden:**

| Orden | Parte | Integrante | Duración | Acumulado |
|---|---|---|---|---|
| 1 | Presentación, la compuerta, el consumo y el ciclo de cobro | Jordin | 6:05 | 6:05 |
| 2 | La cuenta del proveedor, su equipo, los pagos y el cambio de plan | Emilio | 5:30 | 11:35 |
| 3 | Publicar una API y su portal de marca blanca | Dominique | 5:30 | 17:05 |
| 4 | Soporte, administración, cuentas y el cierre | José Pablo | 5:30 | **22:35** |

Cada parte está completa: tiene la funcionalidad que construyó esa persona y su elemento de código, en ese orden. **Cada parte se graba en un ambiente recién levantado y sembrado** y solo usa los datos de la siembra y los que crea esa misma parte, así que no depende de lo que grabaron los demás. Dentro de una parte se puede pausar entre bloques y repetir solo el bloque que salió mal (ver «Repetir una toma»).

Si al unir el video pasa de 24 minutos, se recorta en este orden: el paso opcional de 3.2 (portal de Agro Precios), B1.5 en 1.3 y A6.1 en 4.2.

---

## Antes de grabar (cada integrante, en su computadora)

1. **Ambiente desde cero con la siembra (RNF-14).** Con `main` actualizado, en la raíz del repositorio y con el `.env` de `.env.example`:
   ```powershell
   docker compose --env-file .env -f infra/compose.yml -f infra/compose.prod.yml --profile demo down -v
   docker compose --env-file .env -f infra/compose.yml -f infra/compose.prod.yml --profile demo up -d --build
   docker compose --env-file .env -f infra/compose.yml -f infra/compose.prod.yml --profile demo ps -a siembra-demo
   ```
   `down -v` borra la base, Redis, el reloj de demostración y la autoridad certificadora, así que no quedan datos de ensayos ni el reloj adelantado. `siembra-demo` debe terminar con `Exited (0)`.
2. **Verificar y calentar la compuerta:** `node infra/verificar.mjs --demo`. Tarda unos dos minutos. Luego espere 30 segundos, para que B3.1 muestre la compuerta sana.
3. **Importar el certificado.** `down -v` crea una autoridad certificadora nueva. En Windows:
   ```powershell
   docker compose --env-file .env -f infra/compose.yml cp borde:/data/caddy/pki/authorities/local/root.crt caddy-root.crt
   certutil -user -addstore Root caddy-root.crt
   ```
   Confirme el aviso de Windows con **Sí** y borre `caddy-root.crt`. En Linux, siga el manual técnico («Confiar en la autoridad certificadora local»). Cierre y vuelva a abrir el navegador por completo: https://shapi.localhost debe abrir con candado y sin aviso.
4. **Pantalla:** resolución de 1920 × 1080 (o 1366 × 768 si no hay más). Navegador maximizado, zoom al 100 %, sin marcadores ni otras pestañas. Para el código: el editor con fuente de 16 pt o más, tema claro y los archivos de su elemento ya abiertos.
5. **Notificaciones:** active el modo No molestar (en Windows, el Asistente de concentración). Cierre Teams, WhatsApp, el correo y los avisos del antivirus.
6. **No haga nada de su parte antes de grabar:** crearía a las personas nuevas y la toma daría «ya existe».
7. **Prueba de audio** de 10 segundos.
8. **Formato del video:** MP4, 1920 × 1080 (o la resolución de su pantalla), 30 cuadros por segundo. Nombre del archivo: `parte-<n>-<nombre>.mp4`. Empiece y termine con un segundo de pantalla quieta, para unir las partes sin cortes bruscos.

**Si varias personas graban en la misma computadora,** repitan los pasos 1 a 3 entre una parte y otra. Es obligatorio después de la parte de Jordin, porque adelanta el reloj y el reloj no se puede retroceder.

### Datos que usa cada parte

Las cuentas de la siembra usan la contraseña **`Shapi2026!demo`**.

| Parte | Cuentas | Datos nuevos que crea |
|---|---|---|
| 1 · Jordin | Ana Lucía Morales (ana.morales@enviosxelaju.com), Lucía Pérez (nueva), Rodrigo Alvarado (rodrigo.alvarado@shapi.localhost) | Lucía y su suscripción |
| 2 · Emilio | Carmen Ixchop (nueva), Tomás Chávez (nuevo), María José Quiñónez (mariajose@mercaditoantigua.com) | «Café del Volcán» con Tomás de editor; María José cambia de contraseña y pasa al plan Volumen |
| 3 · Dominique | Ana Lucía Morales, Pedro Ajú (nuevo) | API «Rastreo de Paquetes» con su plan «Gratis», dominio verificado y Pedro con su suscripción |
| 4 · José Pablo | Ana Lucía Morales, Sofía Menchú Cojtí (sofia.menchu@shapi.localhost), Rodrigo Alvarado, Mónica Tuyuc (nueva) | Un caso de soporte y la cuenta de soporte de Mónica |

| Persona nueva | Correo | Contraseña | Papel |
|---|---|---|---|
| Carmen Ixchop | carmen@cafedelvolcan.com | `CafeVolcan2026` | Propietaria de «Café del Volcán» (parte 2) |
| Lucía Pérez | lucia@tiendazunil.com | `TiendaZunil2026` | Consumidora de «Tienda Zunil» (parte 1) |
| Tomás Chávez | tomas@cafedelvolcan.com | `TomasChavez2026` | Editor de «Café del Volcán» (parte 2) |
| Mónica Tuyuc | monica.tuyuc@shapi.localhost | `MonicaTuyuc2026` | Soporte de Shapi (parte 4) |
| Pedro Ajú | pedro@libreriaxela.com | `LibreriaXela2026` | Consumidor de «Librería Xela» (parte 3) |

| Tarjeta (pasarela simulada) | Resultado |
|---|---|
| 4242 4242 4242 4242 · 12/28 · CVV 123 | Aprobada |
| 4000 0000 0000 0002 · 12/28 · CVV 123 | Rechazada por fondos insuficientes |
| 4000 0000 0000 0341 · 12/28 · CVV 123 | Aprobada al contratar; rechazada al renovar |

- Consola, `POST /cotizaciones`: origen `0901`, destino `0301`, peso_kg `2.5`.
- Clave de pruebas de la siembra (Mercadito Antigua): `shp_prueba_Jp5sX1cV8nB3yG7tQe2Kv0a19d`.
- Especificación OpenAPI: `origenes-demo/envios-xelaju/cotizacion-envios.yaml`, del repositorio (5 rutas).
- Correo simulado: https://correo.shapi.localhost.
- Cerrar sesión: en el panel y en el portal, el ícono junto al nombre, abajo en la barra lateral.
- El panel y la administración comparten el host `shapi.localhost`. Cuando una parte necesita a la vez una cuenta del panel y una de la administración, la de la administración va en una ventana de incógnito.

---

## Parte 1 · Jordin · Presentación, la compuerta, el consumo y el ciclo de cobro (6:05)

**Ventanas:** una normal, con https://shapi.localhost, https://envios.shapi.localhost, https://correo.shapi.localhost y el editor, y una de incógnito para Rodrigo (bloque 1.4). **El bloque 1.4 adelanta el reloj: grábelo después de los bloques 1.1 a 1.3.** El código (1.5) no usa el ambiente y se puede grabar en cualquier momento.

### 1.1 · Presentación y sitio público · 0:30
**Pantalla:** https://shapi.localhost (A0.1). **Datos:** los cuatro planes de plataforma de la siembra.
1. Baje despacio por «Tres pasos para publicar y cobrar por su API» hasta «Los planes de Shapi».
- *Qué decir:* «Shapi es una plataforma como servicio para publicar APIs, controlar quién las usa y cobrar por ellas. Somos Jordin, Emilio, Dominique y José Pablo; cada uno les muestra lo que construyó y una decisión de diseño de su código.» Señale los planes: «estos planes, con su precio y su vigencia, se leen de la base de datos».

### 1.2 · La compuerta desde la consola: 200, 401 y 429 · 0:50
**Pantalla:** A5.2 en https://envios.shapi.localhost/consola, sin sesión. **Datos:** la clave de pruebas de Mercadito Antigua.
1. `POST /cotizaciones`, origen `0901`, destino `0301`, peso_kg `2.5`, pegue la clave de pruebas → **Enviar petición** → **200**, el JSON del origen y **Límites y cuota** (`X-RateLimit-*` y `X-Cuota-*`). *Qué decir:* «cada petición a una API publicada pasa por la compuerta de Shapi, que valida la clave, el plan y los límites antes de llegar al servidor del proveedor».
2. Cambie el último carácter de la clave → **Enviar petición** → **401** `clave_invalida`. Restaure la clave.
3. Pulse **Enviar petición** unas 10 veces seguidas, hasta el **429** `limite_por_minuto` con `Retry-After`. El verificador del paso 2 usa esta misma clave: si el 429 sale antes de tiempo, espere un minuto y repita el bloque. *Qué decir:* «la clave de pruebas admite 10 peticiones por minuto; el límite y la cuota se evalúan en Redis con un script atómico. El 403 lo veremos al suspender una suscripción, al final de esta parte».

### 1.3 · El proveedor: claves, consumo y facturación · 0:45
**Pantallas:** A1.3, A4.3, B1.1, B1.2 y B1.5. **Datos:** Envíos Xelajú de la siembra y sus cuatro consumidores.
1. https://shapi.localhost/entrar → **Entrar** con Ana → selector de API: **API de Cotización de Envíos** → **Claves (A4.3):** solo los últimos 4 caracteres, una **rotada** y una **revocada**. *Qué decir:* «Shapi solo guarda el hash de cada clave; revocarla tarda menos de 10 segundos en la compuerta».
2. **Consumo (B1.1):** peticiones, latencia p95, errores por código y consumo por ruta. *Qué decir:* «la compuerta mide cada petición en Redis y un trabajador lo consolida en PostgreSQL».
3. **Consumidores (B1.2):** consumo y facturación por consumidor → **Invitar consumidores** (B1.5).
4. **Cerrar sesión.**

### 1.4 · El ciclo de cobro con el reloj: renovación, gracia, suspensión (403) y reactivación · 1:45
**Pantallas:** A5.3, A5.4, A5.6, A5.4b, B3.1 (tarjeta «Modo demostración»), B2.3 y A5.2. **Datos:** Lucía (nueva), la tarjeta 0341 y Rodrigo.
1. https://envios.shapi.localhost → **Crear cuenta (A5.3):** «Lucía Pérez», lucia@tiendazunil.com, «Tienda Zunil», `TiendaZunil2026` → correo «Verifique su correo» → **Planes (A5.4)** → **Contratar** en **Básico** → **Pago (A5.6):** **4000 0000 0000 0341**, «Lucía Pérez», `12/28`, `123` → **Pagar Q 149.00 y contratar el plan** → **Plan contratado (A5.4b)**. **Copie** la clave de **Pruebas** en el Bloc de notas, fuera de cámara. *Qué decir:* «esta tarjeta se aprueba al contratar y falla al renovar».
2. Incógnito → https://shapi.localhost/entrar con Rodrigo → **Estado de los componentes (B3.1)** → tarjeta **Modo demostración** → **Adelantar 30 días**.
3. Ventana normal → **Suscripción y claves (B2.3):** **en gracia**, «Quedan 7 días antes de que sus claves dejen de responder». Si no aparece, recargue unos segundos después. *Qué decir:* «el trabajador intentó renovar, el cobro falló y empezaron 7 días de gracia en los que la API sigue funcionando».
4. Incógnito → **Adelantar 7 días**. Ventana normal → recargue B2.3: **Suspendida** → **Consola de pruebas (A5.2)** → borre la clave del campo, que todavía tiene la de Mercadito Antigua del bloque 1.2, y pegue la de Lucía del Bloc de notas → **Enviar petición** → **403** `suscripcion_inactiva`.
5. B2.3 → **Pagar con otra tarjeta** → 4242 4242 4242 4242, «Lucía Pérez», `12/28`, `123` → **Pagar Q 149.00 y reactivar** → espere a que termine «Procesando…» y diga **Activa** → Consola, con la clave de Lucía → **200**. Si sale de la página mientras dice «Procesando…», el pago puede no completarse.

### 1.5 · Código: cadena de responsabilidad en la compuerta · 2:15
Texto, archivos y orden de la pantalla en [elementos/jordin.md](elementos/jordin.md).

---

## Parte 2 · Emilio · La cuenta del proveedor, su equipo, los pagos y el cambio de plan (5:30)

**Ventanas:** una normal, con https://shapi.localhost, https://envios.shapi.localhost y https://correo.shapi.localhost.

### 2.1 · Una proveedora nueva se registra y contrata su plan · 1:30
**Pantallas:** A1.1, A1.2, el correo, B1.4, A2.1, A2.2, A2.4, A2.3 y B1.3. **Datos:** Carmen (nueva) y las tarjetas 0002 y 4242.
1. https://shapi.localhost → **Crear cuenta** → **Registro (A1.1):** «Carmen Ixchop», carmen@cafedelvolcan.com, organización «Café del Volcán», `CafeVolcan2026` → **Crear cuenta** → «Revise su correo» (A1.2).
2. Correo → «Verifique su correo» → **Verificar mi correo** → entra al panel. *Qué decir:* «el correo se valida con un enlace de un solo uso que vence en 24 horas; la cuenta empieza en el plan Prueba, sin costo».
3. **Suscripción de plataforma (B1.4)** → **Cambiar de plan** → **Planes (A2.1)** → **Contratar** en **Lanzamiento** (Q 199).
4. **Contratación (A2.2):** **4000 0000 0000 0002**, «Carmen Ixchop», `12`, `28`, `123` → **Pagar Q 199.00 y activar el plan** → **Tarjeta rechazada (A2.4)**. *Qué decir:* «la pasarela es simulada; esta tarjeta simula fondos insuficientes y el plan no cambia».
5. **Usar otra tarjeta** → **4242 4242 4242 4242** → **Plan activado (A2.3)** → **Suscripción de plataforma**: Lanzamiento y Visa •••• 4242 → **Historial de pagos (B1.3)**. *Qué decir:* «Shapi no guarda el número, solo un token, la marca y los últimos cuatro dígitos; la renovación se cobra sola cada 30 días».

### 2.2 · Su equipo: miembros y roles · 0:45
**Pantallas:** A4.2, el correo y A8.2. **Datos:** Carmen, con el plan Lanzamiento del bloque 2.1 (3 miembros), y Tomás (nuevo).
1. Panel de Carmen → **Miembros y roles (A4.2):** «Su plan Lanzamiento permite 3 miembros» → **Invitar miembro:** tomas@cafedelvolcan.com, rol **Editor** → **Enviar invitación** → «Acción confirmada» y «tiene 2». *Qué decir:* «el propietario invita a su equipo; el editor configura APIs y planes, el lector solo consulta, y el cobro y los miembros son del propietario».
2. **Cerrar sesión** → correo de la invitación para tomas@cafedelvolcan.com → el enlace abre **Aceptar la invitación (A8.2):** «Tomás Chávez», `TomasChavez2026` → **Aceptar la invitación** → «Ya es parte de la organización» → **Entrar a Shapi** → entre con Tomás.
3. La barra lateral de Tomás no tiene «Miembros y roles», «Suscripción de plataforma» ni «Historial de pagos», y abajo dice **Editor**. *Qué decir:* «cada rol ve solo lo que puede hacer». → **Cerrar sesión.**

### 2.3 · Una consumidora con historia: recuperación, consumo y cambio de plan · 1:00
**Pantallas:** A5.7, A5.9, A5.10, B2.1, B2.2 y B2.7. **Datos:** María José, plan Comercio y Mastercard •••• 3057.
1. https://envios.shapi.localhost/entrar → **¿Olvidó su contraseña?** (A5.9) → mariajose@mercaditoantigua.com → **Enviar el enlace** → correo «Recupere su contraseña» → **Contraseña nueva (A5.10)** `Mercadito2026` → entra a su cuenta. *Qué decir:* «la respuesta es la misma aunque el correo no exista, y el enlace es de un solo uso».
2. **Consumo (B2.1):** el consumo por ruta del ciclo. **Pagos (B2.2):** su historial.
3. **Suscripción y claves** → **Cambiar de plan (B2.7)** → **Elegir Volumen**: crédito por los días de Comercio, cargo por los de Volumen y **A pagar hoy** → **Pagar Q 225.00 y cambiar de plan**. *Qué decir:* «al subir de plan se cobra solo la diferencia prorrateada; al bajar, el cambio se programa para la renovación».
4. **Cerrar sesión.**

### 2.4 · Código: puertos y adaptadores en los pagos · 2:15
Texto, archivos y orden de la pantalla en [elementos/emilio.md](elementos/emilio.md).

---

## Parte 3 · Dominique · Publicar una API y su portal de marca blanca (5:30)

**Ventanas:** una normal, con https://shapi.localhost, https://envios.shapi.localhost y https://correo.shapi.localhost.

### 3.1 · Ana publica una API · 1:30
**Pantallas:** A1.3, A3.1, A3.2, A3.3, A3.4, A3.5 y A4.1. **Datos:** Ana y «Envíos Xelajú» de la siembra; origen `http://origen-envios:8080`.
1. https://shapi.localhost/entrar → **Entrar** con Ana → **APIs (A3.1):** «API de Cotización de Envíos» (publicada) y «API de Recolecciones».
2. **Registrar una API (A3.2):** `Rastreo de Paquetes`, `http://origen-envios:8080`, subdominio `rastreo` → **Registrar y continuar** → «Conexión probada» y el **secreto de origen**, visible una sola vez. *Qué decir:* «Shapi prueba la conexión y no acepta direcciones de la red interna; lo explico en mi parte del código».
3. **Especificación (A3.3):** elija `origenes-demo/envios-xelaju/cotizacion-envios.yaml` → «Rutas encontradas 5» → **Continuar a la selección de rutas**.
4. **Rutas expuestas (A3.4):** marque `GET /rastreo` y `GET /cobertura` → **Guardar rutas expuestas**. *Qué decir:* «una ruta oculta no aparece en el portal y la compuerta la rechaza con 403».
5. **Configuración por ruta (A3.5):** `GET /rastreo` con límite **5** por minuto y caché de **1 minuto** → **Guardar configuración**.
6. **Planes (A4.1)** → **Crear un plan**: «Gratis», «Para probar la API sin costo», cuota 1000, límite 5; **Plan gratuito** ya viene marcado (no lo toque) → **Crear plan**.
7. **APIs** → **Publicar** en «Rastreo de Paquetes». *Qué decir:* «al publicar, la configuración se copia a Redis y la compuerta ya enruta la API».

### 3.2 · El portal: personalización, dominio propio, inicio y documentación · 0:45
**Pantallas:** A3.7, A3.6, A5.0 y A5.1. **Datos:** «API de Cotización de Envíos» y su dominio pendiente de la siembra.
1. Selector de API: **API de Cotización de Envíos** → **Portal (A3.7):** cambie el color principal y muestre la **vista previa en vivo**. Deje el color de la siembra antes de salir.
2. **Dominios (A3.6):** `api.enviosxelaju.localhost` **pendiente de verificación** con su CNAME → **Simular la creación del registro** → espere «El registro DNS simulado se creó» → **Verificar registro DNS** → **Verificado**. *Qué decir:* «el proveedor apunta su dominio a Shapi con un CNAME; aquí el DNS es simulado».
3. https://envios.shapi.localhost → **Inicio (A5.0)** con la marca de Envíos Xelajú, sin la de Shapi → **Documentación (A5.1)** → `POST /cotizaciones`, ya con la dirección del dominio propio.
4. *(Opcional)* https://agro.shapi.localhost (A5.5): el mismo portal con otra marca.

### 3.3 · Un consumidor nuevo: registro, contratación y claves · 1:00
**Pantallas:** A5.3, A5.8, A5.4, A5.6, A5.4b, B2.3, B2.4 y B2.5. **Datos:** Pedro (nuevo) y la tarjeta 4242.
1. Inicio del portal → **Crear cuenta (A5.3):** «Pedro Ajú», pedro@libreriaxela.com, «Librería Xela», `LibreriaXela2026` → correo «Verifique su correo», **de Envíos Xelajú** → el enlace (A5.8) abre **Planes (A5.4)**.
2. **Contratar** en **Básico** → **Pago (A5.6):** 4242 4242 4242 4242, «Pedro Ajú», `12/28`, `123` → **Pagar Q 149.00 y contratar el plan** → **Plan contratado (A5.4b):** sus dos claves, una sola vez. *Qué decir:* «el portal no muestra la marca de Shapi en ninguna pantalla, ni en los correos».
3. **Suscripción y claves (B2.3)** → **Rotar** en la clave de pruebas → la confirmación (B2.4) dice hasta cuándo funciona la anterior → **Rotar clave** → la nueva, una sola vez (B2.5) → **Ya la guardé**: la anterior aparece como **Rotada**.

### 3.4 · Código: método de fábrica al validar el origen (SSRF) · 2:15
Texto, archivos y orden de la pantalla en [elementos/dominique.md](elementos/dominique.md).

---

## Parte 4 · José Pablo · Soporte, administración, cuentas y el cierre (5:30)

**Ventanas:** una normal, con https://shapi.localhost y https://correo.shapi.localhost, y una de incógnito para Sofía y Rodrigo.

### 4.1 · Soporte · 1:00
**Pantallas:** A7.1, A6.4, A6.4b, el correo y A7.2. **Datos:** casos CAS-100 y CAS-104 de la siembra; Ana y Sofía.
1. Ventana normal → **Entrar** con Ana → **Casos de soporte (A7.1)** → «Abrir un caso»: «¿Cómo cambio el color de mi portal?», API «API de Cotización de Envíos» y una descripción → **Abrir caso**: aparece en la lista.
2. Incógnito → **Entrar** con Sofía → **Casos (A6.4)** → **Abrir** el caso nuevo (A6.4b): la conversación y los datos de la organización en solo lectura → **Asignarme el caso** → una respuesta → **Enviar respuesta**.
3. Correo: «Nuevo caso» para Sofía y «Nueva respuesta en su caso» para Ana. Ventana normal: recargue el caso; Ana ve la respuesta (A7.2). *Qué decir:* «el correo se guarda junto con la respuesta y el trabajador lo envía después; lo explico en mi parte del código».
4. **Cerrar sesión** de Sofía (incógnito) y de Ana (ventana normal).

### 4.2 · Administración, estado y bitácora · 1:15
**Pantallas:** A6.2, A6.2b, A6.1, A6.3, A6.3b, B3.1 y B3.2. **Datos:** organizaciones de la siembra (Transportes Petén en gracia, Datos Chapines suspendida) y Agro Precios.
1. Incógnito → **Entrar** con Rodrigo → **Organizaciones (A6.2):** plan, ciclo y estado → **Suspender** en **Agro Precios** (A6.2b): APIs y consumidores afectados y un motivo → **Suspender organización** → **Reactivar**. *Qué decir:* «mientras está suspendida, la compuerta responde 403 a todas sus APIs».
2. **Planes de plataforma (A6.1).** **Pagos (A6.3):** **Revertir** en un pago (A6.3b): la advertencia → **Cancelar**.
3. **Estado de los componentes (B3.1):** los siete componentes y el p95 de la compuerta. **Bitácora (B3.2):** las acciones de hoy, como la reactivación y la suspensión de Agro Precios y el caso.

### 4.3 · Cuentas de administración y soporte · 0:45
**Pantallas:** A6.5, el correo y A6.5b. **Datos:** Rodrigo, Julio Estrada (desactivado) de la siembra y Mónica (nueva).
1. Incógnito (Rodrigo) → **Cuentas de plataforma (A6.5):** administradores y soporte; Julio Estrada, **desactivado** → **Crear cuenta:** «Mónica Tuyuc», monica.tuyuc@shapi.localhost, rol **Soporte** → **Crear cuenta**. *Qué decir:* «el administrador crea las cuentas del personal; la persona define su contraseña con un enlace de un solo uso».
2. Correo: el mensaje para monica.tuyuc@shapi.localhost → abra el enlace en la ventana normal, donde Ana ya cerró sesión → **Contraseña nueva** (A6.5b): `MonicaTuyuc2026` → **Guardar la contraseña** → Mónica entra a **Casos**, con el menú de soporte.

### 4.4 · Código: bandeja de salida transaccional del correo · 2:15
Texto, archivos y orden de la pantalla en [elementos/jose-pablo.md](elementos/jose-pablo.md).

### 4.5 · Cierre · 0:15
**Pantalla:** https://shapi.localhost.
- *Qué decir:* «Eso es Shapi: publicar una API, protegerla y cobrar por ella, todo funcionando en un ambiente productivo simulado que se levanta con un solo comando, y con pruebas automáticas en cada cambio. Gracias.»

---

## Los elementos de código

Cada elemento dura 2:15 y va después de la funcionalidad de su autor; en la parte 4, el cierre va después del elemento. Se muestra en el editor: el código y las pruebas, sin ejecutarlas en la terminal. Su narración tiene entre 285 y 340 palabras, unos 2 minutos a ritmo de presentación; si una toma pasa de 2:30, se acorta su penúltimo párrafo. Las líneas citadas se comprobaron contra `main` el 30 de octubre.

| Bloque | Integrante | Elemento | Archivo |
|---|---|---|---|
| 1.5 | Jordin | Cadena de responsabilidad en la tubería de la compuerta (RNF-13) | [elementos/jordin.md](elementos/jordin.md) |
| 2.4 | Emilio | Puertos y adaptadores: `IPasarelaPagos` y `PasarelaSimulada` (ADR-12 y ADR-13) | [elementos/emilio.md](elementos/emilio.md) |
| 3.4 | Dominique | Método de fábrica con objeto resultado en `ValidadorDireccionOrigen` (RNF-10) | [elementos/dominique.md](elementos/dominique.md) |
| 4.4 | José Pablo | Bandeja de salida transaccional del correo (RF-46, ADR-27) | [elementos/jose-pablo.md](elementos/jose-pablo.md) |

---

## Cobertura de la funcionalidad

| Lineamiento o requisito | Bloque |
|---|---|
| Lineamientos 4.1 · Registro y validación de correo (RF-01, RF-02, RF-05) | 2.1 (proveedor), 1.4 y 3.3 (consumidores) |
| Lineamientos 4.1 · Recuperación de contraseña (RF-03) | 2.3 |
| Lineamientos 4.1 · Autenticación segura y sesiones (RF-04) | 1.3, 2.1, 2.2, 2.3, 3.1, 4.1, 4.2 y 4.3 (entrar y cerrar sesión) |
| Lineamientos 4.1 · Roles: administrador, cliente y soporte (RF-06, 04 §2) | 2.2 (propietario y editor, A4.2 y A8.2), 4.1 (soporte), 4.3 (administrador y soporte, A6.5 y A6.5b) |
| Lineamientos 4.2 · Planes con nombre, descripción, precio y vigencia; visualización de planes (RF-17, RF-18, RF-19) | 1.1 (A0.1), 2.1 (A2.1), 3.1 (A4.1), 1.4 y 3.3 (A5.4), 4.2 (A6.1) |
| Lineamientos 4.2 · Contratación de servicios (RF-20) | 2.1 (plataforma), 1.4 y 3.3 (API) |
| Lineamientos 4.2 · Historial de pagos (RF-21, RF-24) | 2.1 (B1.3), 2.3 (B2.2), 4.2 (A6.3) |
| Lineamientos 4.2 · Renovación, gracia, suspensión y reactivación (RF-22, RF-23) | 1.4 |
| Cambio de plan con prorrateo (RF-25) | 2.3 (B2.7) |
| Lineamientos 4.3 (opción C) · Crear entornos: publicar una API con su especificación, rutas y configuración (RF-08, RF-09, RF-10, RF-13, RF-14) | 3.1 |
| Lineamientos 4.3 · Gestión de dominios simulados (RF-11, RF-12) | 3.2 (A3.6) |
| Personalización del portal de marca blanca (RF-15, RF-16) | 3.2 (A3.7, A5.0, A5.1, A5.5) |
| Claves: emisión, rotación y revocación (RF-26, RF-27, RF-28) | 1.3 (A4.3), 3.3 (A5.4b, B2.4 y B2.5) |
| Compuerta: 200, 401, 403 y 429 (RF-29, RF-30, RF-31, RF-45) | 1.2 (200, 401 y 429), 1.4 (403) |
| Lineamientos 4.3 · Monitorear consumo: consumo y facturación (RF-35, RF-36, RF-37) | 1.3 (B1.1 y B1.2), 2.3 (B2.1) |
| Invitar consumidores (RF-05) | 1.3 (B1.5) |
| Administración de organizaciones (RF-38) | 4.2 (A6.2 y A6.2b) |
| Estado de los componentes (RF-39) | 4.2 (B3.1) |
| Soporte (RF-40) y correo con reintentos (RF-46) | 4.1, 4.4 |
| Bitácora de acciones sensibles (RF-41) | 4.2 (B3.2) |
| Cuentas de administración y soporte (RF-42) | 4.3 (A6.5 y A6.5b) |
| Protección del origen contra SSRF (RNF-10) | 3.1 (A3.2), 3.4 |
| Agregar una regla a la compuerta sin tocar las demás (RNF-13) | 1.5 |
| Lineamientos (Avance 3) · 4 elementos de código con Clean Code y patrones | 1.5, 2.4, 3.4 y 4.4 |

## Qué evitar

- **Repetir un bloque de la parte 1 después del 1.4:** el reloj ya está adelantado y no se puede retroceder.
- **Registrar a la misma persona nueva dos veces:** la segunda vez da «ya existe». Use otro correo (agregue un 2) o vuelva a sembrar.
- **Entrar con una contraseña incorrecta cinco veces:** bloquea la cuenta 15 minutos.
- **Más de 10 inicios de sesión o registros por minuto desde el mismo equipo:** el límite de las pantallas de acceso responde «demasiados intentos»; espere un minuto.

## Si algo falla

- **Una página da error:** pause la grabación, revise y repita el bloque.
- **No llega un correo:** espere unos 5 segundos (el trabajador revisa la bandeja cada 5 s) y recargue el buzón.
- **Se cerró una sesión:** vuelva a entrar; lo hecho se conserva.
- **B3.1 marca la compuerta como degradada:** el umbral es un p95 de 15 ms en la última hora (06 §8). En el ensayo, después de siete corridas seguidas en una PC de dos núcleos, llegó a 38 ms. Ejecute otra vez `node infra/verificar.mjs --demo` y espere 30 segundos. Si sigue degradada, no lo oculte: «B3.1 detecta que la compuerta pasa de 15 ms en este equipo».

### Repetir una toma

- **Partes 2, 3 y 4, y los bloques 1.1 a 1.3:** vuelva a sembrar sin reconstruir:
  ```powershell
  docker compose --env-file .env -f infra/compose.yml -f infra/compose.prod.yml exec trabajador dotnet Shapi.Trabajador.dll sembrar-demo --reiniciar
  ```
  Borra las organizaciones y los consumidores que no son de la siembra (Café del Volcán, Lucía, Pedro, la API «Rastreo de Paquetes») y deja los datos sembrados como al principio. Conserva el personal y la bitácora, que es solo de inserción: si repite 4.3, cree la cuenta con otro correo (por ejemplo, monica.tuyuc2@shapi.localhost), y si al repetir 2.2 el correo de Tomás ya existe, use otro. Después, `node infra/verificar.mjs --demo`.
- **Bloque 1.4, o si ya se adelantó el reloj:** empiece desde cero, con los pasos 1 a 3 de «Antes de grabar».

---

## Ensayo del 30 de octubre

El guion se ensayó completo, solo por la interfaz, en un ambiente productivo simulado recién levantado y sembrado (proyecto `shapi-jg29`, imágenes construidas desde `main` en `8dc94e0`, `node infra/verificar.mjs --demo` en verde). Lo recorrió un navegador automatizado (Playwright, 1366 × 768) que seguía los pasos de cada bloque. El tiempo medido es el de la interfaz, sin escribir a mano ni narrar. **Cada integrante cronometra su primera toma real** y, si pasa de lo planeado, aplica el orden de recortes del inicio.

El ensayo se hizo antes de reorganizar el guion por integrante. Las pantallas y los botones son los mismos, pero algunos pasos ahora usan otros datos y **no se ensayaron con ellos**; se revisaron en el código y en la siembra: 1.2 usa la clave de pruebas de la siembra y entra sin sesión; 3.3 registra a Pedro, contrata con 4242 y rota su clave; 4.1 abre el caso sobre «API de Cotización de Envíos»; 4.2 revierte, y cancela, un pago de la siembra. La primera toma de esos bloques sirve de ensayo.

| Bloque actual | Interfaz medida | Resultado del ensayo |
|---|---|---|
| 1.1 | 2 s | Correcto: los cuatro planes |
| 1.2 | 9 s | Con la clave de Lucía y su sesión: 200 con `X-RateLimit-*` y `X-Cuota-*`; 401 `clave_invalida`; 429 `limite_por_minuto` en el décimo envío, con `Retry-After` |
| 1.3 | 8 s | Correcto: claves rotada y revocada; consumo; consumidores (el mismo recorrido incluía A4.2 de Ana) |
| 1.4 | 25 s más el registro (unos 8 s) | Contratación con 0341; gracia visible 2,5 s después de +30 días; suspensión 0,6 s después de +7; 403 `suscripcion_inactiva`; pago 200 en 1,5 s y consola 200 |
| 2.1 | 16 s | Correcto: rechazo con 0002, activación con 4242 y Visa •••• 4242 en B1.4 |
| 2.2 | 14 s | Ensayado el 30 oct, después de reorganizar, con una proveedora en Lanzamiento creada por el recorrido: invitación como Editor, «tiene 2», aceptación desde el correo, «Ya es parte de la organización» y entrada como Editor, sin «Miembros y roles» ni «Suscripción de plataforma» en la barra |
| 2.3 | 15 s | Correcto: recuperación, Mastercard •••• 3057 y «Pagar Q 225.00 y cambiar de plan» |
| 3.1 | 9 s | Correcto. «Plan gratuito» viene marcado; la pantalla dice «Rutas encontradas 5» |
| 3.2 | 9 s | Correcto: la siembra trae el dominio pendiente; A5.1 muestra `https://api.enviosxelaju.localhost/cotizaciones` |
| 3.3 | 15 s | Con otros datos: registro, verificación y contratación de Lucía con 0341 y rotación de la clave de pruebas de María José. Con Pedro y 4242 no se ensayó; la contratación con 4242 la cubren las pruebas E2E |
| 4.1 | 15 s | Con el caso sobre «Rastreo de Paquetes»: aparece sin recargar; llegan «Nuevo caso» y «Nueva respuesta en su caso» |
| 4.2 | 14 s | Con el pago de Carmen en A6.3; incluía A6.5, que ahora va en 4.3. B3.1 marcó la compuerta degradada (p95 de 38 ms tras siete corridas; ver «Si algo falla») |
| 4.3 | 10 s | Ensayado el 30 oct, después de reorganizar: cuenta de soporte creada por Rodrigo, contraseña definida desde el correo y entrada directa a «Casos» |

También se comprobó «Repetir una toma»: `sembrar-demo --reiniciar` tardó 16 s, borró las organizaciones y los consumidores nuevos y las APIs «rastreo», y conservó a María José.

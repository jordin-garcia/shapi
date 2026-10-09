# Calendario y entregas

**Supuestos:** cada integrante dedica unas **3 horas por semana**, repartidas en 2 o 3 sesiones con su agente. Cada tarea está pensada para caber en **una sesión** del agente, de 45 a 90 minutos, que corre sola mientras la persona hace otra cosa. Eso da de 3 a 4 tareas por persona y por semana.

| Fecha | Hito | Qué se entrega |
|---|---|---|
| **Jue 24 sep** | Fin del Avance 1 | Tareas del Avance 1 integradas en `main` |
| **Vie 25 sep** | **Avance funcional 1 (30 %)** | Demostración en vivo (guion 1) |
| **Jue 8 oct** | Convergencia (JG-08) | Informe de brechas y tareas nuevas |
| **Vie 9 oct** | **Avance funcional 2 (50 %)** | Demostración en vivo (guion 2) |
| **Jue 22 oct** | Convergencia (JG-14) | Informe de brechas y tareas nuevas |
| **Vie 23 oct** | **Avance funcional 3 (80 %)** | Demostración en vivo (guion 3) |
| **Jue 29 oct** | Convergencia final (JG-17) | Lista de lo que queda fuera |
| **Vie 30 oct** | **Congelamiento del código** | Después de esta fecha solo se corrigen errores |
| **Lun 2 nov** | Documentación en PDF | Documento de requisitos, documento de diseño, manual técnico y manual de usuario (JG-16, JZ-15, JZ-16) |
| **Mar 3 nov** | Ensayo de la exposición (las cuatro personas) | Recorrido completo del guion final |
| **Semana del 2 al 6 de nov** | **Entrega final y exposición** | Repositorio, PDF y sistema funcionando |

> La docente no ha fijado el día exacto de la exposición. Si cae el lunes 2 o el martes 3, adelantar la documentación al viernes 30 y el ensayo al sábado 31.

---

## Calendario por día

Cada tarea pendiente tiene en su archivo el campo `programada: AAAA-MM-DD`: el día en que el calendario espera que se integre. Es la única fuente de las fechas; la tabla de abajo se genera a partir de ellas.

- **Es una meta, no una restricción.** Si una tarea ya está disponible, se puede adelantar. Lo que sí se respeta siempre son `depende_de` y `no_antes_de`.
- **Qué me toca hoy:** `node scripts/tareas.mjs --hoy <persona>` muestra la tarea del día, las atrasadas (con quién las espera) y la siguiente. `--persona` y `--siguiente` ordenan por esta fecha, así que "continúa" toma la que dice el calendario.
- **Aviso diario:** a las 07:00 (Guatemala), el issue "Tablero del plan" menciona a quien tiene una tarea programada ese día o una atrasada. A quien no tiene nada, no lo menciona.
- **Una tarea atrasada** es la que no está hecha y su fecha ya pasó. Se marca con ⏰.
- **Reprogramar:** solo Jordin, con un PR que cambie `programada` en los archivos de las tareas y ejecute `node scripts/tareas.mjs --calendario --escribir`. La CI (`--validar`) rechaza una fecha anterior a la de una dependencia pendiente o a `no_antes_de`, y una tabla que no coincida con los archivos.
- Los días de convergencia (jueves 8 y 22) los demás ensayan el guion y corrigen errores; los días de entrega son los del cuadro de arriba.
- Las tablas de cada avance, más abajo, dicen a qué entrega pertenece cada tarea. El día exacto, incluidas las que se adelantan a un avance anterior para repartir la carga (JG-09, EM-09, EM-12, DC-09, JZ-10, JZ-11 y JZ-14), es el de esta tabla.

<!-- calendario:inicio (lo genera node scripts/tareas.mjs --calendario --escribir; no lo edites a mano) -->

| Día | Jordin | Emilio | Dominique | José Pablo |
|---|---|---|---|---|
| Lun 28 sep | **JG-04** Publicador de configuración en Redis y resincronización | **EM-04** Recuperación de contraseña y Mi perfil (A1.4a, A1.4b y A8.1) | **DC-03** Estructura del portal de marca blanca | — |
| Mar 29 sep | **JG-07** Servicio de claves: emisión, rotación y revocación (backend) | — | **DC-04** Registrar una API y lista de APIs (A3.1 y A3.2) | **JZ-06** Imágenes Docker, ambiente productivo simulado y publicación en GHCR |
| Mié 30 sep | **JG-05** Compuerta: organización, suscripción, ruta, secreto, SSRF y CORS | **EM-05** Identidad del consumidor (backend del portal) | — | — |
| Jue 1 oct | — | **EM-07** Planes de API (A4.1) | **DC-05** Especificación OpenAPI y rutas expuestas (A3.3 y A3.4) | **JZ-07** Pruebas de extremo a extremo y herramienta de capturas |
| Vie 2 oct | **JG-06** Compuerta: límites por minuto, cuotas y cabeceras (Lua) | **EM-18** Publicar el destino de la sesión del consumidor | **DC-08** Pantallas de acceso del consumidor (A5.3, A5.3b y A5.7 a A5.10) | — |
| Sáb 3 oct | **JG-18** Optimización de las pruebas y la CI | **EM-08** Contratación de un plan de API (backend) | — | **JZ-05** Siembra de demostración |
| Dom 4 oct | **JG-09** Medición en la compuerta y consolidación del consumo | — | **DC-06** Configuración por ruta y publicación (A3.5) | — |
| Lun 5 oct | — | **EM-09** Suscripción de plataforma: contratar y cambiar de plan (A2 y B1.4) | — | **JZ-11** Plantillas de correo completas |
| Mar 6 oct | — | — | **DC-07** Portal público: inicio y documentación (A5.0, A5.1 y A5.5) | — |
| Mié 7 oct | — | **EM-12** Miembros e invitaciones (A4.2 y A8.2) | **DC-09** Planes y contratación en el portal (A5.4, A5.6 y A5.4b) | **JZ-10** Casos de soporte (A6.4, A6.4b, A7.1 y A7.2) |
| Jue 8 oct | **JG-08** Convergencia del Avance 2 | — | — | **JZ-17** Siembra de demostración en el ambiente productivo simulado |
| Sáb 10 oct | **JG-11** Consumo, latencia y errores por API (B1.1) | **EM-10** Cierre de ciclo: renovación, gracia, suspensión y fin de la Prueba | **DC-11** Suscripción y claves del consumidor (B2.3 a B2.6) | **JZ-08** Administración de organizaciones (A6.2 y A6.2b) |
| Dom 11 oct | — | — | — | **JZ-18** Levantar el ambiente con la siembra en un solo comando (RNF-14) |
| Lun 12 oct | **JG-10** Pantalla de claves del proveedor (A4.3 y A4.3b) | **EM-11** Historial de pagos (B1.3 y la API de B2.2) | **DC-10** Consola de pruebas del portal (A5.2) | **JZ-09** Cuentas de administración y soporte (A6.5) |
| Mar 13 oct | — | **EM-19** Pruebas de backend del acceso del consumidor | **DC-17** Barra lateral del portal en planes y pago, y texto de A5.3 | — |
| Mié 14 oct | **JG-12** Consumo del ciclo para el consumidor (B2.1) | **EM-13** Límites del plan de plataforma y cambio de plan del consumidor | **DC-14** Dominio propio, DNS simulado y secreto de origen (A3.6) | **JZ-12** Estado de los componentes (B3.1) |
| Jue 15 oct | **JG-20** Ajustes de interfaz del ensayo del Avance 2 (tarjeta, barra lateral, cursor y contraseña) | — | — | — |
| Vie 16 oct | **JG-13** Consumo y facturación por consumidor (B1.2) | **EM-14** Administración: planes de plataforma y pagos (A6.1 y A6.3) | **DC-13** Pagos y cambio de plan del consumidor (B2.2 y B2.7) | — |
| Sáb 17 oct | — | **EM-20** Medio de pago en la confirmación de la contratación (A5.4b) | — | **JZ-14** Pruebas de carga (RNF-01 y RNF-03) |
| Dom 18 oct | **JG-19** Prueba de la compuerta sin PostgreSQL, API de control ni trabajador (RNF-02 y RNF-04) | **EM-15** Invitar consumidores (B1.5) | **DC-15** Sitio público: inicio de Shapi (A0.1) | — |
| Mar 20 oct | — | — | **DC-12** Personalización del portal (A3.7) | — |
| Jue 22 oct | **JG-14** Convergencia del Avance 3 | — | — | — |
| Sáb 24 oct | **JG-16** Generador de PDF y documentos de requisitos y de diseño | **EM-16** Pruebas de aislamiento entre organizaciones y de permisos | **DC-16** Revisión visual contra los mockups | **JZ-13** Pruebas E2E de los flujos principales |
| Lun 26 oct | **JG-15** Caché de respuestas en la compuerta | — | — | **JZ-15** Manual técnico |
| Mar 27 oct | — | — | — | **JZ-16** Manual de usuario |
| Jue 29 oct | **JG-17** Convergencia final y congelamiento del código | — | — | — |

<!-- calendario:fin -->

---

## Avance 1 · 23 al 25 de septiembre · "El esqueleto funciona"

| Tarea | Persona | Qué deja listo |
|---|---|---|
| JG-01 | Jordin | Solución .NET, CI, protección de `main` e invitación a Emilio. **Es la primera: desbloquea a todos. Hoy mismo** |
| JZ-01 | José Pablo | Docker Compose con PostgreSQL, Redis, Mailpit y Caddy con HTTPS en `*.shapi.localhost` |
| DC-01 | Dominique | Workspace del frontend y sistema de diseño (variante 4) |
| EM-01 | Emilio | Esquema completo de la base de datos y datos base |
| JZ-02 | José Pablo | Orígenes de demostración (Envíos Xelajú y Agro Precios) |
| DC-02 | Dominique | Estructura del panel, barra lateral y todas las rutas |
| EM-02 | Emilio | Registro, verificación e inicio de sesión (backend) |
| JG-02 | Jordin | Compuerta mínima: host → API, clave y reenvío |
| JZ-03 | José Pablo | Envío de correos a Mailpit |
| EM-03 | Emilio | Pantallas de registro, verificación y acceso |
| EM-17 | Emilio | Publicar el contrato OpenAPI de identidad y reemplazar el contrato provisional de la sesión |

**Guion de demostración 1:**
1. `docker compose --env-file .env -f infra/compose.yml up -d` y los tres procesos .NET en marcha. Se muestra https://shapi.localhost con candado.
2. Registro de un proveedor en A1.1, el correo de verificación en https://correo.shapi.localhost, el enlace abierto y el inicio de sesión en A1.3.
3. El panel vacío, con la barra lateral de la variante 4.
4. `curl` con una clave de demostración a `https://envios.api.shapi.localhost/cotizaciones` → responde el origen. Con una clave inválida → 401 JSON. Con un host desconocido → 404.
5. La CI en verde en GitHub y el plan de tareas (`node scripts/tareas.mjs`).

**Si no alcanza el tiempo:** lo mínimo es JG-01, JZ-01, DC-01, DC-02, EM-01 y EM-02. Se muestra el registro por API (con Swagger o `curl`) y el panel con datos simulados.

---

## Avance 2 · 26 de septiembre al 9 de octubre · "Publicar y proteger una API"

| Persona | Tareas |
|---|---|
| Jordin | JG-03 (revisión con Claude y tablero del plan), JG-04 (publicación en Redis), JG-05 (filtros de la compuerta), JG-06 (límites y cuotas), JG-07 (claves), JG-08 (convergencia, jueves 8) |
| Emilio | EM-04 (recuperación y perfil), EM-05 (identidad del consumidor), EM-06 (pasarela simulada), EM-07 (planes de API), EM-08 (contratación de un plan de API) |
| Dominique | DC-03 (estructura del portal), DC-04 (registrar una API), DC-05 (especificación y rutas), DC-06 (configuración y publicación), DC-07 (portal público), DC-08 (acceso del consumidor) |
| José Pablo | JZ-04 (bitácora), JZ-05 (siembra de demostración), JZ-06 (imágenes y ambiente productivo), JZ-07 (E2E y capturas), JZ-17 (siembra en el ambiente productivo, creada en JG-08) |

**Guion de demostración 2:**
1. Ambiente productivo simulado (`compose.prod.yml`) con la siembra de demostración.
2. Ana (proveedora) registra una API, sube su especificación OpenAPI, expone rutas, las configura, crea un plan y la publica.
3. El portal `https://envios.shapi.localhost` muestra el inicio y la documentación con la marca de Envíos Xelajú.
4. María José (consumidora) se registra en el portal y verifica su correo.
5. Con una clave de la siembra: petición válida → 200, con las cabeceras `X-RateLimit-*` y `X-Cuota-*`. Al superar el límite por minuto → 429. Con una ruta oculta → 403. Con una clave revocada → 401.
6. Recuperación de contraseña.

---

## Avance 3 · 10 al 23 de octubre · "Cobrar y administrar"

| Persona | Tareas |
|---|---|
| Jordin | JG-09 (medición y consolidación), JG-10 (claves en el panel), JG-11 (consumo B1.1), JG-12 (consumo B2.1), JG-13 (consumo por consumidor), JG-19 (compuerta sin PostgreSQL), JG-20 (ajustes de interfaz del ensayo), JG-14 (convergencia, jueves 22) |
| Emilio | EM-09 (suscripción de plataforma), EM-10 (renovación, gracia y suspensión), EM-11 (historial de pagos), EM-12 (miembros), EM-13 (límites y cambio de plan del consumidor), EM-14 (administración de planes y pagos), EM-15 (invitar consumidores), EM-19 (pruebas del acceso del consumidor), EM-20 (medio de pago en A5.4b, P3) |
| Dominique | DC-09 (contratación en el portal), DC-10 (consola de pruebas), DC-11 (suscripción y claves del consumidor), DC-12 (personalización del portal), DC-13 (pagos y cambio de plan del consumidor), DC-14 (dominio propio), DC-15 (inicio de Shapi), DC-17 (barra lateral en planes y pago) |
| José Pablo | JZ-08 (organizaciones), JZ-09 (cuentas de plataforma), JZ-10 (casos de soporte), JZ-11 (plantillas de correo), JZ-12 (estado de los componentes), JZ-18 (levantar y sembrar con un comando) |

**Guion de demostración 3:**
1. María José contrata el plan Comercio con la tarjeta 4242…, recibe sus claves una sola vez, prueba la consola, consume la API y ve su consumo por ruta.
2. Rota una clave: la anterior sigue funcionando.
3. Ana ve el consumo con el p95 y los errores por código, la facturación por consumidor y sube su plan de plataforma con prorrateo.
4. Modo demostración: se adelanta el reloj 30 días. Con la tarjeta 4000 0000 0000 0341 → gracia → suspensión → la compuerta responde 403 → se paga → se reactiva.
5. El administrador suspende una organización y revierte un pago. El soporte atiende un caso. Se revisan la bitácora y el estado de los componentes.

---

## Semana final · 24 al 30 de octubre · "Pulir, probar y documentar"

| Persona | Tareas |
|---|---|
| Jordin | JG-15 (caché, P3), JG-16 (PDF de requisitos y diseño), JG-17 (convergencia final y congelamiento, jueves 29) |
| Emilio | EM-16 (pruebas de aislamiento y permisos) |
| Dominique | DC-16 (revisión visual contra los mockups) |
| José Pablo | JZ-13 (E2E de los flujos principales), JZ-14 (pruebas de carga), JZ-15 (manual técnico), JZ-16 (manual de usuario) |

**Guion final (exposición):** el guion 3 completo, más un arranque limpio del ambiente productivo simulado, las pruebas E2E en verde, el resultado de las pruebas de carga (RNF-01 y RNF-03) y la defensa de la arquitectura con `docs/specs/12-decisiones.md`.

---

## Qué hacer si vamos atrasados

1. Las tareas **P3** son las primeras en quedar fuera. Después, las **P2** del avance en curso, que pasan a la semana final.
2. **Las P1 no se recortan**: cubren el mínimo de cada lineamiento obligatorio (`docs/specs/03-requisitos.md` §3). Estas P2 completan requisitos de esa tabla de trazabilidad, así que, si hay que recortarlas, Jordin lo decide y lo anota en la bitácora:
   - EM-12: RF-06 (miembros con rol);
   - DC-14: RF-11 (hosts con TLS automático, en A3.6) y RF-12 (dominio propio);
   - JG-13: RF-36 (consumo por consumidor);
   - JZ-12: RF-39 (estado de los componentes).

   EM-12 y EM-13 cubren además RF-43 (límites del plan de plataforma), que no está en esa tabla.
3. En cada convergencia (JG-08, JG-14, JG-17), Jordin revisa la carga de cada persona, reprioriza y reprograma las fechas (`programada`) de lo atrasado. Si una tarea atrasada detiene a otras, puede reprogramarla antes, en cualquier día.
4. Si alguien no va a poder avanzar en una semana, avisa al grupo. Jordin puede reasignar una tarea mediante un PR que cambie en su archivo `persona` y `responsable` y agregue la línea `reasignada: si`. El ID **no cambia**, para no romper las dependencias.

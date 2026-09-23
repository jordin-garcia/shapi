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

**Guion de demostración 1:**
1. `docker compose -f infra/compose.yml up -d` y los tres procesos .NET en marcha. Se muestra https://shapi.localhost con candado.
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
| José Pablo | JZ-04 (bitácora), JZ-05 (siembra de demostración), JZ-06 (imágenes y ambiente productivo), JZ-07 (E2E y capturas) |

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
| Jordin | JG-09 (medición y consolidación), JG-10 (claves en el panel), JG-11 (consumo B1.1), JG-12 (consumo B2.1), JG-13 (consumo por consumidor), JG-14 (convergencia, jueves 22) |
| Emilio | EM-09 (suscripción de plataforma), EM-10 (renovación, gracia y suspensión), EM-11 (historial de pagos), EM-12 (miembros), EM-13 (límites y cambio de plan del consumidor), EM-14 (administración de planes y pagos), EM-15 (invitar consumidores) |
| Dominique | DC-09 (contratación en el portal), DC-10 (consola de pruebas), DC-11 (suscripción y claves del consumidor), DC-12 (personalización del portal), DC-13 (pagos y cambio de plan del consumidor), DC-14 (dominio propio), DC-15 (inicio de Shapi) |
| José Pablo | JZ-08 (organizaciones), JZ-09 (cuentas de plataforma), JZ-10 (casos de soporte), JZ-11 (plantillas de correo), JZ-12 (estado de los componentes) |

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
2. **Las P1 no se recortan**: cubren los lineamientos obligatorios.
3. En cada convergencia (JG-08, JG-14, JG-17), Jordin revisa la carga de cada persona y reprioriza.
4. Si alguien no va a poder avanzar en una semana, avisa al grupo. Jordin puede reasignar una tarea mediante un PR que cambie en su archivo `persona` y `responsable` y agregue la línea `reasignada: si`. El ID **no cambia**, para no romper las dependencias.

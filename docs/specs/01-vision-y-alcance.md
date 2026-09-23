# 01 · Visión y alcance

> Fuente de verdad del proyecto. Sustituye a la propuesta y al documento de requisitos y diseño que se entregaron a la docente (28 de agosto y 11 de septiembre de 2026). Las reglas del curso están en [`../lineamientos.md`](../lineamientos.md).

## 1. Introducción

Shapi es una plataforma como servicio que permite a una empresa **publicar su API, controlar quién la usa y cobrar por ella, sin modificar el servidor que ya tiene**. La empresa conecta su servidor, sube su especificación OpenAPI y define planes. Shapi le crea un portal con la marca de esa empresa, emite las claves de acceso, aplica las cuotas en cada petición y registra el consumo y los pagos.

Proyecto final de Ingeniería de Software I, Universidad Rafael Landívar, campus de Quetzaltenango, 2026.

| Integrante | Carné | Responsabilidad |
|---|---|---|
| Héctor Jordin Adolfo García Coyoy | 2427124 | Coordinación y compuerta de tráfico |
| Ronaldo Emilio Méndez Mayorga | 1563224 | Usuarios, planes, suscripciones y pagos |
| Dominique Guillermo Contreras Sierra | 1508224 | Interfaz de usuario y portales de marca blanca |
| José Pablo Zúñiga de León | 1507524 | Infraestructura, despliegue, pruebas y documentación |

## 1.1 Objetivos

**Objetivo general:** diseñar, desarrollar, documentar e implementar una plataforma como servicio que permita a una empresa publicar su API, administrar a sus consumidores mediante planes y claves de acceso, y cobrar por su uso desde un portal con su propia marca.

**Objetivos específicos:**
1. Levantar los requisitos funcionales y no funcionales del sistema y modelar los casos de uso de sus actores: proveedor, consumidor, administrador, soporte y sistema ([03](03-requisitos.md), [05](05-casos-de-uso.md)).
2. Diseñar la arquitectura, el modelo entidad-relación y la base de datos, y justificar cada decisión ([06](06-arquitectura.md), [07](07-modelo-de-datos.md), [12](12-decisiones.md)).
3. Implementar la gestión de usuarios: registro, verificación de correo, recuperación de contraseña, autenticación con sesiones y control de roles, tanto para el personal como para los consumidores.
4. Desarrollar la compuerta de tráfico: validar claves, aplicar límites por minuto y cuotas, y medir el consumo de cada consumidor ([08](08-compuerta.md)).
5. Implementar las suscripciones y los pagos: ver planes, contratar, historial de pagos, renovación automática, gracia y suspensión, con una pasarela simulada ([09](09-cobros-y-suscripciones.md)).
6. Generar automáticamente, a partir de la especificación OpenAPI de cada proveedor, un portal de marca blanca con documentación y consola de pruebas.
7. Desplegar el sistema en un ambiente productivo simulado y reproducible, y elaborar el manual técnico y el manual de usuario.

## 2. Categoría del enunciado

De las tres categorías del enunciado se eligió **Servidores Web como Servicio**. La propuesta ya fue **aprobada por la docente**.

El recurso que se contrata es un **entorno web gestionado por API**, que Shapi crea, configura y monitorea desde una interfaz web. Cada entorno tiene:

- un subdominio para el portal y otro para la API, ambos con certificado TLS;
- el enrutamiento hacia el servidor de origen, con las rutas que el proveedor decide exponer;
- un portal web alojado por la plataforma;
- el monitoreo de peticiones, latencia, errores y datos transferidos.

| Lo que exige el enunciado (opción C) | Cómo lo cumple Shapi |
|---|---|
| Crear entornos web | Cada API registrada crea un entorno con subdominio, certificado, rutas y portal ([RF-08] a [RF-11]) |
| Gestionar configuraciones | Origen, rutas, límites, caché, planes, claves, marca, dominio propio y secreto de origen |
| Gestión de dominios simulados | Subdominios bajo `shapi.localhost` y dominios propios verificados con un DNS simulado ([06 · Arquitectura](06-arquitectura.md#6-dominios-simulados)) |
| Monitorear consumo de recursos | Peticiones, llamadas, latencia p95, errores por código y bytes transferidos ([RF-35]) |

## 3. Problema y justificación

Una empresa que ya tiene un servicio funcionando puede convertirlo en un ingreso. Pero entre un servidor que responde y un producto que alguien pueda contratar siempre hay que construir lo mismo: claves, límites de uso, medición, documentación, planes y cobro. Esa parte no diferencia a nadie y cuesta semanas de trabajo.

Hay plataformas internacionales que ya lo resuelven, pero cobran a través de pasarelas que no admiten empresas guatemaltecas, o imponen su marca y se quedan con una comisión del 20 % al 25 %.

| Alternativa existente | Modelo de cobro | Limitación en Guatemala |
|---|---|---|
| Puertas de enlace con monetización integrada | Suscripción mensual | Liquidan por una pasarela que excluye a Guatemala |
| Plataformas de tienda de API | Suscripción mensual | El mismo obstáculo, y la interfaz y el soporte solo están en inglés |
| Mercados centralizados de API | Comisión del 20 % al 25 % | Imponen su marca y se quedan con la relación con el cliente |

**Propuesta de valor de Shapi:**

- Cobra una suscripción fija, sin comisión.
- Conserva la marca del proveedor.
- Su interfaz está en español.
- **El cobro queda detrás de una interfaz de pasarela de pagos** (`IPasarelaPagos`). En esta versión académica esa pasarela es un **simulador**, como permite el enunciado. La arquitectura permite conectar después una pasarela local sin cambiar el resto del sistema ([ADR-12](12-decisiones.md)).

## 4. Actores

| Actor | Quién es | Dónde trabaja |
|---|---|---|
| **Proveedor** | La empresa que publica y cobra su API. Es el **cliente** de Shapi y corresponde al rol "Cliente" del enunciado. Dentro de su organización, cada persona es propietario, editor o lector. | Panel de Shapi (`shapi.localhost/panel`) |
| **Consumidor** | El desarrollador o la empresa que contrata un plan y usa la API. **Su cuenta pertenece a la organización proveedora**; no se registra en Shapi. | Portal de marca blanca (`{sub}.shapi.localhost`) y la API (`{sub}.api.shapi.localhost`) |
| **Administrador** | Quien opera la plataforma. Corresponde al rol "Administrador" del enunciado. | Panel de administración (`shapi.localhost/admin`) |
| **Soporte** | Quien atiende los casos de los proveedores. Tiene acceso de **solo lectura** a los datos de las organizaciones. Es uno de los roles opcionales del enunciado. | Panel de administración, con menos opciones |
| **Sistema** | Los procesos automáticos: la compuerta y el trabajador en segundo plano. | Sin interfaz |

El administrador y el soporte entran por el mismo formulario de inicio de sesión que el proveedor, y el sistema los enruta según su rol. No tienen registro público: la primera cuenta de administrador se crea en la siembra inicial del sistema.

## 5. Descripción del funcionamiento

### Uso por parte del proveedor
1. Se registra, confirma su correo y su organización queda en el plan **Prueba** (30 días, sin tarjeta).
2. Registra una API con un nombre, la URL de su servidor de origen y un **subdominio** que él elige. Shapi prueba la conexión con el origen antes de guardar.
3. Sube su especificación OpenAPI. Shapi extrae las rutas y el proveedor decide cuáles expone.
4. Configura cada ruta: límite por minuto, caché y peso en llamadas. También personaliza el portal: logotipo, color, nombre y texto de bienvenida.
5. Define los planes de su API y la publica.
6. Consulta el consumo, la facturación por consumidor y su historial de pagos. Puede invitar miembros, invitar consumidores y abrir casos de soporte.

### Uso por parte del consumidor
1. Entra al portal del proveedor, revisa la documentación y crea su cuenta, o acepta una invitación.
2. Confirma su correo, elige un plan y paga con tarjeta. Los planes gratuitos no piden tarjeta.
3. Recibe **una clave de producción y una de pruebas**, que se muestran completas **una sola vez**.
4. Llama a la API con la cabecera `X-Api-Key`. Desde el portal consulta su consumo por ruta y sus pagos, rota o revoca sus claves y cambia de plan.

### Procesos automáticos
- **La compuerta** valida cada petición contra Redis, aplica los límites y las cuotas, reenvía la petición al origen y registra las métricas en Redis.
- **El trabajador** hace varias tareas en segundo plano:
  - cada 10 segundos pasa las métricas a PostgreSQL;
  - cierra los ciclos de las suscripciones, las renueva, las pone en gracia o las suspende;
  - envía los correos pendientes;
  - verifica los dominios propios.

## 6. Modelo de negocio

Shapi cobra una **suscripción fija** a cada proveedor, sin comisión. Cada proveedor define además los planes que vende a sus consumidores (**dos niveles de planes**). Shapi cobra a los consumidores por cuenta del proveedor. **Liquidarle ese dinero al proveedor queda fuera del alcance** de esta versión.

### Planes de plataforma (datos de siembra)

| Plan | Descripción | Precio | Vigencia | APIs | Peticiones por ciclo | Miembros | Dominio propio | Se renueva |
|---|---|---|---|---|---|---|---|---|
| Prueba | Publicar una primera API sin costo | Q 0.00 | 30 días | 1 | 10,000 | 1 | No | **No**: vence a los 30 días ([RF-44]) |
| Lanzamiento | Empezar a cobrar por una API existente | Q 199.00 | 30 días | 3 | 250,000 | 3 | No | Sí |
| Producto | Proveedores con clientes establecidos | Q 599.00 | 30 días | 10 | 2,000,000 | 10 | Sí | Sí |
| Escala mensual | Varias APIs y alto volumen | Q 1,500.00 | 30 días | Sin límite | 10,000,000 | Sin límite | Sí | Sí |
| Escala anual | Alto volumen, con pago anual | Q 15,000.00 | 365 días | Sin límite | 120,000,000 | Sin límite | Sí | Sí |

La cuota de plataforma se cuenta en **peticiones** y no en llamadas, para que el proveedor no pueda saltársela poniéndoles peso 0 a sus rutas ([Glosario](02-glosario.md)).

## 7. Alcance

### Incluido
- Gestión de usuarios con registro, verificación de correo, recuperación de contraseña, sesiones, roles y organizaciones, **tanto para el personal como para los consumidores**.
- Publicación de APIs con especificación OpenAPI, rutas expuestas, subdominios, dominio propio verificado con DNS simulado y secreto de origen.
- Un portal de marca blanca por API, con inicio, documentación generada, consola de pruebas, planes y la cuenta del consumidor.
- La compuerta: validación de claves, límites por minuto, cuotas del consumidor y de la plataforma, caché opcional, reenvío al origen y medición.
- Planes en dos niveles, contratación con pasarela simulada, cambio de plan con prorrateo, renovación automática, periodo de gracia, suspensión y reversión de pagos.
- Paneles de consumo para el proveedor y el consumidor, el panel de administración, la bandeja de casos de soporte (del lado del proveedor y del lado de soporte), el estado de los componentes y la bitácora de acciones sensibles.
- Notificaciones por correo a través de un **servidor de correo simulado** (Mailpit).
- Despliegue reproducible con Docker Compose en un **ambiente productivo simulado** ([06 · Arquitectura](06-arquitectura.md#7-despliegue)).

### Excluido
- Integración con un procesador de pagos real. Se usa la pasarela simulada que permiten los lineamientos.
- Liquidarle al proveedor lo que cobran sus consumidores.
- Emisión de documentos tributarios electrónicos.
- Dominios y certificados públicos reales. Todo corre en `*.localhost` con la autoridad certificadora interna del borde ([ADR-09](12-decisiones.md)).
- Un mercado público de APIs: el portal siempre es de marca blanca.
- Ejecución de código subido por los clientes. La plataforma solo enruta tráfico.
- Verificación en dos pasos.
- Que un usuario pertenezca a más de una organización.
- Transferir la propiedad de una organización.
- Que el consumidor cancele la renovación automática.
- Que el proveedor bloquee a un consumidor completo. El proveedor revoca claves, y las suspensiones solo ocurren por falta de pago.
- Bibliotecas propias por lenguaje, alta disponibilidad en varias regiones y aplicación móvil.

## 8. Restricciones

- El presupuesto es **Q 0.00**: no se compran dominios, servidores ni servicios.
- El stack es el que permiten los lineamientos: .NET 10 (ASP.NET Core), React y PostgreSQL. También se usan Redis, YARP y Caddy.
- Las entregas son el Avance 1 (30 %) el 25 de septiembre, el Avance 2 (50 %) el 9 de octubre, el Avance 3 (80 %) el 23 de octubre y la entrega final en la primera semana de noviembre de 2026.
- El desarrollo es **dirigido por especificaciones** y lo ejecutan agentes de IA. Estas especificaciones son el contrato que deben respetar.

[RF-08]: 03-requisitos.md#rf-08
[RF-11]: 03-requisitos.md#rf-11
[RF-35]: 03-requisitos.md#rf-35
[RF-44]: 03-requisitos.md#rf-44

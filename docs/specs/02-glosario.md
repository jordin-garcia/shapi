# 02 · Glosario

Este glosario define el significado de cada término en todo el proyecto: especificaciones, código, base de datos, interfaz y pruebas. En el código se usan estos mismos nombres en español (por ejemplo `Suscripcion`, `PlanApi` o `consumo_diario`).

| Término | Definición |
|---|---|
| **Organización** | La empresa registrada en Shapi. Hay dos tipos: `proveedor` (publica APIs) y `plataforma` (una sola, creada en la siembra, a la que pertenecen el administrador y el soporte). |
| **Proveedor** | Una organización de tipo `proveedor` o una persona de ella. Es el cliente de Shapi y corresponde al rol "Cliente" del enunciado. |
| **Miembro** | Un usuario que pertenece a una organización con un rol: `propietario`, `editor`, `lector`, `administrador` o `soporte`. En esta versión cada usuario pertenece a **una sola** organización. |
| **Personal** | El ámbito de identidad de los usuarios de Shapi, es decir, los miembros de cualquier organización. Se autentican en `shapi.localhost`. |
| **Consumidor** | El cliente de un proveedor. Es una identidad aparte de *personal*, con correo y contraseña propios, y queda aislada por organización: el mismo correo puede tener cuentas distintas en portales de organizaciones distintas. |
| **API** | El servicio que publica un proveedor. Tiene un servidor de origen, un subdominio, rutas, planes y un portal. Equivale al "entorno web" de la categoría del enunciado. |
| **Servidor de origen** | El servidor del proveedor que responde las peticiones. Shapi no lo modifica. |
| **Subdominio** | El identificador de la API dentro de la plataforma, por ejemplo `envios`. Con él se forman el host del portal (`envios.shapi.localhost`) y el host de la API (`envios.api.shapi.localhost`). Tiene entre 3 y 30 caracteres `[a-z0-9-]`, debe ser único y no puede estar en la lista de reservados. |
| **Dominio base** | El dominio de la plataforma. Se configura con `SHAPI_DOMINIO_BASE` y en el entorno simulado vale `shapi.localhost`. |
| **Dominio propio** | Un dominio del proveedor que se conecta a la **API** con un registro CNAME, por ejemplo `api.enviosxelaju.localhost` → `envios.api.shapi.localhost`. |
| **DNS simulado** | Una tabla interna que reemplaza la consulta DNS real en el entorno simulado ([ADR-10](12-decisiones.md)). |
| **Ruta** | Un par método + patrón que sale de la especificación, por ejemplo `GET /rastreo/{guia}`. Puede estar *expuesta* u *oculta*. |
| **Portal** | El sitio de marca blanca de una API. Tiene inicio, documentación, consola de pruebas, planes, registro y cuenta del consumidor. |
| **Petición** | Una solicitud HTTP que llega a la compuerta. |
| **Llamada** | La unidad de la **cuota del consumidor**. Cada ruta tiene un *peso en llamadas* (1 o más), y cada petición a esa ruta que llega al origen descuenta ese peso. |
| **Peso en llamadas** | Las llamadas que descuenta una petición a una ruta. Por ejemplo, `POST /guias` descuenta 5. |
| **Límite por minuto** | El máximo de **peticiones** por minuto de una suscripción. Lo hay a nivel del plan y, opcionalmente, a nivel de cada ruta. Se aplican los dos. |
| **Cuota del consumidor** | Las **llamadas** que incluye el plan de API por ciclo. En la interfaz se muestra como "cuota mensual". |
| **Cuota de plataforma** | Las **peticiones** por ciclo que incluye el plan de plataforma, sumando todas las APIs de la organización. |
| **Plan de plataforma** | El plan que Shapi vende a los proveedores. |
| **Plan de API** | El plan que un proveedor vende a sus consumidores. Puede ser gratuito (precio Q 0.00). |
| **Suscripción** | El contrato entre una organización y un plan de plataforma (*suscripción de plataforma*) o entre un consumidor y un plan de API (*suscripción de API*). Los dos tipos usan la misma máquina de estados ([09](09-cobros-y-suscripciones.md)). |
| **Ciclo** | El periodo de vigencia de una suscripción, desde `inicio` (incluido) hasta `fin` (excluido), con `fin = inicio + vigencia_dias`. En pantalla se muestra el último día con servicio, `fin − 1 día`. Las cuotas se reinician en cada ciclo. |
| **Periodo de gracia** | Los 7 días que siguen a un cobro de renovación rechazado. El servicio sigue funcionando mientras dure. |
| **Suspensión** | El estado en que la compuerta responde 403. Puede deberse a una suscripción suspendida (por falta de pago o por el vencimiento del plan Prueba) o a que el administrador suspendió la organización. |
| **Clave** | La credencial del consumidor. Hay una de `produccion` y una de `pruebas` por suscripción, con formato `shp_prod_…` y `shp_prueba_…`. Se guarda como hash SHA-256 y se muestra completa una sola vez. |
| **Rotar** | Emitir una clave nueva del mismo tipo. La anterior queda en estado `rotada` y sigue funcionando 24 horas. Solo lo hace el consumidor. |
| **Revocar** | Invalidar una clave de inmediato. Lo hace el consumidor con las suyas o el proveedor con las de sus consumidores. |
| **Secreto de origen** | Un valor que la compuerta envía al origen en la cabecera `X-Shapi-Secreto` para que el proveedor pueda confirmar que la petición pasó por Shapi. Validarlo es opcional. |
| **Compuerta** | El proceso del plano de datos (YARP) que valida, limita, reenvía y mide cada petición. |
| **Filtro** | Una etapa de la tubería de la compuerta. Deja pasar la petición al siguiente filtro o la rechaza ([08](08-compuerta.md)). |
| **Plano de control** | La API de control (ASP.NET Core en tres capas) y el trabajador. Administran los datos de negocio. |
| **Trabajador** | El proceso en segundo plano que consolida el consumo, cierra ciclos, envía correos y verifica dominios. |
| **Consolidación** | El paso de las métricas acumuladas en Redis a la tabla `consumo_diario`. Se hace por lotes y es idempotente. |
| **Pasarela simulada** | La implementación de `IPasarelaPagos` que autoriza o rechaza pagos según reglas fijas y entrega un token por tarjeta ([09](09-cobros-y-suscripciones.md#2-pasarela-simulada)). |
| **Token de pago** | El identificador opaco de una tarjeta, emitido por la pasarela. Shapi guarda el token, la marca, los últimos 4 dígitos y el vencimiento, pero **nunca** el número completo ni el CVV. |
| **Prorrateo** | El ajuste del monto cuando alguien sube de plan a mitad del ciclo ([09](09-cobros-y-suscripciones.md#5-cambio-de-plan)). |
| **Bitácora** | El registro de acciones sensibles. Solo se le agregan filas, nunca se modifican ni se borran ([RF-41](03-requisitos.md#rf-41)). |
| **Caso** | Una solicitud de soporte de una organización proveedora hacia Shapi. Se atiende como una conversación (`caso_mensaje`). |
| **Modo demostración** | La configuración `SHAPI_MODO_DEMO=true`, que habilita el reloj ajustable y el botón de DNS simulado para mostrar en vivo las renovaciones y la verificación de dominios. |

## Formatos

- **Moneda:** quetzales, `Q 1,234.56`, almacenados como `numeric(12,2)`.
- **Fechas:** se guardan en UTC (`timestamptz`) y se muestran en la zona `America/Guatemala`, con formato `24 ago 2026` o `24 de agosto de 2026`.
- **Números:** separador de miles `,` y decimal `.`, igual que en los mockups.
- **Idioma:** toda la interfaz está en español y trata al usuario de *usted*, igual que en los mockups.

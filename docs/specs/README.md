# Especificaciones de Shapi

> **Fuente de verdad del proyecto.** Sustituye a la propuesta y al documento de requisitos y diseño que se entregaron a la docente el 11 de septiembre de 2026, que ya no forman parte del repositorio. Toda la especificación se revisó y se alineó el 22 de septiembre de 2026 ([12 · Decisiones](12-decisiones.md)). Las reglas del curso están en [`../lineamientos.md`](../lineamientos.md).

## Cómo leer estas especificaciones

| # | Documento | Qué define | Para quién sobre todo |
|---|---|---|---|
| 01 | [Visión y alcance](01-vision-y-alcance.md) | Problema, actores, categoría, planes de plataforma, qué entra y qué no | Todos |
| 02 | [Glosario](02-glosario.md) | Vocabulario común (petición o llamada, ciclo, gracia, etc.) y formatos | Todos |
| 03 | [Requisitos](03-requisitos.md) | RF-01 a RF-47 y RNF-01 a RNF-15, con criterios de aceptación y pantallas | Todos |
| 04 | [Roles y permisos](04-roles-y-permisos.md) | Ámbitos de identidad y la matriz de permisos | Backend y frontend |
| 05 | [Casos de uso](05-casos-de-uso.md) | Diagramas y especificaciones de CU-01 a CU-26 | Todos, y las pruebas E2E |
| 06 | [Arquitectura](06-arquitectura.md) | Planos, procesos, hosts, secuencias, dominios simulados, despliegue y estructura del repositorio | Todos |
| 07 | [Modelo de datos](07-modelo-de-datos.md) | Diagrama de clases, MER, diseño físico, llaves de Redis, estados y siembra | Backend |
| 08 | [Compuerta](08-compuerta.md) | Filtros, script Lua, errores, cabeceras, CORS y medición | Compuerta |
| 09 | [Cobros y suscripciones](09-cobros-y-suscripciones.md) | Pasarela simulada, máquina de estados, ciclos, prorrateo y límites | Backend (pagos) |
| 10 | [Identidad y seguridad](10-identidad-y-seguridad.md) | Sesiones, secretos, protección contra SSRF, correos y bitácora | Backend |
| 11 | [Interfaz y mockups](11-interfaz.md) | Sistema visual, catálogo de pantallas con rutas y estados | Frontend |
| 12 | [Decisiones (ADR)](12-decisiones.md) | Por qué se decidió cada cosa y qué alternativas se descartaron | Todos, y la exposición |

> **Plan de desarrollo y tareas:** [`docs/plan/`](../plan/README.md). Cada tarea dice qué secciones de estas especificaciones hay que leer.

## Reglas para quien implementa (personas o agentes de IA)

1. **No se inventan requisitos.** Si algo no está especificado, se agrega aquí primero, mediante un *pull request* que modifique la especificación, y después se implementa.
2. **Los nombres del dominio van en español** y siguen el [glosario](02-glosario.md) en el código, la base de datos, la API y las pruebas.
3. **Cada requisito tiene su criterio de aceptación**, que debe quedar cubierto por al menos una prueba automatizada que mencione su código (por ejemplo `// RF-28`).
4. **La interfaz se implementa igual a los mockups** de [11](11-interfaz.md), con los datos de la siembra de demostración.
5. **Los diagramas son Mermaid dentro de los `.md`.** Si cambia el diseño, se actualiza el diagrama en el mismo *pull request*.

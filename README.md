# Shapi

Plataforma como servicio que permite a una empresa **publicar su API, controlar quién la usa y cobrar por ella, sin modificar el servidor que ya tiene**.

Proyecto final de Ingeniería de Software I, Universidad Rafael Landívar (Quetzaltenango), 2026. Categoría del enunciado: *Servidores Web como Servicio* (propuesta aprobada).

| Integrante | Responsabilidad |
|---|---|
| Héctor Jordin Adolfo García Coyoy | Coordinación y compuerta de tráfico |
| Ronaldo Emilio Méndez Mayorga | Usuarios, planes, suscripciones y pagos |
| Dominique Guillermo Contreras Sierra | Interfaz de usuario y portales de marca blanca |
| José Pablo Zúñiga de León | Infraestructura, despliegue, pruebas y documentación |

## Contenido del repositorio

| Ruta | Contenido |
|---|---|
| [`docs/specs/`](docs/specs/README.md) | **Especificaciones: la fuente de verdad** (requisitos, arquitectura, modelo de datos, compuerta, cobros, seguridad, interfaz y decisiones) |
| [`mockups/`](mockups/) | Diseños aprobados. Los `.html` de la raíz se abren directamente en el navegador. El catálogo está en [11 · Interfaz](docs/specs/11-interfaz.md) |
| [`docs/lineamientos.md`](docs/lineamientos.md) | Lineamientos oficiales del curso (transcripción del PDF de la docente) |

## ¿Cómo trabajamos?

Con **desarrollo dirigido por especificaciones** y agentes de IA. Si es tu primera vez, lee [`docs/plan/README.md`](docs/plan/README.md) (la guía rápida, 10 minutos) y sigue [`docs/plan/instalacion.md`](docs/plan/instalacion.md). Después, dentro de la carpeta del proyecto, pídele a tu agente:

> Soy <tu nombre>. Revisa el plan y dime qué tareas me tocan.

| Ruta | Contenido |
|---|---|
| [`AGENTS.md`](AGENTS.md) | Instrucciones que leen todos los agentes (Claude Code, Codex, Antigravity, Copilot, Cursor y Gemini) |
| [`docs/plan/`](docs/plan/README.md) | Plan de desarrollo: guía, instalación, protocolo, convenciones, calendario y **una tarea por archivo** |
| `node scripts/tareas.mjs` | Estado del plan y tareas de cada persona |

## Arquitectura en una línea

Borde **Caddy** → **compuerta** (ASP.NET Core + YARP, tubería de filtros sobre **Redis**) para el tráfico de las APIs, y **API de control** (ASP.NET Core, tres capas, **PostgreSQL**) más un **trabajador** en segundo plano para la administración. Hay dos frontends **React**: el panel y el portal de marca blanca. Todo corre con Docker Compose, con dominios simulados bajo `*.shapi.localhost`. Los detalles están en [06 · Arquitectura](docs/specs/06-arquitectura.md).

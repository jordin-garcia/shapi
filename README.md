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
| [`src/`](src/) | Backend en .NET: `Shapi.Api` (API de control), `Shapi.Compuerta`, `Shapi.Trabajador` y las capas `Shapi.Aplicacion`, `Shapi.Dominio`, `Shapi.Infraestructura` y `Shapi.Contratos` |
| [`tests/`](tests/) | Pruebas del backend (xUnit y Testcontainers). Más adelante, también las E2E y las de carga |
| [`frontend/`](frontend/) | Panel y portal en React (`apps/`) y los paquetes comunes `ui` y `api` (`packages/`), con pnpm |
| [`contratos/openapi/`](contratos/openapi/) | Contratos HTTP de la API de control, uno por módulo. Los tipos del frontend se generan desde aquí |
| [`infra/`](infra/) | Docker Compose y Caddy del entorno local |
| [`origenes-demo/`](origenes-demo/) | Las dos APIs de ejemplo que se publican con Shapi en la demostración |
| [`scripts/`](scripts/) | Herramientas del equipo: tareas del plan, tablero, verificación del entorno y veredicto de la revisión |

## ¿Cómo trabajamos?

Con **desarrollo dirigido por especificaciones** y agentes de IA. Si es tu primera vez, lee [`docs/plan/README.md`](docs/plan/README.md) (la guía rápida, 10 minutos) y sigue [`docs/plan/instalacion.md`](docs/plan/instalacion.md). Después, dentro de la carpeta del proyecto, pídele a tu agente:

> Soy <tu nombre>. Revisa el plan y dime qué tareas me tocan.

| Ruta | Contenido |
|---|---|
| [`AGENTS.md`](AGENTS.md) | Instrucciones que leen todos los agentes (Claude Code, Codex, Antigravity, Copilot, Cursor y Gemini) |
| [`docs/plan/`](docs/plan/README.md) | Plan de desarrollo: guía, instalación, protocolo, convenciones, calendario y **una tarea por archivo** |
| `node scripts/tareas.mjs` | Estado del plan y tareas de cada persona |

## Arranque rápido

Con las herramientas de [`docs/plan/instalacion.md`](docs/plan/instalacion.md) instaladas y Docker abierto:

```bash
node scripts/verificar-entorno.mjs                         # comprueba el equipo
cp .env.example .env                                       # PowerShell: Copy-Item .env.example .env
docker compose --env-file .env -f infra/compose.yml up -d  # PostgreSQL, Redis, Mailpit, Caddy y los orígenes de demostración
dotnet run --project src/Shapi.Api                         # terminal 1: API de control
dotnet run --project src/Shapi.Compuerta                   # terminal 2: compuerta
dotnet run --project src/Shapi.Trabajador                  # terminal 3: trabajador
cd frontend && pnpm install && pnpm dev                    # terminal 4: panel y portal
```

Luego abre https://shapi.localhost. El correo simulado se ve en https://correo.shapi.localhost. Las pruebas se ejecutan con `dotnet test Shapi.slnx` y, en `frontend/`, con `pnpm test`. Los demás comandos están en [`AGENTS.md`](AGENTS.md#comandos).

## Arquitectura en una línea

Borde **Caddy** → **compuerta** (ASP.NET Core + YARP, tubería de filtros sobre **Redis**) para el tráfico de las APIs, y **API de control** (ASP.NET Core, tres capas, **PostgreSQL**) más un **trabajador** en segundo plano para la administración. Hay dos frontends **React**: el panel y el portal de marca blanca. Todo corre con Docker Compose, con dominios simulados bajo `*.shapi.localhost`. Los detalles están en [06 · Arquitectura](docs/specs/06-arquitectura.md).

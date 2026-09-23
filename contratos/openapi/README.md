# Contratos HTTP de la API de control

Cada módulo tiene su propio archivo OpenAPI 3.1, y lo modifica **solo el dueño del módulo**. Así, dos personas no editan el mismo contrato. El frontend genera sus tipos TypeScript con `pnpm generar:api`, desde la carpeta `frontend/`.

| Archivo | Módulo | Dueño | Prefijo de rutas |
|---|---|---|---|
| `identidad.yaml` | Autenticación del personal y de los consumidores, sesiones, perfil (la definición de contraseña de las cuentas de plataforma la agrega José Pablo en JZ-09) | Emilio | `/api/auth/*`, `/api/portal/auth/*`, `/api/perfil` |
| `organizaciones.yaml` | Miembros, invitaciones de miembros y de consumidores | Emilio | `/api/miembros/*`, `/api/invitaciones/*`, `/api/apis/{id}/invitaciones` |
| `planes.yaml` | Planes de plataforma (públicos y administración) y planes de API | Emilio | `/api/planes-plataforma`, `/api/admin/planes-plataforma/*`, `/api/apis/{id}/planes/*`, `/api/portal/planes` |
| `suscripciones.yaml` | Suscripción de plataforma y de API, contratación, cambio de plan y reloj de demostración | Emilio | `/api/suscripcion/*`, `/api/portal/suscripcion/*`, `/api/portal/suscripciones`, `/api/admin/demo/reloj` |
| `pagos.yaml` | Historiales de pagos y reversión | Emilio | `/api/pagos/*`, `/api/portal/pagos`, `/api/admin/pagos/*` |
| `apis.yaml` | Registro, especificación, rutas, publicación, portal, dominios y secreto | Dominique | `/api/apis/*` |
| `portal.yaml` | Configuración pública y documentación del portal por host | Dominique | `/api/portal/configuracion`, `/api/portal/documentacion/*` |
| `claves.yaml` | Claves de los consumidores | Jordin | `/api/apis/{id}/claves/*`, `/api/portal/claves/*` |
| `consumo.yaml` | Métricas de consumo | Jordin | `/api/apis/{id}/consumo`, `/api/apis/{id}/consumidores`, `/api/portal/consumo` |
| `administracion.yaml` | Organizaciones y cuentas de plataforma | José Pablo | `/api/admin/organizaciones/*`, `/api/admin/cuentas/*` |
| `soporte.yaml` | Casos de soporte | José Pablo | `/api/casos/*`, `/api/admin/casos/*` |
| `sistema.yaml` | Estado de los componentes y bitácora | José Pablo | `/api/admin/estado`, `/api/admin/bitacora` |

Las convenciones comunes (errores, nombres, paginación) están en `docs/plan/convenciones.md` §5.

# Mockups de Shapi

Son los diseños aprobados y corregidos el 22 de septiembre de 2026. El catálogo completo, con cada pantalla, su ruta, sus requisitos y su estado, está en [`docs/specs/11-interfaz.md`](../docs/specs/11-interfaz.md).

## Cómo verlos

Se abre con doble clic en el navegador cualquier paquete de esta carpeta:

| Paquete | Contenido |
|---|---|
| `a0-inicio.html` | Sitio público y lámina de estilo oficial (variante 4, "Plano azul") |
| `a1-acceso-proveedor.html` | Registro, verificación, inicio de sesión y recuperación |
| `a2-contratacion-plan.html` | Planes de plataforma, contratación, rechazo y cambio de plan |
| `a3-publicacion-api.html` | Registrar una API, especificación, rutas, direcciones y portal |
| `a4-planes-claves-equipo.html` | Planes de API, claves (solo revocar) y miembros |
| `a5-portal-marca-blanca.html` | Portal del consumidor: inicio, documentación, consola, registro, acceso y contratación |
| `a6-administracion-soporte.html` | Planes de plataforma, organizaciones, pagos, casos y cuentas |
| `a7-soporte-proveedor.html` | Casos de soporte desde el panel del proveedor |
| `a8-cuenta.html` | Perfil del personal y aceptación de la invitación de un miembro |
| `b1-salidas-proveedor.html` | Consumo, consumidores, invitaciones, pagos y suscripción de plataforma |
| `b2-salidas-consumidor.html` | Consumo, pagos, suscripción y claves (rotar y revocar), cambio de plan |
| `b3-salidas-administracion-soporte.html` | Estado de los componentes y bitácora |
| `navegacion-proveedor.html` | Barra lateral del proveedor |

## Cómo editarlos

1. Edite el `.dc.html` de la carpeta de la tanda (por ejemplo `A3/Dominio.dc.html`). Es HTML estático de 1440 px de ancho.
2. Si agrega o renombra una pantalla, actualice el `canvas.json` de esa carpeta, con el archivo, el título, la posición y el tamaño.
3. Vuelva a generar los paquetes:
   ```
   python mockups/herramientas/empaquetar.py
   ```
4. Actualice el catálogo en `docs/specs/11-interfaz.md`.

Para revisar una pantalla sola, sirva la carpeta con `python -m http.server` y abra el `.dc.html` directamente. El aviso de que falta `support.js` es normal y no afecta cómo se ve.

`_descartadas/` guarda las variantes de inicio que no se eligieron y la pantalla de rotación de claves del proveedor que se eliminó (ADR-02). Se conservan solo como historial.

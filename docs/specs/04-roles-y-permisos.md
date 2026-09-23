# 04 · Roles y permisos

Esta matriz define el [RF-07](03-requisitos.md#rf-07). En el backend se implementa con *policies* de ASP.NET Core, una por permiso, y la regla es **denegar todo por defecto**. El frontend oculta las opciones no permitidas, pero **la autorización la decide siempre el backend**.

## 1. Ámbitos de identidad

| Ámbito | Quiénes | Dónde se autentican | Cookie |
|---|---|---|---|
| `personal` | Miembros de cualquier organización: administrador, soporte, propietario, editor y lector | `https://shapi.localhost` | `shapi_sesion` (host `shapi.localhost`) |
| `consumidor` | Consumidores de una organización proveedora | `https://{sub}.shapi.localhost`, el portal de cada API | `portal_sesion` (host del portal; no se comparte entre portales) |

Una sesión de un ámbito **nunca** da acceso a las rutas del otro. Un consumidor que entra a otro portal de la **misma** organización usa las mismas credenciales, pero inicia sesión otra vez, porque la cookie pertenece a cada host.

## 2. Roles

| Rol | Organización | Descripción |
|---|---|---|
| `administrador` | Plataforma | Opera Shapi. |
| `soporte` | Plataforma | Atiende casos. Ve los datos de las organizaciones en solo lectura. |
| `propietario` | Proveedor | Hay uno por organización y lo tiene quien la registró. Puede hacer todo en su organización. |
| `editor` | Proveedor | Configura APIs, planes y el portal. No toca el cobro ni los miembros. |
| `lector` | Proveedor | Consulta todo lo de su organización, incluidos los pagos, sin modificar nada. |
| consumidor | — (identidad aparte) | Administra su cuenta, sus suscripciones y sus claves en un portal. |

## 3. Matriz de permisos

Leyenda: ✅ permitido · 👁 solo lectura · — no permitido.

### 3.1 Panel del proveedor

| Permiso | Propietario | Editor | Lector | Admin | Soporte |
|---|---|---|---|---|---|
| Ver APIs, rutas, planes, portal y dominios | ✅ | ✅ | 👁 | — | 👁 (desde el caso) |
| Registrar, configurar, publicar y despublicar APIs | ✅ | ✅ | — | — | — |
| Cargar la especificación y exponer u ocultar rutas | ✅ | ✅ | — | — | — |
| Configurar rutas (límite, caché y peso) | ✅ | ✅ | — | — | — |
| Personalizar el portal | ✅ | ✅ | — | — | — |
| Conectar un dominio propio | ✅ | ✅ | — | — | — |
| Ver y regenerar el secreto de origen | ✅ | ✅ | — | — | — |
| Crear, editar y desactivar planes de API | ✅ | ✅ | — | — | — |
| Ver las claves de los consumidores (enmascaradas) | ✅ | ✅ | 👁 | — | — |
| **Revocar** claves de los consumidores | ✅ | ✅ | — | — | — |
| **Rotar** claves de los consumidores | — | — | — | — | — |
| Ver consumo y consumidores | ✅ | ✅ | 👁 | — | 👁 (desde el caso) |
| Invitar consumidores | ✅ | ✅ | — | — | — |
| Invitar y quitar miembros, y cambiar su rol | ✅ | — | — | — | — |
| Contratar o cambiar el plan de plataforma y registrar la tarjeta | ✅ | — | — | — | — |
| Ver la suscripción de plataforma y el historial de pagos | ✅ | — | 👁 | — | 👁 (resumen en el caso) |
| Abrir casos de soporte y responder en ellos | ✅ | ✅ | ✅ | — | — |
| Editar el perfil propio y cerrar sesión | ✅ | ✅ | ✅ | ✅ | ✅ |

### 3.2 Panel de administración

| Permiso | Admin | Soporte |
|---|---|---|
| Crear, editar y desactivar planes de plataforma | ✅ | — |
| Listar organizaciones | ✅ | 👁 |
| Suspender o reactivar organizaciones | ✅ | — |
| Ver los pagos de plataforma | ✅ | — |
| Revertir pagos | ✅ | — |
| Ver casos | ✅ | ✅ |
| Registrar casos a nombre de una organización, responder, asignarse y cerrar casos | ✅ | ✅ |
| Ver los datos de la organización desde un caso (solo lectura) | ✅ | ✅ |
| Ver el estado de los componentes | ✅ | ✅ |
| Ver la bitácora | ✅ | ✅ |
| Crear, desactivar y reactivar cuentas de administración y de soporte | ✅ | — |

### 3.3 Portal del consumidor

| Permiso | Visitante sin sesión | Consumidor |
|---|---|---|
| Ver el inicio, la documentación y los planes | ✅ | ✅ |
| Usar la consola de pruebas (con su clave pegada) | ✅ | ✅ |
| Registrarse, iniciar sesión y recuperar la contraseña | ✅ | — |
| Contratar o cambiar de plan (con el correo verificado) | — | ✅ |
| Ver su suscripción, sus claves enmascaradas, su consumo y sus pagos | — | ✅ |
| **Rotar** y **revocar** sus propias claves, y emitir una nueva si una está revocada | — | ✅ |

## 4. Reglas adicionales

1. **Aislamiento entre organizaciones ([RNF-08](03-requisitos.md#rnf-08)).** Toda entidad que pertenece a una organización se filtra por el `organizacion_id` de la sesión. Si el recurso pedido pertenece a otra organización, se responde **404**, no 403, para no revelar que existe.
2. **El soporte nunca modifica** datos de las organizaciones. Solo puede escribir en `caso` y `caso_mensaje`.
3. **Operaciones sobre uno mismo:** el propietario no puede quitarse ni cambiarse el rol, y un administrador no puede desactivar su propia cuenta.
4. **Organización suspendida:** mientras está suspendida, su personal puede entrar al panel para pagar, consultar datos y abrir casos, pero **no puede publicar APIs nuevas**.
5. **Consumidor con suscripción suspendida:** puede entrar al portal y pagar con otra tarjeta. Sus claves responden 403 hasta que pague.

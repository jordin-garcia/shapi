# 11 · Interfaz y mockups

## 1. Sistema visual oficial: variante 4, "Plano azul"

La variante 4 es la oficial ([ADR-32](12-decisiones.md)). Las otras cinco quedaron en `mockups/_descartadas/` solo como historial. Los tokens se implementan como variables CSS y como tema de Tailwind en `frontend/packages/ui`.

### Tipografía
| Uso | Familia | Pesos |
|---|---|---|
| Títulos, cifras destacadas y precios | **Sora** | 300 (titular del inicio), 400 y 500 |
| Interfaz y datos | **IBM Plex Sans** | 400, 500 y 600. Cifras tabulares (`font-variant-numeric: tabular-nums`) en toda tabla |

Escala: título de página 32/1.2 (Sora 400) · título de tarjeta 22/1.3 · cuerpo 16/1.6 · dato de tabla 15 · etiqueta 12, en mayúsculas, con `letter-spacing .16em`, color `--tinta-suave` · encabezado de tabla 11, en mayúsculas, con `letter-spacing .12em`.

### Colores
| Token | Superficie clara (aplicación y portal) | Superficie oscura (inicio y barra lateral) |
|---|---|---|
| `--principal` | `#3B6FF0` (cursor encima: `#2C57C9`) | `#7FA6FF` |
| `--tinta` | `#0B1220` | `#E8EDF7` |
| `--tinta-suave` | `#5A6884` | `#8B98B0` |
| `--fondo` | `#F4F6FA` | `#060910` |
| `--panel` | `#FFFFFF` | `#0C1220` |
| `--borde` | `#DCE3EE` (campos: `#C9D2E1`, filas: `#EBEFF5`) | `#1B2436` (`#2A3550` en los controles) |
| `--correcto` | texto `#146542` · fondo `#E4F3EC` · borde `#B6DCC9` | `#3FBF88` |
| `--alerta` | texto `#8E3315` · fondo `#FBE9E3` · borde `#EDC3B4` | `#F08A5F` |
| `--neutro` | texto `#5A6884` · fondo `#F4F6FA` · borde `#DCE3EE` | — |

**Solo hay tres colores de estado**: correcto (activa, pagado, en servicio), alerta (suspendida, rechazado, en gracia, degradado) y neutro (despublicada, pendiente, cerrado).

### Componentes base
- **Radio único:** 8 px. **Espaciado:** 4 · 8 · 12 · 16 · 20 · 24 · 32 · 44 · 64 · 80.
- **Botón principal:** 46 px de alto, fondo `--principal`. **Botón secundario:** borde `#C9D2E1`. **Botón deshabilitado:** borde `#E4E9F1` y texto `#A3AEC2`.
- **Etiqueta de estado:** clases `eti-c` (correcto), `eti-a` (alerta) y `eti-n` (neutro).
- **Tabla:** filas de 42 px, encabezado de 11 px en mayúsculas y separador `#EBEFF5`.
- **Tarjeta:** fondo blanco, borde de 1 px `#DCE3EE` y radio de 8 px, **sin sombra**.
- **Estructura del panel:** barra superior oscura de 76 px, barra lateral oscura de 272 px con grupos *Publicación · API · Organización* y contenido con un margen de 44 px. Se diseña para 1440 × 900 y funciona desde 1280 px.
- **Portal:** usa la misma estructura clara, pero el color principal es `--marca-principal`, el de la API. En el portal no aparece la marca de Shapi.

## 2. Formato de los mockups

- **Fuente editable:** `mockups/<Tanda>/*.dc.html` (HTML estático de 1440 × 900) y `mockups/<Tanda>/canvas.json`, que tiene el orden y los títulos de las mesas de trabajo.
- **Para verlos:** los `.html` de la raíz de `mockups/` (por ejemplo `a3-publicacion-api.html`) son **paquetes autocontenidos**; se abren con doble clic en el navegador.
- **Después de editar** cualquier `.dc.html` o `canvas.json`, hay que volver a generar los paquetes:
  ```
  python mockups/herramientas/empaquetar.py
  ```
- Los mockups muestran **datos de ejemplo** con las mismas cifras que la siembra de demostración ([07 §6](07-modelo-de-datos.md#6-datos-de-siembra)). Los frontends tienen que verse igual a los mockups con esos datos.

## 3. Catálogo de pantallas

Estado: **=** sin cambios · **✎** corregida el 22 de septiembre de 2026 · **★** nueva.

> Todas las pantallas del panel del proveedor (A2, A3, A4, B1 y N.1) llevan además el nuevo elemento **Casos de soporte** en la barra lateral. No se marca en cada fila.

### A0 · Sitio público (`mockups/A0`, paquete `a0-inicio.html`)
| ID | Pantalla | Archivo | Ruta | RF | Estado |
|---|---|---|---|---|---|
| A0.1 | Inicio de Shapi con planes | `Main.dc.html` | `shapi.localhost/` | RF-19 | ✎ (peticiones, miembros, Escala mensual/anual, texto de las claves) |
| A0.2 | Lámina de estilo oficial | `Lamina.dc.html` | `shapi.localhost/_ui` | — | ✎ (subdominio de ejemplo) |

> A0.2 lleva el host completo a propósito: la prueba del catálogo (`frontend/apps/panel/src/tests/rutas.test.tsx`) exige que cada ruta que empieza con `/` muestre su ID, y la lámina no lo muestra. La ruta `/_ui` se prueba aparte.

### A1 · Acceso del proveedor (`a1-acceso-proveedor.html`)
| ID | Pantalla | Archivo | Ruta | RF | Estado |
|---|---|---|---|---|---|
| A1.1 | Registro del proveedor | `Main.dc.html` | `/registro` | RF-01 | = |
| A1.2 | Aviso de verificación de correo | `Verificacion.dc.html` | `/verificar-correo` | RF-02 | ✎ (peticiones, vence en 24 h, reenviar) |
| A1.3 | Inicio de sesión | `Sesion.dc.html` | `/entrar` | RF-04 | = |
| A1.4a | Recuperación: pedir el enlace | `Recuperacion.dc.html` | `/recuperar` | RF-03 | = |
| A1.4b | Recuperación: contraseña nueva | `NuevaContrasena.dc.html` | `/restablecer?token=` | RF-03 | ✎ (se cierran las demás sesiones) |

### A2 · Plan de plataforma (`a2-contratacion-plan.html`)
| ID | Pantalla | Archivo | Ruta | RF | Estado |
|---|---|---|---|---|---|
| A2.1 | Planes de plataforma | `Main.dc.html` | `/panel/suscripcion/planes` | RF-19 | ✎ (peticiones, miembros, Escala mensual/anual) |
| A2.2 | Contratar un plan superior | `Contratacion.dc.html` | `/panel/suscripcion/contratar/:plan` | RF-20 | ✎ (fechas de 30 días, texto del token) |
| A2.3 | Confirmación | `Confirmacion.dc.html` | — | RF-20 | ✎ (fechas) |
| A2.4 | Tarjeta rechazada | `Rechazo.dc.html` | — | RF-20 | = |
| A2.5 | Cambio de plan con prorrateo | `CambioPlan.dc.html` | `/panel/suscripcion/cambiar/:plan` | RF-25 | ✎ (fechas) |

### A3 · Publicación de una API (`a3-publicacion-api.html`)
| ID | Pantalla | Archivo | Ruta | RF | Estado |
|---|---|---|---|---|---|
| A3.1 | APIs de la organización (y su variante vacía) | `Main.dc.html`, `ListaVacia.dc.html` | `/panel/apis` | RF-14, RF-43 | ✎ (uso del límite del plan) |
| A3.2 | Registrar una API | `Registro.dc.html` | `/panel/apis/nueva` | RF-08 | ✎ (subdominio y prueba de conexión) |
| A3.3 | Cargar especificación | `Especificacion.dc.html` | `/panel/apis/:id/especificacion` | RF-09 | ✎ (las rutas nuevas quedan ocultas) |
| A3.4 | Rutas expuestas | `Rutas.dc.html` | `/panel/apis/:id/rutas` | RF-10 | = |
| A3.5 | Configuración por ruta | `ConfigRutas.dc.html` | `/panel/apis/:id/configuracion-rutas` | RF-13 | ✎ (peso en llamadas) |
| A3.6 | Direcciones, dominio propio y secreto de origen | `Dominio.dc.html` | `/panel/apis/:id/dominios` | RF-11, RF-12, RF-47 | ✎ (hosts `.localhost`, DNS simulado, secreto) |
| A3.7 | Personalización del portal | `Portal.dc.html` | `/panel/apis/:id/portal` | RF-15 | ✎ (host del portal, peticiones por minuto) |
| A3.7r | Referencia: inicio del portal a tamaño real | `InicioPortal.dc.html` | — | RF-16 | ✎ (peticiones por minuto) |

### A4 · Planes, claves y equipo (`a4-planes-claves-equipo.html`)
| ID | Pantalla | Archivo | Ruta | RF | Estado |
|---|---|---|---|---|---|
| A4.1 | Planes de la API (vacía, editar, nuevo) | `Main`, `PlanesVacia`, `EditarPlan`, `NuevoPlan` | `/panel/apis/:id/planes` | RF-18 | ✎ (límite en peticiones) |
| A4.2 | Miembros y roles (vacía, quitar) | `Miembros`, `MiembrosVacia`, `QuitarMiembro` | `/panel/miembros` | RF-06, RF-43 | ✎ (uso del límite de miembros) |
| A4.3 | Claves de los consumidores (vacía) | `Claves`, `ClavesVacia` | `/panel/apis/:id/claves` | RF-26, RF-28 | ✎ (**solo revocar**; ya no hay "Rotar") |
| A4.3b | Revocar una clave | `RevocarClave.dc.html` | — | RF-28 | = |
| ~~A4.3r~~ | ~~Rotar una clave (el proveedor)~~ | eliminada → `_descartadas/A4-RotarClave.dc.html` | — | — | Reemplazada por B2.4 |

### A5 · Portal de marca blanca (`a5-portal-marca-blanca.html`)
| ID | Pantalla | Archivo | Ruta (`{sub}.shapi.localhost`) | RF | Estado |
|---|---|---|---|---|---|
| A5.0 | Inicio del portal | `Main.dc.html` | `/` | RF-15, RF-16 | ✎ (peticiones por minuto) |
| A5.1 | Documentación | `Documentacion.dc.html` | `/documentacion/:ruta` | RF-16 | ✎ (URL de la API en `.api.`) |
| A5.2 | Consola de pruebas | `Consola.dc.html` | `/consola` | RF-16, RF-45 | ✎ (la clave de pruebas se pega) |
| A5.3 | Registro del consumidor | `Registro.dc.html` | `/registro` | RF-05 | = |
| A5.3b | Registro por invitación | `RegistroInvitacion.dc.html` | `/invitacion?token=` | RF-05 | = |
| A5.4 | Planes de la API (vacía) | `Planes`, `PlanesVacia` | `/planes` | RF-19 | ✎ (peticiones por minuto) |
| A5.4b | Plan contratado y claves (única vez) | `Confirmacion.dc.html` | — | RF-20, RF-26 | ✎ (aviso de que no se volverán a mostrar) |
| A5.5 | El mismo portal con otra marca (inicio y documentación) | `InicioAgro`, `DocumentacionAgro` | `agro.shapi.localhost` | RF-15 | ✎ (URL y peticiones) |
| A5.6 | Pago en el portal | `Pago.dc.html` | `/contratar/:plan` | RF-20 | ✎ (texto del token) |
| A5.7 | Inicio de sesión del consumidor | `Acceso.dc.html` | `/entrar` | RF-04 | ✎ (enlace para recuperar la contraseña) |
| A5.8 | Verificación de correo del consumidor | `Verificacion.dc.html` | `/verificar-correo` | RF-02 | ★ |
| A5.9 | Recuperación: pedir el enlace | `Recuperacion.dc.html` | `/recuperar` | RF-03 | ★ |
| A5.10 | Recuperación: contraseña nueva | `NuevaContrasena.dc.html` | `/restablecer?token=` | RF-03 | ★ |

### A6 · Administración y soporte (`a6-administracion-soporte.html`)
| ID | Pantalla | Archivo | Ruta | RF | Estado |
|---|---|---|---|---|---|
| A6.1 | Planes de plataforma (nuevo y editar) | `Main`, `NuevoPlan`, `EditarPlan` | `/admin/planes` | RF-17 | ✎ (peticiones, miembros, Escala anual) |
| A6.2 | Organizaciones (vacía) | `Organizaciones`, `OrganizacionesVacia` | `/admin/organizaciones` | RF-38 | ✎ (propietario en vez de subdominio; fechas) |
| A6.2b | Suspender una organización | `Suspender.dc.html` | — | RF-38 | ✎ (sin subdominio) |
| A6.3 | Pagos de plataforma (vacía) | `Pagos`, `PagosVacia` | `/admin/pagos` | RF-24 | = |
| A6.3b | Revertir un pago | `RevertirPago.dc.html` | — | RF-24 | = |
| A6.4 | Casos de soporte (vacía) | `Casos`, `CasosVacia` | `/admin/casos` | RF-40 | ✎ (casos abiertos por el proveedor) |
| A6.4b | Atención de un caso | `Caso.dc.html` | `/admin/casos/:numero` | RF-40 | ✎ (conversación, API afectada, fechas) |
| A6.5 | Cuentas de administración y soporte (vacía) | `Cuentas`, `CuentasVacia` | `/admin/cuentas` | RF-42 | = |

### A7 · Soporte del proveedor ★ (`a7-soporte-proveedor.html`)
| ID | Pantalla | Archivo | Ruta | RF | Estado |
|---|---|---|---|---|---|
| A7.1 | Casos de soporte de la organización y formulario para abrir uno | `Casos.dc.html` | `/panel/soporte` | RF-40 | ★ |
| A7.2 | Conversación de un caso | `Caso.dc.html` | `/panel/soporte/:numero` | RF-40 | ★ |

### A8 · Cuenta del personal ★ (`a8-cuenta.html`)
| ID | Pantalla | Archivo | Ruta | RF | Estado |
|---|---|---|---|---|---|
| A8.1 | Mi perfil (nombre, contraseña, cerrar sesión) | `Perfil.dc.html` | `/panel/perfil` y `/admin/perfil` | RF-04 | ★ |
| A8.2 | Aceptar la invitación de un miembro | `Invitacion.dc.html` | `/invitacion?token=` | RF-06 | ★ |

### B1 · Salidas del proveedor (`b1-salidas-proveedor.html`)
| ID | Pantalla | Archivo | Ruta | RF | Estado |
|---|---|---|---|---|---|
| B1.1 | Consumo, latencia y errores (vacía) | `Main`, `ConsumoVacia` | `/panel/apis/:id/consumo` | RF-35 | ✎ (peticiones y llamadas, bytes) |
| B1.2 | Consumo y facturación por consumidor (vacía) | `Consumidores`, `ConsumidoresVacia` | `/panel/apis/:id/consumidores` | RF-36 | ✎ (fechas y botón para invitar) |
| B1.3 | Historial de pagos (vacía) | `Pagos`, `PagosVacia` | `/panel/pagos` | RF-21 | ✎ (fechas) |
| B1.4 | Suscripción de plataforma: vigente, en gracia y suspendida | `Suscripcion*` | `/panel/suscripcion` | RF-22, RF-23 | ✎ (fechas de 30 días y 7 días de gracia) |
| B1.5 | Invitar consumidores | `InvitarConsumidores.dc.html` | `/panel/apis/:id/consumidores/invitar` | RF-05 | ★ |

### B2 · Salidas del consumidor (`b2-salidas-consumidor.html`)
| ID | Pantalla | Archivo | Ruta (`{sub}.shapi.localhost`) | RF | Estado |
|---|---|---|---|---|---|
| B2.1 | Consumo del ciclo por ruta (vacía) | `Main`, `ConsumoVacia` | `/cuenta/consumo` | RF-37 | = |
| B2.2 | Historial de pagos (vacía) | `Pagos`, `PagosVacia` | `/cuenta/pagos` | RF-21 | = |
| B2.3 | Suscripción y claves: vigente, en gracia, no disponible y vacía | `Suscripcion*` | `/cuenta/suscripcion` | RF-22, RF-23, RF-26 a RF-28 | ✎ (claves enmascaradas; **Rotar** y **Revocar**; cambiar de plan) |
| B2.4 | Rotar una clave | `RotarClave.dc.html` | — | RF-27 | ★ |
| B2.5 | Clave nueva (única vez) | `ClaveNueva.dc.html` | — | RF-27 | ★ |
| B2.6 | Revocar una clave propia | `RevocarClave.dc.html` | — | RF-28 | ★ |
| B2.7 | Cambio de plan del consumidor | `CambioPlan.dc.html` | `/cuenta/cambiar-plan/:plan` | RF-25 | ★ |

### B3 · Salidas del administrador y del soporte (`b3-salidas-administracion-soporte.html`)
| ID | Pantalla | Archivo | Ruta | RF | Estado |
|---|---|---|---|---|---|
| B3.1 | Estado de los componentes: normal, degradado y vista de soporte | `Main`, `Degradado`, `Soporte` | `/admin/estado` | RF-39 | ✎ (los 7 componentes de la arquitectura) |
| B3.2 | Bitácora (vacía) | `Bitacora`, `BitacoraVacia` | `/admin/bitacora` | RF-41 | ✎ (acción de un consumidor) |

### Navegación
| ID | Pantalla | Archivo | Estado |
|---|---|---|---|
| N.1 | Barra lateral del proveedor, aplicada a A4.1 | `Navegacion/Main.dc.html` | ✎ (se agregó "Casos de soporte") |

**Barra lateral del proveedor**:
- *Publicación*: APIs.
- *API* (con selector): Especificación · Rutas expuestas · Configuración por ruta · Dominios · Portal · Planes · Claves · Consumo · Consumidores.
- *Organización*: Miembros y roles · Suscripción de plataforma · Historial de pagos · **Casos de soporte**.
- Al pie: el nombre y el rol del usuario (lleva a **Mi perfil**) y el botón para cerrar sesión.

## 4. Estados que toda pantalla debe cubrir

Estos estados no tienen mockup propio. Se implementan con los componentes base:

- **Cargando:** esqueleto con la forma de la tabla o la tarjeta.
- **Error de red o del servidor:** un aviso con el estilo `eti-a` y el botón "Reintentar".
- **Error de validación:** el mensaje va debajo del campo, en `--alerta`, y el campo lleva un borde de ese color.
- **Sin permiso:** el menú oculta la opción. Si se entra directo por la URL, se muestra "No tiene permiso para ver esta página".
- **Acción confirmada:** un aviso breve (4 s) en la esquina superior derecha.

### Comportamiento de la estructura de navegación (DC-02)

- La API seleccionada se representa en `/panel/apis/:id/...`. Al elegir desde una página sin ID, se abre su especificación; al cambiar de API desde una sección, se conserva esa sección. Sin selección, se indica que debe elegir una API y no se crean enlaces a un ID ficticio.
- Mientras no exista el listado de DC-04, una respuesta 404 o 501 se presenta como «Sin APIs». Los demás errores muestran el aviso recuperable con «Reintentar».
- Una sesión ausente (401) redirige a `/entrar`; un error de red o servidor mantiene la dirección y ofrece reintentar. Al cerrar sesión se espera la revocación del servidor antes de salir y limpiar los datos privados del cliente; si falla, se conserva la pantalla con un aviso y reintento.
- El encabezado del proveedor muestra el nombre de su organización. Mientras el contrato provisional de sesión no lo suministre, muestra «Panel del proveedor».

### Comportamiento de las pantallas de acceso (EM-03)

- **A1.1 y A1.3:**
  - en A1.1, los errores de validación de la API (`errores` por campo) se muestran debajo de cada campo;
  - `correo_ya_registrado` se muestra debajo del correo;
  - los demás errores con `codigo` (credenciales incorrectas, cuenta bloqueada o desactivada, demasiadas peticiones) se muestran en un aviso de alerta arriba del formulario, con el mensaje de la API;
  - si falla la red o el servidor (un 5xx, aunque venga como ProblemDetails sin `codigo` del contrato), el aviso dice «No se pudo completar la solicitud. Revise su conexión e intente de nuevo.» y ofrece «Reintentar», que vuelve a enviar el formulario. Si la sesión no quedó iniciada después de entrar, se avisa lo mismo.
- **A1.2 sin `token`:** es el aviso «Revise su correo» del mockup, con el correo de `?correo=`. «Enviar el enlace otra vez» pide un enlace nuevo y confirma con «Si su correo todavía no está confirmado, le llegará un enlace nuevo en unos minutos.», un mensaje que no revela si la cuenta existe (10 §1).
- **A1.2 con `?token=`**, el destino del enlace del correo (10 §1):
  - muestra «Confirmando su correo» mientras llama a la API, una sola vez por token;
  - si la verificación funciona, consulta la sesión y lleva al destino según el rol;
  - si el enlace venció o ya se usó (`token_invalido`), muestra «Enlace no válido» y un campo de correo para pedir otro, porque el enlace no trae la dirección;
  - cualquier otro error muestra «No se pudo confirmar su correo» con el mensaje. Si es de red o del servidor, ofrece «Reintentar», y si el correo ya se confirmó y solo falló la consulta de la sesión, el reintento no vuelve a enviar el token.

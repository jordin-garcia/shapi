# Elemento de José Pablo · Correo con bandeja de salida transaccional

## Patrón y código que mostraré

**Bandeja de salida transaccional (Transactional Outbox), patrón de arquitectura.** La notificación se guarda en PostgreSQL, en `correo_saliente`, y el trabajador la entrega después por SMTP. Resuelve la coordinación entre un cambio de negocio y su aviso sin hacer depender la petición HTTP de la disponibilidad del servidor de correo. Se eligió según ADR-27 y RF-46: permite conservar el aviso pendiente y reintentar su envío en otro proceso.

Un ejemplo de mi código es soporte: `src/Shapi.Aplicacion/Soporte/GestionarCasos.cs:165-188` abre la transacción, guarda el mensaje y solicita la notificación antes de confirmar. `NotificadorCasos` encola `respuesta_caso` en ese mismo archivo, líneas 257-283. `src/Shapi.Infraestructura/Correo/ColaCorreoBaseDatos.cs:8-21` usa el contexto compartido para guardar la fila dentro de esa transacción. Después, `src/Shapi.Trabajador/Correo/ProcesadorCorreos.cs:19-62` toma correos pendientes en transacciones individuales con `FOR UPDATE SKIP LOCKED`; las líneas 64-90 renderizan, envían y registran el resultado. `src/Shapi.Trabajador/Correo/DespachadorCorreos.cs:8-35` establece el intervalo de revisión de cinco segundos y llama al procesador.

La transacción de negocio comprende el mensaje y su notificación pendiente; SMTP ocurre después. El bloqueo evita que dos procesadores tomen simultáneamente la misma fila. Esto no garantiza entrega exactamente una vez: si SMTP acepta el correo y el proceso cae antes de confirmar el estado en PostgreSQL, un envío posterior puede repetirlo. No se presenta el patrón como una transacción distribuida con SMTP.

**Clean Code que se ve en este elemento:**

1. **Responsabilidades delimitadas y nombres que revelan intención.** `ColaCorreoBaseDatos.Encolar` persiste el aviso; `ProcesadorCorreos` coordina el envío; `CorreoSaliente.RegistrarFallo` decide la transición y la próxima fecha. Los nombres `EsperasReintento`, `ProximoIntentoEn`, `MarcarEnviado` y `RegistrarFallo` expresan las reglas sin tener que deducirlas del código SMTP (`src/Shapi.Dominio/Correo/CorreoSaliente.cs:8-15`, `:49-77`).
2. **Manejo explícito de errores y reglas comprobables.** `ProcesadorCorreos.cs:76-89` propaga la cancelación solicitada y registra los otros fallos mediante la entidad. `CorreoSaliente.cs:58-77` rechaza una transición desde un estado que no es pendiente, cuenta el fallo y programa el próximo intento con `ahora`, sin consultar el reloj por su cuenta. Las esperas son 5 s, 30 s, 2 min, 10 min y 1 h: hay un intento inicial y cinco reintentos; al fallar el sexto intento se marca `fallido` y se elimina el token de los datos (`:79-89`).

Fragmento de `main`, `src/Shapi.Dominio/Correo/CorreoSaliente.cs:58-77` (20 líneas, copiado sin cambios):

```csharp
    public void RegistrarFallo(string error, DateTimeOffset ahora)
    {
        if (Estado != EstadoCorreo.Pendiente)
        {
            throw new InvalidOperationException("Solo un correo pendiente puede registrar un fallo.");
        }

        Intentos++;
        UltimoError = error;

        if (Intentos > EsperasReintento.Length)
        {
            Estado = EstadoCorreo.Fallido;
            ProximoIntentoEn = null;
            QuitarToken();
            return;
        }

        ProximoIntentoEn = ahora + EsperasReintento[Intentos - 1];
    }
```

**Pruebas automatizadas del fragmento:**

- `Shapi.Dominio.Tests.Correo.CorreoSalienteTests.RF_46_RegistrarFallo_ProgramaLosCincoReintentosConSusEsperas` (`tests/Shapi.Dominio.Tests/Correo/CorreoSalienteTests.cs:9-33`) verifica cada espera, el contador, el error y que el estado siga pendiente.
- `RF_46_RegistrarFallo_AlFallarElQuintoReintentoQuedaFallido` (misma clase y archivo, `:35-50`) comprueba el sexto intento fallido y la ausencia de una próxima fecha.
- `RF_46_RegistrarFallo_AlQuedarFallidoQuitaElTokenDeLosDatos` (misma clase y archivo, `:76-90`) verifica que el token se conserva durante los reintentos y se borra al terminar en fallo.

Para comprobarlas antes de grabar, desde la raíz (el video no muestra la terminal, solo el código de las pruebas en el editor):

```powershell
dotnet test tests/Shapi.Dominio.Tests --filter "FullyQualifiedName~Shapi.Dominio.Tests.Correo.CorreoSalienteTests"
```

Estas pruebas verifican la política de la entidad; no envían correo ni prueban la transacción de soporte o el bloqueo de PostgreSQL. En esta tarea se ejecutaron las cinco pruebas de la clase: cinco correctas, cero fallos y cero omitidas.

## Texto para narrar (2 a 3 minutos)

«Yo trabajé en la infraestructura, los correos y varias funciones de administración y soporte. Para explicar una decisión de diseño voy a mostrar la bandeja de salida transaccional, que usamos para guardar una notificación y enviarla después.

Un ejemplo es la respuesta a un caso de soporte. Aquí abro una transacción, guardo el mensaje y solicito su notificación antes de confirmar. La cola guarda una fila en correo saliente usando el mismo contexto de base de datos. Así, el mensaje y su aviso pendiente se confirman juntos. El envío por SMTP queda a cargo de otro proceso: el trabajador. Elegimos este patrón de arquitectura para que una caída del correo no impida guardar la respuesta ni haga desaparecer el aviso pendiente.

El trabajador revisa la bandeja cada cinco segundos. Toma un correo pendiente cuyo próximo intento ya corresponde y bloquea esa fila mientras la procesa. Con FOR UPDATE SKIP LOCKED, otro trabajador puede tomar la siguiente fila sin enviar simultáneamente la misma. Después renderiza la plantilla, intenta enviarla y guarda el resultado. Esto no garantiza entrega exactamente una vez: una caída entre la aceptación de SMTP y la confirmación en la base todavía puede producir un duplicado.

En este código se ven dos características de Clean Code. Primero, las responsabilidades y los nombres son claros: la cola encola, el procesador coordina y la entidad decide qué hacer con un fallo. Segundo, los errores tienen un comportamiento explícito. RegistrarFallo incrementa el contador, conserva el último error y calcula la próxima fecha con las esperas que vemos arriba: cinco segundos, treinta segundos, dos minutos, diez minutos y una hora. Son cinco reintentos además del intento inicial. Si falla el sexto intento, el correo queda fallido y se elimina el token que ya no necesita.

Finalmente, estas pruebas comprueban las cinco esperas, el estado final y la eliminación del token. Usan una fecha controlada, por lo que verifican la regla sin esperar una hora real. Así puedo explicar el diseño y demostrar automáticamente la política de reintentos que lo acompaña.»

**Orden de lo que se muestra en pantalla** (duración objetivo: 2 min 15 s, la que le da el guion de JG-29; la narración tiene 337 palabras):

| Tiempo | Archivo y líneas | Qué resaltar |
|---|---|---|
| 0:00–0:35 | `src/Shapi.Aplicacion/Soporte/GestionarCasos.cs:165-188`, `:257-283`; `src/Shapi.Infraestructura/Correo/ColaCorreoBaseDatos.cs:8-21` | Mensaje y aviso pendiente dentro de la transacción; confirmación al final. |
| 0:35–1:10 | `src/Shapi.Trabajador/Correo/DespachadorCorreos.cs:8-35`; `src/Shapi.Trabajador/Correo/ProcesadorCorreos.cs:19-62`, `:64-90` | Revisión periódica, bloqueo por fila, envío y persistencia; límite ante una caída después de SMTP. |
| 1:10–1:50 | `src/Shapi.Dominio/Correo/CorreoSaliente.cs:8-15`, `:58-89` | Esperas, fragmento anterior, nombres y transición al sexto fallo; eliminación del token. |
| 1:50–2:15 | `tests/Shapi.Dominio.Tests/Correo/CorreoSalienteTests.cs:9-50`, `:76-90` | Aserciones de fechas, estado y token. |

## Funcionalidades de José Pablo para el recorrido del video

JG-29 puede repartir la narración con esta tabla. Las pantallas corresponden al catálogo de `docs/specs/11-interfaz.md`; los pasos sin pantalla propia se muestran en la terminal, Mailpit o el informe indicado.

| Funcionalidad construida | Tareas | Pantallas o evidencia que conviene mostrar |
|---|---|---|
| Infraestructura local con HTTPS, imágenes Docker, ambiente productivo simulado y arranque con siembra en un solo comando | JZ-01, JZ-06, JZ-18 | Terminal: comando del perfil `demo` de arquitectura §7, salida 0 de `siembra-demo` y `node infra/verificar.mjs --demo`; después abrir el panel y el portal. |
| Orígenes Envíos Xelajú y Agro Precios con OpenAPI y datos de demostración | JZ-02 | A5.1 (documentación) y A5.2 (consola) de ambos portales; las pantallas son de Dominique, los orígenes son de José Pablo. |
| Siembra reproducible, cuentas, claves y datos de la demostración | JZ-05, JZ-17 | Terminal y datos poblados en A6.2, A6.4 y B3.2; JZ-17 fue terminada por Jordin a partir del problema del ambiente productivo. |
| Envío de correos, plantillas HTML/texto y marca del portal | JZ-03, JZ-11 | Mailpit en `https://correo.shapi.localhost`: correo de verificación o recuperación y aviso de respuesta a un caso; el elemento de código explica la bandeja y sus reintentos. |
| Administración de organizaciones: consulta, suspensión y reactivación | JZ-08 | A6.2 y A6.2b; comprobar el rechazo de la compuerta al suspender. |
| Cuentas de administración y soporte, activación/desactivación y definición inicial de contraseña | JZ-09 | A6.5 y A6.5b (enlace recibido en Mailpit). |
| Casos de soporte de proveedor y atención por plataforma | JZ-10 | A7.1, A7.2, A6.4 y A6.4b: abrir, asignar, responder y cerrar; mostrar el correo de la respuesta. |
| Bitácora de acciones sensibles con filtro y paginación | JZ-04 | B3.2: acción registrada, actor y periodo seleccionado. |
| Estado de los siete componentes | JZ-12 | B3.1: estados, latencia y comprobación automática; el control del reloj fue agregado por Jordin en JG-22. |
| Herramienta de capturas y pruebas de extremo a extremo de los flujos principales | JZ-07, JZ-13 | Terminal e informe Playwright; Jordin terminó la integración de JZ-13 conservando el trabajo de José Pablo. Las ejecuciones que adelantan el reloj usan un ambiente aislado y recién sembrado. |
| Pruebas de carga de 200 peticiones/s y sobrecosto p95 | JZ-14 | `docs/pruebas-de-carga.md`: tabla medida y conclusión de RNF-01/RNF-03 en el equipo documentado. |

## Referencias antes de grabar

Las líneas y el fragmento se toman de `main` en el commit `30b14fee9d6f2aba0302e61b0f168e88a09c9570`. Emilio ya eligió puertos y adaptadores; Jordin tiene asignada la cadena de responsabilidad. Dominique eligió después el método de fábrica con objeto resultado (DC-18). Se reserva aquí la bandeja de salida transaccional para evitar repetirla.

Según el aviso de Jordin para JZ-19, JG-25 y JG-27 pueden cambiar parte de este código. JG-29 debe volver a comprobar estas referencias y el fragmento contra `main` actualizado antes de la grabación del viernes 30 de octubre. No se cambió código para preparar el video.

**Comprobación de JG-29 (30 oct 2026).** Se compararon el fragmento y todas las referencias con `main` en `8dc94e0`. El fragmento sigue igual. Se corrigieron cinco rangos que cortaban un método o señalaban otras líneas: `GestionarCasos.cs:257-283` (`NotificadorCasos` completo), `ProcesadorCorreos.cs:19-62`, `:64-90` y `:76-89`, y `DespachadorCorreos.cs:8-35`, que ahora llega al `Task.Delay` de cinco segundos.

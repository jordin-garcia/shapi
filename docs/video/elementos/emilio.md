# Elemento de Emilio · Pagos sin acoplar el negocio a una pasarela

## Patrón y código que mostraré

**Puertos y adaptadores (patrón de arquitectura).** `IPasarelaPagos` es el puerto que ofrece tokenización, cobro y reembolso (`src/Shapi.Aplicacion/Pagos/IPasarelaPagos.cs:3-8`). `PasarelaSimulada` es el adaptador que lo implementa (`src/Shapi.Infraestructura/Pagos/PasarelaSimulada.cs:11-11`); `PagosModulo` lo conecta por inyección de dependencias (`src/Shapi.Api/Modulos/PagosModulo.cs:8-11`). Los flujos de contratación, renovación y reversión necesitan cobrar sin depender de los detalles de una pasarela concreta. Elegimos este diseño por ADR-12: permite demostrar pagos completos con tarjetas de prueba y deja un punto de extensión para conectar una pasarela real sin reescribir los flujos de negocio. La tarjeta se sustituye por un token; no guardamos su número ni CVV (ADR-13).

**Clean Code que se ve en este elemento:**

1. **Inversión de dependencias y responsabilidad delimitada.** Los consumidores reciben `IPasarelaPagos`, no construyen `PasarelaSimulada`: por ejemplo, `src/Shapi.Api/Suscripciones/ContratacionApi.cs:28-28` y `src/Shapi.Trabajador/CierreCiclo/CierreCiclo.cs:29-29`. La interfaz dice *qué* operaciones necesita el negocio y la implementación se ocupa de *cómo* simularlas. La prueba sustituye el puerto para observar cobros (`tests/Shapi.Api.Tests/Suscripciones/ContratacionTests.cs:816-818`).
2. **Nombres que revelan la intención y errores explícitos.** `esRenovacion` distingue el cobro inicial del recurrente; `motivoRechazo` deja visible la decisión, y `CodigosError.FondosInsuficientes` y `CodigosError.TarjetaVencida` evitan números o textos de error dispersos (`src/Shapi.Infraestructura/Pagos/PasarelaSimulada.cs:84-104`). La validación de la tarjeta queda en `TokenizarAsync`, mientras `CobrarAsync` decide el resultado del cobro.

Fragmento de `main`, `src/Shapi.Infraestructura/Pagos/PasarelaSimulada.cs:84-104` (21 líneas, copiado sin cambios):

```csharp
    public async Task<ResultadoCobro> CobrarAsync(string token, decimal monto, string referencia, bool esRenovacion)
    {
        await SimularDemoraAsync();
        if (_falla)
        {
            return new ResultadoCobro { Exitoso = false, Error = CodigosError.PasarelaNoDisponible };
        }

        // 0341 se aprueba al contratar y se rechaza en las renovaciones.
        var motivoRechazo = token switch
        {
            _ when token.StartsWith("tok_sim_0002_", StringComparison.Ordinal) => CodigosError.FondosInsuficientes,
            _ when token.StartsWith("tok_sim_0069_", StringComparison.Ordinal) => CodigosError.TarjetaVencida,
            _ when token.StartsWith("tok_sim_0341_", StringComparison.Ordinal) && esRenovacion => CodigosError.FondosInsuficientes,
            _ => null,
        };

        return motivoRechazo is null
            ? new ResultadoCobro { Exitoso = true, Referencia = $"ch_sim_{Guid.NewGuid()}" }
            : new ResultadoCobro { Exitoso = false, Error = motivoRechazo };
    }
```

**Prueba automatizada:** `Shapi.Api.Tests.Pagos.PasarelaSimuladaTests.RF_20_Tarjeta0341_SeApruebaAlContratarYSeRechazaEnLasRenovaciones` (`tests/Shapi.Api.Tests/Pagos/PasarelaSimuladaTests.cs:60-73`). Tokeniza la tarjeta, cobra al contratar y vuelve a cobrar al renovar: el primero se autoriza y el segundo devuelve `fondos_insuficientes`. La prueba `RF_20_TarjetaEspecial_ElTokenLlevaSuComportamientoYOtroProcesoLoRespeta` (`:75-89`) comprueba además que otra instancia de la pasarela reconoce el token, como ocurre en el trabajador.

## Texto para narrar (2 a 3 minutos)

«Yo trabajé en identidad, planes, suscripciones y pagos. Para explicar una decisión de diseño de esa parte, voy a mostrar cómo cobramos sin acoplar el negocio a la pasarela simulada.

Aquí está `IPasarelaPagos`, una interfaz pequeña con tres operaciones: tokenizar una tarjeta, cobrar con el token y reembolsar un cobro. Es el puerto que usa la aplicación. Los flujos de contratación y el trabajador que renueva suscripciones reciben ese puerto mediante inyección de dependencias; no crean ni conocen una pasarela concreta. `PagosModulo` enlaza el puerto con `PasarelaSimulada`. A esta organización la llamamos puertos y adaptadores. La elegimos porque el proyecto necesita ejecutar cobros de principio a fin sin una cuenta bancaria real y, al mismo tiempo, dejar un lugar claro para conectar otra pasarela en el futuro.

En este método del adaptador se ve una regla de demostración: la tarjeta de prueba terminada en 0341 se aprueba al contratar, pero falla en la renovación. Ese comportamiento viaja en el token, de modo que el trabajador, que corre en otro proceso, puede tomar la misma decisión sin compartir memoria con la API. El parámetro `esRenovacion` y la variable `motivoRechazo` dicen directamente qué se está decidiendo. Los rechazos usan códigos de error con nombre, como `FondosInsuficientes`, en vez de valores mágicos. También se separan las responsabilidades: `TokenizarAsync` valida los datos de la tarjeta y `CobrarAsync` decide el cobro usando el token. El número completo y el CVV no se guardan.

Finalmente, esta prueba ejecuta los dos cobros: confirma que el primero es exitoso y que el de renovación falla por fondos insuficientes. Otra prueba repite el cobro con una instancia nueva, como lo haría el trabajador. Así, el patrón y la regla de negocio se pueden comprobar automáticamente.»

**Orden de lo que se muestra en pantalla:**

1. `src/Shapi.Aplicacion/Pagos/IPasarelaPagos.cs:3-8`: las tres operaciones del puerto.
2. `src/Shapi.Api/Modulos/PagosModulo.cs:8-11`: la unión del puerto con el adaptador.
3. `src/Shapi.Api/Suscripciones/ContratacionApi.cs:23-29` y `src/Shapi.Trabajador/CierreCiclo/CierreCiclo.cs:25-31`: los consumidores del puerto.
4. `src/Shapi.Infraestructura/Pagos/PasarelaSimulada.cs:84-104`: el fragmento anterior; resaltar `esRenovacion`, `motivoRechazo` y los códigos de error.
5. `tests/Shapi.Api.Tests/Pagos/PasarelaSimuladaTests.cs:60-89`: primero la prueba del cobro inicial y la renovación, después la de la otra instancia.

## Funcionalidades de Emilio para el recorrido de la interfaz

| Funcionalidad construida | Pantallas que conviene mostrar |
|---|---|
| Registro del proveedor, verificación del correo, acceso y recuperación de contraseña | A1.1, A1.2, A1.3, A1.4a, A1.4b y A8.1 (perfil) |
| Suscripción de plataforma: planes, contratación, confirmación, rechazo, estado y cambio de plan con prorrateo | A2.1 a A2.5 y B1.4 |
| Planes de cada API y administración de miembros y permisos | A4.1, A4.2 y A8.2 (aceptación de invitación) |
| Invitaciones a consumidores e historial de pagos del proveedor | B1.5 y B1.3 |
| Planes de plataforma y reversión de pagos por administración | A6.1, A6.3 y A6.3b |
| Identidad del consumidor, contratación de un plan de API, claves iniciales, cambio de plan e historial de pagos | A5.1 a A5.4b, A5.8 a A5.10, B2.2, B2.3 y B2.7; las pantallas del portal son de Dominique y usan los endpoints de Emilio |
| Renovación automática, gracia, suspensión y reactivación de las suscripciones de plataforma y de API, con reloj de demostración | B1.4 y B2.3; mostrar el cobro y el cambio de estado después de adelantar el reloj |

La persistencia, los controles de aislamiento entre organizaciones y la pasarela simulada sustentan esos flujos, pero se explican con el código y las pruebas, no con una pantalla adicional.

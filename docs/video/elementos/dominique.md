# Elemento de Dominique · Validar el origen de una API antes de conectarse (RNF-10)

## Patrón y código que mostraré

**Método de fábrica con objeto resultado (patrón creacional).** `ResultadoValidacionDireccionOrigen` se crea, por convención, solo con dos métodos de fábrica con nombre, `Valida` y `Fallo` (`src/Shapi.Contratos/Red/ValidadorDireccionOrigen.cs:15-31`). Cada uno arma un resultado coherente: uno válido siempre trae la URL y sus direcciones IP, y uno fallido siempre trae el error y un detalle para la persona. `ValidadorDireccionOrigen.Validar` (`:55-108`) solo devuelve resultados creados por esas fábricas.

Problema que resuelve aquí: el proveedor escribe la URL de origen de su API, y Shapi luego se conecta a ella. Sin control, alguien podría registrar `http://10.0.0.5` o un dominio que resuelve a la red interna y usar la compuerta para atacar servicios internos (SSRF). La validación tiene tres salidas posibles (formato inválido, DNS fallido o dirección interna) y cada una debe llegar con su mensaje. Se eligió un objeto resultado con fábricas en vez de excepciones porque un rechazo es un caso esperado del negocio, no un error del programa. Además, el mismo validador vive en `Shapi.Contratos` y lo reutilizan la API de control, al registrar la API (`src/Shapi.Aplicacion/Apis/RegistrarApi.cs:74-84`), y la compuerta, al conectarse (`src/Shapi.Compuerta/Reenvio/ConexionOrigen.cs:22-28`, y la conexión a las direcciones validadas en `:30-49`). La compuerta se conecta a una de las direcciones ya comprobadas, lo que evita el *DNS rebinding*.

**Clean Code que se ve en este elemento:**

1. **Nombres que revelan la intención.** `EsValida`, `Valida`, `Fallo`, `EsDireccionNoPermitida`, `ErrorDireccionOrigen.DireccionNoPermitida` y `permitidoEnDemo` se leen como la regla de negocio: «si no está permitido en demostración y alguna dirección no está permitida, falla». No hay códigos numéricos ni banderas sin nombre.
2. **Funciones pequeñas con una sola responsabilidad.** `Validar` decide el flujo (formato, resolución, lista de demostración) y delega en `EsDireccionNoPermitida` (`:110-142`) la pregunta puramente técnica de qué rangos IP son internos. Esa función es estática y pura, así que se prueba sin red.
3. **Dependencias inyectables para pruebas legibles.** El resolver de DNS entra por el constructor (`:45-53`). En producción usa `Dns.GetHostAddressesAsync`; en las pruebas se pasa una función que devuelve las IP que la prueba necesita, sin tocar la red.

Fragmento de `main`, `src/Shapi.Contratos/Red/ValidadorDireccionOrigen.cs:13-31` (19 líneas, copiado sin cambios):

```csharp
public sealed record DireccionOrigenValidada(Uri Direccion, IReadOnlyList<IPAddress> Direcciones);

public sealed record ResultadoValidacionDireccionOrigen(
    DireccionOrigenValidada? Origen,
    ErrorDireccionOrigen? Error,
    string? Detalle)
{
    public bool EsValida => Origen is not null;

    public Uri? Direccion => Origen?.Direccion;

    public IReadOnlyList<IPAddress> Direcciones => Origen?.Direcciones ?? [];

    public static ResultadoValidacionDireccionOrigen Valida(Uri direccion, IReadOnlyList<IPAddress> direcciones) =>
        new(new DireccionOrigenValidada(direccion, direcciones), null, null);

    public static ResultadoValidacionDireccionOrigen Fallo(ErrorDireccionOrigen error, string detalle) =>
        new(null, error, detalle);
}
```

**Prueba automatizada:** `Shapi.Api.Tests.Apis.ValidadorDireccionOrigenTests` (`tests/Shapi.Api.Tests/Apis/ValidadorDireccionOrigenTests.cs`). `RNF_10_Validar_DireccionInterna_Rechaza` (`:21-41`) recorre doce direcciones internas (loopback, privadas, enlace local, multidifusión, IPv6) y espera `DireccionNoPermitida`; `RNF_10_Validar_UnaDireccionInternaEntreVarias_RechazaTodas` (`:43-51`) comprueba que basta una IP interna para rechazar; `RF_08_Validar_DireccionPublica_DevuelveLaUrlYDirecciones` (`:53-63`) comprueba el caso válido.

## Texto para narrar (2 a 3 minutos)

«Yo construí el registro y la publicación de las APIs, el portal de marca blanca y las pantallas del consumidor. Para hablar de diseño, voy a mostrar cómo Shapi decide si puede conectarse a la URL de origen que escribe un proveedor.

El riesgo es real: si aceptáramos cualquier URL, alguien podría registrar una dirección de nuestra red interna y usar la compuerta para llegar a servicios que no deberían estar expuestos. Eso se llama SSRF, y el requisito RNF-10 nos pide evitarlo.

Aquí está `ValidadorDireccionOrigen`. El método `Validar` hace tres cosas en orden: revisa que la URL sea http o https, sin usuario ni contraseña; resuelve el nombre en el DNS; y rechaza el origen si alguna de sus direcciones es interna o reservada. Esa última pregunta la responde una función aparte, `EsDireccionNoPermitida`, pequeña y sin efectos secundarios, que solo mira los rangos de IP.

Lo que quiero destacar es cómo devuelve el resultado. No lanza excepciones: usa un objeto resultado con dos métodos de fábrica, `Valida` y `Fallo`. Así es imposible crear un resultado a medias: uno válido siempre trae la URL y sus direcciones, y uno fallido siempre trae el tipo de error y un mensaje para el proveedor. Es un patrón creacional, el método de fábrica, y lo elegimos porque que una URL sea rechazada es un caso normal del negocio, no un fallo del programa.

Fíjense también en los nombres: `EsValida`, `Fallo`, `permitidoEnDemo`, `DireccionNoPermitida`. El código se lee casi como la regla. Y como el resolver de DNS se recibe en el constructor, las pruebas le pasan las IP que quieren sin salir a la red.

El mismo validador lo usan la API, al registrar la API, y la compuerta, al conectarse, que además reutiliza las direcciones ya comprobadas para que nadie cambie el DNS entre la validación y la conexión.

Por último, esta prueba recorre doce direcciones internas, de IPv4 y de IPv6, y confirma que todas se rechazan; otra confirma que basta una dirección interna entre varias para rechazar el origen.»

**Orden de lo que se muestra en pantalla:**

1. `src/Shapi.Contratos/Red/ValidadorDireccionOrigen.cs:55-108`: el método `Validar` y sus tres pasos.
2. `src/Shapi.Contratos/Red/ValidadorDireccionOrigen.cs:110-142`: `EsDireccionNoPermitida`.
3. `src/Shapi.Contratos/Red/ValidadorDireccionOrigen.cs:13-31`: el fragmento anterior; resaltar `Valida` y `Fallo`.
4. `src/Shapi.Contratos/Red/ValidadorDireccionOrigen.cs:45-53`: el resolver inyectado.
5. `src/Shapi.Aplicacion/Apis/RegistrarApi.cs:74-84` y `src/Shapi.Compuerta/Reenvio/ConexionOrigen.cs:22-49`: los dos usos.
6. `tests/Shapi.Api.Tests/Apis/ValidadorDireccionOrigenTests.cs:21-51`: las pruebas de direcciones internas.

## Funcionalidades de Dominique para el recorrido de la interfaz

| Funcionalidad construida | Pantallas que conviene mostrar |
|---|---|
| Registrar una API, lista de APIs, carga de la especificación OpenAPI y rutas expuestas | A3.1, A3.2, A3.3 y A3.4 |
| Configuración por ruta y publicación de la API | A3.5 |
| Dominio propio con DNS simulado y secreto de origen | A3.6 |
| Personalización del portal con vista previa en vivo | A3.7 |
| Portal de marca blanca: inicio y documentación | A5.0, A5.1 y A5.5 |
| Consola de pruebas del portal | A5.2 |
| Acceso del consumidor en el portal | A5.3, A5.3b y A5.7 a A5.10 |
| Planes y contratación en el portal | A5.4, A5.6 y A5.4b |
| Cuenta del consumidor: suscripción, claves, pagos y cambio de plan | B2.2 a B2.7 |
| Sitio público de Shapi | A0.1 |

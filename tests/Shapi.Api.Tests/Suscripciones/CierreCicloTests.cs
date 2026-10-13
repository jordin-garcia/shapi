using NSubstitute;
using Shapi.Api.Tests.Persistencia;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Pagos;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Bitacora;
using Shapi.Infraestructura.Correo;
using Shapi.Infraestructura.Persistencia;
using Shapi.Trabajador.CierreCiclo;

namespace Shapi.Api.Tests.Suscripciones;

[Collection(nameof(PostgresPersistencia))]
public sealed class CierreCicloTests(PostgresPersistencia postgres) : BaseDePrueba(postgres)
{
    // RF-22: el trabajador renueva el ciclo de pago y procesa una suscripción una sola vez.
    [Fact]
    public async Task RF_22_EjecutarDosVeces_NoCobraDosVeces()
    {
        var organizacion = await NuevaOrganizacion();
        var usuario = await NuevoUsuario();
        await NuevaMembresia(usuario, organizacion, "propietario");
        var plan = await NuevoPlanPlataforma();
        var siguientePlan = await NuevoPlanPlataforma();
        var medio = await NuevoMedioPago(organizacion, null);
        var suscripcion = await NuevaSuscripcionPlataforma(organizacion, plan);
        await Ejecutar($"UPDATE suscripcion_plataforma SET medio_pago_id = '{medio}', plan_siguiente_id = '{siguientePlan}', inicio = '{Reloj.Ahora.AddDays(-31):O}', fin = '{Reloj.Ahora.AddDays(-1):O}' WHERE id = '{suscripcion}'");

        var pasarela = Substitute.For<IPasarelaPagos>();
        pasarela.CobrarAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), true)
            .Returns(new ResultadoCobro { Exitoso = true, Referencia = "ch-renovacion" });
        var publicador = Substitute.For<IPublicadorCache>();
        var correo = Substitute.For<IColaCorreo>();
        var bitacora = Substitute.For<IBitacora>();

        await using (var db = CrearDb())
        {
            var trabajo = new CerrarCiclo(db, Reloj, pasarela, publicador, correo, bitacora, new OpcionesCierreCiclo());
            await trabajo.EjecutarAsync();
            await trabajo.EjecutarAsync();
        }

        (await Escalar<string>($"SELECT estado FROM suscripcion_plataforma WHERE id = '{suscripcion}'")).Should().Be("activa");
        (await Escalar<Guid>($"SELECT plan_id FROM suscripcion_plataforma WHERE id = '{suscripcion}'")).Should().Be(siguientePlan);
        (await Escalar<DateTime>($"SELECT inicio FROM suscripcion_plataforma WHERE id = '{suscripcion}'")).Should().Be(Reloj.Ahora.AddDays(-1).UtcDateTime);
        (await Escalar<long>($"SELECT count(*) FROM pago WHERE suscripcion_plataforma_id = '{suscripcion}' AND concepto = 'renovacion' AND estado = 'autorizado'")).Should().Be(1);
        await pasarela.Received(1).CobrarAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), true);
        await publicador.Received(1).PublicarSuscripcion(suscripcion, Arg.Any<CancellationToken>());
    }

    // RF-22: el rechazo pone la suscripción en gracia siete días desde el fin vencido.
    [Fact]
    public async Task RF_22_RenovacionRechazada_EntraEnGraciaYEncolaAvisos()
    {
        var organizacion = await NuevaOrganizacion();
        var usuario = await NuevoUsuario();
        await NuevaMembresia(usuario, organizacion, "propietario");
        var plan = await NuevoPlanPlataforma();
        var medio = await NuevoMedioPago(organizacion, null);
        var suscripcion = await NuevaSuscripcionPlataforma(organizacion, plan);
        var finAnterior = Reloj.Ahora.AddDays(-1);
        await Ejecutar($"UPDATE suscripcion_plataforma SET medio_pago_id = '{medio}', inicio = '{finAnterior.AddDays(-30):O}', fin = '{finAnterior:O}' WHERE id = '{suscripcion}'");

        var pasarela = Substitute.For<IPasarelaPagos>();
        pasarela.CobrarAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), true)
            .Returns(new ResultadoCobro { Exitoso = false, Error = "fondos_insuficientes" });

        await using (var db = CrearDb())
        {
            await new CerrarCiclo(db, Reloj, pasarela, Substitute.For<IPublicadorCache>(), new ColaCorreoBaseDatos(db, Reloj),
                Substitute.For<IBitacora>(), new OpcionesCierreCiclo()).EjecutarAsync();
        }

        (await Escalar<string>($"SELECT estado FROM suscripcion_plataforma WHERE id = '{suscripcion}'")).Should().Be("en_gracia");
        (await Escalar<DateTime>($"SELECT gracia_hasta FROM suscripcion_plataforma WHERE id = '{suscripcion}'")).Should().Be(finAnterior.AddDays(7).UtcDateTime);
        (await Escalar<long>($"SELECT count(*) FROM pago WHERE suscripcion_plataforma_id = '{suscripcion}' AND concepto = 'renovacion' AND estado = 'rechazado'")).Should().Be(1);
        (await Escalar<long>($"SELECT count(*) FROM correo_saliente WHERE destinatario = (SELECT correo FROM usuario WHERE id = '{usuario}') AND plantilla IN ('pago_rechazado','suscripcion_en_gracia')")).Should().Be(2);
    }

    // RF-23: al vencer la gracia se suspende, se publica y se registra como sistema.
    [Fact]
    public async Task RF_23_GraciaVencida_SuspendeYRegistra()
    {
        var organizacion = await NuevaOrganizacion();
        var usuario = await NuevoUsuario();
        await NuevaMembresia(usuario, organizacion, "propietario");
        var plan = await NuevoPlanPlataforma();
        var suscripcion = await NuevaSuscripcionPlataforma(organizacion, plan, "en_gracia");
        await Ejecutar($"UPDATE suscripcion_plataforma SET inicio = '{Reloj.Ahora.AddDays(-37):O}', fin = '{Reloj.Ahora.AddDays(-7):O}', gracia_hasta = '{Reloj.Ahora:O}' WHERE id = '{suscripcion}'");
        var publicador = Substitute.For<IPublicadorCache>();
        var correo = Substitute.For<IColaCorreo>();

        await using (var db = CrearDb())
        {
            await new CerrarCiclo(db, Reloj, Substitute.For<IPasarelaPagos>(), publicador, correo, new BitacoraBaseDatos(db, Reloj),
                new OpcionesCierreCiclo()).EjecutarAsync();
        }

        (await Escalar<string>($"SELECT estado FROM suscripcion_plataforma WHERE id = '{suscripcion}'")).Should().Be("suspendida");
        (await Escalar<long>($"SELECT count(*) FROM bitacora WHERE accion = 'suscripcion.suspendida' AND actor_tipo = 'sistema' AND organizacion_id = '{organizacion}'")).Should().Be(1);
        await publicador.Received(1).PublicarSuscripcion(suscripcion, Arg.Any<CancellationToken>());
        await correo.Received(1).Encolar("suscripcion_suspendida", Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    // RF-22: la Prueba termina en gracia y no intenta cobrar.
    [Fact]
    public async Task RF_22_PruebaVencida_EntraEnGraciaSinCobro()
    {
        var organizacion = await NuevaOrganizacion();
        var usuario = await NuevoUsuario();
        await NuevaMembresia(usuario, organizacion, "propietario");
        var plan = await NuevoPlanPlataforma(esPrueba: true);
        var suscripcion = await NuevaSuscripcionPlataforma(organizacion, plan);
        var finAnterior = Reloj.Ahora.AddDays(-1);
        await Ejecutar($"UPDATE suscripcion_plataforma SET inicio = '{finAnterior.AddDays(-30):O}', fin = '{finAnterior:O}' WHERE id = '{suscripcion}'");
        var pasarela = Substitute.For<IPasarelaPagos>();

        await using (var db = CrearDb())
        {
            await new CerrarCiclo(db, Reloj, pasarela, Substitute.For<IPublicadorCache>(), new ColaCorreoBaseDatos(db, Reloj),
                Substitute.For<IBitacora>(), new OpcionesCierreCiclo()).EjecutarAsync();
        }

        (await Escalar<string>($"SELECT estado FROM suscripcion_plataforma WHERE id = '{suscripcion}'")).Should().Be("en_gracia");
        (await Escalar<DateTime>($"SELECT gracia_hasta FROM suscripcion_plataforma WHERE id = '{suscripcion}'")).Should().Be(finAnterior.AddDays(7).UtcDateTime);
        await pasarela.DidNotReceive().CobrarAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<bool>());
        (await Escalar<long>($"SELECT count(*) FROM correo_saliente WHERE destinatario = (SELECT correo FROM usuario WHERE id = '{usuario}') AND plantilla = 'suscripcion_en_gracia'")).Should().Be(1);
    }

    // RF-46: el aviso de una Prueba que vence se encola una sola vez aun si el trabajo se ejecuta repetidamente.
    [Fact]
    public async Task RF_46_PruebaPorVencer_EncolaUnaSolaVez()
    {
        var organizacion = await NuevaOrganizacion();
        var usuario = await NuevoUsuario();
        await NuevaMembresia(usuario, organizacion, "propietario");
        var plan = await NuevoPlanPlataforma(esPrueba: true);
        var suscripcion = await NuevaSuscripcionPlataforma(organizacion, plan);
        await Ejecutar($"UPDATE suscripcion_plataforma SET inicio = '{Reloj.Ahora.AddDays(-8):O}', fin = '{Reloj.Ahora.AddDays(6):O}' WHERE id = '{suscripcion}'");

        await using (var db = CrearDb())
        {
            var correo = new Shapi.Infraestructura.Correo.ColaCorreoBaseDatos(db, Reloj);
            var trabajo = new CerrarCiclo(db, Reloj, Substitute.For<IPasarelaPagos>(), Substitute.For<IPublicadorCache>(),
                correo, Substitute.For<IBitacora>(), new OpcionesCierreCiclo());
            await trabajo.EjecutarAsync();
            await trabajo.EjecutarAsync();
        }

        (await Escalar<long>($"SELECT count(*) FROM correo_saliente WHERE destinatario = (SELECT correo FROM usuario WHERE id = '{usuario}') AND plantilla = 'prueba_por_vencer'")).Should().Be(1);
    }

    // RF-22: un plan gratuito de API renueva el ciclo sin acudir a la pasarela.
    [Fact]
    public async Task RF_22_PlanApiGratuito_RenuevaSinCobro()
    {
        var organizacion = await NuevaOrganizacion();
        var api = await NuevaApi(organizacion);
        var consumidor = await NuevoConsumidor(organizacion);
        var plan = await NuevoPlanApi(api);
        var siguientePlan = await NuevoPlanApi(api);
        await Ejecutar($"UPDATE plan_api SET precio = 0, es_gratuito = true WHERE id = '{plan}'");
        var suscripcion = await NuevaSuscripcionApi(consumidor, api, plan);
        var finAnterior = Reloj.Ahora.AddDays(-1);
        await Ejecutar($"UPDATE suscripcion_api SET inicio = '{finAnterior.AddDays(-30):O}', fin = '{finAnterior:O}' WHERE id = '{suscripcion}'");
        var pasarela = Substitute.For<IPasarelaPagos>();

        await using (var db = CrearDb())
        {
            await new CerrarCiclo(db, Reloj, pasarela, Substitute.For<IPublicadorCache>(), Substitute.For<IColaCorreo>(),
                Substitute.For<IBitacora>(), new OpcionesCierreCiclo()).EjecutarAsync();
        }

        (await Escalar<string>($"SELECT estado FROM suscripcion_api WHERE id = '{suscripcion}'")).Should().Be("activa");
        (await Escalar<DateTime>($"SELECT inicio FROM suscripcion_api WHERE id = '{suscripcion}'")).Should().Be(finAnterior.UtcDateTime);
        (await Escalar<long>($"SELECT count(*) FROM pago WHERE suscripcion_api_id = '{suscripcion}'")).Should().Be(0);
        await pasarela.DidNotReceive().CobrarAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<bool>());
    }

    // RF-22: el pago autorizado de la renovación de API guarda el pago y abre el ciclo desde el fin anterior.
    [Fact]
    public async Task RF_22_RenovacionApiAutorizada_GuardaPagoYPublica()
    {
        var organizacion = await NuevaOrganizacion();
        var api = await NuevaApi(organizacion);
        var consumidor = await NuevoConsumidor(organizacion);
        var plan = await NuevoPlanApi(api);
        var siguientePlan = await NuevoPlanApi(api);
        var medio = await NuevoMedioPago(null, consumidor);
        var suscripcion = await NuevaSuscripcionApi(consumidor, api, plan);
        var finAnterior = Reloj.Ahora.AddDays(-1);
        await Ejecutar($"UPDATE suscripcion_api SET medio_pago_id = '{medio}', plan_siguiente_id = '{siguientePlan}', inicio = '{finAnterior.AddDays(-30):O}', fin = '{finAnterior:O}' WHERE id = '{suscripcion}'");
        var pasarela = Substitute.For<IPasarelaPagos>();
        pasarela.CobrarAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), true)
            .Returns(new ResultadoCobro { Exitoso = true, Referencia = "ch-api-renovacion" });
        var publicador = Substitute.For<IPublicadorCache>();

        await using (var db = CrearDb())
        {
            await new CerrarCiclo(db, Reloj, pasarela, publicador, Substitute.For<IColaCorreo>(),
                Substitute.For<IBitacora>(), new OpcionesCierreCiclo()).EjecutarAsync();
        }

        (await Escalar<string>($"SELECT estado FROM suscripcion_api WHERE id = '{suscripcion}'")).Should().Be("activa");
        (await Escalar<Guid>($"SELECT plan_id FROM suscripcion_api WHERE id = '{suscripcion}'")).Should().Be(siguientePlan);
        (await Escalar<DateTime>($"SELECT inicio FROM suscripcion_api WHERE id = '{suscripcion}'")).Should().Be(finAnterior.UtcDateTime);
        (await Escalar<long>($"SELECT count(*) FROM pago WHERE suscripcion_api_id = '{suscripcion}' AND concepto = 'renovacion' AND estado = 'autorizado'")).Should().Be(1);
        await publicador.Received(1).PublicarSuscripcion(suscripcion, Arg.Any<CancellationToken>());
    }

    // RF-22: el rechazo de una renovación API registra el pago, entra en gracia y conserva la marca del portal en los correos.
    [Fact]
    public async Task RF_22_RenovacionApiRechazada_EntraEnGraciaYEncolaCorreosConLogoPortal()
    {
        var organizacion = await NuevaOrganizacion();
        var api = await NuevaApi(organizacion);
        await Ejecutar($"UPDATE api SET portal_logo = decode('89504E470D0A1A0A', 'hex'), portal_logo_tipo = 'image/png' WHERE id = '{api}'");
        var consumidor = await NuevoConsumidor(organizacion);
        var plan = await NuevoPlanApi(api);
        var medio = await NuevoMedioPago(null, consumidor);
        var suscripcion = await NuevaSuscripcionApi(consumidor, api, plan);
        var finAnterior = Reloj.Ahora.AddDays(-1);
        await Ejecutar($"UPDATE suscripcion_api SET medio_pago_id = '{medio}', inicio = '{finAnterior.AddDays(-30):O}', fin = '{finAnterior:O}' WHERE id = '{suscripcion}'");
        var pasarela = Substitute.For<IPasarelaPagos>();
        pasarela.CobrarAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), true)
            .Returns(new ResultadoCobro { Exitoso = false, Error = "fondos_insuficientes" });

        await using (var db = CrearDb())
        {
            var cola = new Shapi.Infraestructura.Correo.ColaCorreoBaseDatos(db, Reloj);
            await new CerrarCiclo(db, Reloj, pasarela, Substitute.For<IPublicadorCache>(), cola,
                Substitute.For<IBitacora>(), new OpcionesCierreCiclo()).EjecutarAsync();
        }

        (await Escalar<string>($"SELECT estado FROM suscripcion_api WHERE id = '{suscripcion}'")).Should().Be("en_gracia");
        (await Escalar<DateTime>($"SELECT gracia_hasta FROM suscripcion_api WHERE id = '{suscripcion}'")).Should().Be(finAnterior.AddDays(7).UtcDateTime);
        (await Escalar<long>($"SELECT count(*) FROM pago WHERE suscripcion_api_id = '{suscripcion}' AND concepto = 'renovacion' AND estado = 'rechazado'")).Should().Be(1);
        (await Escalar<long>($"SELECT count(*) FROM correo_saliente WHERE destinatario = (SELECT correo FROM consumidor WHERE id = '{consumidor}') AND plantilla IN ('pago_rechazado','suscripcion_en_gracia') AND datos->>'logoPortal' = 'true'")).Should().Be(2);
    }

    // RF-23: la suscripción de API también se suspende al vencer la gracia y queda en la bitácora.
    [Fact]
    public async Task RF_23_GraciaApiVencida_SuspendeYRegistra()
    {
        var organizacion = await NuevaOrganizacion();
        var api = await NuevaApi(organizacion);
        var consumidor = await NuevoConsumidor(organizacion);
        var plan = await NuevoPlanApi(api);
        var suscripcion = await NuevaSuscripcionApi(consumidor, api, plan, "en_gracia");
        await Ejecutar($"UPDATE suscripcion_api SET inicio = '{Reloj.Ahora.AddDays(-37):O}', fin = '{Reloj.Ahora.AddDays(-7):O}', gracia_hasta = '{Reloj.Ahora:O}' WHERE id = '{suscripcion}'");
        var publicador = Substitute.For<IPublicadorCache>();

        await using (var db = CrearDb())
        {
            await new CerrarCiclo(db, Reloj, Substitute.For<IPasarelaPagos>(), publicador, Substitute.For<IColaCorreo>(),
                new BitacoraBaseDatos(db, Reloj), new OpcionesCierreCiclo()).EjecutarAsync();
        }

        (await Escalar<string>($"SELECT estado FROM suscripcion_api WHERE id = '{suscripcion}'")).Should().Be("suspendida");
        (await Escalar<long>($"SELECT count(*) FROM bitacora WHERE accion = 'suscripcion.suspendida' AND actor_tipo = 'sistema' AND organizacion_id = '{organizacion}'")).Should().Be(1);
        await publicador.Received(1).PublicarSuscripcion(suscripcion, Arg.Any<CancellationToken>());
    }
}

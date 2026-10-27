using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shapi.Api.Tests.Persistencia;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Pagos;
using Shapi.Dominio.Apis;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Bitacora;
using Shapi.Infraestructura.Correo;
using Shapi.Infraestructura.Persistencia;
using Shapi.Trabajador.CierreCiclo;

namespace Shapi.Api.Tests.Suscripciones;

[Collection(nameof(PostgresPersistencia))]
public sealed class CierreCicloTests(PostgresPersistencia postgres) : BaseDePrueba(postgres)
{
    // RF-25, RF-43: la bajada desconecta todos los dominios solo cuando se aplica, después del commit.
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task RF_25_RF_43_Bajada_DominiosSeDesconectanAlAplicar(bool permiteDominio, bool rechazarPago)
    {
        var org = await NuevaOrganizacion();
        var otraOrg = await NuevaOrganizacion();
        var actual = await NuevoPlanPlataforma();
        var destino = await NuevoPlanPlataforma();
        await Ejecutar($"UPDATE plan_plataforma SET dominio_propio = true WHERE id = '{actual}'");
        await Ejecutar($"UPDATE plan_plataforma SET dominio_propio = {permiteDominio} WHERE id = '{destino}'");
        var sub = await NuevaSuscripcionPlataforma(org, actual);
        var medio = await NuevoMedioPago(org, null);
        var fin = Reloj.Ahora.AddDays(1);
        await Ejecutar($"UPDATE suscripcion_plataforma SET medio_pago_id = '{medio}', plan_siguiente_id = '{destino}', inicio = '{fin.AddDays(-30):O}', fin = '{fin:O}' WHERE id = '{sub}'");
        var apis = new[] { await NuevaApi(org), await NuevaApi(org), await NuevaApi(org), await NuevaApi(otraOrg) };
        await using var db = CrearDb();
        var dominios = apis.Select((id, i) => new DominioPropio(id, $"api{i}.jg24.localhost", $"api{i}.api.shapi.localhost", Reloj.Ahora)).ToList();
        dominios[0].Verificar(dominios[0].DestinoCname, Reloj.Ahora);
        dominios[2].MarcarFallido(Reloj.Ahora);
        db.AddRange(dominios);
        await db.SaveChangesAsync();
        var publicador = Substitute.For<IPublicadorCache>();
        publicador.EliminarHost(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(async llamada =>
        {
            await using var comprobacion = CrearDb();
            // Otro contexto solo ve la eliminación después de confirmar la transacción.
            (await comprobacion.Set<DominioPropio>().IgnoreQueryFilters().AnyAsync(d => d.ApiId == llamada.Arg<Guid>())).Should().BeFalse();
        });
        var pasarela = Substitute.For<IPasarelaPagos>();
        pasarela.CobrarAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), true)
            .Returns(rechazarPago ? new ResultadoCobro { Error = "pago_rechazado" } : new ResultadoCobro { Exitoso = true, Referencia = "ch-dominio-jg24" });
        var trabajo = new CerrarCiclo(db, Reloj, pasarela, publicador, Substitute.For<IColaCorreo>(), Substitute.For<IBitacora>(), new OpcionesCierreCiclo());
        await trabajo.EjecutarAsync();
        (await db.Set<DominioPropio>().IgnoreQueryFilters().CountAsync()).Should().Be(4);
        await publicador.DidNotReceive().EliminarHost(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        Reloj.Ahora = fin;
        await trabajo.EjecutarAsync();
        (await Escalar<Guid>($"SELECT plan_id FROM suscripcion_plataforma WHERE id = '{sub}'")).Should().Be(destino);
        (await db.Set<DominioPropio>().IgnoreQueryFilters().CountAsync()).Should().Be(permiteDominio ? 4 : 1);
        foreach (var dominio in dominios.Take(3))
        {
            if (permiteDominio)
            {
                await publicador.DidNotReceive().EliminarHost(dominio.Dominio, dominio.ApiId, Arg.Any<CancellationToken>());
            }
            else
            {
                await publicador.Received(1).EliminarHost(dominio.Dominio, dominio.ApiId, Arg.Any<CancellationToken>());
                await publicador.Received(1).PublicarApi(dominio.ApiId, Arg.Any<CancellationToken>());
            }
        }
        await publicador.DidNotReceive().PublicarApi(apis[3], Arg.Any<CancellationToken>());
    }

    // RF-22: el fallo de la segunda renovación no deshace ni repite el primer cobro, en ambos niveles.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RF_22_FallaSegundaSuscripcion_ConservaPrimeraYNoRepiteCobro(bool api)
    {
        var org = await NuevaOrganizacion();
        var apiId = await NuevaApi(org);
        var consumidor = await NuevoConsumidor(org);
        var consumidor2 = await NuevoConsumidor(org);
        var org2 = await NuevaOrganizacion();
        var plan = api ? await NuevoPlanApi(apiId) : await NuevoPlanPlataforma();
        var medio = await NuevoMedioPago(api ? null : org, api ? consumidor : null);
        var medio2 = await NuevoMedioPago(api ? null : org2, api ? consumidor2 : null);
        var primera = api ? await NuevaSuscripcionApi(consumidor, apiId, plan) : await NuevaSuscripcionPlataforma(org, plan);
        var segunda = api ? await NuevaSuscripcionApi(consumidor2, apiId, plan) : await NuevaSuscripcionPlataforma(org2, plan);
        var tabla = api ? "suscripcion_api" : "suscripcion_plataforma";
        var columna = api ? "suscripcion_api_id" : "suscripcion_plataforma_id";
        var fin = Reloj.Ahora.AddDays(-1);
        await Ejecutar($"UPDATE {tabla} SET medio_pago_id = '{medio}', inicio = '{fin.AddDays(-30):O}', fin = '{fin:O}' WHERE id = '{primera}'");
        await Ejecutar($"UPDATE {tabla} SET medio_pago_id = '{medio2}', inicio = '{fin.AddHours(1).AddDays(-30):O}', fin = '{fin.AddHours(1):O}' WHERE id = '{segunda}'");
        var pasarela = Substitute.For<IPasarelaPagos>();
        var referencias = new List<string>();
        pasarela.CobrarAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), true).Returns(llamada =>
        {
            var referencia = llamada.ArgAt<string>(2);
            referencias.Add(referencia);
            if (referencia.Contains(segunda.ToString()))
            {
                throw new InvalidOperationException("Fallo de la segunda suscripción");
            }

            return new ResultadoCobro { Exitoso = true, Referencia = "ch-primera" };
        });
        var publicador = Substitute.For<IPublicadorCache>();
        for (var intento = 0; intento < 2; intento++)
        {
            await using var db = CrearDb();
            var trabajo = new CerrarCiclo(db, Reloj, pasarela, publicador, Substitute.For<IColaCorreo>(), Substitute.For<IBitacora>(), new OpcionesCierreCiclo());
            await Assert.ThrowsAsync<InvalidOperationException>(() => trabajo.EjecutarAsync());
            (await Escalar<long>($"SELECT count(*) FROM pago WHERE {columna} = '{primera}' AND estado = 'autorizado'")).Should().Be(1);
            (await Escalar<DateTime>($"SELECT inicio FROM {tabla} WHERE id = '{primera}'")).Should().Be(fin.UtcDateTime);
        }
        referencias.Count(x => x.Contains(primera.ToString())).Should().Be(1);
        await publicador.Received(1).PublicarSuscripcion(primera, Arg.Any<CancellationToken>());
    }

    // RF-22: si la pasarela autoriza pero PostgreSQL rechaza el pago, se reembolsa y no se publica.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RF_22_FallaGuardarPago_ReembolsaYConservaCiclo(bool api)
    {
        var org = await NuevaOrganizacion();
        var apiId = await NuevaApi(org);
        var consumidor = await NuevoConsumidor(org);
        var plan = api ? await NuevoPlanApi(apiId) : await NuevoPlanPlataforma();
        var medio = await NuevoMedioPago(api ? null : org, api ? consumidor : null);
        var suscripcion = api ? await NuevaSuscripcionApi(consumidor, apiId, plan) : await NuevaSuscripcionPlataforma(org, plan);
        var tabla = api ? "suscripcion_api" : "suscripcion_plataforma";
        var fin = Reloj.Ahora.AddDays(-1);
        await Ejecutar($"UPDATE {tabla} SET medio_pago_id = '{medio}', inicio = '{fin.AddDays(-30):O}', fin = '{fin:O}' WHERE id = '{suscripcion}'");
        if (!api)
        {
            // RF-25, RF-43: la desconexión también debe revertirse si falla el guardado del cobro.
            var siguiente = await NuevoPlanPlataforma();
            await Ejecutar($"UPDATE suscripcion_plataforma SET plan_siguiente_id = '{siguiente}' WHERE id = '{suscripcion}'");
            await using var preparacion = CrearDb();
            preparacion.Add(new DominioPropio(apiId, "rollback.jg24.localhost", "api.api.shapi.localhost", Reloj.Ahora));
            await preparacion.SaveChangesAsync();
        }
        await Ejecutar("ALTER TABLE pago ADD CONSTRAINT fallo_pago_jg23 CHECK (referencia_pasarela <> 'ch-falla-guardar') NOT VALID");
        var pasarela = Substitute.For<IPasarelaPagos>();
        pasarela.CobrarAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), true)
            .Returns(new ResultadoCobro { Exitoso = true, Referencia = "ch-falla-guardar" });
        pasarela.ReembolsarAsync("ch-falla-guardar").Returns(new ResultadoReembolso { Exitoso = true, Referencia = "re-falla-guardar" });
        var publicador = Substitute.For<IPublicadorCache>();
        await using var db = CrearDb();
        var trabajo = new CerrarCiclo(db, Reloj, pasarela, publicador, Substitute.For<IColaCorreo>(), Substitute.For<IBitacora>(), new OpcionesCierreCiclo());
        await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(() => trabajo.EjecutarAsync());
        await pasarela.Received(1).ReembolsarAsync("ch-falla-guardar");
        await publicador.DidNotReceive().PublicarSuscripcion(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        (await Escalar<DateTime>($"SELECT fin FROM {tabla} WHERE id = '{suscripcion}'")).Should().Be(fin.UtcDateTime);
        (await Escalar<long>("SELECT count(*) FROM pago WHERE referencia_pasarela = 'ch-falla-guardar'")).Should().Be(0);
        if (!api)
        {
            (await Escalar<long>($"SELECT count(*) FROM dominio_propio WHERE api_id = '{apiId}'")).Should().Be(1);
            await publicador.DidNotReceive().EliminarHost(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
            await publicador.DidNotReceive().PublicarApi(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        }
    }

    // RF-17: editar el precio no modifica el ciclo vigente y se cobra al renovar.
    [Fact]
    public async Task RF_17_PrecioEditado_SeCobraEnLaSiguienteRenovacion()
    {
        var org = await NuevaOrganizacion();
        var plan = await NuevoPlanPlataforma();
        var medio = await NuevoMedioPago(org, null);
        var suscripcion = await NuevaSuscripcionPlataforma(org, plan);
        var fin = Reloj.Ahora.AddDays(1);
        await Ejecutar($"UPDATE suscripcion_plataforma SET medio_pago_id = '{medio}', inicio = '{fin.AddDays(-30):O}', fin = '{fin:O}' WHERE id = '{suscripcion}'");
        await Ejecutar($"UPDATE plan_plataforma SET precio = 299 WHERE id = '{plan}'");
        var pasarela = Substitute.For<IPasarelaPagos>();
        pasarela.CobrarAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), true)
            .Returns(new ResultadoCobro { Exitoso = true, Referencia = "ch-precio-editado" });
        await using var db = CrearDb();
        var trabajo = new CerrarCiclo(db, Reloj, pasarela, Substitute.For<IPublicadorCache>(), Substitute.For<IColaCorreo>(), Substitute.For<IBitacora>(), new OpcionesCierreCiclo());
        await trabajo.EjecutarAsync();
        await pasarela.DidNotReceive().CobrarAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), true);
        (await Escalar<DateTime>($"SELECT fin FROM suscripcion_plataforma WHERE id = '{suscripcion}'")).Should().Be(fin.UtcDateTime);
        Reloj.Ahora = fin;
        await trabajo.EjecutarAsync();
        await pasarela.Received(1).CobrarAsync(Arg.Any<string>(), 299, Arg.Any<string>(), true);
        (await Escalar<decimal>($"SELECT monto FROM pago WHERE suscripcion_plataforma_id = '{suscripcion}'")).Should().Be(299);
    }

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

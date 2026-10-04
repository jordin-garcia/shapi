using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Shapi.Trabajador.Siembra;
using Xunit;

namespace Shapi.Api.Tests.Siembra;

// RNF-14 · Criterio 6 de JZ-05 (auditoría 2026-10-03, H-33): el comando sembrar-demo del trabajador.
public sealed class ComandoSembrarDemoTests
{
    [Theory]
    [InlineData(new[] { "sembrar-demo" }, true)]
    [InlineData(new[] { "SEMBRAR-DEMO", "--reiniciar" }, true)]
    [InlineData(new string[0], false)]
    [InlineData(new[] { "otro" }, false)]
    public void RNF_14_ReconoceElComando(string[] args, bool esperado) =>
        ComandoSembrarDemo.EsElComando(args).Should().Be(esperado);

    [Fact]
    public async Task RNF_14_LeeLaConfiguracionSiembraYDespuesResincroniza()
    {
        var configuracion = Configuracion(("SHAPI_MODO_DEMO", "true"), ("SHAPI_URL_ORIGEN_ENVIOS", "http://envios-propio"),
            ("SHAPI_SECRETO_ORIGEN_ENVIOS", "shps_envios"), ("SHAPI_SECRETO_ORIGEN_AGRO", "shps_agro"));
        var pasos = new List<string>();
        (bool ModoDemo, bool Reiniciar, string Envios, string Agro, string? SecretoEnvios, string? SecretoAgro)? recibido = null;

        await ComandoSembrarDemo.EjecutarAsync(["sembrar-demo", "--reiniciar"], configuracion, esProduccion: true,
            (modoDemo, reiniciar, envios, agro, secretoEnvios, secretoAgro) =>
            {
                pasos.Add("sembrar");
                recibido = (modoDemo, reiniciar, envios, agro, secretoEnvios, secretoAgro);
                return Task.CompletedTask;
            },
            () =>
            {
                pasos.Add("resincronizar");
                return Task.CompletedTask;
            });

        pasos.Should().Equal("sembrar", "resincronizar");
        recibido.Should().Be((true, true, "http://envios-propio", "http://origen-agro:8080", "shps_envios", "shps_agro"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    [InlineData("no")]
    public async Task RNF_14_SinModoDemo_PasaFalseALaSiembra(string? valor)
    {
        // La siembra rechaza el modo falso (RNF_14_SinModoDemo_RechazaLaSiembra); aquí, que el comando lo lea bien.
        var configuracion = valor is null ? Configuracion() : Configuracion(("SHAPI_MODO_DEMO", valor));
        bool? modoDemo = null;
        var reiniciar = true;

        await ComandoSembrarDemo.EjecutarAsync(["sembrar-demo"], configuracion, esProduccion: false,
            (modo, reinicio, _, _, _, _) =>
            {
                modoDemo = modo;
                reiniciar = reinicio;
                return Task.CompletedTask;
            },
            () => Task.CompletedTask);

        modoDemo.Should().BeFalse();
        reiniciar.Should().BeFalse("sin --reiniciar");
    }

    [Fact]
    public async Task RNF_14_SiLaSiembraFalla_NoResincroniza()
    {
        var resincronizo = false;

        var accion = () => ComandoSembrarDemo.EjecutarAsync(["sembrar-demo"], Configuracion(("SHAPI_MODO_DEMO", "false")), false,
            (_, _, _, _, _, _) => throw new InvalidOperationException("La siembra de demostración solo funciona con SHAPI_MODO_DEMO=true."),
            () =>
            {
                resincronizo = true;
                return Task.CompletedTask;
            });

        await accion.Should().ThrowAsync<InvalidOperationException>();
        resincronizo.Should().BeFalse();
    }

    private static IConfiguration Configuracion(params (string Clave, string Valor)[] valores) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(valores.Select(v => new KeyValuePair<string, string?>(v.Clave, v.Valor)))
            .Build();
}

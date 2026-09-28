using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Xunit;

namespace Shapi.Api.Tests.Comun;

/// <summary>06 §8: los tres procesos escriben registros estructurados en JSON en la salida estándar.</summary>
public class RegistrosJsonTests
{
    [Theory]
    [InlineData("Shapi.Api")]
    [InlineData("Shapi.Compuerta")]
    [InlineData("Shapi.Trabajador")]
    public void Proceso_EscribeSusRegistrosDeConsolaEnJson(string proyecto)
    {
        var configuracion = new ConfigurationBuilder()
            .AddJsonFile(RaizRepositorio.Ruta("src", proyecto, "appsettings.json"))
            .Build();
        using var servicios = new ServiceCollection()
            .AddLogging(registro => registro.AddConfiguration(configuracion.GetSection("Logging")).AddConsole())
            .BuildServiceProvider();

        var opciones = servicios.GetRequiredService<IOptionsMonitor<ConsoleLoggerOptions>>().CurrentValue;

        opciones.FormatterName.Should().Be(ConsoleFormatterNames.Json);
    }
}

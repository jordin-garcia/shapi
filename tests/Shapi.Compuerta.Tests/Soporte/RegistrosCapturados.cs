using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Shapi.Compuerta.Tests.Soporte;

/// <summary>Guarda en memoria todo lo que la compuerta escribe en sus registros, para buscar datos sensibles.</summary>
public sealed class RegistrosCapturados : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _lineas = new();

    public IReadOnlyCollection<string> Lineas => _lineas;

    public ILogger CreateLogger(string categoryName) => new Registrador(categoryName, _lineas);

    public void Dispose()
    {
    }

    private sealed class Registrador(string categoria, ConcurrentQueue<string> lineas) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            lineas.Enqueue($"{categoria} [ámbito] {state}");
            return null;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            lineas.Enqueue($"{categoria} [{logLevel}] {formatter(state, exception)} {state} {exception}");
        }
    }
}

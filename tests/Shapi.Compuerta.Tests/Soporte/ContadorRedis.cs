using System.Collections.Concurrent;
using System.Reflection;
using StackExchange.Redis;

namespace Shapi.Compuerta.Tests.Soporte;

/// <summary>
/// Cuenta los viajes a Redis de la compuerta (08 §8): envuelve la conexión real y registra cada comando suelto como un
/// viaje y cada lote (<c>CreateBatch</c> + <c>Execute</c>) como un solo viaje con todos sus comandos.
/// </summary>
public sealed class ContadorRedis
{
    private readonly ConcurrentQueue<IReadOnlyList<string>> _viajes = new();

    /// <summary>Cada viaje, con sus comandos como "<c>Metodo llave</c>".</summary>
    public IReadOnlyList<IReadOnlyList<string>> Viajes => [.. _viajes];

    public void Reiniciar() => _viajes.Clear();

    /// <summary>La conexión envuelta. No cierra la real al desecharse: es la del entorno compartido.</summary>
    public IConnectionMultiplexer Envolver(IConnectionMultiplexer real) => Proxy<IConnectionMultiplexer>.Crear(real, this);

    private void Registrar(IReadOnlyList<string> comandos) => _viajes.Enqueue(comandos);

    private static string Describir(MethodInfo metodo, object?[]? argumentos) =>
        argumentos is [RedisKey llave, ..] ? $"{metodo.Name} {llave}" : metodo.Name;

    private static object? Llamar(MethodInfo metodo, object real, object?[]? argumentos)
    {
        try
        {
            return metodo.Invoke(real, argumentos);
        }
        catch (TargetInvocationException excepcion) when (excepcion.InnerException is not null)
        {
            throw excepcion.InnerException;
        }
    }

    /// <summary>Envuelve la conexión, cada base de datos y cada lote.</summary>
#pragma warning disable CA1852 // DispatchProxy necesita una clase que pueda heredar.
    public class Proxy<T> : DispatchProxy
#pragma warning restore CA1852
    {
        private object _real = null!;
        private ContadorRedis _contador = null!;
        private List<string>? _lote;

        public static T Crear(object real, ContadorRedis contador)
        {
            var proxy = Create<T, Proxy<T>>();
            var interno = (Proxy<T>)(object)proxy!;
            interno._real = real;
            interno._contador = contador;
            interno._lote = real is IBatch ? [] : null;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? metodo, object?[]? argumentos)
        {
            ArgumentNullException.ThrowIfNull(metodo);
            if (metodo.Name is nameof(IDisposable.Dispose) or nameof(IAsyncDisposable.DisposeAsync) or "Close" or "CloseAsync")
            {
                return metodo.ReturnType == typeof(void) ? null : Activator.CreateInstance(metodo.ReturnType);
            }

            var resultado = Llamar(metodo, _real, argumentos);
            switch (resultado)
            {
                case IBatch lote when metodo.Name == nameof(IDatabase.CreateBatch):
                    return Proxy<IBatch>.Crear(lote, _contador);
                case IDatabase db when metodo.Name == nameof(IConnectionMultiplexer.GetDatabase):
                    return Proxy<IDatabase>.Crear(db, _contador);
            }

            if (_lote is not null)
            {
                if (metodo.Name == nameof(IBatch.Execute))
                {
                    _contador.Registrar([.. _lote]);
                    _lote.Clear();
                }
                else if (resultado is Task)
                {
                    _lote.Add(Describir(metodo, argumentos));
                }
            }
            else if (_real is IDatabase && resultado is Task)
            {
                _contador.Registrar([Describir(metodo, argumentos)]);
            }

            return resultado;
        }
    }
}

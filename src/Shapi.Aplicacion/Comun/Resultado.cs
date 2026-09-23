using System.Diagnostics.CodeAnalysis;

namespace Shapi.Aplicacion.Comun;

/// <summary>Resultado de un caso de uso que no devuelve un valor: éxito o un <see cref="Comun.Error"/>.</summary>
public sealed class Resultado
{
    private static readonly Resultado ExitoUnico = new(null);

    private Resultado(Error? error) => Error = error;

    public Error? Error { get; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool EsExito => Error is null;

    public static Resultado Exito() => ExitoUnico;

    public static Resultado Fallo(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Resultado(error);
    }

    public static implicit operator Resultado(Error error) => Fallo(error);
}

/// <summary>Resultado de un caso de uso: un valor o un <see cref="Comun.Error"/> con su código.</summary>
public sealed class Resultado<T>
{
    private readonly T? _valor;

    private Resultado(T? valor, Error? error)
    {
        _valor = valor;
        Error = error;
    }

    public Error? Error { get; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool EsExito => Error is null;

    /// <exception cref="InvalidOperationException">Si el resultado es un error.</exception>
    public T Valor => EsExito
        ? _valor!
        : throw new InvalidOperationException($"El resultado es un error ({Error.Codigo}) y no tiene valor.");

    public static Resultado<T> Exito(T valor) => new(valor, null);

    public static Resultado<T> Fallo(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Resultado<T>(default, error);
    }

    public static implicit operator Resultado<T>(T valor) => Exito(valor);

    public static implicit operator Resultado<T>(Error error) => Fallo(error);
}

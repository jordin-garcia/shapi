using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Shapi.Api.Tests.Identidad;

/// <summary>
/// Cuenta los comandos que EF Core manda a la base, para comprobar que dos rutas hacen el mismo trabajo (10 §8:
/// el mismo tiempo de respuesta exista o no la cuenta).
/// </summary>
internal sealed class ContadorComandos : DbCommandInterceptor
{
    private int _comandos;

    public int Comandos { get => _comandos; set => _comandos = value; }

    public override DbCommand CommandInitialized(CommandEndEventData eventData, DbCommand result)
    {
        Interlocked.Increment(ref _comandos);
        return result;
    }
}

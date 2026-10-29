using Npgsql;

namespace Shapi.Api.Tests.Persistencia;

/// <summary>
/// Para las pruebas de carreras: espera a que otra conexión de la misma base quede esperando un bloqueo de fila. Si
/// nadie espera dentro del plazo, devuelve <c>false</c> y la prueba sigue (sin el bloqueo, la carrera ya ocurrió y la
/// prueba falla por su resultado).
/// </summary>
internal static class EsperaDeBloqueos
{
    public static async Task<bool> EsperarAsync(string cadena, TimeSpan? plazo = null)
    {
        var limite = DateTime.UtcNow + (plazo ?? TimeSpan.FromSeconds(5));
        await using var conexion = new NpgsqlConnection(cadena);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand("""
            SELECT count(*) FROM pg_stat_activity
            WHERE datname = current_database() AND wait_event_type = 'Lock' AND pid <> pg_backend_pid()
            """, conexion);
        while (DateTime.UtcNow < limite)
        {
            if ((long)(await comando.ExecuteScalarAsync())! > 0)
            {
                return true;
            }

            await Task.Delay(50);
        }

        return false;
    }
}

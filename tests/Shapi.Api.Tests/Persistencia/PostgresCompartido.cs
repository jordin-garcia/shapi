using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shapi.Infraestructura.Comun;
using Shapi.Infraestructura.Persistencia;
using Testcontainers.PostgreSql;

namespace Shapi.Api.Tests.Persistencia;

/// <summary>
/// Un solo PostgreSQL 16 para todo el proyecto de pruebas (JG-18). Antes cada clase arrancaba su propio contenedor
/// (unos 12) y cada prueba migraba su base desde cero.
/// <para>
/// Al arrancar, se aplican las migraciones a <c>template1</c>, la plantilla de la que PostgreSQL copia toda base
/// nueva. Así, la base propia de cada prueba (<c>prueba_&lt;guid&gt;</c>) nace ya migrada: EF Core la crea con
/// <c>CREATE DATABASE</c>, encuentra todas las migraciones en <c>__EFMigrationsHistory</c> y no aplica nada. Cada
/// prueba sigue teniendo su propia base, y la siembra se ejecuta igual que antes. Al terminar, cada clase la borra con
/// <see cref="EliminarBaseAsync"/>.
/// </para>
/// <para>
/// Nadie más se conecta a <c>template1</c>: PostgreSQL no copia una plantilla que tiene conexiones abiertas. El
/// contenedor lo elimina Ryuk de Testcontainers al terminar el proceso.
/// </para>
/// </summary>
public static class PostgresCompartido
{
    private static readonly Lazy<Task> Inicio = new(ArrancarYMigrarAsync);

    /// <summary>
    /// Las pruebas no necesitan durabilidad: sin fsync ni escrituras síncronas PostgreSQL es mucho más rápido.
    /// Las conexiones suben a 300 porque ahora todas las clases comparten el servidor.
    /// </summary>
    public static PostgreSqlContainer Contenedor { get; } = new PostgreSqlBuilder("postgres:16-alpine")
        .WithCommand(
            "-c", "fsync=off",
            "-c", "synchronous_commit=off",
            "-c", "full_page_writes=off",
            "-c", "max_connections=300")
        .Build();

    /// <summary>Arranca el contenedor y migra la plantilla, una sola vez por proceso.</summary>
    public static Task IniciarAsync() => Inicio.Value;

    /// <summary>La cadena de conexión a una base nueva y única, que EF Core crea ya migrada al usarla.</summary>
    public static string NuevaCadena(string prefijo = "prueba") => Cadena($"{prefijo}_{Guid.NewGuid():N}");

    /// <summary>
    /// Borra la base de una prueba al terminar. Sin esto, las unas 300 bases (7 MB o más cada una) se acumulaban en el
    /// contenedor, y Ryuk borraba varios GB de golpe al final, justo cuando empezaban las pruebas de la compuerta, que
    /// miden tiempos y fallaban por el disco saturado (PR #58).
    /// </summary>
    public static async Task EliminarBaseAsync(string cadena)
    {
        var baseDeDatos = new NpgsqlConnectionStringBuilder(cadena).Database;
        await using var conexion = new NpgsqlConnection(Cadena("postgres"));
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{baseDeDatos}\" WITH (FORCE)", conexion);
        await comando.ExecuteNonQueryAsync();
    }

    private static string Cadena(string baseDeDatos) =>
        new NpgsqlConnectionStringBuilder(Contenedor.GetConnectionString()) { Database = baseDeDatos }.ConnectionString;

    private static async Task ArrancarYMigrarAsync()
    {
        await Contenedor.StartAsync();

        var opciones = new DbContextOptionsBuilder<ShapiDbContext>().UseNpgsql(Cadena("template1")).Options;
        await using (var db = new ShapiDbContext(opciones, new ContextoOrganizacionNulo()))
        {
            await db.Database.MigrateAsync();
        }

        // Cierra la conexión que quedó en el pool: con ella abierta, CREATE DATABASE no puede copiar template1.
        NpgsqlConnection.ClearAllPools();
    }
}

/// <summary>
/// Fixture de las clases que antes arrancaban su propio PostgreSQL: ahora todas usan <see cref="PostgresCompartido"/>.
/// </summary>
public class PostgresDePrueba : IAsyncLifetime
{
    public PostgreSqlContainer Contenedor => PostgresCompartido.Contenedor;

    public Task InitializeAsync() => PostgresCompartido.IniciarAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}

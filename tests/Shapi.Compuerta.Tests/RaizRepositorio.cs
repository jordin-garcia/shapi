namespace Shapi.Compuerta.Tests;

/// <summary>Ubica la raíz del repositorio (la carpeta de Shapi.slnx) para leer las especificaciones.</summary>
internal static class RaizRepositorio
{
    public static string Ruta(params string[] partes)
    {
        var carpeta = new DirectoryInfo(AppContext.BaseDirectory);
        while (carpeta is not null && !File.Exists(Path.Combine(carpeta.FullName, "Shapi.slnx")))
        {
            carpeta = carpeta.Parent;
        }

        if (carpeta is null)
        {
            throw new InvalidOperationException("No se encontró Shapi.slnx en ninguna carpeta superior.");
        }

        return Path.Combine([carpeta.FullName, .. partes]);
    }
}

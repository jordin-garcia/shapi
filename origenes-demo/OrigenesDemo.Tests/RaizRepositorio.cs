namespace OrigenesDemo.Tests;

internal static class RaizRepositorio
{
    public static string Ruta
    {
        get
        {
            var directorio = new DirectoryInfo(AppContext.BaseDirectory);

            while (directorio is not null && !File.Exists(Path.Combine(directorio.FullName, "Shapi.slnx")))
            {
                directorio = directorio.Parent;
            }

            return directorio?.FullName
                ?? throw new DirectoryNotFoundException("No se encontro la raiz del repositorio.");
        }
    }
}

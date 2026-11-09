namespace Shapi.Infraestructura.Comun;

/// <summary>Resuelve la carpeta del anillo de llaves de Data Protection (<c>SHAPI_DPKEYS_DIR</c>, 10 §3).</summary>
internal static class DirectorioLlaves
{
    private const string MarcaRaiz = "Shapi.slnx";

    /// <summary>
    /// Una ruta absoluta se usa tal cual. Una relativa se resuelve desde la raíz del repositorio (la primera carpeta
    /// superior a <paramref name="directorioInicio"/> que tiene Shapi.slnx), para que la API y el trabajador compartan
    /// el anillo aunque cada uno arranque en la carpeta de su proyecto. Fuera del repositorio, se resuelve desde
    /// <paramref name="directorioInicio"/>.
    /// </summary>
    public static string Resolver(string valor, string directorioInicio)
    {
        if (Path.IsPathRooted(valor))
        {
            return Path.GetFullPath(valor);
        }

        var raiz = BuscarRaiz(directorioInicio) ?? directorioInicio;
        return Path.GetFullPath(Path.Combine(raiz, valor));
    }

    private static string? BuscarRaiz(string directorioInicio)
    {
        for (var carpeta = new DirectoryInfo(Path.GetFullPath(directorioInicio)); carpeta is not null; carpeta = carpeta.Parent)
        {
            if (File.Exists(Path.Combine(carpeta.FullName, MarcaRaiz)))
            {
                return carpeta.FullName;
            }
        }

        return null;
    }
}

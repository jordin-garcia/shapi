namespace Shapi.Aplicacion.Comun;

/// <summary>Fragmentos de texto que comparten las descripciones de la bitácora (10 §7).</summary>
public static class TextoBitacora
{
    /// <summary>"la API de Cotización de Envíos". Si el nombre no empieza con "API", se le antepone.</summary>
    public static string LaApi(string nombre) =>
        nombre.StartsWith("API ", StringComparison.OrdinalIgnoreCase) ? $"la {nombre}" : $"la API {nombre}";
}

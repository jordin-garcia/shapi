namespace Shapi.Infraestructura.Siembra.Demo;

public static class ConfiguracionSiembraDemo
{
    public static (string Envios, string Agro) ResolverUrls(
        string? enviosConfigurada,
        string? agroConfigurada,
        bool esProduccion) =>
        (
            enviosConfigurada ?? (esProduccion ? "http://origen-envios:8080" : "http://localhost:5101"),
            agroConfigurada ?? (esProduccion ? "http://origen-agro:8080" : "http://localhost:5102")
        );
}

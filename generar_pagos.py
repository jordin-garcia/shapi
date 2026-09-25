import os

base_path = '/home/milo/Documentos/URL/6th Semester/Ingeniería de Software 1/Code/shapi'
app_path = os.path.join(base_path, 'src/Shapi.Aplicacion/Pagos')
infra_path = os.path.join(base_path, 'src/Shapi.Infraestructura/Pagos')

os.makedirs(app_path, exist_ok=True)
os.makedirs(infra_path, exist_ok=True)

archivos_app = {
    "DatosTarjeta.cs": """namespace Shapi.Aplicacion.Pagos;

public class DatosTarjeta
{
    public string Numero { get; set; } = string.Empty;
    public string MesVencimiento { get; set; } = string.Empty;
    public string AnioVencimiento { get; set; } = string.Empty;
    public string Cvv { get; set; } = string.Empty;
    public string Titular { get; set; } = string.Empty;
}
""",
    "ResultadoTokenizacion.cs": """namespace Shapi.Aplicacion.Pagos;

public class ResultadoTokenizacion
{
    public bool Exitoso { get; set; }
    public string? Token { get; set; }
    public string? Error { get; set; }
    public string? Marca { get; set; }
    public string? Ultimos4 { get; set; }
    public string? Titular { get; set; }
}
""",
    "ResultadoCobro.cs": """namespace Shapi.Aplicacion.Pagos;

public class ResultadoCobro
{
    public bool Exitoso { get; set; }
    public string? Referencia { get; set; }
    public string? Error { get; set; }
}
""",
    "ResultadoReembolso.cs": """namespace Shapi.Aplicacion.Pagos;

public class ResultadoReembolso
{
    public bool Exitoso { get; set; }
    public string? Referencia { get; set; }
    public string? Error { get; set; }
}
""",
    "IPasarelaPagos.cs": """namespace Shapi.Aplicacion.Pagos;

public interface IPasarelaPagos
{
    Task<ResultadoTokenizacion> TokenizarAsync(DatosTarjeta tarjeta);
    Task<ResultadoCobro> CobrarAsync(string token, decimal monto, string referencia, bool esRenovacion);
    Task<ResultadoReembolso> ReembolsarAsync(string referenciaCobro);
}
"""
}

for name, content in archivos_app.items():
    with open(os.path.join(app_path, name), "w") as f:
        f.write(content)

print("Archivos de aplicación creados.")

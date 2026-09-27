using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shapi.Api.Identidad;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Identidad;
using Shapi.Contratos;
using Shapi.Dominio.Identidad;

namespace Shapi.Api.Modulos;

public static class IdentidadModulo
{
    /// <summary>Redes desde las que llega el borde (Caddy), en CIDR y separadas por comas (10 §1).</summary>
    public const string VariableRedesBorde = "SHAPI_REDES_BORDE";

    /// <summary>Sin <see cref="VariableRedesBorde"/> solo se confía en la máquina: así llega Caddy con Docker Desktop.</summary>
    public const string RedesBordePorDefecto = "127.0.0.0/8,::1/128";

    public static IServiceCollection AgregarModuloIdentidad(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IContextoOrganizacion, ContextoOrganizacionHttp>();
        services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
        services.AddScoped<IValidator<RegistroProveedor>, ValidadorRegistroProveedor>();
        services.AgregarPoliticasShapi();

        services.AddAuthentication(PersonalAutenticacionOpciones.Esquema)
            .AddScheme<PersonalAutenticacionOpciones, PersonalAutenticacionHandler>(PersonalAutenticacionOpciones.Esquema, null);

        // Detrás del borde, la IP del cliente llega en X-Forwarded-For. Solo se confía en ella si la conexión viene de
        // las redes del borde (SHAPI_REDES_BORDE). Caddy reemplaza la X-Forwarded-For que mande el cliente.
        services.AddOptions<ForwardedHeadersOptions>().Configure<IConfiguration>((opciones, configuracion) =>
        {
            opciones.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            opciones.ForwardLimit = 1;
            opciones.KnownIPNetworks.Clear();
            opciones.KnownProxies.Clear();
            var redes = configuracion[VariableRedesBorde] is { Length: > 0 } valor ? valor : RedesBordePorDefecto;
            foreach (var red in redes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                opciones.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(red));
            }
        });

        // Convenciones §5: un cuerpo que no se puede leer (JSON mal formado) es un 400 datos_invalidos, con código.
        // Sin ThrowOnBadRequest, que en Development lo convertiría en una excepción (500), responde igual en todo entorno.
        services.Configure<RouteHandlerOptions>(opciones => opciones.ThrowOnBadRequest = false);
        services.Configure<ProblemDetailsOptions>(opciones =>
        {
            var anterior = opciones.CustomizeProblemDetails;
            opciones.CustomizeProblemDetails = contexto =>
            {
                anterior?.Invoke(contexto);
                if (contexto.ProblemDetails.Status == StatusCodes.Status400BadRequest && !contexto.ProblemDetails.Extensions.ContainsKey("codigo"))
                {
                    contexto.ProblemDetails.Title = "La petición no es válida. Revise que el cuerpo sea un JSON correcto.";
                    contexto.ProblemDetails.Extensions["codigo"] = CodigosError.DatosInvalidos;
                }
            };
        });

        // 10 §1: los endpoints que reciben credenciales o tokens aceptan 10 peticiones por minuto por IP.
        services.AddRateLimiter(opciones =>
        {
            opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opciones.OnRejected = async (contexto, _) =>
            {
                var espera = contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var restante) ? restante : TimeSpan.FromMinutes(1);
                contexto.HttpContext.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(espera.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
                await Problemas.Escribir(contexto.HttpContext, StatusCodes.Status429TooManyRequests, CodigosError.DemasiadasPeticiones,
                    "Demasiadas peticiones. Espere un minuto e intente de nuevo.");
            };
            opciones.AddPolicy(Endpoints.PoliticaLimiteAutenticacion, contexto =>
            {
                var limite = contexto.RequestServices.GetRequiredService<IConfiguration>().GetValue<int?>("Autenticacion:LimitePorMinuto") ?? 10;
                return RateLimitPartition.GetFixedWindowLimiter(
                    contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = limite, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
            });
        });

        return services;
    }

    public static WebApplication MapearModuloIdentidad(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseMiddleware<CsrfMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        app.MapearEndpointsIdentidad();

        return app;
    }
}

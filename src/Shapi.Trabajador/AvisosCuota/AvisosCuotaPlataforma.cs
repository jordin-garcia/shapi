using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos.Redis;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Persistencia;
using StackExchange.Redis;

namespace Shapi.Trabajador.AvisosCuota;

public sealed class AvisosCuotaPlataforma(
    ShapiDbContext db,
    IConnectionMultiplexer redis,
    IColaCorreo correo,
    IReloj reloj,
    IConfiguration configuracion)
{
    public async Task EjecutarAsync(CancellationToken ct = default)
    {
        var activas = await (from suscripcion in db.Set<SuscripcionPlataforma>().IgnoreQueryFilters().AsNoTracking()
                             join plan in db.Set<PlanPlataforma>().IgnoreQueryFilters().AsNoTracking() on suscripcion.PlanId equals plan.Id
                             join membresia in db.Set<Membresia>().IgnoreQueryFilters().AsNoTracking() on suscripcion.OrganizacionId equals membresia.OrganizacionId
                             join usuario in db.Set<Usuario>().IgnoreQueryFilters().AsNoTracking() on membresia.UsuarioId equals usuario.Id
                             where (suscripcion.Estado == EstadoSuscripcion.Activa || suscripcion.Estado == EstadoSuscripcion.EnGracia)
                                && membresia.Rol == Rol.Propietario
                                && usuario.Estado == EstadoCuenta.Activo
                                && plan.CuotaPeticiones > 0
                             select new
                             {
                                 suscripcion.OrganizacionId,
                                 suscripcion.Inicio,
                                 suscripcion.Fin,
                                 plan.CuotaPeticiones,
                                 usuario.Nombre,
                                 usuario.Correo,
                             }).ToListAsync(ct);

        var database = redis.GetDatabase();
        var dominio = configuracion["SHAPI_DOMINIO_BASE"] ?? "shapi.localhost";
        foreach (var suscripcion in activas)
        {
            var cicloInicio = suscripcion.Inicio.ToUnixTimeSeconds();
            var llaveUso = LlavesRedis.CuotaOrganizacion(suscripcion.OrganizacionId, cicloInicio);
            var valor = await database.StringGetAsync(llaveUso);
            if (!long.TryParse(valor.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var uso))
            {
                uso = 0;
            }

            foreach (var porcentaje in new[] { 80, 100 })
            {
                if (uso * 100m < suscripcion.CuotaPeticiones * porcentaje)
                {
                    continue;
                }

                var marcador = LlavesRedis.AvisoCuotaOrganizacion(suscripcion.OrganizacionId, cicloInicio, porcentaje);
                var expiracion = suscripcion.Fin.AddDays(8) - reloj.Ahora;
                if (expiracion <= TimeSpan.Zero)
                {
                    expiracion = TimeSpan.FromSeconds(1);
                }

                if (!await database.StringSetAsync(marcador, "1", expiracion, When.NotExists))
                {
                    continue;
                }

                try
                {
                    await correo.Encolar("aviso_cuota_plataforma", suscripcion.Correo, new
                    {
                        nombre = suscripcion.Nombre,
                        porcentaje = porcentaje.ToString(CultureInfo.InvariantCulture),
                        enlace = $"https://{dominio}/panel/suscripcion",
                    }, ct);
                }
                catch
                {
                    await database.KeyDeleteAsync(marcador);
                    throw;
                }
            }
        }
    }
}

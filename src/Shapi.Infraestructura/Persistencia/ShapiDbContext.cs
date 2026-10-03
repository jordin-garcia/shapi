using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Apis;
using Shapi.Dominio.Claves;
using Shapi.Dominio.Comun;
using Shapi.Dominio.Consumo;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Pagos;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Soporte;
using Shapi.Dominio.Suscripciones;

namespace Shapi.Infraestructura.Persistencia;

public class ShapiDbContext : DbContext
{
    /// <summary>Secuencia de <c>caso.numero</c>: empieza en 100 y avanza de 1 en 1 (07 §3.6).</summary>
    public const string SecuenciaNumeroCaso = "caso_numero_seq";

    private readonly IContextoOrganizacion _contextoOrganizacion;

    public ShapiDbContext(DbContextOptions<ShapiDbContext> options, IContextoOrganizacion contextoOrganizacion)
        : base(options)
    {
        _contextoOrganizacion = contextoOrganizacion;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSnakeCaseNamingConvention();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<int>(SecuenciaNumeroCaso).StartsAt(100).IncrementsBy(1);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShapiDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var actualizadoEn = entityType.FindProperty("ActualizadoEn");
            if (actualizadoEn != null && actualizadoEn.ClrType == typeof(DateTimeOffset))
            {
                actualizadoEn.SetDefaultValueSql("now()");
            }
        }

        AplicarFiltrosPorOrganizacion(modelBuilder);
    }

    /// <summary>
    /// Filtro global por organización (10 §2, RNF-08) para toda entidad que pertenece a una organización.
    /// Sin contexto de organización no se devuelve nada. Solo la administración, el trabajador y la compuerta
    /// lo desactivan, con <c>IgnoreQueryFilters</c>.
    /// Quedan sin filtro las tablas globales o que se usan antes de conocer la organización: organizacion,
    /// plan_plataforma, token, sesion, correo_saliente, registro_dns_simulado y lote_consolidado.
    /// </summary>
    private void AplicarFiltrosPorOrganizacion(ModelBuilder modelBuilder)
    {
        // Las que tienen organizacion_id propio.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IPerteneceAOrganizacion).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(IPerteneceAOrganizacion.OrganizacionId));
                var contextProperty = Expression.Property(Expression.Constant(this), nameof(OrganizacionId));

                var castedProperty = Expression.Convert(property, typeof(Guid?));
                var equalExpression = Expression.Equal(castedProperty, contextProperty);
                var notNullCheck = Expression.NotEqual(contextProperty, Expression.Constant(null, typeof(Guid?)));
                var andExpression = Expression.AndAlso(notNullCheck, equalExpression);

                var lambda = Expression.Lambda(andExpression, parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }

        // La bitácora de acciones del sistema sin organización solo la ve la administración.
        modelBuilder.Entity<Shapi.Dominio.Bitacora.EntradaBitacora>().HasQueryFilter(e =>
            OrganizacionId != null && e.OrganizacionId == OrganizacionId);

        modelBuilder.Entity<MedioPago>().HasQueryFilter(e =>
            OrganizacionId != null
            && (e.OrganizacionId == OrganizacionId
                || Set<Consumidor>().Any(c => c.Id == e.ConsumidorId && c.OrganizacionId == OrganizacionId)));

        // Las que pertenecen a una organización a través de su padre.
        modelBuilder.Entity<Usuario>().HasQueryFilter(e =>
            OrganizacionId != null
            && Set<Membresia>().Any(m => m.UsuarioId == e.Id && m.OrganizacionId == OrganizacionId));

        modelBuilder.Entity<Ruta>().HasQueryFilter(e =>
            OrganizacionId != null && Set<Api>().Any(a => a.Id == e.ApiId && a.OrganizacionId == OrganizacionId));
        modelBuilder.Entity<DominioPropio>().HasQueryFilter(e =>
            OrganizacionId != null && Set<Api>().Any(a => a.Id == e.ApiId && a.OrganizacionId == OrganizacionId));
        modelBuilder.Entity<PlanApi>().HasQueryFilter(e =>
            OrganizacionId != null && Set<Api>().Any(a => a.Id == e.ApiId && a.OrganizacionId == OrganizacionId));
        modelBuilder.Entity<SuscripcionApi>().HasQueryFilter(e =>
            OrganizacionId != null && Set<Api>().Any(a => a.Id == e.ApiId && a.OrganizacionId == OrganizacionId));
        modelBuilder.Entity<ConsumoDiario>().HasQueryFilter(e =>
            OrganizacionId != null && Set<Api>().Any(a => a.Id == e.ApiId && a.OrganizacionId == OrganizacionId));

        modelBuilder.Entity<Clave>().HasQueryFilter(e =>
            OrganizacionId != null
            && Set<SuscripcionApi>().Any(s => s.Id == e.SuscripcionId
                && Set<Api>().Any(a => a.Id == s.ApiId && a.OrganizacionId == OrganizacionId)));

        modelBuilder.Entity<Pago>().HasQueryFilter(e =>
            OrganizacionId != null
            && (Set<SuscripcionPlataforma>().Any(s => s.Id == e.SuscripcionPlataformaId && s.OrganizacionId == OrganizacionId)
                || Set<SuscripcionApi>().Any(s => s.Id == e.SuscripcionApiId
                    && Set<Api>().Any(a => a.Id == s.ApiId && a.OrganizacionId == OrganizacionId))
                || (e.Estado == EstadoPago.Rechazado
                    && Set<Consumidor>().Any(c => c.Id == e.ConsumidorId && c.OrganizacionId == OrganizacionId)
                    && Set<Api>().Any(a => a.Id == e.ApiId && a.OrganizacionId == OrganizacionId))));

        modelBuilder.Entity<CasoMensaje>().HasQueryFilter(e =>
            OrganizacionId != null
            && Set<Caso>().Any(c => c.Id == e.CasoId && c.OrganizacionId == OrganizacionId));
    }

    public Guid? OrganizacionId => _contextoOrganizacion.OrganizacionId;
}

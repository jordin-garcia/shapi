using Microsoft.EntityFrameworkCore;
using Shapi.Dominio.Apis;
using Shapi.Dominio.Bitacora;
using Shapi.Dominio.Claves;
using Shapi.Dominio.Comun;
using Shapi.Dominio.Consumo;
using Shapi.Dominio.Correo;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Pagos;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Soporte;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Comun;

namespace Shapi.Infraestructura.Persistencia;

public class ShapiDbContext : DbContext
{
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShapiDbContext).Assembly);

        // Filtro global para entidades que pertenecen a una organización
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IPerteneceAOrganizacion).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, nameof(IPerteneceAOrganizacion.OrganizacionId));
                var contextProperty = System.Linq.Expressions.Expression.Property(
                    System.Linq.Expressions.Expression.Constant(this),
                    nameof(OrganizacionIdActual));
                
                var castedProperty = System.Linq.Expressions.Expression.Convert(property, typeof(Guid?));
                var equalExpression = System.Linq.Expressions.Expression.Equal(castedProperty, contextProperty);
                var nullCheck = System.Linq.Expressions.Expression.Equal(contextProperty, System.Linq.Expressions.Expression.Constant(null, typeof(Guid?)));
                var orExpression = System.Linq.Expressions.Expression.OrElse(nullCheck, equalExpression);
                
                var lambda = System.Linq.Expressions.Expression.Lambda(orExpression, parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    public Guid? OrganizacionIdActual => _contextoOrganizacion.OrganizacionIdActual;
}

import os

configs_dir = 'src/Shapi.Infraestructura/Persistencia/Configuraciones'

fks = {
    'MembresiaConfiguracion.cs': [
        'builder.HasOne<Shapi.Dominio.Identidad.Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);',
        'builder.HasOne<Organizacion>().WithMany().HasForeignKey(x => x.OrganizacionId).OnDelete(DeleteBehavior.Restrict);'
    ],
    'ConsumidorConfiguracion.cs': [
        'builder.HasOne<Shapi.Dominio.Organizaciones.Organizacion>().WithMany().HasForeignKey(x => x.OrganizacionId).OnDelete(DeleteBehavior.Restrict);'
    ],
    'SuscripcionPlataformaConfiguracion.cs': [
        'builder.HasOne<Shapi.Dominio.Organizaciones.Organizacion>().WithMany().HasForeignKey(x => x.OrganizacionId).OnDelete(DeleteBehavior.Restrict);',
        'builder.HasOne<Shapi.Dominio.Planes.PlanPlataforma>().WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);'
    ],
    'SuscripcionApiConfiguracion.cs': [
        'builder.HasOne<Shapi.Dominio.Identidad.Consumidor>().WithMany().HasForeignKey(x => x.ConsumidorId).OnDelete(DeleteBehavior.Restrict);',
        'builder.HasOne<Shapi.Dominio.Apis.Api>().WithMany().HasForeignKey(x => x.ApiId).OnDelete(DeleteBehavior.Restrict);',
        'builder.HasOne<Shapi.Dominio.Planes.PlanApi>().WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);'
    ],
    'ClaveConfiguracion.cs': [
        'builder.HasOne<Shapi.Dominio.Suscripciones.SuscripcionApi>().WithMany().HasForeignKey(x => x.SuscripcionId).OnDelete(DeleteBehavior.Restrict);'
    ],
    'MedioPagoConfiguracion.cs': [
        'builder.HasOne<Shapi.Dominio.Organizaciones.Organizacion>().WithMany().HasForeignKey(x => x.OrganizacionId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);',
        'builder.HasOne<Shapi.Dominio.Identidad.Consumidor>().WithMany().HasForeignKey(x => x.ConsumidorId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);'
    ],
    'PagoConfiguracion.cs': [
        'builder.HasOne<Shapi.Dominio.Pagos.MedioPago>().WithMany().HasForeignKey(x => x.MedioPagoId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);',
        'builder.HasOne<Shapi.Dominio.Suscripciones.SuscripcionPlataforma>().WithMany().HasForeignKey(x => x.SuscripcionPlataformaId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);',
        'builder.HasOne<Shapi.Dominio.Suscripciones.SuscripcionApi>().WithMany().HasForeignKey(x => x.SuscripcionApiId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);'
    ],
    'ConsumoDiarioConfiguracion.cs': [
        'builder.HasOne<Shapi.Dominio.Apis.Api>().WithMany().HasForeignKey(x => x.ApiId).OnDelete(DeleteBehavior.Restrict);',
        'builder.HasOne<Shapi.Dominio.Apis.Ruta>().WithMany().HasForeignKey(x => x.RutaId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);',
        'builder.HasOne<Shapi.Dominio.Suscripciones.SuscripcionApi>().WithMany().HasForeignKey(x => x.SuscripcionId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);'
    ],
    'CasoConfiguracion.cs': [
        'builder.HasOne<Shapi.Dominio.Organizaciones.Organizacion>().WithMany().HasForeignKey(x => x.OrganizacionId).OnDelete(DeleteBehavior.Restrict);',
        'builder.HasOne<Shapi.Dominio.Apis.Api>().WithMany().HasForeignKey(x => x.ApiId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);',
        'builder.HasOne<Shapi.Dominio.Identidad.Usuario>().WithMany().HasForeignKey(x => x.CreadoPor).OnDelete(DeleteBehavior.Restrict);',
        'builder.HasOne<Shapi.Dominio.Identidad.Usuario>().WithMany().HasForeignKey(x => x.AsignadoA).IsRequired(false).OnDelete(DeleteBehavior.Restrict);'
    ],
    'CasoMensajeConfiguracion.cs': [
        'builder.HasOne<Shapi.Dominio.Soporte.Caso>().WithMany().HasForeignKey(x => x.CasoId).OnDelete(DeleteBehavior.Cascade);',
        'builder.HasOne<Shapi.Dominio.Identidad.Usuario>().WithMany().HasForeignKey(x => x.AutorId).OnDelete(DeleteBehavior.Restrict);'
    ],
    'SesionConfiguracion.cs': [
        'builder.HasOne<Shapi.Dominio.Identidad.Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);',
        'builder.HasOne<Shapi.Dominio.Identidad.Consumidor>().WithMany().HasForeignKey(x => x.ConsumidorId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);'
    ],
    'TokenConfiguracion.cs': [
        'builder.HasOne<Shapi.Dominio.Identidad.Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);',
        'builder.HasOne<Shapi.Dominio.Identidad.Consumidor>().WithMany().HasForeignKey(x => x.ConsumidorId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);',
        'builder.HasOne<Shapi.Dominio.Organizaciones.Organizacion>().WithMany().HasForeignKey(x => x.OrganizacionId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);'
    ],
    'EntradaBitacoraConfiguracion.cs': [
        'builder.HasOne<Shapi.Dominio.Organizaciones.Organizacion>().WithMany().HasForeignKey(x => x.OrganizacionId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);'
    ],
    'ApiConfiguracion.cs': [
        'builder.HasOne<Shapi.Dominio.Organizaciones.Organizacion>().WithMany().HasForeignKey(x => x.OrganizacionId).OnDelete(DeleteBehavior.Restrict);'
    ],
    'DominioPropioConfiguracion.cs': [
        'builder.HasOne<Shapi.Dominio.Apis.Api>().WithMany().HasForeignKey(x => x.ApiId).OnDelete(DeleteBehavior.Cascade);'
    ],
    'PlanApiConfiguracion.cs': [
        'builder.HasOne<Shapi.Dominio.Apis.Api>().WithMany().HasForeignKey(x => x.ApiId).OnDelete(DeleteBehavior.Cascade);'
    ]
}

for filename, lines in fks.items():
    path = os.path.join(configs_dir, filename)
    with open(path, 'r') as f:
        content = f.read()

    # insert before the last '}'
    insert_block = '\n        ' + '\n        '.join(lines) + '\n    }\n}'
    content = content.replace('\n    }\n}', insert_block)

    with open(path, 'w') as f:
        f.write(content)


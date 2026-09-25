import re

with open('src/Shapi.Infraestructura/Correo/ColaCorreoBaseDatos.cs', 'r') as f:
    correo_content = f.read()

correo_content = correo_content.replace('public async Task EncolarAsync', 'public async Task Encolar')

with open('src/Shapi.Infraestructura/Correo/ColaCorreoBaseDatos.cs', 'w') as f:
    f.write(correo_content)

bitacora = """using System.Text.Json;
using System.Net;
using Shapi.Aplicacion.Comun;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Infraestructura.Bitacora;

public class BitacoraBaseDatos : IBitacora
{
    private readonly ShapiDbContext _context;

    public BitacoraBaseDatos(ShapiDbContext context)
    {
        _context = context;
    }

    public async Task Registrar(Shapi.Aplicacion.Comun.EntradaBitacora entrada, CancellationToken cancelacion = default)
    {
        // map App record to Domain entity
        var tipo = entrada.ActorTipo switch
        {
            Shapi.Aplicacion.Comun.TipoActor.Usuario => Shapi.Dominio.Bitacora.ActorTipo.Usuario,
            Shapi.Aplicacion.Comun.TipoActor.Consumidor => Shapi.Dominio.Bitacora.ActorTipo.Consumidor,
            Shapi.Aplicacion.Comun.TipoActor.Sistema => Shapi.Dominio.Bitacora.ActorTipo.Sistema,
            _ => throw new ArgumentOutOfRangeException()
        };

        var domEntrada = new Shapi.Dominio.Bitacora.EntradaBitacora(
            tipo,
            entrada.ActorId,
            entrada.ActorNombre,
            entrada.OrganizacionId,
            entrada.Accion,
            entrada.ObjetivoTipo,
            entrada.ObjetivoId,
            entrada.Descripcion,
            entrada.Detalle != null ? JsonSerializer.Serialize(entrada.Detalle) : null,
            entrada.Ip != null ? IPAddress.Parse(entrada.Ip) : null
        );

        _context.EntradasBitacora.Add(domEntrada);
        await _context.SaveChangesAsync(cancelacion);
    }
}
"""

with open('src/Shapi.Infraestructura/Bitacora/BitacoraBaseDatos.cs', 'w') as f:
    f.write(bitacora)


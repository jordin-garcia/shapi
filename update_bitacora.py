import re
with open('src/Shapi.Dominio/Bitacora/EntradaBitacora.cs', 'r') as f:
    content = f.read()

insert = """
    public EntradaBitacora(ActorTipo actorTipo, Guid? actorId, string actorNombre, Guid? organizacionId, string accion, string? objetivoTipo, Guid? objetivoId, string descripcion, string? detalle, IPAddress? ip)
    {
        Fecha = DateTimeOffset.UtcNow;
        ActorTipo = actorTipo;
        ActorId = actorId;
        ActorNombre = actorNombre;
        OrganizacionId = organizacionId;
        Accion = accion;
        ObjetivoTipo = objetivoTipo;
        ObjetivoId = objetivoId;
        Descripcion = descripcion;
        Detalle = detalle;
        Ip = ip;
    }
"""

content = content.replace('protected EntradaBitacora() { }', insert + '\n    protected EntradaBitacora() { }')
with open('src/Shapi.Dominio/Bitacora/EntradaBitacora.cs', 'w') as f:
    f.write(content)

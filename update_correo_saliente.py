import re
with open('src/Shapi.Dominio/Correo/CorreoSaliente.cs', 'r') as f:
    content = f.read()

# I will add a public constructor.
insert = """
    public CorreoSaliente(EstadoCorreo estado, string plantilla, string destinatario, string datos, string asunto)
    {
        Id = Guid.NewGuid();
        Estado = estado;
        Plantilla = plantilla;
        Destinatario = destinatario;
        Datos = datos;
        Asunto = asunto;
    }
"""
content = content.replace('protected CorreoSaliente() { }', insert + '\n    protected CorreoSaliente() { }')
with open('src/Shapi.Dominio/Correo/CorreoSaliente.cs', 'w') as f:
    f.write(content)


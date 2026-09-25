import re

with open('src/Shapi.Infraestructura/Siembra/Base/SiembraBase.cs', 'r') as f:
    content = f.read()

content = 'using Microsoft.Extensions.Logging;\n' + content

with open('src/Shapi.Infraestructura/Siembra/Base/SiembraBase.cs', 'w') as f:
    f.write(content)

with open('src/Shapi.Infraestructura/Correo/ColaCorreoBaseDatos.cs', 'r') as f:
    content = f.read()
content = content.replace('_context.CorreosSalientes.Add', '_context.Set<CorreoSaliente>().Add')
with open('src/Shapi.Infraestructura/Correo/ColaCorreoBaseDatos.cs', 'w') as f:
    f.write(content)

with open('src/Shapi.Infraestructura/Bitacora/BitacoraBaseDatos.cs', 'r') as f:
    content = f.read()
content = content.replace('_context.EntradasBitacora.Add', '_context.Set<Shapi.Dominio.Bitacora.EntradaBitacora>().Add')
with open('src/Shapi.Infraestructura/Bitacora/BitacoraBaseDatos.cs', 'w') as f:
    f.write(content)


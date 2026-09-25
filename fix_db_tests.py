with open('tests/Shapi.Api.Tests/Persistencia/ShapiDbContextTests.cs', 'r') as f:
    content = f.read()

content = content.replace('await SiembraBase.EjecutarAsync(db, null, null, null, new RelojSistema(), hasher);', 'await SiembraBase.EjecutarAsync(db, null, null, null, new RelojSistema(), hasher, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);')
content = content.replace('await SiembraBase.EjecutarAsync(db, correo, "Admin", "Contra123!", new RelojSistema(), hasher);', 'await SiembraBase.EjecutarAsync(db, correo, "Admin", "Contra123!", new RelojSistema(), hasher, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);')
content = content.replace('await SiembraBase.EjecutarAsync(db, null, null, null, new Shapi.Infraestructura.Comun.RelojSistema(), hasher);', 'await SiembraBase.EjecutarAsync(db, null, null, null, new Shapi.Infraestructura.Comun.RelojSistema(), hasher, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);')

# Let's just do a regex replace to catch any call to EjecutarAsync that doesn't have logger
import re
content = re.sub(r'(await SiembraBase\.EjecutarAsync\(.*?hasher)(\));', r'\1, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance\2;', content)

with open('tests/Shapi.Api.Tests/Persistencia/ShapiDbContextTests.cs', 'w') as f:
    f.write(content)


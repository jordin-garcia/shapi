import re

for filepath in ['tests/Shapi.Api.Tests/SaludTests.cs', 'tests/Shapi.Api.Tests/Comun/ServiciosComunesTests.cs']:
    with open(filepath, 'r') as f:
        content = f.read()

    # We want to remove the entire line containing UseSetting
    lines = content.split('\n')
    lines = [line for line in lines if 'UseSetting("SHAPI_POSTGRES_CADENA"' not in line]
    
    with open(filepath, 'w') as f:
        f.write('\n'.join(lines))

with open('tests/Shapi.Api.Tests/Persistencia/ShapiDbContextTests.cs', 'r') as f:
    content = f.read()

content = content.replace('await SiembraBase.EjecutarAsync(db, null, null, null, new RelojSistema(), hasher);', 'await SiembraBase.EjecutarAsync(db, null, null, null, new RelojSistema(), hasher, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);')
content = content.replace('await SiembraBase.EjecutarAsync(db, correo, "Admin", "Contra123!", new RelojSistema(), hasher);', 'await SiembraBase.EjecutarAsync(db, correo, "Admin", "Contra123!", new RelojSistema(), hasher, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);')

with open('tests/Shapi.Api.Tests/Persistencia/ShapiDbContextTests.cs', 'w') as f:
    f.write(content)


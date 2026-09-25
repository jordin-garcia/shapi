import re

for filepath in ['tests/Shapi.Api.Tests/SaludTests.cs', 'tests/Shapi.Api.Tests/Comun/ServiciosComunesTests.cs']:
    with open(filepath, 'r') as f:
        content = f.read()

    content = re.sub(r'\s*\.UseSetting\("SHAPI_POSTGRES_CADENA", "Host=localhost;Database=dummy"\)', '', content)
    content = re.sub(r'constructor\.UseSetting\("SHAPI_POSTGRES_CADENA", "Host=localhost;Database=dummy"\);', '', content)

    with open(filepath, 'w') as f:
        f.write(content)


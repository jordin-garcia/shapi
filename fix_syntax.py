import re

with open('tests/Shapi.Api.Tests/SaludTests.cs', 'r') as f:
    content = f.read()

content = content.replace('var fabricaConfigurada = fabrica.WithWebHostBuilder(builder =>\n        using var cliente = fabricaConfigurada.CreateClient();', 'var fabricaConfigurada = fabrica;\n        using var cliente = fabricaConfigurada.CreateClient();')

with open('tests/Shapi.Api.Tests/SaludTests.cs', 'w') as f:
    f.write(content)

with open('tests/Shapi.Api.Tests/Comun/ServiciosComunesTests.cs', 'r') as f:
    content = f.read()

content = content.replace('var app = fabrica.WithWebHostBuilder(builder =>', 'var app = fabrica;')
content = content.replace('var fabricaConfigurada = fabrica.WithWebHostBuilder(builder =>', 'var fabricaConfigurada = fabrica;')

with open('tests/Shapi.Api.Tests/Comun/ServiciosComunesTests.cs', 'w') as f:
    f.write(content)


with open('src/Shapi.Api/Program.cs', 'r') as f:
    content = f.read()

# Replace condition
old_cond = 'if (builder.Configuration.GetValue<bool>("SHAPI_APLICAR_MIGRACIONES"))'
new_cond = 'if (bool.TryParse(app.Configuration["SHAPI_APLICAR_MIGRACIONES"], out var aplicar) && aplicar || app.Environment.IsDevelopment())'

content = content.replace(old_cond, new_cond)

# Replace EjecutarAsync call
old_call = 'await Shapi.Infraestructura.Siembra.Base.SiembraBase.EjecutarAsync(db, correo, nombre, contrasena, reloj, hasher);'
new_call = """var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();
    await Shapi.Infraestructura.Siembra.Base.SiembraBase.EjecutarAsync(db, correo, nombre, contrasena, reloj, hasher, logger);"""

content = content.replace(old_call, new_call)

with open('src/Shapi.Api/Program.cs', 'w') as f:
    f.write(content)

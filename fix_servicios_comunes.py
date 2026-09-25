with open('src/Shapi.Infraestructura/Comun/ServiciosComunes.cs', 'r') as f:
    content = f.read()

# Replace ColaCorreoNula and BitacoraNula
content = content.replace('services.AddSingleton<IColaCorreo, ColaCorreoNula>();', 'services.AddScoped<IColaCorreo, Shapi.Infraestructura.Correo.ColaCorreoBaseDatos>();')
content = content.replace('services.AddSingleton<IBitacora, BitacoraNula>();', 'services.AddScoped<IBitacora, Shapi.Infraestructura.Bitacora.BitacoraBaseDatos>();')

# Replace connection string logic
old_cs_logic = """var connectionString = configuration["SHAPI_POSTGRES_CADENA"] ?? throw new InvalidOperationException("Falta la cadena de conexión 'SHAPI_POSTGRES_CADENA'.");
        services.AddDbContext<ShapiDbContext>(options => options.UseNpgsql(connectionString));"""

new_cs_logic = """services.AddDbContext<ShapiDbContext>(options =>
        {
            var cs = configuration["SHAPI_POSTGRES_CADENA"];
            if (!string.IsNullOrEmpty(cs))
                options.UseNpgsql(cs);
            else
                options.UseNpgsql("Host=localhost;Database=shapi_placeholder"); // placeholder que falla al usarse, no al registrarse
        });"""

content = content.replace(old_cs_logic, new_cs_logic)

with open('src/Shapi.Infraestructura/Comun/ServiciosComunes.cs', 'w') as f:
    f.write(content)

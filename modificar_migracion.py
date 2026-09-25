import sys
import re

archivo = "src/Shapi.Infraestructura/Persistencia/Migraciones/20260925073244_Inicial.cs"

with open(archivo, "r") as f:
    lines = f.readlines()

out = []
for line in lines:
    if "protected override void Down(MigrationBuilder migrationBuilder)" in line:
        out.append('            migrationBuilder.Sql("CREATE UNIQUE INDEX ix_usuario_correo_lower ON usuario (lower(correo));");\n')
        out.append('            migrationBuilder.Sql("CREATE UNIQUE INDEX ix_consumidor_correo_lower ON consumidor (organizacion_id, lower(correo));");\n')
        out.append("\n")
        out.append(line)
        out.append('            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_usuario_correo_lower;");\n')
        out.append('            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_consumidor_correo_lower;");\n')
    else:
        out.append(line)

with open(archivo, "w") as f:
    f.writelines(out)

print("Modificado correctamente")

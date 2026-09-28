using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shapi.Infraestructura.Persistencia.Migraciones;

/// <inheritdoc />
public partial class IndiceCorreoSalientePendientes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "ix_correo_saliente_estado_proximo_intento_en",
            table: "correo_saliente",
            columns: new[] { "estado", "proximo_intento_en" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_correo_saliente_estado_proximo_intento_en",
            table: "correo_saliente");
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shapi.Infraestructura.Persistencia.Migraciones;

/// <inheritdoc />
public partial class ReferenciaHistoricaBitacora : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_bitacora_organizacion_organizacion_id",
            table: "bitacora");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddForeignKey(
            name: "fk_bitacora_organizacion_organizacion_id",
            table: "bitacora",
            column: "organizacion_id",
            principalTable: "organizacion",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }
}

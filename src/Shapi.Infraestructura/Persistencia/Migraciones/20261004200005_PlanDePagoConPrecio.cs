using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shapi.Infraestructura.Persistencia.Migraciones;

/// <inheritdoc />
public partial class PlanDePagoConPrecio : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddCheckConstraint(
            name: "ck_plan_api_pago",
            table: "plan_api",
            sql: "es_gratuito OR precio > 0");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_plan_api_pago",
            table: "plan_api");
    }
}

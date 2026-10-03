using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shapi.Infraestructura.Persistencia.Migraciones;

/// <inheritdoc />
public partial class PagoRechazadoSinSuscripcion : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_pago_suscripcion",
            table: "pago");

        migrationBuilder.AddColumn<Guid>(
            name: "api_id",
            table: "pago",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "consumidor_id",
            table: "pago",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_pago_api_id",
            table: "pago",
            column: "api_id");

        migrationBuilder.CreateIndex(
            name: "ix_pago_consumidor_id",
            table: "pago",
            column: "consumidor_id");

        migrationBuilder.AddCheckConstraint(
            name: "ck_pago_suscripcion",
            table: "pago",
            sql: "(num_nonnulls(suscripcion_plataforma_id, suscripcion_api_id) = 1 AND consumidor_id IS NULL AND api_id IS NULL) OR (suscripcion_plataforma_id IS NULL AND suscripcion_api_id IS NULL AND consumidor_id IS NOT NULL AND api_id IS NOT NULL AND estado = 'rechazado')");

        migrationBuilder.AddForeignKey(
            name: "fk_pago_api_api_id",
            table: "pago",
            column: "api_id",
            principalTable: "api",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "fk_pago_consumidor_consumidor_id",
            table: "pago",
            column: "consumidor_id",
            principalTable: "consumidor",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_pago_api_api_id",
            table: "pago");

        migrationBuilder.DropForeignKey(
            name: "fk_pago_consumidor_consumidor_id",
            table: "pago");

        migrationBuilder.DropIndex(
            name: "ix_pago_api_id",
            table: "pago");

        migrationBuilder.DropIndex(
            name: "ix_pago_consumidor_id",
            table: "pago");

        migrationBuilder.DropCheckConstraint(
            name: "ck_pago_suscripcion",
            table: "pago");

        migrationBuilder.DropColumn(
            name: "api_id",
            table: "pago");

        migrationBuilder.DropColumn(
            name: "consumidor_id",
            table: "pago");

        migrationBuilder.AddCheckConstraint(
            name: "ck_pago_suscripcion",
            table: "pago",
            sql: "num_nonnulls(suscripcion_plataforma_id, suscripcion_api_id) = 1");
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shapi.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class TextoConLargoEnCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_organizacion_nombre",
                table: "organizacion");

            migrationBuilder.AlterColumn<string>(
                name: "nombre",
                table: "organizacion",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "procesado_en",
                table: "lote_consolidado",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "asunto",
                table: "caso",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "portal_bienvenida",
                table: "api",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(280)",
                oldMaxLength: 280,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_organizacion_nombre",
                table: "organizacion",
                sql: "char_length(nombre) BETWEEN 2 AND 120");

            migrationBuilder.AddCheckConstraint(
                name: "ck_caso_asunto",
                table: "caso",
                sql: "char_length(asunto) <= 120");

            migrationBuilder.AddCheckConstraint(
                name: "ck_api_portal_bienvenida",
                table: "api",
                sql: "portal_bienvenida IS NULL OR char_length(portal_bienvenida) <= 280");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_organizacion_nombre",
                table: "organizacion");

            migrationBuilder.DropCheckConstraint(
                name: "ck_caso_asunto",
                table: "caso");

            migrationBuilder.DropCheckConstraint(
                name: "ck_api_portal_bienvenida",
                table: "api");

            migrationBuilder.AlterColumn<string>(
                name: "nombre",
                table: "organizacion",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "procesado_en",
                table: "lote_consolidado",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<string>(
                name: "asunto",
                table: "caso",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "portal_bienvenida",
                table: "api",
                type: "character varying(280)",
                maxLength: 280,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_organizacion_nombre",
                table: "organizacion",
                sql: "char_length(nombre) >= 2");
        }
    }
}

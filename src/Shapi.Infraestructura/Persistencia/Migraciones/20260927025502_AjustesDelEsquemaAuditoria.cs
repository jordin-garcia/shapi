using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shapi.Infraestructura.Persistencia.Migraciones;

/// <inheritdoc />
public partial class AjustesDelEsquemaAuditoria : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_suscripcion_plat_estado",
            table: "suscripcion_plataforma");

        migrationBuilder.DropCheckConstraint(
            name: "CK_suscripcion_plat_fechas",
            table: "suscripcion_plataforma");

        migrationBuilder.DropCheckConstraint(
            name: "CK_registro_dns_tipo",
            table: "registro_dns_simulado");

        migrationBuilder.DropCheckConstraint(
            name: "CK_medio_pago_org_cons",
            table: "medio_pago");

        migrationBuilder.DropCheckConstraint(
            name: "CK_consumo_entorno",
            table: "consumo_diario");

        migrationBuilder.DropSequence(
            name: "caso_numero_seq");

        migrationBuilder.RenameColumn(
            name: "rechazos429",
            table: "consumo_diario",
            newName: "rechazos_429");

        migrationBuilder.RenameColumn(
            name: "rechazos404",
            table: "consumo_diario",
            newName: "rechazos_404");

        migrationBuilder.RenameColumn(
            name: "rechazos403",
            table: "consumo_diario",
            newName: "rechazos_403");

        migrationBuilder.RenameColumn(
            name: "rechazos401",
            table: "consumo_diario",
            newName: "rechazos_401");

        migrationBuilder.RenameColumn(
            name: "origen5xx",
            table: "consumo_diario",
            newName: "origen_5xx");

        migrationBuilder.RenameColumn(
            name: "origen4xx",
            table: "consumo_diario",
            newName: "origen_4xx");

        migrationBuilder.RenameColumn(
            name: "origen3xx",
            table: "consumo_diario",
            newName: "origen_3xx");

        migrationBuilder.RenameColumn(
            name: "origen2xx",
            table: "consumo_diario",
            newName: "origen_2xx");

        migrationBuilder.CreateSequence<int>(
            name: "caso_numero_seq",
            startValue: 100L);

        migrationBuilder.AlterColumn<string>(
            name: "estado",
            table: "usuario",
            type: "text",
            nullable: false,
            defaultValue: "activo",
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "actualizado_en",
            table: "usuario",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "now()");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "creado_en",
            table: "usuario",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "now()");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "actualizado_en",
            table: "token",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "now()");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "creado_en",
            table: "token",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "now()");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "actualizado_en",
            table: "sesion",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "now()");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "actualizado_en",
            table: "registro_dns_simulado",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "now()");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "creado_en",
            table: "registro_dns_simulado",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "now()");

        migrationBuilder.AlterColumn<string>(
            name: "estado_admin",
            table: "organizacion",
            type: "text",
            nullable: false,
            defaultValue: "activa",
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "actualizado_en",
            table: "membresia",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "now()");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "creado_en",
            table: "membresia",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "now()");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "actualizado_en",
            table: "correo_saliente",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "now()");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "creado_en",
            table: "correo_saliente",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "now()");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "actualizado_en",
            table: "consumo_diario",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "now()");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "creado_en",
            table: "consumo_diario",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "now()");

        migrationBuilder.AlterColumn<string>(
            name: "estado",
            table: "consumidor",
            type: "text",
            nullable: false,
            defaultValue: "activo",
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "actualizado_en",
            table: "consumidor",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "now()");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "creado_en",
            table: "consumidor",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "now()");

        migrationBuilder.AlterColumn<int>(
            name: "numero",
            table: "caso",
            type: "integer",
            nullable: false,
            defaultValueSql: "nextval('caso_numero_seq')",
            oldClrType: typeof(int),
            oldType: "integer");

        migrationBuilder.AlterColumn<string>(
            name: "estado",
            table: "api",
            type: "text",
            nullable: false,
            defaultValue: "borrador",
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.CreateIndex(
            name: "ix_suscripcion_plataforma_plan_siguiente_id",
            table: "suscripcion_plataforma",
            column: "plan_siguiente_id");

        migrationBuilder.AddCheckConstraint(
            name: "ck_suscripcion_plataforma_estado",
            table: "suscripcion_plataforma",
            sql: "estado IN ('activa','en_gracia','suspendida','finalizada')");

        migrationBuilder.AddCheckConstraint(
            name: "ck_suscripcion_plataforma_fechas",
            table: "suscripcion_plataforma",
            sql: "fin > inicio");

        migrationBuilder.CreateIndex(
            name: "ix_suscripcion_api_plan_siguiente_id",
            table: "suscripcion_api",
            column: "plan_siguiente_id");

        migrationBuilder.AddCheckConstraint(
            name: "ck_ruta_cache_solo_get",
            table: "ruta",
            sql: "cache_segundos = 0 OR metodo = 'GET'");

        migrationBuilder.AddCheckConstraint(
            name: "ck_registro_dns_simulado_tipo",
            table: "registro_dns_simulado",
            sql: "tipo IN ('CNAME')");

        migrationBuilder.AddCheckConstraint(
            name: "ck_organizacion_nombre",
            table: "organizacion",
            sql: "char_length(nombre) >= 2");

        migrationBuilder.AddCheckConstraint(
            name: "ck_medio_pago_dueno",
            table: "medio_pago",
            sql: "num_nonnulls(organizacion_id, consumidor_id) = 1");

        migrationBuilder.AddCheckConstraint(
            name: "ck_medio_pago_mes_vencimiento",
            table: "medio_pago",
            sql: "mes_vencimiento BETWEEN 1 AND 12");

        migrationBuilder.AddCheckConstraint(
            name: "ck_consumo_diario_entorno",
            table: "consumo_diario",
            sql: "entorno IN ('produccion','pruebas')");

        migrationBuilder.AddCheckConstraint(
            name: "ck_consumo_diario_hist_latencia_compuerta",
            table: "consumo_diario",
            sql: "cardinality(hist_latencia_compuerta) = 10");

        migrationBuilder.AddCheckConstraint(
            name: "ck_consumo_diario_hist_latencia_total",
            table: "consumo_diario",
            sql: "cardinality(hist_latencia_total) = 10");

        migrationBuilder.AddCheckConstraint(
            name: "ck_api_portal_logo",
            table: "api",
            sql: "portal_logo IS NULL OR octet_length(portal_logo) <= 524288");

        migrationBuilder.AddForeignKey(
            name: "fk_suscripcion_api_plan_api_plan_siguiente_id",
            table: "suscripcion_api",
            column: "plan_siguiente_id",
            principalTable: "plan_api",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "fk_suscripcion_plataforma_plan_plataforma_plan_siguiente_id",
            table: "suscripcion_plataforma",
            column: "plan_siguiente_id",
            principalTable: "plan_plataforma",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        // Nombres de restricciones en snake_case. EF compara los nombres sin distinguir mayúsculas,
        // así que solo recreó las que cambiaron de nombre; las demás se renombran aquí.
        migrationBuilder.Sql("""
            DO $$
            DECLARE r record;
            BEGIN
                FOR r IN
                    SELECT c.conname, t.relname
                    FROM pg_constraint c
                    JOIN pg_class t ON t.oid = c.conrelid
                    JOIN pg_namespace n ON n.oid = t.relnamespace
                    WHERE n.nspname = current_schema() AND c.conname <> lower(c.conname)
                LOOP
                    EXECUTE format('ALTER TABLE %I RENAME CONSTRAINT %I TO %I', r.relname, r.conname, lower(r.conname));
                END LOOP;
            END $$;
            """);

        // La bitácora solo admite inserciones (RF-41, 07 §3.6): se rechazan UPDATE, DELETE y TRUNCATE.
        migrationBuilder.Sql("""
            DROP TRIGGER IF EXISTS bitacora_append_only ON bitacora;
            DROP FUNCTION IF EXISTS check_append_only();

            CREATE FUNCTION bitacora_rechazar_cambios() RETURNS trigger AS $$
            BEGIN
                RAISE EXCEPTION 'La bitácora solo admite inserciones: no se permite %.', TG_OP;
            END;
            $$ LANGUAGE plpgsql;

            CREATE TRIGGER bitacora_solo_inserciones
            BEFORE UPDATE OR DELETE ON bitacora
            FOR EACH ROW EXECUTE FUNCTION bitacora_rechazar_cambios();

            CREATE TRIGGER bitacora_sin_vaciado
            BEFORE TRUNCATE ON bitacora
            FOR EACH STATEMENT EXECUTE FUNCTION bitacora_rechazar_cambios();
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TRIGGER IF EXISTS bitacora_sin_vaciado ON bitacora;
            DROP TRIGGER IF EXISTS bitacora_solo_inserciones ON bitacora;
            DROP FUNCTION IF EXISTS bitacora_rechazar_cambios();

            CREATE FUNCTION check_append_only() RETURNS trigger AS $$
            BEGIN
                RAISE EXCEPTION 'This table is append-only';
            END;
            $$ LANGUAGE plpgsql;

            CREATE TRIGGER bitacora_append_only
            BEFORE UPDATE OR DELETE ON bitacora
            FOR EACH ROW EXECUTE FUNCTION check_append_only();
            """);

        migrationBuilder.Sql("""
            DO $$
            DECLARE r record;
            BEGIN
                FOR r IN
                    SELECT c.conname, t.relname
                    FROM pg_constraint c
                    JOIN pg_class t ON t.oid = c.conrelid
                    JOIN pg_namespace n ON n.oid = t.relnamespace
                    WHERE n.nspname = current_schema() AND c.conname = ANY (ARRAY[
                        'ck_api_especificacion_formato', 'ck_api_estado', 'ck_api_portal_color', 'ck_api_portal_logo_tipo',
                        'ck_api_subdominio', 'ck_bitacora_actor_tipo', 'ck_caso_estado', 'ck_clave_estado',
                        'ck_clave_revocada_por', 'ck_clave_tipo', 'ck_consumidor_estado', 'ck_correo_saliente_estado',
                        'ck_dominio_propio_estado', 'ck_medio_pago_marca', 'ck_membresia_rol', 'ck_organizacion_estado_admin',
                        'ck_organizacion_tipo', 'ck_pago_concepto', 'ck_pago_estado', 'ck_pago_monto', 'ck_pago_suscripcion',
                        'ck_plan_api_cuota', 'ck_plan_api_gratuito', 'ck_plan_api_limite', 'ck_plan_api_precio',
                        'ck_plan_api_vigencia', 'ck_plan_plataforma_cuota', 'ck_plan_plataforma_precio',
                        'ck_plan_plataforma_vigencia', 'ck_ruta_cache_segundos', 'ck_ruta_limite_minuto', 'ck_ruta_metodo',
                        'ck_ruta_peso_llamadas', 'ck_sesion_actor', 'ck_sesion_ambito', 'ck_suscripcion_api_estado',
                        'ck_suscripcion_api_fechas', 'ck_token_tipo', 'ck_usuario_estado'])
                LOOP
                    EXECUTE format('ALTER TABLE %I RENAME CONSTRAINT %I TO %I', r.relname, r.conname, 'CK_' || substr(r.conname, 4));
                END LOOP;
            END $$;
            """);

        migrationBuilder.DropForeignKey(
            name: "fk_suscripcion_api_plan_api_plan_siguiente_id",
            table: "suscripcion_api");

        migrationBuilder.DropForeignKey(
            name: "fk_suscripcion_plataforma_plan_plataforma_plan_siguiente_id",
            table: "suscripcion_plataforma");

        migrationBuilder.DropIndex(
            name: "ix_suscripcion_plataforma_plan_siguiente_id",
            table: "suscripcion_plataforma");

        migrationBuilder.DropCheckConstraint(
            name: "ck_suscripcion_plataforma_estado",
            table: "suscripcion_plataforma");

        migrationBuilder.DropCheckConstraint(
            name: "ck_suscripcion_plataforma_fechas",
            table: "suscripcion_plataforma");

        migrationBuilder.DropIndex(
            name: "ix_suscripcion_api_plan_siguiente_id",
            table: "suscripcion_api");

        migrationBuilder.DropCheckConstraint(
            name: "ck_ruta_cache_solo_get",
            table: "ruta");

        migrationBuilder.DropCheckConstraint(
            name: "ck_registro_dns_simulado_tipo",
            table: "registro_dns_simulado");

        migrationBuilder.DropCheckConstraint(
            name: "ck_organizacion_nombre",
            table: "organizacion");

        migrationBuilder.DropCheckConstraint(
            name: "ck_medio_pago_dueno",
            table: "medio_pago");

        migrationBuilder.DropCheckConstraint(
            name: "ck_medio_pago_mes_vencimiento",
            table: "medio_pago");

        migrationBuilder.DropCheckConstraint(
            name: "ck_consumo_diario_entorno",
            table: "consumo_diario");

        migrationBuilder.DropCheckConstraint(
            name: "ck_consumo_diario_hist_latencia_compuerta",
            table: "consumo_diario");

        migrationBuilder.DropCheckConstraint(
            name: "ck_consumo_diario_hist_latencia_total",
            table: "consumo_diario");

        migrationBuilder.DropCheckConstraint(
            name: "ck_api_portal_logo",
            table: "api");

        migrationBuilder.DropColumn(
            name: "actualizado_en",
            table: "usuario");

        migrationBuilder.DropColumn(
            name: "creado_en",
            table: "usuario");

        migrationBuilder.DropColumn(
            name: "actualizado_en",
            table: "token");

        migrationBuilder.DropColumn(
            name: "creado_en",
            table: "token");

        migrationBuilder.DropColumn(
            name: "actualizado_en",
            table: "sesion");

        migrationBuilder.DropColumn(
            name: "actualizado_en",
            table: "registro_dns_simulado");

        migrationBuilder.DropColumn(
            name: "creado_en",
            table: "registro_dns_simulado");

        migrationBuilder.DropColumn(
            name: "actualizado_en",
            table: "membresia");

        migrationBuilder.DropColumn(
            name: "creado_en",
            table: "membresia");

        migrationBuilder.DropColumn(
            name: "actualizado_en",
            table: "correo_saliente");

        migrationBuilder.DropColumn(
            name: "creado_en",
            table: "correo_saliente");

        migrationBuilder.DropColumn(
            name: "actualizado_en",
            table: "consumo_diario");

        migrationBuilder.DropColumn(
            name: "creado_en",
            table: "consumo_diario");

        migrationBuilder.DropColumn(
            name: "actualizado_en",
            table: "consumidor");

        migrationBuilder.DropColumn(
            name: "creado_en",
            table: "consumidor");

        // Primero se quita el DEFAULT nextval: PostgreSQL no deja borrar una secuencia que una columna usa.
        migrationBuilder.AlterColumn<int>(
            name: "numero",
            table: "caso",
            type: "integer",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer",
            oldDefaultValueSql: "nextval('caso_numero_seq')");

        migrationBuilder.DropSequence(
            name: "caso_numero_seq");

        migrationBuilder.RenameColumn(
            name: "rechazos_429",
            table: "consumo_diario",
            newName: "rechazos429");

        migrationBuilder.RenameColumn(
            name: "rechazos_404",
            table: "consumo_diario",
            newName: "rechazos404");

        migrationBuilder.RenameColumn(
            name: "rechazos_403",
            table: "consumo_diario",
            newName: "rechazos403");

        migrationBuilder.RenameColumn(
            name: "rechazos_401",
            table: "consumo_diario",
            newName: "rechazos401");

        migrationBuilder.RenameColumn(
            name: "origen_5xx",
            table: "consumo_diario",
            newName: "origen5xx");

        migrationBuilder.RenameColumn(
            name: "origen_4xx",
            table: "consumo_diario",
            newName: "origen4xx");

        migrationBuilder.RenameColumn(
            name: "origen_3xx",
            table: "consumo_diario",
            newName: "origen3xx");

        migrationBuilder.RenameColumn(
            name: "origen_2xx",
            table: "consumo_diario",
            newName: "origen2xx");

        migrationBuilder.CreateSequence(
            name: "caso_numero_seq",
            incrementBy: 10);

        migrationBuilder.AlterColumn<string>(
            name: "estado",
            table: "usuario",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text",
            oldDefaultValue: "activo");

        migrationBuilder.AlterColumn<string>(
            name: "estado_admin",
            table: "organizacion",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text",
            oldDefaultValue: "activa");

        migrationBuilder.AlterColumn<string>(
            name: "estado",
            table: "consumidor",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text",
            oldDefaultValue: "activo");

        migrationBuilder.AlterColumn<string>(
            name: "estado",
            table: "api",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text",
            oldDefaultValue: "borrador");

        migrationBuilder.AddCheckConstraint(
            name: "CK_suscripcion_plat_estado",
            table: "suscripcion_plataforma",
            sql: "estado IN ('activa','en_gracia','suspendida','finalizada')");

        migrationBuilder.AddCheckConstraint(
            name: "CK_suscripcion_plat_fechas",
            table: "suscripcion_plataforma",
            sql: "fin > inicio");

        migrationBuilder.AddCheckConstraint(
            name: "CK_registro_dns_tipo",
            table: "registro_dns_simulado",
            sql: "tipo IN ('CNAME')");

        migrationBuilder.AddCheckConstraint(
            name: "CK_medio_pago_org_cons",
            table: "medio_pago",
            sql: "num_nonnulls(organizacion_id, consumidor_id) = 1");

        migrationBuilder.AddCheckConstraint(
            name: "CK_consumo_entorno",
            table: "consumo_diario",
            sql: "entorno IN ('produccion','pruebas')");
    }
}

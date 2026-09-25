using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Shapi.Infraestructura.Migrations;

/// <inheritdoc />
public partial class Inicial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "api",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                organizacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                nombre = table.Column<string>(type: "text", nullable: false),
                subdominio = table.Column<string>(type: "text", nullable: false),
                url_origen = table.Column<string>(type: "text", nullable: false),
                estado = table.Column<string>(type: "text", nullable: false, defaultValue: "Borrador"),
                especificacion = table.Column<string>(type: "text", nullable: true),
                especificacion_formato = table.Column<string>(type: "text", nullable: true),
                especificacion_titulo = table.Column<string>(type: "text", nullable: true),
                especificacion_descripcion = table.Column<string>(type: "text", nullable: true),
                especificacion_version = table.Column<string>(type: "text", nullable: true),
                especificacion_cargada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                portal_nombre = table.Column<string>(type: "text", nullable: true),
                portal_color = table.Column<string>(type: "character(7)", fixedLength: true, maxLength: 7, nullable: false, defaultValue: "#3B6FF0"),
                portal_logo = table.Column<byte[]>(type: "bytea", nullable: true),
                portal_logo_tipo = table.Column<string>(type: "text", nullable: true),
                portal_bienvenida = table.Column<string>(type: "text", nullable: true),
                secreto_origen_cifrado = table.Column<string>(type: "text", nullable: false),
                publicada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_api", x => x.id);
                table.CheckConstraint("CK_api_especificacion_formato", "especificacion_formato IS NULL OR especificacion_formato IN ('Json','Yaml')");
                table.CheckConstraint("CK_api_estado", "estado IN ('Borrador','Publicada','Despublicada')");
                table.CheckConstraint("CK_api_portal_color", "portal_color ~ '^#[0-9A-Fa-f]{6}$'");
                table.CheckConstraint("CK_api_portal_logo_tipo", "portal_logo_tipo IS NULL OR portal_logo_tipo IN ('Png','Svg')");
                table.CheckConstraint("CK_api_subdominio", "subdominio ~ '^[a-z0-9][a-z0-9-]{1,28}[a-z0-9]$'");
            });

        migrationBuilder.CreateTable(
            name: "bitacora",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                actor_tipo = table.Column<string>(type: "text", nullable: false),
                actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                actor_nombre = table.Column<string>(type: "text", nullable: false),
                organizacion_id = table.Column<Guid>(type: "uuid", nullable: true),
                accion = table.Column<string>(type: "text", nullable: false),
                objetivo_tipo = table.Column<string>(type: "text", nullable: true),
                objetivo_id = table.Column<Guid>(type: "uuid", nullable: true),
                descripcion = table.Column<string>(type: "text", nullable: false),
                detalle = table.Column<string>(type: "jsonb", nullable: true),
                ip = table.Column<IPAddress>(type: "inet", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_bitacora", x => x.id);
                table.CheckConstraint("CK_bitacora_actor_tipo", "actor_tipo IN ('Usuario','Consumidor','Sistema')");
            });

        migrationBuilder.CreateTable(
            name: "caso",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                numero = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:IdentitySequenceOptions", "'100', '1', '', '', 'False', '1'")
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                organizacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                api_id = table.Column<Guid>(type: "uuid", nullable: true),
                creado_por = table.Column<Guid>(type: "uuid", nullable: false),
                asignado_a = table.Column<Guid>(type: "uuid", nullable: true),
                asunto = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                estado = table.Column<string>(type: "text", nullable: false),
                cerrado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_caso", x => x.id);
                table.CheckConstraint("CK_caso_estado", "estado IN ('Abierto','Cerrado')");
            });

        migrationBuilder.CreateTable(
            name: "caso_mensaje",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                caso_id = table.Column<Guid>(type: "uuid", nullable: false),
                autor_id = table.Column<Guid>(type: "uuid", nullable: false),
                cuerpo = table.Column<string>(type: "text", nullable: false),
                creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_caso_mensaje", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "clave",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                suscripcion_id = table.Column<Guid>(type: "uuid", nullable: false),
                tipo = table.Column<string>(type: "text", nullable: false),
                prefijo = table.Column<string>(type: "text", nullable: false),
                ultimos4 = table.Column<string>(type: "character(4)", fixedLength: true, maxLength: 4, nullable: false),
                hash_sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                estado = table.Column<string>(type: "text", nullable: false),
                expira_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                revocada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                revocada_por = table.Column<string>(type: "text", nullable: true),
                creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_clave", x => x.id);
                table.CheckConstraint("CK_clave_estado", "estado IN ('Activa','Rotada','Revocada')");
                table.CheckConstraint("CK_clave_revocada_por", "revocada_por IS NULL OR revocada_por IN ('Consumidor','Proveedor')");
                table.CheckConstraint("CK_clave_tipo", "tipo IN ('Produccion','Pruebas')");
            });

        migrationBuilder.CreateTable(
            name: "consumidor",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                organizacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                nombre = table.Column<string>(type: "text", nullable: false),
                nombre_empresa = table.Column<string>(type: "text", nullable: false),
                correo = table.Column<string>(type: "text", nullable: false),
                hash_contrasena = table.Column<string>(type: "text", nullable: false),
                correo_verificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                estado = table.Column<string>(type: "text", nullable: false, defaultValue: "Activo"),
                intentos_fallidos = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                bloqueado_hasta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_consumidor", x => x.id);
                table.CheckConstraint("CK_consumidor_estado", "estado IN ('Activo','Desactivado')");
            });

        migrationBuilder.CreateTable(
            name: "consumo_diario",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                fecha = table.Column<DateOnly>(type: "date", nullable: false),
                api_id = table.Column<Guid>(type: "uuid", nullable: false),
                ruta_id = table.Column<Guid>(type: "uuid", nullable: true),
                suscripcion_id = table.Column<Guid>(type: "uuid", nullable: true),
                entorno = table.Column<string>(type: "text", nullable: false),
                peticiones = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                llamadas = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                bytes_entrada = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                bytes_salida = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                rechazos401 = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                rechazos403 = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                rechazos404 = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                rechazos429 = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                origen2xx = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                origen3xx = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                origen4xx = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                origen5xx = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                origen_fallo = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                hist_latencia_total = table.Column<int[]>(type: "integer[]", nullable: false),
                hist_latencia_compuerta = table.Column<int[]>(type: "integer[]", nullable: false),
                latencia_total_suma_ms = table.Column<long>(type: "bigint", nullable: false),
                latencia_compuerta_suma_ms = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_consumo_diario", x => x.id);
                table.CheckConstraint("CK_consumo_entorno", "entorno IN ('Produccion','Pruebas')");
            });

        migrationBuilder.CreateTable(
            name: "correo_saliente",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                destinatario = table.Column<string>(type: "text", nullable: false),
                asunto = table.Column<string>(type: "text", nullable: false),
                plantilla = table.Column<string>(type: "text", nullable: false),
                datos = table.Column<string>(type: "jsonb", nullable: false),
                estado = table.Column<string>(type: "text", nullable: false),
                intentos = table.Column<int>(type: "integer", nullable: false),
                proximo_intento_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ultimo_error = table.Column<string>(type: "text", nullable: true),
                enviado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_correo_saliente", x => x.id);
                table.CheckConstraint("CK_correo_saliente_estado", "estado IN ('Pendiente','Enviado','Fallido')");
            });

        migrationBuilder.CreateTable(
            name: "dominio_propio",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                api_id = table.Column<Guid>(type: "uuid", nullable: false),
                dominio = table.Column<string>(type: "text", nullable: false),
                destino_cname = table.Column<string>(type: "text", nullable: false),
                estado = table.Column<string>(type: "text", nullable: false),
                motivo = table.Column<string>(type: "text", nullable: true),
                verificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ultimo_intento_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_dominio_propio", x => x.id);
                table.CheckConstraint("CK_dominio_propio_estado", "estado IN ('Pendiente','Verificado','Fallido')");
            });

        migrationBuilder.CreateTable(
            name: "lote_consolidado",
            columns: table => new
            {
                lote_id = table.Column<Guid>(type: "uuid", nullable: false),
                procesado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_lote_consolidado", x => x.lote_id);
            });

        migrationBuilder.CreateTable(
            name: "medio_pago",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                organizacion_id = table.Column<Guid>(type: "uuid", nullable: true),
                consumidor_id = table.Column<Guid>(type: "uuid", nullable: true),
                token_pasarela = table.Column<string>(type: "text", nullable: false),
                marca = table.Column<string>(type: "text", nullable: false),
                ultimos4 = table.Column<string>(type: "character(4)", fixedLength: true, maxLength: 4, nullable: false),
                titular = table.Column<string>(type: "text", nullable: false),
                mes_vencimiento = table.Column<short>(type: "smallint", nullable: false),
                anio_vencimiento = table.Column<short>(type: "smallint", nullable: false),
                creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_medio_pago", x => x.id);
                table.CheckConstraint("CK_medio_pago_marca", "marca IN ('Visa','Mastercard','AmericanExpress')");
                table.CheckConstraint("CK_medio_pago_org_cons", "num_nonnulls(organizacion_id, consumidor_id) = 1");
            });

        migrationBuilder.CreateTable(
            name: "membresia",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                organizacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                rol = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_membresia", x => x.id);
                table.CheckConstraint("CK_membresia_rol", "rol IN ('Administrador','Soporte','Propietario','Editor','Lector')");
            });

        migrationBuilder.CreateTable(
            name: "organizacion",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                tipo = table.Column<string>(type: "text", nullable: false),
                estado_admin = table.Column<string>(type: "text", nullable: false, defaultValue: "Activa"),
                motivo_suspension = table.Column<string>(type: "text", nullable: true),
                creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_organizacion", x => x.id);
                table.CheckConstraint("CK_organizacion_estado_admin", "estado_admin IN ('Activa','Suspendida')");
                table.CheckConstraint("CK_organizacion_tipo", "tipo IN ('Plataforma','Proveedor')");
            });

        migrationBuilder.CreateTable(
            name: "pago",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                suscripcion_plataforma_id = table.Column<Guid>(type: "uuid", nullable: true),
                suscripcion_api_id = table.Column<Guid>(type: "uuid", nullable: true),
                medio_pago_id = table.Column<Guid>(type: "uuid", nullable: true),
                concepto = table.Column<string>(type: "text", nullable: false),
                descripcion = table.Column<string>(type: "text", nullable: false),
                monto = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                estado = table.Column<string>(type: "text", nullable: false),
                referencia_pasarela = table.Column<string>(type: "text", nullable: true),
                motivo_rechazo = table.Column<string>(type: "text", nullable: true),
                periodo_inicio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                periodo_fin = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                revertido_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                revertido_por = table.Column<Guid>(type: "uuid", nullable: true),
                creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_pago", x => x.id);
                table.CheckConstraint("CK_pago_concepto", "concepto IN ('Contratacion','Renovacion','CambioPlan','Reactivacion')");
                table.CheckConstraint("CK_pago_estado", "estado IN ('Autorizado','Rechazado','Revertido')");
                table.CheckConstraint("CK_pago_monto", "monto > 0");
                table.CheckConstraint("CK_pago_suscripcion", "num_nonnulls(suscripcion_plataforma_id, suscripcion_api_id) = 1");
            });

        migrationBuilder.CreateTable(
            name: "plan_api",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                api_id = table.Column<Guid>(type: "uuid", nullable: false),
                nombre = table.Column<string>(type: "text", nullable: false),
                descripcion = table.Column<string>(type: "text", nullable: false),
                precio = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                es_gratuito = table.Column<bool>(type: "boolean", nullable: false),
                vigencia_dias = table.Column<int>(type: "integer", nullable: false),
                cuota_llamadas = table.Column<long>(type: "bigint", nullable: false),
                limite_minuto = table.Column<int>(type: "integer", nullable: false),
                activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_plan_api", x => x.id);
                table.CheckConstraint("CK_plan_api_cuota", "cuota_llamadas > 0");
                table.CheckConstraint("CK_plan_api_gratuito", "NOT es_gratuito OR precio = 0");
                table.CheckConstraint("CK_plan_api_limite", "limite_minuto > 0");
                table.CheckConstraint("CK_plan_api_precio", "precio >= 0");
                table.CheckConstraint("CK_plan_api_vigencia", "vigencia_dias BETWEEN 1 AND 366");
            });

        migrationBuilder.CreateTable(
            name: "plan_plataforma",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                nombre = table.Column<string>(type: "text", nullable: false),
                descripcion = table.Column<string>(type: "text", nullable: false),
                precio = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                vigencia_dias = table.Column<int>(type: "integer", nullable: false),
                max_apis = table.Column<int>(type: "integer", nullable: true),
                max_miembros = table.Column<int>(type: "integer", nullable: true),
                cuota_peticiones = table.Column<long>(type: "bigint", nullable: false),
                dominio_propio = table.Column<bool>(type: "boolean", nullable: false),
                es_prueba = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                orden = table.Column<int>(type: "integer", nullable: false),
                creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_plan_plataforma", x => x.id);
                table.CheckConstraint("CK_plan_plataforma_cuota", "cuota_peticiones > 0");
                table.CheckConstraint("CK_plan_plataforma_precio", "precio >= 0");
                table.CheckConstraint("CK_plan_plataforma_vigencia", "vigencia_dias BETWEEN 1 AND 366");
            });

        migrationBuilder.CreateTable(
            name: "registro_dns_simulado",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                nombre = table.Column<string>(type: "text", nullable: false),
                tipo = table.Column<string>(type: "text", nullable: false),
                valor = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_registro_dns_simulado", x => x.id);
                table.CheckConstraint("CK_registro_dns_tipo", "tipo IN ('CNAME')");
            });

        migrationBuilder.CreateTable(
            name: "sesion",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                ambito = table.Column<string>(type: "text", nullable: false),
                usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                consumidor_id = table.Column<Guid>(type: "uuid", nullable: true),
                hash_identificador = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                host = table.Column<string>(type: "text", nullable: false),
                creada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ultimo_uso_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                expira_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                revocada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ip = table.Column<IPAddress>(type: "inet", nullable: true),
                agente_usuario = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_sesion", x => x.id);
                table.CheckConstraint("CK_sesion_ambito", "ambito IN ('Personal','Consumidor')");
                table.CheckConstraint("CK_sesion_usuario_consumidor", "num_nonnulls(usuario_id, consumidor_id) = 1");
            });

        migrationBuilder.CreateTable(
            name: "suscripcion_api",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                consumidor_id = table.Column<Guid>(type: "uuid", nullable: false),
                api_id = table.Column<Guid>(type: "uuid", nullable: false),
                plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                estado = table.Column<string>(type: "text", nullable: false),
                inicio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                fin = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                gracia_hasta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                plan_siguiente_id = table.Column<Guid>(type: "uuid", nullable: true),
                medio_pago_id = table.Column<Guid>(type: "uuid", nullable: true),
                creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_suscripcion_api", x => x.id);
                table.CheckConstraint("CK_suscripcion_api_estado", "estado IN ('Activa','EnGracia','Suspendida','Finalizada')");
                table.CheckConstraint("CK_suscripcion_api_fechas", "fin > inicio");
            });

        migrationBuilder.CreateTable(
            name: "suscripcion_plataforma",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                organizacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                estado = table.Column<string>(type: "text", nullable: false),
                inicio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                fin = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                gracia_hasta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                plan_siguiente_id = table.Column<Guid>(type: "uuid", nullable: true),
                medio_pago_id = table.Column<Guid>(type: "uuid", nullable: true),
                creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_suscripcion_plataforma", x => x.id);
                table.CheckConstraint("CK_suscripcion_plat_estado", "estado IN ('Activa','EnGracia','Suspendida','Finalizada')");
                table.CheckConstraint("CK_suscripcion_plat_fechas", "fin > inicio");
            });

        migrationBuilder.CreateTable(
            name: "token",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tipo = table.Column<string>(type: "text", nullable: false),
                hash_token = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                consumidor_id = table.Column<Guid>(type: "uuid", nullable: true),
                organizacion_id = table.Column<Guid>(type: "uuid", nullable: true),
                correo = table.Column<string>(type: "text", nullable: false),
                rol = table.Column<string>(type: "text", nullable: true),
                expira_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                usado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_token", x => x.id);
                table.CheckConstraint("CK_token_tipo", "tipo IN ('VerificacionCorreo','Recuperacion','InvitacionMiembro','InvitacionConsumidor','DefinirContrasena')");
            });

        migrationBuilder.CreateTable(
            name: "usuario",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                nombre = table.Column<string>(type: "text", nullable: false),
                correo = table.Column<string>(type: "text", nullable: false),
                hash_contrasena = table.Column<string>(type: "text", nullable: true),
                correo_verificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                estado = table.Column<string>(type: "text", nullable: false, defaultValue: "Activo"),
                intentos_fallidos = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                bloqueado_hasta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_usuario", x => x.id);
                table.CheckConstraint("CK_usuario_estado", "estado IN ('Activo','Desactivado')");
            });

        migrationBuilder.CreateTable(
            name: "ruta",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                api_id = table.Column<Guid>(type: "uuid", nullable: false),
                metodo = table.Column<string>(type: "text", nullable: false),
                patron = table.Column<string>(type: "text", nullable: false),
                resumen = table.Column<string>(type: "text", nullable: true),
                descripcion = table.Column<string>(type: "text", nullable: true),
                definicion = table.Column<string>(type: "jsonb", nullable: false),
                expuesta = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                limite_minuto = table.Column<int>(type: "integer", nullable: true),
                cache_segundos = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                peso_llamadas = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_ruta", x => x.id);
                table.CheckConstraint("CK_ruta_cache_segundos", "cache_segundos BETWEEN 0 AND 86400");
                table.CheckConstraint("CK_ruta_limite_minuto", "limite_minuto IS NULL OR limite_minuto > 0");
                table.CheckConstraint("CK_ruta_metodo", "metodo IN ('Get','Post','Put','Patch','Delete','Head','Options')");
                table.CheckConstraint("CK_ruta_peso_llamadas", "peso_llamadas BETWEEN 1 AND 1000");
                table.ForeignKey(
                    name: "fk_ruta_api_api_id",
                    column: x => x.api_id,
                    principalTable: "api",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_api_subdominio",
            table: "api",
            column: "subdominio",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_caso_numero",
            table: "caso",
            column: "numero",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_clave_hash_sha256",
            table: "clave",
            column: "hash_sha256",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_clave_suscripcion_id_tipo",
            table: "clave",
            columns: new[] { "suscripcion_id", "tipo" },
            unique: true,
            filter: "estado = 'Activa'");

        migrationBuilder.CreateIndex(
            name: "ix_consumidor_organizacion_id_correo",
            table: "consumidor",
            columns: new[] { "organizacion_id", "correo" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_consumo_diario_api_id_fecha",
            table: "consumo_diario",
            columns: new[] { "api_id", "fecha" });

        migrationBuilder.CreateIndex(
            name: "ix_consumo_diario_fecha_api_id_ruta_id_suscripcion_id_entorno",
            table: "consumo_diario",
            columns: new[] { "fecha", "api_id", "ruta_id", "suscripcion_id", "entorno" },
            unique: true)
            .Annotation("Npgsql:NullsDistinct", false);

        migrationBuilder.CreateIndex(
            name: "ix_consumo_diario_suscripcion_id_fecha",
            table: "consumo_diario",
            columns: new[] { "suscripcion_id", "fecha" });

        migrationBuilder.CreateIndex(
            name: "ix_dominio_propio_api_id",
            table: "dominio_propio",
            column: "api_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_dominio_propio_dominio",
            table: "dominio_propio",
            column: "dominio",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_membresia_organizacion_id",
            table: "membresia",
            column: "organizacion_id",
            unique: true,
            filter: "rol = 'Propietario'");

        migrationBuilder.CreateIndex(
            name: "ix_membresia_usuario_id",
            table: "membresia",
            column: "usuario_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_organizacion_tipo",
            table: "organizacion",
            column: "tipo",
            unique: true,
            filter: "tipo = 'Plataforma'");

        migrationBuilder.CreateIndex(
            name: "ix_pago_creado_en",
            table: "pago",
            column: "creado_en",
            descending: new bool[0]);

        migrationBuilder.CreateIndex(
            name: "ix_pago_suscripcion_api_id_creado_en",
            table: "pago",
            columns: new[] { "suscripcion_api_id", "creado_en" },
            descending: new[] { false, true });

        migrationBuilder.CreateIndex(
            name: "ix_pago_suscripcion_plataforma_id_creado_en",
            table: "pago",
            columns: new[] { "suscripcion_plataforma_id", "creado_en" },
            descending: new[] { false, true });

        migrationBuilder.CreateIndex(
            name: "ix_plan_api_api_id_nombre",
            table: "plan_api",
            columns: new[] { "api_id", "nombre" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_plan_plataforma_es_prueba",
            table: "plan_plataforma",
            column: "es_prueba",
            unique: true,
            filter: "es_prueba = true");

        migrationBuilder.CreateIndex(
            name: "ix_plan_plataforma_nombre",
            table: "plan_plataforma",
            column: "nombre",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_registro_dns_simulado_nombre",
            table: "registro_dns_simulado",
            column: "nombre",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_ruta_api_id_metodo_patron",
            table: "ruta",
            columns: new[] { "api_id", "metodo", "patron" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_sesion_hash_identificador",
            table: "sesion",
            column: "hash_identificador",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_suscripcion_api_consumidor_id_api_id",
            table: "suscripcion_api",
            columns: new[] { "consumidor_id", "api_id" },
            unique: true,
            filter: "estado <> 'Finalizada'");

        migrationBuilder.CreateIndex(
            name: "ix_suscripcion_api_estado_fin",
            table: "suscripcion_api",
            columns: new[] { "estado", "fin" });

        migrationBuilder.CreateIndex(
            name: "ix_suscripcion_api_estado_gracia_hasta",
            table: "suscripcion_api",
            columns: new[] { "estado", "gracia_hasta" });

        migrationBuilder.CreateIndex(
            name: "ix_suscripcion_plataforma_estado_fin",
            table: "suscripcion_plataforma",
            columns: new[] { "estado", "fin" });

        migrationBuilder.CreateIndex(
            name: "ix_suscripcion_plataforma_estado_gracia_hasta",
            table: "suscripcion_plataforma",
            columns: new[] { "estado", "gracia_hasta" });

        migrationBuilder.CreateIndex(
            name: "ix_suscripcion_plataforma_organizacion_id",
            table: "suscripcion_plataforma",
            column: "organizacion_id",
            unique: true,
            filter: "estado <> 'Finalizada'");

        migrationBuilder.CreateIndex(
            name: "ix_token_hash_token",
            table: "token",
            column: "hash_token",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_usuario_correo",
            table: "usuario",
            column: "correo",
            unique: true);

        migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION trg_prevent_update_delete()
RETURNS TRIGGER AS $$
BEGIN
    RAISE EXCEPTION 'This table is append-only';
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_bitacora_append_only
BEFORE UPDATE OR DELETE ON bitacora
FOR EACH ROW
EXECUTE FUNCTION trg_prevent_update_delete();
            ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DROP TRIGGER IF EXISTS trg_bitacora_append_only ON bitacora;
DROP FUNCTION IF EXISTS trg_prevent_update_delete();
            ");

        migrationBuilder.DropTable(
            name: "bitacora");

        migrationBuilder.DropTable(
            name: "caso");

        migrationBuilder.DropTable(
            name: "caso_mensaje");

        migrationBuilder.DropTable(
            name: "clave");

        migrationBuilder.DropTable(
            name: "consumidor");

        migrationBuilder.DropTable(
            name: "consumo_diario");

        migrationBuilder.DropTable(
            name: "correo_saliente");

        migrationBuilder.DropTable(
            name: "dominio_propio");

        migrationBuilder.DropTable(
            name: "lote_consolidado");

        migrationBuilder.DropTable(
            name: "medio_pago");

        migrationBuilder.DropTable(
            name: "membresia");

        migrationBuilder.DropTable(
            name: "organizacion");

        migrationBuilder.DropTable(
            name: "pago");

        migrationBuilder.DropTable(
            name: "plan_api");

        migrationBuilder.DropTable(
            name: "plan_plataforma");

        migrationBuilder.DropTable(
            name: "registro_dns_simulado");

        migrationBuilder.DropTable(
            name: "ruta");

        migrationBuilder.DropTable(
            name: "sesion");

        migrationBuilder.DropTable(
            name: "suscripcion_api");

        migrationBuilder.DropTable(
            name: "suscripcion_plataforma");

        migrationBuilder.DropTable(
            name: "token");

        migrationBuilder.DropTable(
            name: "usuario");

        migrationBuilder.DropTable(
            name: "api");
    }
}

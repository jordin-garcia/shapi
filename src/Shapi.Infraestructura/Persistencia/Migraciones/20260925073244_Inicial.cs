using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Shapi.Infraestructura.Persistencia.Migraciones;

/// <inheritdoc />
public partial class Inicial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateSequence(
            name: "caso_numero_seq",
            incrementBy: 10);

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
                intentos = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                proximo_intento_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ultimo_error = table.Column<string>(type: "text", nullable: true),
                enviado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_correo_saliente", x => x.id);
                table.CheckConstraint("CK_correo_saliente_estado", "estado IN ('pendiente','enviado','fallido')");
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
            name: "organizacion",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                tipo = table.Column<string>(type: "text", nullable: false),
                estado_admin = table.Column<string>(type: "text", nullable: false),
                motivo_suspension = table.Column<string>(type: "text", nullable: true),
                creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_organizacion", x => x.id);
                table.CheckConstraint("CK_organizacion_estado_admin", "estado_admin IN ('activa','suspendida')");
                table.CheckConstraint("CK_organizacion_tipo", "tipo IN ('plataforma','proveedor')");
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
            name: "usuario",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                nombre = table.Column<string>(type: "text", nullable: false),
                correo = table.Column<string>(type: "text", nullable: false),
                hash_contrasena = table.Column<string>(type: "text", nullable: true),
                correo_verificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                estado = table.Column<string>(type: "text", nullable: false),
                intentos_fallidos = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                bloqueado_hasta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_usuario", x => x.id);
                table.CheckConstraint("CK_usuario_estado", "estado IN ('activo','desactivado')");
            });

        migrationBuilder.CreateTable(
            name: "api",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                organizacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                nombre = table.Column<string>(type: "text", nullable: false),
                subdominio = table.Column<string>(type: "text", nullable: false),
                url_origen = table.Column<string>(type: "text", nullable: false),
                estado = table.Column<string>(type: "text", nullable: false),
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
                portal_bienvenida = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: true),
                secreto_origen_cifrado = table.Column<string>(type: "text", nullable: false),
                publicada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_api", x => x.id);
                table.CheckConstraint("CK_api_especificacion_formato", "especificacion_formato IS NULL OR especificacion_formato IN ('json','yaml')");
                table.CheckConstraint("CK_api_estado", "estado IN ('borrador','publicada','despublicada')");
                table.CheckConstraint("CK_api_portal_color", "portal_color ~ '^#[0-9A-Fa-f]{6}$'");
                table.CheckConstraint("CK_api_portal_logo_tipo", "portal_logo_tipo IS NULL OR portal_logo_tipo IN ('image/png','image/svg+xml')");
                table.CheckConstraint("CK_api_subdominio", "subdominio ~ '^[a-z0-9][a-z0-9-]{1,28}[a-z0-9]$'");
                table.ForeignKey(
                    name: "fk_api_organizacion_organizacion_id",
                    column: x => x.organizacion_id,
                    principalTable: "organizacion",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
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
                table.CheckConstraint("CK_bitacora_actor_tipo", "actor_tipo IN ('usuario','consumidor','sistema')");
                table.ForeignKey(
                    name: "fk_bitacora_organizacion_organizacion_id",
                    column: x => x.organizacion_id,
                    principalTable: "organizacion",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
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
                estado = table.Column<string>(type: "text", nullable: false),
                intentos_fallidos = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                bloqueado_hasta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_consumidor", x => x.id);
                table.CheckConstraint("CK_consumidor_estado", "estado IN ('activo','desactivado')");
                table.ForeignKey(
                    name: "fk_consumidor_organizacion_organizacion_id",
                    column: x => x.organizacion_id,
                    principalTable: "organizacion",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
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
                table.CheckConstraint("CK_membresia_rol", "rol IN ('administrador','soporte','propietario','editor','lector')");
                table.ForeignKey(
                    name: "fk_membresia_organizacion_organizacion_id",
                    column: x => x.organizacion_id,
                    principalTable: "organizacion",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_membresia_usuario_usuario_id",
                    column: x => x.usuario_id,
                    principalTable: "usuario",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "caso",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                numero = table.Column<int>(type: "integer", nullable: false),
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
                table.CheckConstraint("CK_caso_estado", "estado IN ('abierto','cerrado')");
                table.ForeignKey(
                    name: "fk_caso_api_api_id",
                    column: x => x.api_id,
                    principalTable: "api",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_caso_organizacion_organizacion_id",
                    column: x => x.organizacion_id,
                    principalTable: "organizacion",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_caso_usuario_asignado_a",
                    column: x => x.asignado_a,
                    principalTable: "usuario",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_caso_usuario_creado_por",
                    column: x => x.creado_por,
                    principalTable: "usuario",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
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
                table.CheckConstraint("CK_dominio_propio_estado", "estado IN ('pendiente','verificado','fallido')");
                table.ForeignKey(
                    name: "fk_dominio_propio_api_api_id",
                    column: x => x.api_id,
                    principalTable: "api",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
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
                table.ForeignKey(
                    name: "fk_plan_api_api_api_id",
                    column: x => x.api_id,
                    principalTable: "api",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
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
                table.CheckConstraint("CK_ruta_metodo", "metodo IN ('GET','POST','PUT','PATCH','DELETE','HEAD','OPTIONS')");
                table.CheckConstraint("CK_ruta_peso_llamadas", "peso_llamadas BETWEEN 1 AND 1000");
                table.ForeignKey(
                    name: "fk_ruta_api_api_id",
                    column: x => x.api_id,
                    principalTable: "api",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
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
                table.CheckConstraint("CK_medio_pago_marca", "marca IN ('Visa','Mastercard','American Express')");
                table.CheckConstraint("CK_medio_pago_org_cons", "num_nonnulls(organizacion_id, consumidor_id) = 1");
                table.ForeignKey(
                    name: "fk_medio_pago_consumidor_consumidor_id",
                    column: x => x.consumidor_id,
                    principalTable: "consumidor",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_medio_pago_organizacion_organizacion_id",
                    column: x => x.organizacion_id,
                    principalTable: "organizacion",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
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
                creada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                ultimo_uso_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                expira_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                revocada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ip = table.Column<IPAddress>(type: "inet", nullable: true),
                agente_usuario = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_sesion", x => x.id);
                table.CheckConstraint("CK_sesion_actor", "num_nonnulls(usuario_id, consumidor_id) = 1");
                table.CheckConstraint("CK_sesion_ambito", "ambito IN ('personal','consumidor')");
                table.ForeignKey(
                    name: "fk_sesion_consumidor_consumidor_id",
                    column: x => x.consumidor_id,
                    principalTable: "consumidor",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_sesion_usuario_usuario_id",
                    column: x => x.usuario_id,
                    principalTable: "usuario",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
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
                table.CheckConstraint("CK_token_tipo", "tipo IN ('verificacion_correo','recuperacion','invitacion_miembro','invitacion_consumidor','definir_contrasena')");
                table.ForeignKey(
                    name: "fk_token_consumidor_consumidor_id",
                    column: x => x.consumidor_id,
                    principalTable: "consumidor",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_token_organizacion_organizacion_id",
                    column: x => x.organizacion_id,
                    principalTable: "organizacion",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_token_usuario_usuario_id",
                    column: x => x.usuario_id,
                    principalTable: "usuario",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
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
                table.ForeignKey(
                    name: "fk_caso_mensaje_caso_caso_id",
                    column: x => x.caso_id,
                    principalTable: "caso",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_caso_mensaje_usuario_autor_id",
                    column: x => x.autor_id,
                    principalTable: "usuario",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
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
                table.CheckConstraint("CK_suscripcion_api_estado", "estado IN ('activa','en_gracia','suspendida','finalizada')");
                table.CheckConstraint("CK_suscripcion_api_fechas", "fin > inicio");
                table.ForeignKey(
                    name: "fk_suscripcion_api_api_api_id",
                    column: x => x.api_id,
                    principalTable: "api",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_suscripcion_api_consumidor_consumidor_id",
                    column: x => x.consumidor_id,
                    principalTable: "consumidor",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_suscripcion_api_medio_pago_medio_pago_id",
                    column: x => x.medio_pago_id,
                    principalTable: "medio_pago",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_suscripcion_api_plan_api_plan_id",
                    column: x => x.plan_id,
                    principalTable: "plan_api",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
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
                table.CheckConstraint("CK_suscripcion_plat_estado", "estado IN ('activa','en_gracia','suspendida','finalizada')");
                table.CheckConstraint("CK_suscripcion_plat_fechas", "fin > inicio");
                table.ForeignKey(
                    name: "fk_suscripcion_plataforma_medio_pago_medio_pago_id",
                    column: x => x.medio_pago_id,
                    principalTable: "medio_pago",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_suscripcion_plataforma_organizacion_organizacion_id",
                    column: x => x.organizacion_id,
                    principalTable: "organizacion",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_suscripcion_plataforma_plan_plataforma_plan_id",
                    column: x => x.plan_id,
                    principalTable: "plan_plataforma",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
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
                table.CheckConstraint("CK_clave_estado", "estado IN ('activa','rotada','revocada')");
                table.CheckConstraint("CK_clave_revocada_por", "revocada_por IS NULL OR revocada_por IN ('consumidor','proveedor')");
                table.CheckConstraint("CK_clave_tipo", "tipo IN ('produccion','pruebas')");
                table.ForeignKey(
                    name: "fk_clave_suscripcion_api_suscripcion_id",
                    column: x => x.suscripcion_id,
                    principalTable: "suscripcion_api",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
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
                table.CheckConstraint("CK_consumo_entorno", "entorno IN ('produccion','pruebas')");
                table.ForeignKey(
                    name: "fk_consumo_diario_api_api_id",
                    column: x => x.api_id,
                    principalTable: "api",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_consumo_diario_ruta_ruta_id",
                    column: x => x.ruta_id,
                    principalTable: "ruta",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_consumo_diario_suscripcion_api_suscripcion_id",
                    column: x => x.suscripcion_id,
                    principalTable: "suscripcion_api",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
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
                table.CheckConstraint("CK_pago_concepto", "concepto IN ('contratacion','renovacion','cambio_plan','reactivacion')");
                table.CheckConstraint("CK_pago_estado", "estado IN ('autorizado','rechazado','revertido')");
                table.CheckConstraint("CK_pago_monto", "monto > 0");
                table.CheckConstraint("CK_pago_suscripcion", "num_nonnulls(suscripcion_plataforma_id, suscripcion_api_id) = 1");
                table.ForeignKey(
                    name: "fk_pago_medio_pago_medio_pago_id",
                    column: x => x.medio_pago_id,
                    principalTable: "medio_pago",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_pago_suscripcion_api_suscripcion_api_id",
                    column: x => x.suscripcion_api_id,
                    principalTable: "suscripcion_api",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_pago_suscripcion_plataforma_suscripcion_plataforma_id",
                    column: x => x.suscripcion_plataforma_id,
                    principalTable: "suscripcion_plataforma",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_pago_usuario_revertido_por",
                    column: x => x.revertido_por,
                    principalTable: "usuario",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_api_organizacion_id",
            table: "api",
            column: "organizacion_id");

        migrationBuilder.CreateIndex(
            name: "ix_api_subdominio",
            table: "api",
            column: "subdominio",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_bitacora_organizacion_id",
            table: "bitacora",
            column: "organizacion_id");

        migrationBuilder.CreateIndex(
            name: "ix_caso_api_id",
            table: "caso",
            column: "api_id");

        migrationBuilder.CreateIndex(
            name: "ix_caso_asignado_a",
            table: "caso",
            column: "asignado_a");

        migrationBuilder.CreateIndex(
            name: "ix_caso_creado_por",
            table: "caso",
            column: "creado_por");

        migrationBuilder.CreateIndex(
            name: "ix_caso_numero",
            table: "caso",
            column: "numero",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_caso_organizacion_id",
            table: "caso",
            column: "organizacion_id");

        migrationBuilder.CreateIndex(
            name: "ix_caso_mensaje_autor_id",
            table: "caso_mensaje",
            column: "autor_id");

        migrationBuilder.CreateIndex(
            name: "ix_caso_mensaje_caso_id",
            table: "caso_mensaje",
            column: "caso_id");

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
            filter: "estado = 'activa'");

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
            name: "ix_consumo_diario_ruta_id",
            table: "consumo_diario",
            column: "ruta_id");

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
            name: "ix_medio_pago_consumidor_id",
            table: "medio_pago",
            column: "consumidor_id");

        migrationBuilder.CreateIndex(
            name: "ix_medio_pago_organizacion_id",
            table: "medio_pago",
            column: "organizacion_id");

        migrationBuilder.CreateIndex(
            name: "ix_membresia_organizacion_id",
            table: "membresia",
            column: "organizacion_id",
            unique: true,
            filter: "rol = 'propietario'");

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
            filter: "tipo = 'plataforma'");

        migrationBuilder.CreateIndex(
            name: "ix_pago_creado_en",
            table: "pago",
            column: "creado_en",
            descending: new bool[0]);

        migrationBuilder.CreateIndex(
            name: "ix_pago_medio_pago_id",
            table: "pago",
            column: "medio_pago_id");

        migrationBuilder.CreateIndex(
            name: "ix_pago_revertido_por",
            table: "pago",
            column: "revertido_por");

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
            name: "ix_sesion_consumidor_id",
            table: "sesion",
            column: "consumidor_id");

        migrationBuilder.CreateIndex(
            name: "ix_sesion_hash_identificador",
            table: "sesion",
            column: "hash_identificador",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_sesion_usuario_id",
            table: "sesion",
            column: "usuario_id");

        migrationBuilder.CreateIndex(
            name: "ix_suscripcion_api_api_id",
            table: "suscripcion_api",
            column: "api_id");

        migrationBuilder.CreateIndex(
            name: "ix_suscripcion_api_consumidor_id_api_id",
            table: "suscripcion_api",
            columns: new[] { "consumidor_id", "api_id" },
            unique: true,
            filter: "estado <> 'finalizada'");

        migrationBuilder.CreateIndex(
            name: "ix_suscripcion_api_estado_fin",
            table: "suscripcion_api",
            columns: new[] { "estado", "fin" });

        migrationBuilder.CreateIndex(
            name: "ix_suscripcion_api_estado_gracia_hasta",
            table: "suscripcion_api",
            columns: new[] { "estado", "gracia_hasta" });

        migrationBuilder.CreateIndex(
            name: "ix_suscripcion_api_medio_pago_id",
            table: "suscripcion_api",
            column: "medio_pago_id");

        migrationBuilder.CreateIndex(
            name: "ix_suscripcion_api_plan_id",
            table: "suscripcion_api",
            column: "plan_id");

        migrationBuilder.CreateIndex(
            name: "ix_suscripcion_plataforma_estado_fin",
            table: "suscripcion_plataforma",
            columns: new[] { "estado", "fin" });

        migrationBuilder.CreateIndex(
            name: "ix_suscripcion_plataforma_estado_gracia_hasta",
            table: "suscripcion_plataforma",
            columns: new[] { "estado", "gracia_hasta" });

        migrationBuilder.CreateIndex(
            name: "ix_suscripcion_plataforma_medio_pago_id",
            table: "suscripcion_plataforma",
            column: "medio_pago_id");

        migrationBuilder.CreateIndex(
            name: "ix_suscripcion_plataforma_organizacion_id",
            table: "suscripcion_plataforma",
            column: "organizacion_id",
            unique: true,
            filter: "estado <> 'finalizada'");

        migrationBuilder.CreateIndex(
            name: "ix_suscripcion_plataforma_plan_id",
            table: "suscripcion_plataforma",
            column: "plan_id");

        migrationBuilder.CreateIndex(
            name: "ix_token_consumidor_id",
            table: "token",
            column: "consumidor_id");

        migrationBuilder.CreateIndex(
            name: "ix_token_hash_token",
            table: "token",
            column: "hash_token",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_token_organizacion_id",
            table: "token",
            column: "organizacion_id");

        migrationBuilder.CreateIndex(
            name: "ix_token_usuario_id",
            table: "token",
            column: "usuario_id");

        migrationBuilder.CreateIndex(
            name: "ix_usuario_correo",
            table: "usuario",
            column: "correo",
            unique: true);

        migrationBuilder.Sql("CREATE UNIQUE INDEX ix_usuario_correo_lower ON usuario (lower(correo));");
        migrationBuilder.Sql("CREATE UNIQUE INDEX ix_consumidor_correo_lower ON consumidor (organizacion_id, lower(correo));");
        migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION check_append_only() RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION 'This table is append-only';
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER bitacora_append_only
BEFORE UPDATE OR DELETE ON bitacora
FOR EACH ROW EXECUTE FUNCTION check_append_only();
");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS bitacora_append_only ON bitacora;");
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS check_append_only();");
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_usuario_correo_lower;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_consumidor_correo_lower;");

        migrationBuilder.DropTable(
            name: "bitacora");

        migrationBuilder.DropTable(
            name: "caso_mensaje");

        migrationBuilder.DropTable(
            name: "clave");

        migrationBuilder.DropTable(
            name: "consumo_diario");

        migrationBuilder.DropTable(
            name: "correo_saliente");

        migrationBuilder.DropTable(
            name: "dominio_propio");

        migrationBuilder.DropTable(
            name: "lote_consolidado");

        migrationBuilder.DropTable(
            name: "membresia");

        migrationBuilder.DropTable(
            name: "pago");

        migrationBuilder.DropTable(
            name: "registro_dns_simulado");

        migrationBuilder.DropTable(
            name: "sesion");

        migrationBuilder.DropTable(
            name: "token");

        migrationBuilder.DropTable(
            name: "caso");

        migrationBuilder.DropTable(
            name: "ruta");

        migrationBuilder.DropTable(
            name: "suscripcion_api");

        migrationBuilder.DropTable(
            name: "suscripcion_plataforma");

        migrationBuilder.DropTable(
            name: "usuario");

        migrationBuilder.DropTable(
            name: "plan_api");

        migrationBuilder.DropTable(
            name: "medio_pago");

        migrationBuilder.DropTable(
            name: "plan_plataforma");

        migrationBuilder.DropTable(
            name: "api");

        migrationBuilder.DropTable(
            name: "consumidor");

        migrationBuilder.DropTable(
            name: "organizacion");

        migrationBuilder.DropSequence(
            name: "caso_numero_seq");
    }
}

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Shapi.Dominio.Apis;
using Shapi.Dominio.Bitacora;
using Shapi.Dominio.Claves;
using Shapi.Dominio.Consumo;
using Shapi.Dominio.Correo;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Pagos;
using Shapi.Dominio.Soporte;
using Shapi.Dominio.Suscripciones;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

/// <summary>Conversores de valor para todos los enumerados del dominio.
/// Usan los strings exactos de 07 §3 (snake_case para multi-palabra, mayúsculas para métodos HTTP).</summary>
internal static class Conversores
{
    public static readonly ValueConverter<TipoOrganizacion, string> TipoOrganizacion =
        new(v => v == Dominio.Organizaciones.TipoOrganizacion.Plataforma ? "plataforma" : "proveedor",
            s => s == "plataforma" ? Dominio.Organizaciones.TipoOrganizacion.Plataforma : Dominio.Organizaciones.TipoOrganizacion.Proveedor);

    public static readonly ValueConverter<EstadoAdmin, string> EstadoAdmin =
        new(v => v == Dominio.Organizaciones.EstadoAdmin.Activa ? "activa" : "suspendida",
            s => s == "activa" ? Dominio.Organizaciones.EstadoAdmin.Activa : Dominio.Organizaciones.EstadoAdmin.Suspendida);

    public static readonly ValueConverter<Rol, string> Rol =
        new(v => v == Dominio.Organizaciones.Rol.Administrador ? "administrador" :
                 v == Dominio.Organizaciones.Rol.Soporte ? "soporte" :
                 v == Dominio.Organizaciones.Rol.Propietario ? "propietario" :
                 v == Dominio.Organizaciones.Rol.Editor ? "editor" : "lector",
            s => s == "administrador" ? Dominio.Organizaciones.Rol.Administrador :
                 s == "soporte" ? Dominio.Organizaciones.Rol.Soporte :
                 s == "propietario" ? Dominio.Organizaciones.Rol.Propietario :
                 s == "editor" ? Dominio.Organizaciones.Rol.Editor : Dominio.Organizaciones.Rol.Lector);

    public static readonly ValueConverter<EstadoCuenta, string> EstadoCuenta =
        new(v => v == Dominio.Identidad.EstadoCuenta.Activo ? "activo" : "desactivado",
            s => s == "activo" ? Dominio.Identidad.EstadoCuenta.Activo : Dominio.Identidad.EstadoCuenta.Desactivado);

    public static readonly ValueConverter<TipoToken, string> TipoToken =
        new(v => v == Dominio.Identidad.TipoToken.VerificacionCorreo ? "verificacion_correo" :
                 v == Dominio.Identidad.TipoToken.Recuperacion ? "recuperacion" :
                 v == Dominio.Identidad.TipoToken.InvitacionMiembro ? "invitacion_miembro" :
                 v == Dominio.Identidad.TipoToken.InvitacionConsumidor ? "invitacion_consumidor" : "definir_contrasena",
            s => s == "verificacion_correo" ? Dominio.Identidad.TipoToken.VerificacionCorreo :
                 s == "recuperacion" ? Dominio.Identidad.TipoToken.Recuperacion :
                 s == "invitacion_miembro" ? Dominio.Identidad.TipoToken.InvitacionMiembro :
                 s == "invitacion_consumidor" ? Dominio.Identidad.TipoToken.InvitacionConsumidor : Dominio.Identidad.TipoToken.DefinirContrasena);

    public static readonly ValueConverter<AmbitoSesion, string> AmbitoSesion =
        new(v => v == Dominio.Identidad.AmbitoSesion.Personal ? "personal" : "consumidor",
            s => s == "personal" ? Dominio.Identidad.AmbitoSesion.Personal : Dominio.Identidad.AmbitoSesion.Consumidor);

    public static readonly ValueConverter<EstadoApi, string> EstadoApi =
        new(v => v == Dominio.Apis.EstadoApi.Borrador ? "borrador" :
                 v == Dominio.Apis.EstadoApi.Publicada ? "publicada" : "despublicada",
            s => s == "borrador" ? Dominio.Apis.EstadoApi.Borrador :
                 s == "publicada" ? Dominio.Apis.EstadoApi.Publicada : Dominio.Apis.EstadoApi.Despublicada);

    public static readonly ValueConverter<EspecificacionFormato, string> EspecificacionFormato =
        new(v => v == Dominio.Apis.EspecificacionFormato.Json ? "json" : "yaml",
            s => s == "json" ? Dominio.Apis.EspecificacionFormato.Json : Dominio.Apis.EspecificacionFormato.Yaml);

    public static readonly ValueConverter<LogoTipo, string> LogoTipo =
        new(v => v == Dominio.Apis.LogoTipo.Png ? "image/png" : "image/svg+xml",
            s => s == "image/png" ? Dominio.Apis.LogoTipo.Png : Dominio.Apis.LogoTipo.Svg);

    public static readonly ValueConverter<MetodoHttp, string> MetodoHttp =
        new(v => v == Dominio.Apis.MetodoHttp.Get ? "GET" :
                 v == Dominio.Apis.MetodoHttp.Post ? "POST" :
                 v == Dominio.Apis.MetodoHttp.Put ? "PUT" :
                 v == Dominio.Apis.MetodoHttp.Patch ? "PATCH" :
                 v == Dominio.Apis.MetodoHttp.Delete ? "DELETE" :
                 v == Dominio.Apis.MetodoHttp.Head ? "HEAD" : "OPTIONS",
            s => s == "GET" ? Dominio.Apis.MetodoHttp.Get :
                 s == "POST" ? Dominio.Apis.MetodoHttp.Post :
                 s == "PUT" ? Dominio.Apis.MetodoHttp.Put :
                 s == "PATCH" ? Dominio.Apis.MetodoHttp.Patch :
                 s == "DELETE" ? Dominio.Apis.MetodoHttp.Delete :
                 s == "HEAD" ? Dominio.Apis.MetodoHttp.Head : Dominio.Apis.MetodoHttp.Options);

    public static readonly ValueConverter<EstadoDominio, string> EstadoDominio =
        new(v => v == Dominio.Apis.EstadoDominio.Pendiente ? "pendiente" :
                 v == Dominio.Apis.EstadoDominio.Verificado ? "verificado" : "fallido",
            s => s == "pendiente" ? Dominio.Apis.EstadoDominio.Pendiente :
                 s == "verificado" ? Dominio.Apis.EstadoDominio.Verificado : Dominio.Apis.EstadoDominio.Fallido);

    public static readonly ValueConverter<EstadoSuscripcion, string> EstadoSuscripcion =
        new(v => v == Dominio.Suscripciones.EstadoSuscripcion.Activa ? "activa" :
                 v == Dominio.Suscripciones.EstadoSuscripcion.EnGracia ? "en_gracia" :
                 v == Dominio.Suscripciones.EstadoSuscripcion.Suspendida ? "suspendida" : "finalizada",
            s => s == "activa" ? Dominio.Suscripciones.EstadoSuscripcion.Activa :
                 s == "en_gracia" ? Dominio.Suscripciones.EstadoSuscripcion.EnGracia :
                 s == "suspendida" ? Dominio.Suscripciones.EstadoSuscripcion.Suspendida : Dominio.Suscripciones.EstadoSuscripcion.Finalizada);

    public static readonly ValueConverter<TipoClave, string> TipoClave =
        new(v => v == Dominio.Claves.TipoClave.Produccion ? "produccion" : "pruebas",
            s => s == "produccion" ? Dominio.Claves.TipoClave.Produccion : Dominio.Claves.TipoClave.Pruebas);

    public static readonly ValueConverter<EstadoClave, string> EstadoClave =
        new(v => v == Dominio.Claves.EstadoClave.Activa ? "activa" :
                 v == Dominio.Claves.EstadoClave.Rotada ? "rotada" : "revocada",
            s => s == "activa" ? Dominio.Claves.EstadoClave.Activa :
                 s == "rotada" ? Dominio.Claves.EstadoClave.Rotada : Dominio.Claves.EstadoClave.Revocada);

    public static readonly ValueConverter<RevocadaPor, string> RevocadaPor =
        new(v => v == Dominio.Claves.RevocadaPor.Consumidor ? "consumidor" : "proveedor",
            s => s == "consumidor" ? Dominio.Claves.RevocadaPor.Consumidor : Dominio.Claves.RevocadaPor.Proveedor);

    public static readonly ValueConverter<MarcaTarjeta, string> MarcaTarjeta =
        new(v => v == Dominio.Pagos.MarcaTarjeta.Visa ? "Visa" :
                 v == Dominio.Pagos.MarcaTarjeta.Mastercard ? "Mastercard" : "American Express",
            s => s == "Visa" ? Dominio.Pagos.MarcaTarjeta.Visa :
                 s == "Mastercard" ? Dominio.Pagos.MarcaTarjeta.Mastercard : Dominio.Pagos.MarcaTarjeta.AmericanExpress);

    public static readonly ValueConverter<ConceptoPago, string> ConceptoPago =
        new(v => v == Dominio.Pagos.ConceptoPago.Contratacion ? "contratacion" :
                 v == Dominio.Pagos.ConceptoPago.Renovacion ? "renovacion" :
                 v == Dominio.Pagos.ConceptoPago.CambioPlan ? "cambio_plan" : "reactivacion",
            s => s == "contratacion" ? Dominio.Pagos.ConceptoPago.Contratacion :
                 s == "renovacion" ? Dominio.Pagos.ConceptoPago.Renovacion :
                 s == "cambio_plan" ? Dominio.Pagos.ConceptoPago.CambioPlan : Dominio.Pagos.ConceptoPago.Reactivacion);

    public static readonly ValueConverter<EstadoPago, string> EstadoPago =
        new(v => v == Dominio.Pagos.EstadoPago.Autorizado ? "autorizado" :
                 v == Dominio.Pagos.EstadoPago.Rechazado ? "rechazado" : "revertido",
            s => s == "autorizado" ? Dominio.Pagos.EstadoPago.Autorizado :
                 s == "rechazado" ? Dominio.Pagos.EstadoPago.Rechazado : Dominio.Pagos.EstadoPago.Revertido);

    public static readonly ValueConverter<EntornoConsumo, string> EntornoConsumo =
        new(v => v == Dominio.Consumo.EntornoConsumo.Produccion ? "produccion" : "pruebas",
            s => s == "produccion" ? Dominio.Consumo.EntornoConsumo.Produccion : Dominio.Consumo.EntornoConsumo.Pruebas);

    public static readonly ValueConverter<EstadoCaso, string> EstadoCaso =
        new(v => v == Dominio.Soporte.EstadoCaso.Abierto ? "abierto" : "cerrado",
            s => s == "abierto" ? Dominio.Soporte.EstadoCaso.Abierto : Dominio.Soporte.EstadoCaso.Cerrado);

    public static readonly ValueConverter<ActorTipo, string> ActorTipo =
        new(v => v == Dominio.Bitacora.ActorTipo.Usuario ? "usuario" :
                 v == Dominio.Bitacora.ActorTipo.Consumidor ? "consumidor" : "sistema",
            s => s == "usuario" ? Dominio.Bitacora.ActorTipo.Usuario :
                 s == "consumidor" ? Dominio.Bitacora.ActorTipo.Consumidor : Dominio.Bitacora.ActorTipo.Sistema);

    public static readonly ValueConverter<EstadoCorreo, string> EstadoCorreo =
        new(v => v == Dominio.Correo.EstadoCorreo.Pendiente ? "pendiente" :
                 v == Dominio.Correo.EstadoCorreo.Enviado ? "enviado" : "fallido",
            s => s == "pendiente" ? Dominio.Correo.EstadoCorreo.Pendiente :
                 s == "enviado" ? Dominio.Correo.EstadoCorreo.Enviado : Dominio.Correo.EstadoCorreo.Fallido);
}

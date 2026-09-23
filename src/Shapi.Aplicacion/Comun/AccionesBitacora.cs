namespace Shapi.Aplicacion.Comun;

/// <summary>Catálogo de acciones de la bitácora (10 §7). Es exactamente la tabla de la especificación.</summary>
public static class AccionesBitacora
{
    public const string OrganizacionSuspendida = "organizacion.suspendida";
    public const string OrganizacionReactivada = "organizacion.reactivada";

    public const string PagoRevertido = "pago.revertido";

    public const string PlanPlataformaCreado = "plan_plataforma.creado";
    public const string PlanPlataformaEditado = "plan_plataforma.editado";
    public const string PlanPlataformaDesactivado = "plan_plataforma.desactivado";

    public const string CuentaPlataformaCreada = "cuenta_plataforma.creada";
    public const string CuentaPlataformaDesactivada = "cuenta_plataforma.desactivada";
    public const string CuentaPlataformaActivada = "cuenta_plataforma.activada";

    public const string SuscripcionPlataformaContratada = "suscripcion_plataforma.contratada";
    public const string SuscripcionPlataformaCambiada = "suscripcion_plataforma.cambiada";

    public const string MiembroInvitado = "miembro.invitado";
    public const string MiembroRolCambiado = "miembro.rol_cambiado";
    public const string MiembroQuitado = "miembro.quitado";

    public const string ApiRegistrada = "api.registrada";
    public const string ApiPublicada = "api.publicada";
    public const string ApiDespublicada = "api.despublicada";

    public const string RutaExpuesta = "ruta.expuesta";
    public const string RutaOcultada = "ruta.ocultada";

    public const string DominioConectado = "dominio.conectado";
    public const string DominioVerificado = "dominio.verificado";

    public const string SecretoOrigenRegenerado = "secreto_origen.regenerado";

    public const string PlanApiCreado = "plan_api.creado";
    public const string PlanApiEditado = "plan_api.editado";
    public const string PlanApiDesactivado = "plan_api.desactivado";

    public const string ClaveRevocadaPorProveedor = "clave.revocada_por_proveedor";
    public const string ClaveRotada = "clave.rotada";
    public const string ClaveRevocadaPorConsumidor = "clave.revocada_por_consumidor";

    public const string CasoAbierto = "caso.abierto";
    public const string CasoCerrado = "caso.cerrado";

    /// <summary>La registra el sistema (actor <see cref="TipoActor.Sistema"/>).</summary>
    public const string SuscripcionSuspendida = "suscripcion.suspendida";

    /// <summary>Todas las acciones, en el orden de la tabla (por ejemplo, para el filtro de B3.2).</summary>
    public static IReadOnlyList<string> Todas { get; } =
    [
        OrganizacionSuspendida, OrganizacionReactivada,
        PagoRevertido,
        PlanPlataformaCreado, PlanPlataformaEditado, PlanPlataformaDesactivado,
        CuentaPlataformaCreada, CuentaPlataformaDesactivada, CuentaPlataformaActivada,
        SuscripcionPlataformaContratada, SuscripcionPlataformaCambiada,
        MiembroInvitado, MiembroRolCambiado, MiembroQuitado,
        ApiRegistrada, ApiPublicada, ApiDespublicada,
        RutaExpuesta, RutaOcultada,
        DominioConectado, DominioVerificado,
        SecretoOrigenRegenerado,
        PlanApiCreado, PlanApiEditado, PlanApiDesactivado,
        ClaveRevocadaPorProveedor, ClaveRotada, ClaveRevocadaPorConsumidor,
        CasoAbierto, CasoCerrado,
        SuscripcionSuspendida,
    ];
}

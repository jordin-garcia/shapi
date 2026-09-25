namespace Shapi.Contratos;

/// <summary>
/// Códigos de error (<c>codigo</c>) de la compuerta y de la API de control.
/// Son los de las especificaciones: nunca se escribe un código "a mano" fuera de esta clase.
/// </summary>
public static class CodigosError
{
    // Compuerta: contrato de errores (08 §4)
    public const string ApiNoEncontrada = "api_no_encontrada";
    public const string ClaveAusente = "clave_ausente";
    public const string ClaveInvalida = "clave_invalida";
    public const string ApiNoDisponible = "api_no_disponible";
    public const string SuscripcionInactiva = "suscripcion_inactiva";
    public const string RutaNoPermitida = "ruta_no_permitida";
    public const string CuerpoDemasiadoGrande = "cuerpo_demasiado_grande";
    public const string LimitePorMinuto = "limite_por_minuto";
    public const string CuotaAgotada = "cuota_agotada";
    public const string CuotaPlataformaAgotada = "cuota_plataforma_agotada";
    public const string OrigenInaccesible = "origen_inaccesible";
    public const string OrigenSinRespuesta = "origen_sin_respuesta";

    // Identidad y organizaciones (03 RF-02 y RF-06)
    public const string CorreoNoVerificado = "correo_no_verificado";
    public const string CorreoEnOtraOrganizacion = "correo_en_otra_organizacion";
    public const string CorreoYaRegistrado = "correo_ya_registrado";
    public const string CredencialesInvalidas = "credenciales_invalidas";
    public const string CuentaBloqueada = "cuenta_bloqueada";
    public const string CuentaDesactivada = "cuenta_desactivada";
    public const string TokenInvalido = "token_invalido";
    public const string ConsumidorExistente = "consumidor_existente";

    // Protección CSRF de la API de control (convenciones §5)
    public const string Csrf = "csrf";

    // Errores generales de la API de control (convenciones §5 y 10 §1)
    public const string DatosInvalidos = "datos_invalidos";
    public const string DemasiadasPeticiones = "demasiadas_peticiones";

    // Planes y suscripciones (09 §5 y §6)
    public const string LimiteDelPlan = "limite_del_plan";
    public const string ExcedeLimitesDelPlan = "excede_limites_del_plan";
    public const string PlanSinDominioPropio = "plan_sin_dominio_propio";
    public const string SuscripcionExistente = "suscripcion_existente";

    // Pagos y pasarela simulada (09 §2)
    public const string PagoRechazado = "pago_rechazado";
    public const string NumeroInvalido = "numero_invalido";
    public const string MarcaNoSoportada = "marca_no_soportada";
    public const string TarjetaVencida = "tarjeta_vencida";
    public const string CvvInvalido = "cvv_invalido";
    public const string FondosInsuficientes = "fondos_insuficientes";
    public const string PasarelaNoDisponible = "pasarela_no_disponible";

    // APIs y portal (RF-09, RNF-10)
    public const string OrigenNoPermitido = "origen_no_permitido";
    public const string SubdominioOcupado = "subdominio_ocupado";
    public const string EspecificacionInvalida = "especificacion_invalida";
    public const string PublicacionIncompleta = "publicacion_incompleta";
    public const string LogoInvalido = "logo_invalido";

    // Claves (rotación, 06 §5.4)
    public const string ClaveNoRotable = "clave_no_rotable";

    // Soporte
    public const string CasoCerrado = "caso_cerrado";
}

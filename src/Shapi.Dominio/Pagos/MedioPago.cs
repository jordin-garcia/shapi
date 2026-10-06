namespace Shapi.Dominio.Pagos;

public class MedioPago
{
    public Guid Id { get; private set; }
    public Guid? OrganizacionId { get; private set; }
    public Guid? ConsumidorId { get; private set; }
    public string TokenPasarela { get; private set; } = null!;
    public MarcaTarjeta Marca { get; private set; }
    public string Ultimos4 { get; private set; } = null!;
    public string Titular { get; private set; } = null!;
    public short MesVencimiento { get; private set; }
    public short AnioVencimiento { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }

    protected MedioPago() { }

    public static MedioPago CrearParaConsumidor(Guid consumidorId, string token, string marca, string ultimos4, string titular, int mes, int anio)
    {
        var tipoMarca = marca switch
        {
            "Visa" => MarcaTarjeta.Visa,
            "Mastercard" => MarcaTarjeta.Mastercard,
            "American Express" => MarcaTarjeta.AmericanExpress,
            _ => throw new ArgumentOutOfRangeException(nameof(marca)),
        };
        if (ultimos4.Length != 4 || mes is < 1 or > 12 || anio < 2000)
        {
            throw new ArgumentException("Los datos de la tarjeta tokenizada no son válidos.");
        }

        return new MedioPago
        {
            Id = Guid.CreateVersion7(),
            ConsumidorId = consumidorId,
            TokenPasarela = token,
            Marca = tipoMarca,
            Ultimos4 = ultimos4,
            Titular = titular,
            MesVencimiento = checked((short)mes),
            AnioVencimiento = checked((short)anio),
        };
    }

    public static MedioPago CrearParaOrganizacion(Guid organizacionId, string token, string marca, string ultimos4, string titular,
        int mes, int anio, DateTimeOffset ahora)
    {
        var medio = CrearParaConsumidor(organizacionId, token, marca, ultimos4, titular, mes, anio);
        medio.ConsumidorId = null;
        medio.OrganizacionId = organizacionId;
        return medio;
    }
}

namespace BacPaymentApi.Models;

public class DetalleTransCB
{
    public decimal NumeroOrigen { get; set; }
    public string CuentaOrigen { get; set; } = "";
    public decimal NumeroDestino { get; set; }
    public string CuentaDestino { get; set; } = "";
    public string Beneficiario { get; set; } = "";
    public string Moneda { get; set; } = "";
    public decimal TipoCambio { get; set; }
    public decimal MontoOrigen { get; set; }
    public decimal MontoDestino { get; set; }
}

/// <summary>
/// Configuración leída desde variables de entorno (ver docker-compose.yml / .env).
/// </summary>
public class BacOptions
{
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string TokenUrl { get; set; } = "";
    public string Scope { get; set; } = "tcd";
    public string PaymentUrl { get; set; } = "";
    public string TrackingUrl { get; set; } = "";
    public string Camt060Url { get; set; } = "";
    public string Camt942Url { get; set; } = "";
    public string CamtSaldoUrl { get; set; } = "";
    public string Camt053Url { get; set; } = "";
    public string IbmClientId { get; set; } = "";
    public string CuentaPagadora { get; set; } = "";
    public string Id1 { get; set; } = "";
    public string Id2 { get; set; } = "";
    public string Id3 { get; set; } = "";
    public string FrBicfi { get; set; } = "CORPFONTXXX";
    public string ToBicfi { get; set; } = "BSNJCRSJ";
}

public class SoftlandOptions
{
    public string ConnectionString { get; set; } = "";
}

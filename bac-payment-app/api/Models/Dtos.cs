namespace BacPaymentApi.Models;

public class TokenResponseDto
{
    public string Token { get; set; } = "";
}

public class ErrorResponseDto
{
    public string Mensaje { get; set; } = "";
    public string? Detalle { get; set; }
}

// Tipo de pago: refleja los checkBox1 (planilla) / checkBox2 (proveedores) del WinForm original
public enum TipoPago
{
    Planilla,
    Proveedores
}

public class SoftlandConsultaRequestDto
{
    public TipoPago TipoPago { get; set; }
}

public class BeneficiarioDto
{
    public string Cliente { get; set; } = "";
    public string CuentaDestino { get; set; } = "";
    public string CodigoBanco { get; set; } = "";
    public string Monto { get; set; } = "";
}

public class SoftlandConsultaResponseDto
{
    public string NombreTransaccion { get; set; } = "";
    public string CategoriaPago { get; set; } = "";
    public decimal MontoTotal { get; set; }
    public List<BeneficiarioDto> Beneficiarios { get; set; } = new();
}

public class PagoRequestDto
{
    public string Token { get; set; } = "";
    public string NombreTransaccion { get; set; } = "";
    public string CategoriaPago { get; set; } = "";
    public decimal MontoTotal { get; set; }
    public List<BeneficiarioDto> Beneficiarios { get; set; } = new();
}

public class PagoResponseDto
{
    public string EvtCd { get; set; } = "";
    public string EvtDesc { get; set; } = "";
}

public class RastreoRequestDto
{
    public string Token { get; set; } = "";
    public string NombreTransaccion { get; set; } = "";
}

public class RastreoItemDto
{
    public string Cliente { get; set; } = "";
    public string Moneda { get; set; } = "";
    public string Monto { get; set; } = "";
    public string Estado { get; set; } = "";
    public string TxSts { get; set; } = "";
}

public class RastreoResponseDto
{
    public bool EnProceso { get; set; }
    public string EvtCd { get; set; } = "";
    public string EvtDesc { get; set; } = "";
    public List<RastreoItemDto> Transacciones { get; set; } = new();
}

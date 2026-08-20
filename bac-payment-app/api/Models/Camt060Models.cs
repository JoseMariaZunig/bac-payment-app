using System.Text.Json.Serialization;

namespace BacPaymentApi.Models;

public class Camt060Request
{
	[JsonPropertyName("file")]
	public Camt060File? File { get; set; }
}

public class Camt060File
{
	public Camt060Envelope? Envelope { get; set; }
}

public class Camt060Envelope
{
	public AppHdr? AppHdr { get; set; }
	public Camt060Document? Document { get; set; }
}

public class Camt060Document
{
	[JsonPropertyName("xmlns")]
	public string? Xmlns { get; set; }
	public AcctRptgReq? AcctRptgReq { get; set; }
}

public class AcctRptgReq
{
	public Camt060GrpHdr? GrpHdr { get; set; }
	public RptgReq? RptgReq { get; set; }
}

public class Camt060GrpHdr
{
	public string? MsgId { get; set; }
	public string? CreDtTm { get; set; }
}

public class RptgReq
{
	public string? ReqdMsgNmId { get; set; }
	public Acct? Acct { get; set; }
	public AcctOwnr? AcctOwnr { get; set; }
	public RptgPrd? RptgPrd { get; set; }
	public RptgSeq? RptgSeq { get; set; }
}

public class Acct
{
	public AcctId? Id { get; set; }
}

public class AcctId
{
	public string? IBAN { get; set; }
}

public class AcctOwnr
{
	public AcctOwnrAgt? Agt { get; set; }
}

public class AcctOwnrAgt
{
	public FinInstnId? FinInstnId { get; set; }
}

public class RptgPrd
{
	public FrToDt? FrToDt { get; set; }
	public FrToTm? FrToTm { get; set; }
	public string? Tp { get; set; }
}
public class FrToTm
{
	public string? FrTm { get; set; }
	public string? ToTm { get; set; }
}
public class FrToDt
{
	public string? FrDt { get; set; }
	public string? ToDt { get; set; }
}

public class RptgSeq
{
	public string? EQSeq { get; set; }
}


public class Camt060RawResponse
{
	[JsonPropertyName("File")]
	public string? File { get; set; }
}


public class EstadoCuentaRequestDto
{
	public string FechaDesde { get; set; } = "";   
	public string FechaHasta { get; set; } = "";   
	public string Pagina { get; set; } = "1";      
}


public class EstadoCuentaConsultaRequestDto
{
	public string Token { get; set; } = "";
	public string FechaDesde { get; set; } = "";
	public string FechaHasta { get; set; } = "";
	public string Pagina { get; set; } = "1";
}

public class MovimientoDto
{
	public string Linea61 { get; set; } = "";
	public string Descripcion86 { get; set; } = "";
}

public class EstadoCuentaResponseDto
{
	public string Referencia { get; set; } = "";
	public string Cuenta { get; set; } = "";
	public string NumeroEstado { get; set; } = "";
	public string SaldoApertura { get; set; } = "";
	public string SaldoCierre { get; set; } = "";
	public string SaldoDisponibleCierre { get; set; } = "";
	public string SaldoDisponibleFuturo { get; set; } = "";
	public string InformacionAdicional { get; set; } = "";
	public List<MovimientoDto> Movimientos { get; set; } = new();
	public string RawMt940 { get; set; } = "";
}
// ---------------------------------------------------------------------------
// MT942 — Reporte de movimientos intradía
// ---------------------------------------------------------------------------

public class EstadoIntradiaConsultaRequestDto
{
	public string Token { get; set; } = "";
}

public class EstadoIntradiaResponseDto
{
	public string Referencia { get; set; } = "";           
	public string Cuenta { get; set; } = "";                
	public string NumeroEstado { get; set; } = "";           
	public string LimiteFloor { get; set; } = "";            
	public string FechaHoraIndicacion { get; set; } = "";    
	public List<MovimientoDto> Movimientos { get; set; } = new();
	public string TotalDebitos { get; set; } = "";           
	public string TotalCreditos { get; set; } = "";          
	public string RawMt942 { get; set; } = "";
}
public class SaldoConsultaRequestDto
{
    public string Token { get; set; } = "";
}

public class SaldoResponseDto
{
    public string Cuenta { get; set; } = "";
    public string Moneda { get; set; } = "";
    public string TipoSaldo { get; set; } = "";       // Bal.Tp.CdOrPrtry.Cd (ej. "ITAV")
    public string Indicador { get; set; } = "";        // Bal.CdtDbtInd (CRDT / DBIT)
    public string Fecha { get; set; } = "";             // Bal.Dt.DtTm
    public string Monto { get; set; } = "";
}

// --- Clases para deserializar la respuesta cruda de BAC ---

public class CamtSaldoRawResponse
{
    [JsonPropertyName("File")]
    public CamtSaldoFile? File { get; set; }
}

public class CamtSaldoFile
{
    public CamtSaldoEnvelope? Envelope { get; set; }
}

public class CamtSaldoEnvelope
{
    public CamtSaldoDocument? Document { get; set; }
}

public class CamtSaldoDocument
{
    public BkToCstmrAcctRpt? BkToCstmrAcctRpt { get; set; }
}

public class BkToCstmrAcctRpt
{
    public RptSaldo? Rpt { get; set; }
}

public class RptSaldo
{
    public AcctSaldo? Acct { get; set; }
    public BalSaldo? Bal { get; set; }
}

public class AcctSaldo
{
    public AcctSaldoId? Id { get; set; }
    public string? Ccy { get; set; }
}

public class AcctSaldoId
{
    public Othr? Othr { get; set; }
}

public class BalSaldo
{
    public BalTp? Tp { get; set; }
    public string? CdtDbtInd { get; set; }
    public BalDt? Dt { get; set; }
    public BalAmt? Amt { get; set; }
}

public class BalTp
{
    public CdOrPrtry? CdOrPrtry { get; set; }
}

public class CdOrPrtry
{
    public string? Cd { get; set; }
}

public class BalDt
{
    public string? DtTm { get; set; }
}

public class BalAmt
{
    public string? Ccy { get; set; }
    public string? Amt { get; set; }
}
public class Camt053ConsultaRequestDto
{
    public string Token { get; set; } = "";
    public string FechaDesde { get; set; } = "";
    public string FechaHasta { get; set; } = "";
    public string Pagina { get; set; } = "1";
}

public class Camt053MovimientoDto
{
    public string Fecha { get; set; } = "";
    public string Monto { get; set; } = "";
    public string Moneda { get; set; } = "";
    public string Indicador { get; set; } = "";   
    public string Descripcion { get; set; } = "";
    public string RawJson { get; set; } = "";      
}

public class Camt053ResponseDto
{
    public string IdEstado { get; set; } = "";
    public string Pagina { get; set; } = "";
    public bool UltimaPagina { get; set; }
    public string Cuenta { get; set; } = "";
    public string Moneda { get; set; } = "";
    public string SaldoApertura { get; set; } = "";
    public string SaldoAperturaIndicador { get; set; } = "";
    public string SaldoCierre { get; set; } = "";
    public string SaldoCierreIndicador { get; set; } = "";
    public List<Camt053MovimientoDto> Movimientos { get; set; } = new();
}
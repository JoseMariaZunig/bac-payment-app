using System.Text.Json.Serialization;

namespace BacPaymentApi.Models;



public class FinInstnId
{
    public string? BICFI { get; set; }

    public string? BIC { get; set; }

    [JsonPropertyName("PstlAdr")]
    public PstlAdr? PstlAdrs { get; set; }

    public ClrSysMmbId? ClrSysMmbId { get; set; }
}

public class PstlAdr
{
    public string? Ctry { get; set; }
}

public class FIId
{
    public FinInstnId? FinInstnId { get; set; }
}

public class Fr
{
    public FIId? FIId { get; set; }
}

public class To
{
    public FIId? FIId { get; set; }
}

public class AppHdr
{
    public Fr? Fr { get; set; }
    public To? To { get; set; }
    public string? BizMsgIdr { get; set; }
    public string? MsgDefIdr { get; set; }
    public string? BizSvc { get; set; }
    public string? CreDt { get; set; }
}

public class Othr
{
    public string? Id { get; set; }
}

public class OrgId
{
    public string? BICOrBEI { get; set; }
    public List<Othr>? Othr { get; set; }
}

public class Id
{
    public OrgId? OrgId { get; set; }
}

public class InitgPty
{
    public string? Nm { get; set; }
    public Id? Id { get; set; }
}

public class GrpHdr
{
    public string? MsgId { get; set; }
    public string? CreDtTm { get; set; }
    public int NbOfTxs { get; set; }
    public decimal CtrlSum { get; set; }
    public InitgPty? InitgPty { get; set; }
}

public class CtgyPurp
{
    public string? Cd { get; set; }
}

public class PmtTpInf
{
    public CtgyPurp? CtgyPurp { get; set; }
    public string? InstrPrty { get; set; }
}

public class Dbtr
{
    public string? Nm { get; set; }
}

public class AccountId
{
    public string? IBAN { get; set; }
    public Othr? Othr { get; set; }
}

public class DbtrAcct
{
    public AccountId? Id { get; set; }
}

public class DbtrAgt
{
    public FinInstnId? FinInstnId { get; set; }
}

public class PmtId
{
    public string? EndToEndId { get; set; }
}

public class InstdAmt
{
    [JsonPropertyName("-Ccy")]
    public string? Ccy { get; set; }

    [JsonPropertyName("InstdAmt")]
    public string? InstdAmts { get; set; }
}

public class Amt
{
    public InstdAmt? InstdAmt { get; set; }
}

public class ClrSysMmbId
{
    public string? MmbId { get; set; }
}

public class CdtrAgt
{
    public FinInstnId? FinInstnId { get; set; }
}

public class Cdtr
{
    public string? Nm { get; set; }

    [JsonPropertyName("PstlAdr")]
    public PstlAdr? PstlAdr { get; set; }

    public Id? Id { get; set; }
}

public class CdtrAcct
{
    public AccountId? Id { get; set; }
}

public class RfrdDocInf
{
    public string? Nb { get; set; }
}

public class Strd
{
    public List<RfrdDocInf>? RfrdDocInf { get; set; }
}

public class RmtInf
{
    public Strd? Strd { get; set; }
}

public class CdtTrfTxInf
{
    public PmtId? PmtId { get; set; }
    public PmtTpInf? PmtTpInf { get; set; }
    public Amt? Amt { get; set; }
    public CdtrAgt? CdtrAgt { get; set; }
    public Cdtr? Cdtr { get; set; }
    public CdtrAcct? CdtrAcct { get; set; }
    public RmtInf? RmtInf { get; set; }
}

public class PmtInf
{
    public string? PmtInfId { get; set; }
    public string? PmtMtd { get; set; }
    public int NbOfTxs { get; set; }
    public decimal CtrlSum { get; set; }
    public PmtTpInf? PmtTpInf { get; set; }
    public string? ReqdExctnDt { get; set; }
    public Dbtr? Dbtr { get; set; }
    public DbtrAcct? DbtrAcct { get; set; }
    public DbtrAgt? DbtrAgt { get; set; }
    public List<CdtTrfTxInf>? CdtTrfTxInf { get; set; }
}

public class CstmrCdtTrfInitn
{
    public GrpHdr? GrpHdr { get; set; }
    public List<PmtInf>? PmtInf { get; set; }
}

public class Document
{
    public CstmrCdtTrfInitn? CstmrCdtTrfInitn { get; set; }
}

public class Envelope
{
    public AppHdr? AppHdr { get; set; }
    public Document? Document { get; set; }
}

public class File
{
    public Envelope? Envelope { get; set; }
}

public class PaymentRequest
{
    [JsonPropertyName("file")]
    public File? File { get; set; }
}

// ---------------------------------------------------------------------------
// Modelos de rastreo (tracking)
// ---------------------------------------------------------------------------

public class ReqId
{
    public string? Id { get; set; }
    public string? CreDtTm { get; set; }
}

public class NttiesToBeRptd
{
    public string? BIC { get; set; }
}

public class StsRptReq
{
    public ReqId? ReqId { get; set; }
    public NttiesToBeRptd? NttiesToBeRptd { get; set; }
}

public class DocumentTracking
{
    [JsonPropertyName("xmlns")]
    public string? Xmlns { get; set; }
    public StsRptReq? StsRptReq { get; set; }
}

public class EnvelopeTracking
{
    public AppHdr? AppHdr { get; set; }
    public DocumentTracking? Document { get; set; }
}

public class FileTracking
{
    public EnvelopeTracking? Envelope { get; set; }
}

public class TrackingRequest
{
    [JsonPropertyName("file")]
    public FileTracking? File { get; set; }
}

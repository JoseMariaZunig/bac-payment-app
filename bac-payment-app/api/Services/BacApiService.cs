using System.Globalization;
using System.Text;
using System.Text.Json;
using BacPaymentApi.Models;
using Microsoft.Extensions.Options;

namespace BacPaymentApi.Services;

public class BacApiException : Exception
{
    public string? Detalle { get; }
    public BacApiException(string message, string? detalle = null) : base(message) => Detalle = detalle;
}

public class BacApiService
{
    private readonly HttpClient _httpClient;
    private readonly BacOptions _options;

    // HttpClient compartido y estático: patrón fire-and-forget seguro contra agotamiento de sockets.
    public BacApiService(HttpClient httpClient, IOptions<BacOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> ObtenerTokenAsync()
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));

        var formData = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "client_credentials"),
            new("scope", _options.Scope)
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenUrl)
        {
            Content = new FormUrlEncodedContent(formData)
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

        var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new BacApiException("No se pudo obtener el token de BAC", responseContent);

        using var document = JsonDocument.Parse(responseContent);
        return document.RootElement.GetProperty("access_token").GetString() ?? "";
    }

    public PaymentRequest ConstruirPaymentRequest(SoftlandConsultaResponseDto datos, BeneficiarioIds ids)
    {
        var beneficiarios = datos.Beneficiarios;
        if (beneficiarios.Count < 3)
            throw new InvalidOperationException("Se requieren al menos 3 beneficiarios para construir el pago.");

        var cdtTrfTxInf = new List<CdtTrfTxInf>();
        var idsList = new[] { ids.Id1, ids.Id2, ids.Id3 };

        for (int i = 0; i < 3; i++)
        {
            var b = beneficiarios[i];
            cdtTrfTxInf.Add(new CdtTrfTxInf
            {
                PmtId = new PmtId { EndToEndId = $"transaccion_{i + 1:D2}" },
                PmtTpInf = new PmtTpInf { InstrPrty = "NORM" },
                Amt = new Amt { InstdAmt = new InstdAmt { Ccy = "CRC", InstdAmts = b.Monto } },
                CdtrAgt = new CdtrAgt { FinInstnId = new FinInstnId { ClrSysMmbId = new ClrSysMmbId { MmbId = b.CodigoBanco } } },
                Cdtr = new Cdtr
                {
                    Nm = b.Cliente,
                    Id = new Id { OrgId = new OrgId { Othr = new List<Othr> { new() { Id = idsList[i] } } } }
                },
                CdtrAcct = new CdtrAcct { Id = new AccountId { Othr = new Othr { Id = b.CuentaDestino } } },
                RmtInf = new RmtInf
                {
                    Strd = new Strd
                    {
                        RfrdDocInf = new List<RfrdDocInf> { new() { Nb = "12345678901234567891234567890123456" } }
                    }
                }
            });
        }

        return new PaymentRequest
        {
            File = new BacPaymentApi.Models.File
            {
                Envelope = new Envelope
                {
                    AppHdr = new AppHdr
                    {
                        Fr = new Fr { FIId = new FIId { FinInstnId = new FinInstnId { BICFI = _options.FrBicfi } } },
                        To = new To { FIId = new FIId { FinInstnId = new FinInstnId { BICFI = _options.ToBicfi } } },
                        BizMsgIdr = datos.NombreTransaccion,
                        MsgDefIdr = "PaymentInitiationServiceV03",
                        BizSvc = "swift.cbprplus.01",
                        CreDt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")
                    },
                    Document = new Document
                    {
                        CstmrCdtTrfInitn = new CstmrCdtTrfInitn
                        {
                            GrpHdr = new GrpHdr
                            {
                                MsgId = datos.NombreTransaccion,
                                CreDtTm = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                                NbOfTxs = 3,
                                CtrlSum = datos.MontoTotal,
                                InitgPty = new InitgPty
                                {
                                    Nm = "BTIS",
                                    Id = new Id { OrgId = new OrgId { BICOrBEI = _options.FrBicfi } }
                                }
                            },
                            PmtInf = new List<PmtInf>
                            {
                                new()
                                {
                                    PmtInfId = datos.NombreTransaccion,
                                    PmtMtd = "TRF",
                                    NbOfTxs = 3,
                                    CtrlSum = datos.MontoTotal,
                                    PmtTpInf = new PmtTpInf { CtgyPurp = new CtgyPurp { Cd = datos.CategoriaPago } },
                                    ReqdExctnDt = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"),
                                    Dbtr = new Dbtr { Nm = "BTIS" },
                                    DbtrAcct = new DbtrAcct { Id = new AccountId { IBAN = _options.CuentaPagadora } },
                                    DbtrAgt = new DbtrAgt
                                    {
                                        FinInstnId = new FinInstnId { BICFI = _options.ToBicfi, PstlAdrs = new PstlAdr { Ctry = "CR" } }
                                    },
                                    CdtTrfTxInf = cdtTrfTxInf
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    public async Task<(string evtCd, string evtDesc)> ProcesarPagoAsync(string token, PaymentRequest paymentRequest)
    {
        var jsonContent = JsonSerializer.Serialize(paymentRequest, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.PaymentUrl)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-IBM-Client-Id", _options.IbmClientId);
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new BacApiException("Error procesando el pago en BAC", responseBody);

        using var doc = JsonDocument.Parse(responseBody);
        var evtInf = doc.RootElement
            .GetProperty("File").GetProperty("Envelope").GetProperty("Document")
            .GetProperty("SysEvtNtfctn").GetProperty("EvtInf");

        return (evtInf.GetProperty("EvtCd").GetString() ?? "", evtInf.GetProperty("EvtDesc").GetString() ?? "");
    }

    public async Task<RastreoResponseDto> RastrearPagoAsync(string token, string nombreTransaccion)
    {
        var trackingRequest = new TrackingRequest
        {
            File = new FileTracking
            {
                Envelope = new EnvelopeTracking
                {
                    AppHdr = new AppHdr
                    {
                        Fr = new Fr { FIId = new FIId { FinInstnId = new FinInstnId { BICFI = _options.FrBicfi } } },
                        To = new To { FIId = new FIId { FinInstnId = new FinInstnId { BICFI = _options.ToBicfi } } },
                        BizMsgIdr = "RastreoFont",
                        MsgDefIdr = "TSMT.038.001.03",
                        BizSvc = "swift.cbprplus.01",
                        CreDt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")
                    },
                    Document = new DocumentTracking
                    {
                        Xmlns = "urn:iso:std:iso:20022:tech:xsd:tsmt.038.001.03",
                        StsRptReq = new StsRptReq
                        {
                            ReqId = new ReqId { Id = nombreTransaccion, CreDtTm = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") },
                            NttiesToBeRptd = new NttiesToBeRptd { BIC = _options.FrBicfi }
                        }
                    }
                }
            }
        };

        var jsonContent = JsonSerializer.Serialize(trackingRequest, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.TrackingUrl)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-IBM-Client-Id", _options.IbmClientId);
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new BacApiException("Error consultando el rastreo en BAC", responseBody);

        using var doc = JsonDocument.Parse(responseBody);
        var document = doc.RootElement.GetProperty("File").GetProperty("Envelope").GetProperty("Document");

        var resultado = new RastreoResponseDto();

        // Caso 1: el pago sigue en proceso — BAC devuelve una notificación de evento genérica,
        // no el detalle por transacción todavía.
        if (document.TryGetProperty("SysEvtNtfctn", out var evtNtfctn))
        {
            var evtInf = evtNtfctn.GetProperty("EvtInf");
            resultado.EnProceso = true;
            resultado.EvtCd = evtInf.TryGetProperty("EvtCd", out var evtCd) ? evtCd.GetString() ?? "" : "";
            resultado.EvtDesc = evtInf.TryGetProperty("EvtDesc", out var evtDesc) ? evtDesc.GetString() ?? "" : "";
            return resultado;
        }

        // Caso 2: el pago ya tiene resolución final — detalle por transacción.
        if (document.TryGetProperty("CstmrPmtStsRpt", out var stsRpt))
        {
            var txList = stsRpt.GetProperty("OrgnlPmtInfAndSts").GetProperty("TxInfAndSts");

            foreach (var tx in txList.EnumerateArray())
            {
                var origTxRef = tx.GetProperty("OrgnlTxRef");
                var instdAmt = origTxRef.GetProperty("Amt").GetProperty("InstdAmt");

                resultado.Transacciones.Add(new RastreoItemDto
                {
                    Cliente = origTxRef.GetProperty("Cdtr").GetProperty("Nm").GetString() ?? "",
                    Moneda = instdAmt.TryGetProperty("-Ccy", out var ccy) ? ccy.GetString() ?? "" : "",
                    Monto = instdAmt.TryGetProperty("InstdAmt", out var monto) ? monto.GetString() ?? "" : "",
                    Estado = tx.TryGetProperty("StsRsnInf", out var rsnInf) && rsnInf.TryGetProperty("AddtlInf", out var addtl)
                        ? addtl.GetString() ?? "" : "",
                    TxSts = tx.TryGetProperty("TxSts", out var txSts) ? txSts.GetString() ?? "" : ""
                });
            }

            return resultado;
        }

        throw new BacApiException("Respuesta de rastreo con formato inesperado", responseBody);
    }
}

public record BeneficiarioIds(string Id1, string Id2, string Id3);

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BacPaymentApi.Models;
using Microsoft.Extensions.Options;

namespace BacPaymentApi.Services;

public class CamtService
{
	private readonly HttpClient _httpClient;
	private readonly BacOptions _options;
    private static readonly TimeZoneInfo CostaRicaTz =
       TimeZoneInfo.CreateCustomTimeZone("CostaRica", TimeSpan.FromHours(-6), "Costa Rica", "Costa Rica");

    private static DateTimeOffset AhoraCostaRica() =>
        TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, CostaRicaTz);

    public CamtService(HttpClient httpClient, IOptions<BacOptions> options)
	{
		_httpClient = httpClient;
		_options = options.Value;
	}

	// -------------------------------------------------------------------
	// MT940 — Estado de cuenta (día específico, elegido por el usuario)
	// -------------------------------------------------------------------
	public async Task<EstadoCuentaResponseDto> ConsultarEstadoCuentaAsync(string token, EstadoCuentaRequestDto request)
	{
		var camtRequest = new Camt060Request
		{
			File = new Camt060File
			{
				Envelope = new Camt060Envelope
				{
					AppHdr = ConstruirAppHdr(),
					Document = new Camt060Document
					{
						Xmlns = "urn:iso:std:iso:20022:tech:xsd:camt.060.001.05",
						AcctRptgReq = new AcctRptgReq
						{
							GrpHdr = ConstruirGrpHdr(),
							RptgReq = new RptgReq
							{
								ReqdMsgNmId = "MT940",
								Acct = new Acct { Id = new AcctId { IBAN = _options.CuentaPagadora } },
								AcctOwnr = ConstruirAcctOwnr(),
								RptgPrd = new RptgPrd
								{
									FrToDt = new FrToDt { FrDt = request.FechaDesde, ToDt = request.FechaHasta },
									Tp = "ALLL"
								},
								RptgSeq = new RptgSeq { EQSeq = request.Pagina }
							}
						}
					}
				}
			}
		};

		var base64File = await EnviarYObtenerFileAsync(token, _options.Camt060Url, camtRequest);
		var resultado = new EstadoCuentaResponseDto();
		var (swiftText, campos) = DecodificarYSepararTags(base64File);
		resultado.RawMt940 = swiftText;

		string? pendienteTag61 = null;
		foreach (var (tag, valor) in campos)
		{
			switch (tag)
			{
				case "20": resultado.Referencia = valor; break;
				case "25": resultado.Cuenta = valor; break;
				case "28C": resultado.NumeroEstado = valor; break;
				case "60F": resultado.SaldoApertura = valor; break;
				case "62F": resultado.SaldoCierre = valor; break;
				case "64": resultado.SaldoDisponibleCierre = valor; break;
				case "65": resultado.SaldoDisponibleFuturo = valor; break;
				case "61": pendienteTag61 = valor; break;
				case "86":
					if (pendienteTag61 != null)
					{
						resultado.Movimientos.Add(new MovimientoDto { Linea61 = pendienteTag61, Descripcion86 = valor });
						pendienteTag61 = null;
					}
					else
					{
						resultado.InformacionAdicional = valor;
					}
					break;
			}
		}

		return resultado;
	}

	// -------------------------------------------------------------------
	// MT942 — Movimientos intradía (siempre el día de hoy, según BAC)
	// -------------------------------------------------------------------
	public async Task<EstadoIntradiaResponseDto> ConsultarEstadoIntradiaAsync(string token)
	{
        var hoy = AhoraCostaRica().ToString("yyyy-MM-dd");

        var camtRequest = new Camt060Request
		{
			File = new Camt060File
			{
				Envelope = new Camt060Envelope
				{
					AppHdr = ConstruirAppHdr(),
					Document = new Camt060Document
					{
						Xmlns = "urn:iso:std:iso:20022:tech:xsd:camt.060.001.05",
						AcctRptgReq = new AcctRptgReq
						{
							GrpHdr = ConstruirGrpHdr(),
							RptgReq = new RptgReq
							{
								ReqdMsgNmId = "MT942",
								Acct = new Acct { Id = new AcctId { IBAN = _options.CuentaPagadora } },
								AcctOwnr = ConstruirAcctOwnr(),
								RptgPrd = new RptgPrd
								{
									FrToDt = new FrToDt { FrDt = hoy, ToDt = hoy },
									FrToTm = new FrToTm { FrTm = "00:00:00.000", ToTm = "23:59:59.000" },
									Tp = "ALLL"
								},
								RptgSeq = new RptgSeq { EQSeq = "1" }
							}
						}
					}
				}
			}
		};

		var base64File = await EnviarYObtenerFileAsync(token, _options.Camt942Url, camtRequest);
		var resultado = new EstadoIntradiaResponseDto();
		var (swiftText, campos) = DecodificarYSepararTags(base64File);
		resultado.RawMt942 = swiftText;

		string? pendienteTag61 = null;
		foreach (var (tag, valor) in campos)
		{
			switch (tag)
			{
				case "20": resultado.Referencia = valor; break;
				case "25": resultado.Cuenta = valor; break;
				case "28C": resultado.NumeroEstado = valor; break;
				case "34F": resultado.LimiteFloor = valor; break;
				case "13D": resultado.FechaHoraIndicacion = valor; break;
				case "61": pendienteTag61 = valor; break;
				case "86":
					if (pendienteTag61 != null)
					{
						resultado.Movimientos.Add(new MovimientoDto { Linea61 = pendienteTag61, Descripcion86 = valor });
						pendienteTag61 = null;
					}
					break;
				case "90D": resultado.TotalDebitos = valor; break;
				case "90C": resultado.TotalCreditos = valor; break;
			}
		}

		return resultado;
	}

    
    public async Task<SaldoResponseDto> ConsultarSaldoAsync(string token)
    {
        var camtRequest = new Camt060Request
        {
            File = new Camt060File
            {
                Envelope = new Camt060Envelope
                {
                    AppHdr = ConstruirAppHdr(),
                    Document = new Camt060Document
                    {
                        Xmlns = "urn:iso:std:iso:20022:tech:xsd:camt.060.001.05",
                        AcctRptgReq = new AcctRptgReq
                        {
                            GrpHdr = ConstruirGrpHdr(),
                            RptgReq = new RptgReq
                            {
                                ReqdMsgNmId = "AccountBalanceReportV08",
                                Acct = new Acct { Id = new AcctId { IBAN = _options.CuentaPagadora } },
                                AcctOwnr = ConstruirAcctOwnr()
                            }
                        }
                    }
                }
            }
        };

        var jsonContent = JsonSerializer.Serialize(camtRequest, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.CamtSaldoUrl)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        httpRequest.Headers.Add("X-IBM-Client-Id", _options.IbmClientId);
        httpRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(httpRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new BacApiException("Error consultando el saldo en BAC", responseBody);

        var raw = JsonSerializer.Deserialize<CamtSaldoRawResponse>(responseBody);
        var rpt = raw?.File?.Envelope?.Document?.BkToCstmrAcctRpt?.Rpt;

        if (rpt?.Bal == null)
            throw new BacApiException("BAC no devolvió información de saldo", responseBody);

        return new SaldoResponseDto
        {
            Cuenta = rpt.Acct?.Id?.Othr?.Id ?? "",
            Moneda = rpt.Acct?.Ccy ?? "",
            TipoSaldo = rpt.Bal.Tp?.CdOrPrtry?.Cd ?? "",
            Indicador = rpt.Bal.CdtDbtInd ?? "",
            Fecha = rpt.Bal.Dt?.DtTm ?? "",
            Monto = rpt.Bal.Amt?.Amt ?? ""
        };
    }

    public async Task<Camt053ResponseDto> ConsultarCamt053Async(string token, Camt053ConsultaRequestDto request)
    {
        var camtRequest = new Camt060Request
        {
            File = new Camt060File
            {
                Envelope = new Camt060Envelope
                {
                    AppHdr = ConstruirAppHdr(),
                    Document = new Camt060Document
                    {
                        Xmlns = "urn:iso:std:iso:20022:tech:xsd:camt.060.001.05",
                        AcctRptgReq = new AcctRptgReq
                        {
                            GrpHdr = ConstruirGrpHdr(),
                            RptgReq = new RptgReq
                            {
                                ReqdMsgNmId = "camt.053.001.08",
                                Acct = new Acct { Id = new AcctId { IBAN = _options.CuentaPagadora } },
                                AcctOwnr = ConstruirAcctOwnr(),
                                RptgPrd = new RptgPrd
                                {
                                    FrToDt = new FrToDt { FrDt = request.FechaDesde, ToDt = request.FechaHasta },
                                    Tp = "ALLL"
                                },
                                RptgSeq = new RptgSeq { EQSeq = request.Pagina }
                            }
                        }
                    }
                }
            }
        };

        var jsonContent = JsonSerializer.Serialize(camtRequest, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        Console.WriteLine("=== REQUEST ENVIADO A camt.053 ===");
        Console.WriteLine(jsonContent);
        Console.WriteLine("====================================");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.Camt053Url)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        httpRequest.Headers.Add("X-IBM-Client-Id", _options.IbmClientId);
        httpRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(httpRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new BacApiException("Error consultando camt.053 en BAC", responseBody);

      

        using var doc = JsonDocument.Parse(responseBody);

        if (!doc.RootElement.TryGetProperty("File", out var fileElement))
            throw new BacApiException("BAC no devolvió el campo 'File'", responseBody);

        // Caso rechazo (misma forma que vimos con MT940/MT942/saldo)
        if (fileElement.TryGetProperty("Envelope", out var envRej) &&
            envRej.TryGetProperty("Document", out var docRej) &&
            docRej.TryGetProperty("admi.002.001.01", out var rechazo))
        {
            var mensaje = "BAC rechazó la solicitud";
            if (rechazo.TryGetProperty("Rsn", out var rsn) && rsn.TryGetProperty("RsnDesc", out var rsnDesc))
                mensaje = rsnDesc.GetString() ?? mensaje;
            throw new BacApiException(mensaje, responseBody);
        }

        var stmt = fileElement.GetProperty("Envelope").GetProperty("Document")
            .GetProperty("BkToCstmrStmt").GetProperty("Stmt");

        var resultado = new Camt053ResponseDto
        {
            IdEstado = stmt.TryGetProperty("Id", out var idEl) ? idEl.GetString() ?? "" : ""
        };

        if (stmt.TryGetProperty("StmtPgntn", out var pgntn))
        {
            resultado.Pagina = pgntn.TryGetProperty("PgNb", out var pg) ? pg.GetString() ?? "" : "";
            resultado.UltimaPagina = pgntn.TryGetProperty("LastPgInd", out var last)
                && string.Equals(last.GetString(), "true", StringComparison.OrdinalIgnoreCase);
        }

        if (stmt.TryGetProperty("Acct", out var acct))
        {
            if (acct.TryGetProperty("Id", out var acctId))
            {
                if (acctId.TryGetProperty("IBAN", out var iban)) resultado.Cuenta = iban.GetString() ?? "";
                else if (acctId.TryGetProperty("Othr", out var othr) && othr.TryGetProperty("Id", out var othrId))
                    resultado.Cuenta = othrId.GetString() ?? "";
            }
            if (acct.TryGetProperty("Ccy", out var ccy)) resultado.Moneda = ccy.GetString() ?? "";
        }

        if (stmt.TryGetProperty("Bal", out var balances) && balances.ValueKind == JsonValueKind.Array)
        {
            foreach (var bal in balances.EnumerateArray())
            {
                var codigo = bal.GetProperty("Tp").GetProperty("CdOrPrtry").GetProperty("Cd").GetString();
                var indicador = bal.TryGetProperty("CdtDbtInd", out var ind) ? ind.GetString() ?? "" : "";
                var monto = bal.TryGetProperty("Amt", out var amtEl) ? ExtraerValorFlexible(amtEl.GetProperty("Amt")) : "";

                if (codigo == "OPBD")
                {
                    resultado.SaldoApertura = monto;
                    resultado.SaldoAperturaIndicador = indicador;
                }
                else if (codigo == "CLBD")
                {
                    resultado.SaldoCierre = monto;
                    resultado.SaldoCierreIndicador = indicador;
                }
            }
        }

        if (stmt.TryGetProperty("Ntry", out var entries) && entries.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in entries.EnumerateArray())
            {
                var mov = new Camt053MovimientoDto { RawJson = entry.GetRawText() };

                try
                {
                    if (entry.TryGetProperty("Amt", out var amt))
                    {
                        if (amt.TryGetProperty("Ccy", out var ccy)) mov.Moneda = ccy.GetString() ?? "";
                        if (amt.TryGetProperty("Amt", out var val)) mov.Monto = ExtraerValorFlexible(val);
                    }
                    if (entry.TryGetProperty("AddtlNtryInf", out var addtl))
                        mov.Descripcion = addtl.GetString() ?? "";

                    // Respaldo: si NtryDtls.TxDtls trae RmtInf.Ustrd, lo usamos si no había AddtlNtryInf
                    if (string.IsNullOrEmpty(mov.Descripcion)
                        && entry.TryGetProperty("NtryDtls", out var dtls) && dtls.ValueKind == JsonValueKind.Array
                        && dtls.GetArrayLength() > 0)
                    {
                        var primerDetalle = dtls[0];
                        if (primerDetalle.TryGetProperty("TxDtls", out var tx))
                        {
                            if (tx.TryGetProperty("RmtInf", out var rmt) && rmt.TryGetProperty("Ustrd", out var ustrd))
                                mov.Descripcion = ustrd.GetString() ?? "";
                        }
                    }
                }
                catch
                {
                    // Estructura de este movimiento distinta a lo previsto; RawJson queda disponible para inspeccionar
                }

                resultado.Movimientos.Add(mov);
            }
        }

        return resultado;
    }

    private static string ExtraerValorFlexible(JsonElement el)
    {
        return el.ValueKind switch
        {
            JsonValueKind.Number => el.GetRawText(),
            JsonValueKind.String => el.GetString() ?? "",
            _ => el.GetRawText()
        };
    }

    private AppHdr ConstruirAppHdr()
    {
        var ahora = AhoraCostaRica();
        return new AppHdr
        {
            Fr = new Fr { FIId = new FIId { FinInstnId = new FinInstnId { BICFI = _options.FrBicfi } } },
            To = new To { FIId = new FIId { FinInstnId = new FinInstnId { BICFI = _options.ToBicfi } } },
            BizMsgIdr = $"idMensaje_{ahora:yyyyMMddHHmmssfff}",
            MsgDefIdr = "camt.060.001.05",
            BizSvc = "swift.cbprplus.01",
            CreDt = ahora.ToString("yyyy-MM-ddTHH:mm:sszzz")
        };
    }

    private Camt060GrpHdr ConstruirGrpHdr()
    {
        var ahora = AhoraCostaRica();
        return new Camt060GrpHdr
        {
            MsgId = $"msg_{ahora:yyyyMMddHHmmssfff}",
            CreDtTm = ahora.ToString("yyyy-MM-ddTHH:mm:sszzz")
        };
    }

    private AcctOwnr ConstruirAcctOwnr() => new AcctOwnr
	{
		Agt = new AcctOwnrAgt { FinInstnId = new FinInstnId { BICFI = _options.FrBicfi } }
	};

	private async Task<string> EnviarYObtenerFileAsync(string token, string url, Camt060Request camtRequest)
	{
		var jsonContent = JsonSerializer.Serialize(camtRequest, new JsonSerializerOptions
		{
			DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
		});

		using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
		{
			Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
		};
		httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
		httpRequest.Headers.Add("X-IBM-Client-Id", _options.IbmClientId);
		httpRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

		var response = await _httpClient.SendAsync(httpRequest);
		var responseBody = await response.Content.ReadAsStringAsync();

		if (!response.IsSuccessStatusCode)
			throw new BacApiException("Error consultando BAC (camt.060)", responseBody);

		using var doc = JsonDocument.Parse(responseBody);

		if (!doc.RootElement.TryGetProperty("File", out var fileElement))
			throw new BacApiException("BAC no devolvió el campo 'File'", responseBody);

		// Caso normal: "File" es el string en Base64
		if (fileElement.ValueKind == JsonValueKind.String)
		{
			var fileStr = fileElement.GetString();
			if (string.IsNullOrWhiteSpace(fileStr))
				throw new BacApiException("BAC devolvió el campo 'File' vacío", responseBody);
			return fileStr;
		}

		// Caso rechazo: "File" viene como objeto con la razón del rechazo
		var mensaje = "BAC rechazó la solicitud";
		try
		{
			var rsnDesc = fileElement.GetProperty("Envelope").GetProperty("Document")
				.GetProperty("admi.002.001.01").GetProperty("Rsn").GetProperty("RsnDesc").GetString();
			if (!string.IsNullOrWhiteSpace(rsnDesc)) mensaje = rsnDesc;
		}
		catch
		{
			// Estructura de rechazo distinta a la esperada; usamos el mensaje genérico
		}

		throw new BacApiException(mensaje, responseBody);
	}

	/// <summary>
	/// Decodifica el Base64 (quitando el prefijo "F{" que antepone BAC) y separa
	/// el contenido del bloque {4: ... } en pares (tag, valor). Soporta que el
	/// bloque cierre con "-}" (MT940) o solo "}" (MT942).
	/// </summary>
	private (string swiftText, List<(string tag, string valor)> campos) DecodificarYSepararTags(string base64File)
	{
		var base64Limpio = base64File.StartsWith("F{") ? base64File.Substring(2) : base64File;
		var bytes = Convert.FromBase64String(base64Limpio);
		var swiftText = Encoding.UTF8.GetString(bytes);

		var idx = swiftText.IndexOf("{4:", StringComparison.Ordinal);
		var texto = idx >= 0 ? swiftText.Substring(idx + 3) : swiftText;
		texto = texto.TrimEnd();
		if (texto.EndsWith("-}")) texto = texto.Substring(0, texto.Length - 2);
		else if (texto.EndsWith("}")) texto = texto.Substring(0, texto.Length - 1);

		var matches = Regex.Matches(texto, @":(\d{2}[A-Z]?):([^:]*?)(?=(\r?\n:\d{2}[A-Z]?:)|$)", RegexOptions.Singleline);

		var campos = new List<(string, string)>();
		foreach (Match m in matches)
		{
			campos.Add((m.Groups[1].Value, m.Groups[2].Value.Trim()));
		}

		return (swiftText, campos);
	}
}
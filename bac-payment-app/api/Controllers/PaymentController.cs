using BacPaymentApi.Models;
using BacPaymentApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BacPaymentApi.Controllers;

[ApiController]
[Route("api/payment")]
public class PaymentController : ControllerBase
{
    private readonly BacApiService _bacApiService;
    private readonly BacOptions _bacOptions;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(BacApiService bacApiService, IOptions<BacOptions> bacOptions, ILogger<PaymentController> logger)
    {
        _bacApiService = bacApiService;
        _bacOptions = bacOptions.Value;
        _logger = logger;
    }

    [HttpPost("procesar")]
    public async Task<ActionResult<PagoResponseDto>> Procesar([FromBody] PagoRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new ErrorResponseDto { Mensaje = "Falta el token. Obtén uno primero." });

        try
        {
            var datos = new SoftlandConsultaResponseDto
            {
                NombreTransaccion = request.NombreTransaccion,
                CategoriaPago = request.CategoriaPago,
                MontoTotal = request.MontoTotal,
                Beneficiarios = request.Beneficiarios
            };

            var ids = new BeneficiarioIds(_bacOptions.Id1, _bacOptions.Id2, _bacOptions.Id3);
            var paymentRequest = _bacApiService.ConstruirPaymentRequest(datos, ids);
            var (evtCd, evtDesc) = await _bacApiService.ProcesarPagoAsync(request.Token, paymentRequest);

            return Ok(new PagoResponseDto { EvtCd = evtCd, EvtDesc = evtDesc });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponseDto { Mensaje = ex.Message });
        }
        catch (BacApiException ex)
        {
            _logger.LogError(ex, "Error procesando pago en BAC");
            return StatusCode(502, new ErrorResponseDto { Mensaje = ex.Message, Detalle = ex.Detalle });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado procesando pago");
            return StatusCode(500, new ErrorResponseDto { Mensaje = "Error procesando pago", Detalle = ex.Message });
        }
    }

    [HttpPost("rastrear")]
    public async Task<ActionResult<RastreoResponseDto>> Rastrear([FromBody] RastreoRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new ErrorResponseDto { Mensaje = "Falta el token. Obtén uno primero." });
        if (string.IsNullOrWhiteSpace(request.NombreTransaccion))
            return BadRequest(new ErrorResponseDto { Mensaje = "Falta el identificador de la transacción a rastrear." });

        try
        {
            var resultado = await _bacApiService.RastrearPagoAsync(request.Token, request.NombreTransaccion);
            return Ok(resultado);
        }
        catch (BacApiException ex)
        {
            _logger.LogError(ex, "Error rastreando pago en BAC");
            return StatusCode(502, new ErrorResponseDto { Mensaje = ex.Message, Detalle = ex.Detalle });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en rastreo");
            return StatusCode(500, new ErrorResponseDto { Mensaje = "Error en rastreo", Detalle = ex.Message });
        }
    }
}

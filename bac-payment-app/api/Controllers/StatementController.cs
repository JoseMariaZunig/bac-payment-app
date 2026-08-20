using BacPaymentApi.Models;
using BacPaymentApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BacPaymentApi.Controllers;

[ApiController]
[Route("api/statement")]
public class StatementController : ControllerBase
{
	private readonly CamtService _camtService;
	private readonly ILogger<StatementController> _logger;

	public StatementController(CamtService camtService, ILogger<StatementController> logger)
	{
		_camtService = camtService;
		_logger = logger;
	}

	
	[HttpPost("consultar")]
	public async Task<ActionResult<EstadoCuentaResponseDto>> Consultar([FromBody] EstadoCuentaConsultaRequestDto request)
	{
		if (string.IsNullOrWhiteSpace(request.Token))
			return BadRequest(new ErrorResponseDto { Mensaje = "Falta el token. Obtén uno primero." });

		if (string.IsNullOrWhiteSpace(request.FechaDesde) || string.IsNullOrWhiteSpace(request.FechaHasta))
			return BadRequest(new ErrorResponseDto { Mensaje = "Debes indicar FechaDesde y FechaHasta (formato yyyy-MM-dd)." });

		try
		{
			var dto = new EstadoCuentaRequestDto
			{
				FechaDesde = request.FechaDesde,
				FechaHasta = request.FechaHasta,
				Pagina = string.IsNullOrWhiteSpace(request.Pagina) ? "1" : request.Pagina
			};

			var resultado = await _camtService.ConsultarEstadoCuentaAsync(request.Token, dto);
			return Ok(resultado);
		}
		catch (BacApiException ex)
		{
			_logger.LogError(ex, "Error consultando estado de cuenta en BAC");
			return StatusCode(502, new ErrorResponseDto { Mensaje = ex.Message, Detalle = ex.Detalle });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error inesperado consultando estado de cuenta");
			return StatusCode(500, new ErrorResponseDto { Mensaje = "Error consultando estado de cuenta", Detalle = ex.Message });
		}
	}
	[HttpPost("intradia")]
	public async Task<ActionResult<EstadoIntradiaResponseDto>> Intradia([FromBody] EstadoIntradiaConsultaRequestDto request)
	{
		if (string.IsNullOrWhiteSpace(request.Token))
			return BadRequest(new ErrorResponseDto { Mensaje = "Falta el token. Obtén uno primero." });

		try
		{
			var resultado = await _camtService.ConsultarEstadoIntradiaAsync(request.Token);
			return Ok(resultado);
		}
		catch (BacApiException ex)
		{
			_logger.LogError(ex, "Error consultando movimientos intradía en BAC");
			return StatusCode(502, new ErrorResponseDto { Mensaje = ex.Message, Detalle = ex.Detalle });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error inesperado consultando movimientos intradía");
			return StatusCode(500, new ErrorResponseDto { Mensaje = "Error consultando movimientos intradía", Detalle = ex.Message });
		}
	}
    [HttpPost("saldo")]
    public async Task<ActionResult<SaldoResponseDto>> Saldo([FromBody] SaldoConsultaRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new ErrorResponseDto { Mensaje = "Falta el token. Obtén uno primero." });

        try
        {
            var resultado = await _camtService.ConsultarSaldoAsync(request.Token);
            return Ok(resultado);
        }
        catch (BacApiException ex)
        {
            _logger.LogError(ex, "Error consultando saldo en BAC");
            return StatusCode(502, new ErrorResponseDto { Mensaje = ex.Message, Detalle = ex.Detalle });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado consultando saldo");
            return StatusCode(500, new ErrorResponseDto { Mensaje = "Error consultando saldo", Detalle = ex.Message });
        }
    }
    [HttpPost("camt053")]
    public async Task<ActionResult<Camt053ResponseDto>> Camt053([FromBody] Camt053ConsultaRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new ErrorResponseDto { Mensaje = "Falta el token. Obtén uno primero." });

        if (string.IsNullOrWhiteSpace(request.FechaDesde) || string.IsNullOrWhiteSpace(request.FechaHasta))
            return BadRequest(new ErrorResponseDto { Mensaje = "Debes indicar FechaDesde y FechaHasta (formato yyyy-MM-dd)." });

        try
        {
            var resultado = await _camtService.ConsultarCamt053Async(request.Token, request);
            return Ok(resultado);
        }
        catch (BacApiException ex)
        {
            _logger.LogError(ex, "Error consultando camt.053 en BAC");
            return StatusCode(502, new ErrorResponseDto { Mensaje = ex.Message, Detalle = ex.Detalle });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado consultando camt.053");
            return StatusCode(500, new ErrorResponseDto { Mensaje = "Error consultando camt.053", Detalle = ex.Message });
        }
    }
}
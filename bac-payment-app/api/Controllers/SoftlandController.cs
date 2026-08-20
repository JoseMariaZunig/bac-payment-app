using BacPaymentApi.Models;
using BacPaymentApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BacPaymentApi.Controllers;

[ApiController]
[Route("api/softland")]
public class SoftlandController : ControllerBase
{
    private readonly SoftlandService _softlandService;
    private readonly ILogger<SoftlandController> _logger;

    public SoftlandController(SoftlandService softlandService, ILogger<SoftlandController> logger)
    {
        _softlandService = softlandService;
        _logger = logger;
    }

    [HttpPost("consultar")]
    public async Task<ActionResult<SoftlandConsultaResponseDto>> Consultar([FromBody] SoftlandConsultaRequestDto request)
    {
        try
        {
            var resultado = await _softlandService.ConsultarParaPagoAsync(request.TipoPago);
            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponseDto { Mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando Softland");
            return StatusCode(500, new ErrorResponseDto { Mensaje = "Error consultando Softland", Detalle = ex.Message });
        }
    }
}

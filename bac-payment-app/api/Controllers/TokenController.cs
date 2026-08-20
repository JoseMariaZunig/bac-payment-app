using BacPaymentApi.Models;
using BacPaymentApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BacPaymentApi.Controllers;

[ApiController]
[Route("api/token")]
public class TokenController : ControllerBase
{
    private readonly BacApiService _bacApiService;
    private readonly ILogger<TokenController> _logger;

    public TokenController(BacApiService bacApiService, ILogger<TokenController> logger)
    {
        _bacApiService = bacApiService;
        _logger = logger;
    }

    [HttpPost("obtain")]
    public async Task<ActionResult<TokenResponseDto>> Obtain()
    {
        try
        {
            var token = await _bacApiService.ObtenerTokenAsync();
            return Ok(new TokenResponseDto { Token = token });
        }
        catch (BacApiException ex)
        {
            _logger.LogError(ex, "Error obteniendo token de BAC");
            return StatusCode(502, new ErrorResponseDto { Mensaje = ex.Message, Detalle = ex.Detalle });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado obteniendo token");
            return StatusCode(500, new ErrorResponseDto { Mensaje = "Error obteniendo token", Detalle = ex.Message });
        }
    }
}

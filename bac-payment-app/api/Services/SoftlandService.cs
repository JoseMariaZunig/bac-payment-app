using System.Data;
using System.Globalization;
using BacPaymentApi.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace BacPaymentApi.Services;

public class SoftlandService
{
    private readonly string _connectionString;

    public SoftlandService(IOptions<SoftlandOptions> options)
    {
        _connectionString = options.Value.ConnectionString;
    }

    public async Task<List<decimal>> ObtenerNumerosOrigenAsync()
    {
        var numerosOrigen = new List<decimal>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("PRC_APIBAC_OBTENER_ENCABEZADO_TRANS_CB", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            numerosOrigen.Add(reader.GetDecimal(reader.GetOrdinal("numero_origen")));
        }

        return numerosOrigen;
    }

    public async Task<List<DetalleTransCB>> ObtenerDetalleTransCBAsync(decimal numeroOrigen)
    {
        var detalles = new List<DetalleTransCB>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("PRC_APIBAC_OBTENER_DETALLE_TRANS_CB", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.Add(new SqlParameter("@p_numero_origen", SqlDbType.Decimal) { Value = numeroOrigen });

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            detalles.Add(new DetalleTransCB
            {
                NumeroOrigen = reader.GetDecimal(reader.GetOrdinal("numero_origen")),
                CuentaOrigen = reader["cuenta_origen"].ToString() ?? "",
                NumeroDestino = reader.GetDecimal(reader.GetOrdinal("numero_destino")),
                CuentaDestino = reader["cuenta_destino"].ToString() ?? "",
                Beneficiario = reader["beneficiario"].ToString() ?? "",
                Moneda = reader["moneda"].ToString() ?? "",
                TipoCambio = reader.GetDecimal(reader.GetOrdinal("tipo_cambio")),
                MontoOrigen = reader.GetDecimal(reader.GetOrdinal("monto_origen")),
                MontoDestino = reader.GetDecimal(reader.GetOrdinal("monto_destino")),
            });
        }

        return detalles;
    }
    public async Task<SoftlandConsultaResponseDto> ConsultarParaPagoAsync(TipoPago tipoPago)
    {
        var numerosOrigen = await ObtenerNumerosOrigenAsync();

        var detalleCompleto = new List<DetalleTransCB>();
        foreach (var numeroOrigen in numerosOrigen)
        {
            var detalles = await ObtenerDetalleTransCBAsync(numeroOrigen);
            detalleCompleto.AddRange(detalles);
        }

        if (detalleCompleto.Count < 3)
        {
            throw new InvalidOperationException(
                $"Se esperaban al menos 3 registros de detalle y se obtuvieron {detalleCompleto.Count}.");
        }

        // Códigos de banco y categoría de pago según el tipo (checkBox1/checkBox2 en el WinForm original)
        string[] codigosBanco;
        string nombreTransaccion;
        string categoriaPago;

        if (tipoPago == TipoPago.Planilla)
        {
            codigosBanco = new[] { "102", "102", "102" };
            nombreTransaccion = $"PAGOPLANILLA_{DateTime.Now:yyyyMMddHHmmssfff}";
            categoriaPago = "SALA"; // Salarios
        }
        else
        {
            codigosBanco = new[] { "151", "102", "152" };
            nombreTransaccion = $"PAGOPROVEEDORES_{DateTime.Now:yyyyMMddHHmmssfff}";
            categoriaPago = "SUPP"; // Proveedores
        }

        var beneficiarios = new List<BeneficiarioDto>();
        decimal montoTotal = 0;

        for (int i = 0; i < 3; i++)
        {
            var d = detalleCompleto[i];
            montoTotal += d.MontoOrigen;

            beneficiarios.Add(new BeneficiarioDto
            {
                Cliente = d.Beneficiario,
                CuentaDestino = d.CuentaDestino,
                CodigoBanco = codigosBanco[i],
                Monto = d.MontoOrigen.ToString("0.00", CultureInfo.InvariantCulture)
            });
        }

        return new SoftlandConsultaResponseDto
        {
            NombreTransaccion = nombreTransaccion,
            CategoriaPago = categoriaPago,
            MontoTotal = montoTotal,
            Beneficiarios = beneficiarios
        };
    }
}

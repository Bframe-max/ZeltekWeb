using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Models;

namespace ZeltekWeb.Services.Interfaces
{
    public interface IVentaService
    {
        Task<VentaHistorialResumenDto> ObtenerHistorialAsync(string? buscar, DateTime? fechaInicio, DateTime? fechaFin, int tenantId);
        Task RegistrarVentaRapidaAsync(CreateVentaDto dto, int tenantId);
        Task<ProductoInfoDTO?> ObtenerProductoInfoAsync(int itemId, int tenantId);
        Task<IEnumerable<dynamic>> BuscarClientesAsync(string termino, int tenantId);
    }
}

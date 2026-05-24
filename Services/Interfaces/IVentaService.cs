using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Models;

namespace ZeltekWeb.Services.Interfaces
{
    public interface IVentaService
    {
        Task<VentaHistorialResumenDto> ObtenerHistorialAsync(string? buscar, DateTime? fechaInicio, DateTime? fechaFin);
        Task RegistrarVentaRapidaAsync(CreateVentaDto dto);
        Task<ProductoInfoDTO?> ObtenerProductoInfoAsync(int itemId);
        Task<IEnumerable<dynamic>> BuscarClientesAsync(string termino);
    }
}

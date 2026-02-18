using ZeltekWeb.Models;
using ZeltekWeb.Models.DTOs;
using System.Data;

namespace ZeltekWeb.Repositories
{
    public interface IVentaRepository
    {
        Task<IEnumerable<VentaListViewModel>> ObtenerHistorialAsync(string? buscar, DateTime? fechaInicio, DateTime? fechaFin, int tenantId);
        
        // Transactional Methods
        Task<int> CrearVentaAsync(CreateVentaDto dto, int clienteId, int tenantId, IDbTransaction transaction);
        Task CrearDetalleVentaAsync(int ventaId, int itemId, int productoId, decimal precio, int tenantId, IDbTransaction transaction);
        Task<bool> ActualizarStockItemAsync(int itemId, int cantidad, bool esUnico, int tenantId, IDbTransaction transaction);
        Task CrearGarantiaAsync(int ventaId, int productoId, int itemId, DateTime inicio, DateTime fin, int tenantId, IDbTransaction transaction);
        
        // Validation / Info Methods
        Task<(int ProductoId, bool EsUnico, int CantidadActual, decimal CostoTotal, string Nombre)> ObtenerInfoItemAsync(int itemId, int tenantId);

    }
}

using ZeltekWeb.Models;
using ZeltekWeb.Models.DTOs;
using System.Data;

namespace ZeltekWeb.Repositories
{
    public interface IVentaRepository
    {
        Task<IEnumerable<VentaListViewModel>> ObtenerHistorialAsync(string? buscar, DateTime? fechaInicio, DateTime? fechaFin);
        Task<int> CrearVentaAsync(CreateVentaDto dto, int clienteId, IDbTransaction transaction);
        Task CrearDetalleVentaAsync(int ventaId, int itemId, int productoId, decimal precio, IDbTransaction transaction);
        Task<bool> ActualizarStockItemAsync(int itemId, int cantidad, bool esUnico, IDbTransaction transaction);
        Task CrearGarantiaAsync(int ventaId, int productoId, int itemId, DateTime inicio, DateTime fin, IDbTransaction transaction);
        Task<(int ProductoId, bool EsUnico, int CantidadActual, decimal CostoTotal, string Nombre)> ObtenerInfoItemAsync(int itemId);
    }
}

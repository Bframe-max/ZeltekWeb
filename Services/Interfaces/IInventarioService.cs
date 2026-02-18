using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Models;

namespace ZeltekWeb.Services.Interfaces
{
    public interface IInventarioService
    {
        Task<(IEnumerable<InventarioItemDto> Items, int TotalLaptops, int TotalAccesorios)> ObtenerInventarioAsync(string? buscar, string filtro, int tenantId);
        Task<InventarioItemDto?> ObtenerPorIdAsync(int id, int tenantId);
        Task<ServiceResult> RecibirProductoAsync(RecepcionProductoDto dto, int tenantId);
        Task MarcarCompraRecibidaManualAsync(int compraId, bool omitirRevision, int tenantId);
        Task<IEnumerable<ItemPorRevisarViewModel>> ObtenerPorRevisarAsync(int tenantId);
        Task ProcesarInspeccionAsync(InspeccionCalidadDto dto, int tenantId);
        Task<IEnumerable<ItemEnReparacionViewModel>> ObtenerReparacionesActivasAsync(int tenantId);
        Task FinalizarReparacionAsync(FinalizarReparacionDto dto, int tenantId);
        Task RegistrarPerdidaReparacionAsync(int itemId, string motivo, int tenantId);
        Task<ServiceResult> ActualizarPrecioAsync(int itemId, decimal nuevoPrecio, int tenantId);
    }
}

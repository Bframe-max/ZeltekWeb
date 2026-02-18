using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Models;

namespace ZeltekWeb.Repositories.Interfaces
{
    public interface IInventarioRepository
    {
        Task<(IEnumerable<InventarioItemDto> Items, int TotalLaptops, int TotalAccesorios)> ObtenerInventarioAsync(string? buscar, string filtro, int tenantId);
        Task<InventarioItemDto?> ObtenerPorIdAsync(int id, int tenantId);
        Task RecibirProductoAsync(RecepcionProductoDto dto, int tenantId);
        Task<bool> ActualizarEstadoCompraSiCompletaAsync(int compraId, int tenantId);
        Task MarcarCompraRecibidaManualAsync(int compraId, bool omitirRevision, int tenantId);
        Task<IEnumerable<ItemPorRevisarViewModel>> ObtenerPorRevisarAsync(int tenantId);
        Task ProcesarInspeccionAsync(InspeccionCalidadDto dto, int tenantId);
        Task<IEnumerable<ItemEnReparacionViewModel>> ObtenerReparacionesActivasAsync(int tenantId);
        Task FinalizarReparacionAsync(FinalizarReparacionDto dto, int tenantId);
        Task RegistrarPerdidaReparacionAsync(int itemId, string motivo, int tenantId);
        Task<bool> ActualizarPrecioAsync(int itemId, decimal nuevoPrecio, int tenantId);
        
        // Helper validations
        Task<bool> ExisteItemEnCompraAsync(int itemId, int compraId, int tenantId);
    }
}

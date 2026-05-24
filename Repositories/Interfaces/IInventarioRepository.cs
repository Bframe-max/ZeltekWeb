using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Models;

namespace ZeltekWeb.Repositories.Interfaces
{
    public interface IInventarioRepository
    {
        Task<(IEnumerable<InventarioItemDto> Items, int TotalLaptops, int TotalAccesorios)> ObtenerInventarioAsync(string? buscar, string filtro);
        Task<InventarioItemDto?> ObtenerPorIdAsync(int id);
        Task RecibirProductoAsync(RecepcionProductoDto dto);
        Task<bool> ActualizarEstadoCompraSiCompletaAsync(int compraId);
        Task MarcarCompraRecibidaManualAsync(int compraId, bool omitirRevision);
        Task<IEnumerable<ItemPorRevisarViewModel>> ObtenerPorRevisarAsync();
        Task ProcesarInspeccionAsync(InspeccionCalidadDto dto);
        Task<IEnumerable<ItemEnReparacionViewModel>> ObtenerReparacionesActivasAsync();
        Task FinalizarReparacionAsync(FinalizarReparacionDto dto);
        Task RegistrarPerdidaReparacionAsync(int itemId, string motivo);
        Task<bool> ActualizarPrecioAsync(int itemId, decimal nuevoPrecio);
        Task<bool> ExisteItemEnCompraAsync(int itemId, int compraId);
    }
}

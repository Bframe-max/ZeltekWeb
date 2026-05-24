using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Models;

namespace ZeltekWeb.Services.Interfaces
{
    public interface IInventarioService
    {
        Task<(IEnumerable<InventarioItemDto> Items, int TotalLaptops, int TotalAccesorios)> ObtenerInventarioAsync(string? buscar, string filtro);
        Task<InventarioItemDto?> ObtenerPorIdAsync(int id);
        Task<ServiceResult> RecibirProductoAsync(RecepcionProductoDto dto);
        Task MarcarCompraRecibidaManualAsync(int compraId, bool omitirRevision);
        Task<IEnumerable<ItemPorRevisarViewModel>> ObtenerPorRevisarAsync();
        Task ProcesarInspeccionAsync(InspeccionCalidadDto dto);
        Task<IEnumerable<ItemEnReparacionViewModel>> ObtenerReparacionesActivasAsync();
        Task FinalizarReparacionAsync(FinalizarReparacionDto dto);
        Task RegistrarPerdidaReparacionAsync(int itemId, string motivo);
        Task<ServiceResult> ActualizarPrecioAsync(int itemId, decimal nuevoPrecio);
    }
}

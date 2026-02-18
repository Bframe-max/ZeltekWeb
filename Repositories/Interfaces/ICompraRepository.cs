using ZeltekWeb.Models.DTOs;
using System.Data;

namespace ZeltekWeb.Repositories
{
    public interface ICompraRepository
    {
        Task<(IEnumerable<CompraListDto> Items, int Activos, decimal InversionFlotante)> ObtenerHistorialAsync(string? buscar, string viewType, int tenantId);
        Task<CompraDto?> ObtenerPorIdAsync(int id, int tenantId);
        Task<IEnumerable<ItemCompraDto>> ObtenerItemsPorCompraAsync(int compraId, int tenantId);
        Task<bool> VerificarTrackingAsync(string tracking, int? excluirCompraId, int tenantId);
        Task<int> CrearCompraAsync(CreateCompraDto dto, int tenantId);
        Task ActualizarCompraAsync(UpdateCompraDto dto, int tenantId);
        Task EliminarCompraAsync(int id, int tenantId);
        Task ForzarCierreAsync(int id, int tenantId);
        
        // Item Operations
        Task<bool> VerificarSerialAsync(string serial, int? excluirItemId, int tenantId);
        Task RecibirItemAsync(RecibirItemDto dto, int tenantId);
        Task<(string Nombre, string Categoria)> GetItemInfoAsync(int itemId, int tenantId);
        Task ActualizarCostosItemsAsync(int compraId, decimal costoUnitario, int tenantId, IDbTransaction? transaction = null);
    }
}

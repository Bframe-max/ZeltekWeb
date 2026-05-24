using ZeltekWeb.Models.DTOs;
using System.Data;

namespace ZeltekWeb.Repositories
{
    public interface ICompraRepository
    {
        Task<(IEnumerable<CompraListDto> Items, int Activos, decimal InversionFlotante)> ObtenerHistorialAsync(string? buscar, string viewType);
        Task<CompraDto?> ObtenerPorIdAsync(int id);
        Task<IEnumerable<ItemCompraDto>> ObtenerItemsPorCompraAsync(int compraId);
        Task<bool> VerificarTrackingAsync(string tracking, int? excluirCompraId);
        Task<int> CrearCompraAsync(CreateCompraDto dto);
        Task ActualizarCompraAsync(UpdateCompraDto dto);
        Task EliminarCompraAsync(int id);
        Task ForzarCierreAsync(int id);
        Task<bool> VerificarSerialAsync(string serial, int? excluirItemId);
        Task RecibirItemAsync(RecibirItemDto dto);
        Task<(string Nombre, string Categoria)> GetItemInfoAsync(int itemId);
        Task ActualizarCostosItemsAsync(int compraId, decimal costoUnitario, IDbTransaction? transaction = null);
    }
}

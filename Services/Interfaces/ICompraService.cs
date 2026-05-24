using ZeltekWeb.Models.DTOs;

namespace ZeltekWeb.Services
{
    public interface ICompraService
    {
        Task<(IEnumerable<CompraListDto> Items, int Activos, decimal InversionFlotante)> ObtenerHistorialAsync(string? buscar, string viewType);
        Task<CompraDto?> ObtenerPorIdAsync(int id);
        Task<IEnumerable<ItemCompraDto>> ObtenerItemsPorCompraAsync(int compraId);
        Task<int> CrearCompraAsync(CreateCompraDto dto);
        Task ActualizarCompraAsync(UpdateCompraDto dto);
        Task EliminarCompraAsync(int id);
        Task ForzarCierreAsync(int id);
        Task<bool> VerificarTrackingAsync(string tracking, int? excluirCompraId);
        Task<bool> VerificarSerialAsync(string serial, int? excluirItemId);
        Task<RecibirItemResult> RecibirItemAsync(RecibirItemDto dto);
        Task<(bool EsAccesorio, string ProductoNombre)> GetItemInfoAsync(int itemId);
    }

    public class RecibirItemResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

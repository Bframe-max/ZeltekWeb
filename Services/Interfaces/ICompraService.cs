using ZeltekWeb.Models.DTOs;

namespace ZeltekWeb.Services
{
    public interface ICompraService
    {
        Task<(IEnumerable<CompraListDto> Items, int Activos, decimal InversionFlotante)> ObtenerHistorialAsync(string? buscar, string viewType, int tenantId);
        Task<CompraDto?> ObtenerPorIdAsync(int id, int tenantId);
        Task<IEnumerable<ItemCompraDto>> ObtenerItemsPorCompraAsync(int compraId, int tenantId);
        Task<int> CrearCompraAsync(CreateCompraDto dto, int tenantId);
        Task ActualizarCompraAsync(UpdateCompraDto dto, int tenantId);
        Task EliminarCompraAsync(int id, int tenantId);
        Task ForzarCierreAsync(int id, int tenantId);

        // Validations
        Task<bool> VerificarTrackingAsync(string tracking, int? excluirCompraId, int tenantId);
        Task<bool> VerificarSerialAsync(string serial, int? excluirItemId, int tenantId);
        
        // Complex Logic
        Task<RecibirItemResult> RecibirItemAsync(RecibirItemDto dto, int tenantId);
        Task<(bool EsAccesorio, string ProductoNombre)> GetItemInfoAsync(int itemId, int tenantId);
    }
    
    public class RecibirItemResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

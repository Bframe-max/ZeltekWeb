using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Services;

namespace ZeltekWeb.Services.Interfaces
{
    public interface IProductoService
    {
        Task<IEnumerable<ProductoDto>> ObtenerTodosAsync(int tenantId);
        Task<ProductoDto?> ObtenerPorIdAsync(int id, int tenantId);
        Task<ServiceResult> CrearAsync(CreateProductoDto dto, int tenantId);
        Task<ServiceResult> ActualizarAsync(UpdateProductoDto dto, int tenantId);
        Task EliminarAsync(int id, int tenantId);
        Task ReactivarAsync(int id, int tenantId);
        
        // Lookups
        Task<IEnumerable<MarcaDto>> ObtenerMarcasAsync(int tenantId);
        Task<IEnumerable<CategoriaDto>> ObtenerCategoriasAsync(int tenantId);
        Task<IEnumerable<ProductoDto>> ObtenerCargadoresYAccesoriosAsync(int tenantId);
        
        // Quick Create
        Task<ServiceResult> CrearMarcaRapidaAsync(string nombre, int tenantId);
        Task<ServiceResult> CrearCategoriaRapidaAsync(string nombre, int tenantId);
        
        // Helpers for Validations
        Task<bool> ExisteNombreAsync(string nombre, int? excludeId, int tenantId);
    }
}

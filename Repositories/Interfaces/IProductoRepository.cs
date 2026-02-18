using ZeltekWeb.Models.DTOs;

namespace ZeltekWeb.Repositories.Interfaces
{
    public interface IProductoRepository
    {
        Task<IEnumerable<ProductoDto>> ObtenerTodosAsync(int tenantId);
        Task<ProductoDto?> ObtenerPorIdAsync(int id, int tenantId);
        Task<int> CrearAsync(CreateProductoDto dto, int tenantId);
        Task ActualizarAsync(UpdateProductoDto dto, int tenantId);
        Task EliminarAsync(int id, int tenantId);
        Task ReactivarAsync(int id, int tenantId);
        
        // Lookups
        Task<IEnumerable<MarcaDto>> ObtenerMarcasAsync(int tenantId);
        Task<IEnumerable<CategoriaDto>> ObtenerCategoriasAsync(int tenantId);
        
        // Custom Lookups
        Task<IEnumerable<ProductoDto>> ObtenerCargadoresYAccesoriosAsync(int tenantId);
        
        // Validation / Helpers
        Task<bool> ExisteNombreAsync(string nombre, int? excludeId, int tenantId);
        Task<bool> ExisteMarcaAsync(string nombre, int tenantId);
        Task<bool> ExisteCategoriaAsync(string nombre, int tenantId);
        
        // Quick Create
        Task<int> CrearMarcaRapidaAsync(string nombre, int tenantId);
        Task<int> CrearCategoriaRapidaAsync(string nombre, int tenantId);
    }
}

using ZeltekWeb.Models.DTOs;

namespace ZeltekWeb.Repositories.Interfaces
{
    public interface IProductoRepository
    {
        Task<IEnumerable<ProductoDto>> ObtenerTodosAsync();
        Task<ProductoDto?> ObtenerPorIdAsync(int id);
        Task<int> CrearAsync(CreateProductoDto dto);
        Task ActualizarAsync(UpdateProductoDto dto);
        Task EliminarAsync(int id);
        Task ReactivarAsync(int id);
        Task<IEnumerable<MarcaDto>> ObtenerMarcasAsync();
        Task<IEnumerable<CategoriaDto>> ObtenerCategoriasAsync();
        Task<IEnumerable<ProductoDto>> ObtenerCargadoresYAccesoriosAsync();
        Task<bool> ExisteNombreAsync(string nombre, int? excludeId);
        Task<bool> ExisteMarcaAsync(string nombre);
        Task<bool> ExisteCategoriaAsync(string nombre);
        Task<int> CrearMarcaRapidaAsync(string nombre);
        Task<int> CrearCategoriaRapidaAsync(string nombre);
    }
}

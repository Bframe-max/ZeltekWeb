using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Services;

namespace ZeltekWeb.Services.Interfaces
{
    public interface IProductoService
    {
        Task<IEnumerable<ProductoDto>> ObtenerTodosAsync();
        Task<ProductoDto?> ObtenerPorIdAsync(int id);
        Task<ServiceResult> CrearAsync(CreateProductoDto dto);
        Task<ServiceResult> ActualizarAsync(UpdateProductoDto dto);
        Task EliminarAsync(int id);
        Task ReactivarAsync(int id);
        Task<IEnumerable<MarcaDto>> ObtenerMarcasAsync();
        Task<IEnumerable<CategoriaDto>> ObtenerCategoriasAsync();
        Task<IEnumerable<ProductoDto>> ObtenerCargadoresYAccesoriosAsync();
        Task<ServiceResult> CrearMarcaRapidaAsync(string nombre);
        Task<ServiceResult> CrearCategoriaRapidaAsync(string nombre);
        Task<bool> ExisteNombreAsync(string nombre, int? excludeId);
    }
}

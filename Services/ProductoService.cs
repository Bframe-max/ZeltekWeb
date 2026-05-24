using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Repositories.Interfaces;
using ZeltekWeb.Services.Interfaces;

namespace ZeltekWeb.Services
{
    public class ProductoService : IProductoService
    {
        private readonly IProductoRepository _productoRepo;

        public ProductoService(IProductoRepository productoRepo)
        {
            _productoRepo = productoRepo;
        }

        public async Task<IEnumerable<ProductoDto>> ObtenerTodosAsync()
            => await _productoRepo.ObtenerTodosAsync();

        public async Task<ProductoDto?> ObtenerPorIdAsync(int id)
            => await _productoRepo.ObtenerPorIdAsync(id);

        public async Task<ServiceResult> CrearAsync(CreateProductoDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return new ServiceResult { Success = false, Message = "El nombre es obligatorio." };
            if (await _productoRepo.ExisteNombreAsync(dto.Nombre, null))
                return new ServiceResult { Success = false, Message = "Ya existe un producto con este nombre." };
            try
            {
                int id = await _productoRepo.CrearAsync(dto);
                return new ServiceResult { Success = true, Data = id };
            }
            catch (Exception ex)
            {
                return new ServiceResult { Success = false, Message = "Error al crear producto: " + ex.Message };
            }
        }

        public async Task<ServiceResult> ActualizarAsync(UpdateProductoDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return new ServiceResult { Success = false, Message = "El nombre es obligatorio." };
            if (await _productoRepo.ExisteNombreAsync(dto.Nombre, dto.ProductoID))
                return new ServiceResult { Success = false, Message = "Ya existe otro producto con este nombre." };
            try
            {
                await _productoRepo.ActualizarAsync(dto);
                return new ServiceResult { Success = true };
            }
            catch (Exception ex)
            {
                return new ServiceResult { Success = false, Message = "Error al actualizar producto: " + ex.Message };
            }
        }

        public async Task EliminarAsync(int id) => await _productoRepo.EliminarAsync(id);
        public async Task ReactivarAsync(int id) => await _productoRepo.ReactivarAsync(id);
        public async Task<IEnumerable<MarcaDto>> ObtenerMarcasAsync() => await _productoRepo.ObtenerMarcasAsync();
        public async Task<IEnumerable<CategoriaDto>> ObtenerCategoriasAsync() => await _productoRepo.ObtenerCategoriasAsync();
        public async Task<IEnumerable<ProductoDto>> ObtenerCargadoresYAccesoriosAsync() => await _productoRepo.ObtenerCargadoresYAccesoriosAsync();

        public async Task<ServiceResult> CrearMarcaRapidaAsync(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return new ServiceResult { Success = false, Message = "Nombre requerido." };
            if (await _productoRepo.ExisteMarcaAsync(nombre))
                return new ServiceResult { Success = false, Message = "Esta marca ya existe." };
            var id = await _productoRepo.CrearMarcaRapidaAsync(nombre);
            return new ServiceResult { Success = true, Data = new { id, nombre } };
        }

        public async Task<ServiceResult> CrearCategoriaRapidaAsync(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return new ServiceResult { Success = false, Message = "Nombre requerido." };
            if (await _productoRepo.ExisteCategoriaAsync(nombre))
                return new ServiceResult { Success = false, Message = "Esta categoría ya existe." };
            var id = await _productoRepo.CrearCategoriaRapidaAsync(nombre);
            return new ServiceResult { Success = true, Data = new { id, nombre } };
        }

        public async Task<bool> ExisteNombreAsync(string nombre, int? excludeId)
            => await _productoRepo.ExisteNombreAsync(nombre, excludeId);
    }
}

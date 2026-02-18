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

        public async Task<IEnumerable<ProductoDto>> ObtenerTodosAsync(int tenantId)
        {
            return await _productoRepo.ObtenerTodosAsync(tenantId);
        }

        public async Task<ProductoDto?> ObtenerPorIdAsync(int id, int tenantId)
        {
            return await _productoRepo.ObtenerPorIdAsync(id, tenantId);
        }

        public async Task<ServiceResult> CrearAsync(CreateProductoDto dto, int tenantId)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return new ServiceResult { Success = false, Message = "El nombre es obligatorio." };

            if (await _productoRepo.ExisteNombreAsync(dto.Nombre, null, tenantId))
                return new ServiceResult { Success = false, Message = "Ya existe un producto con este nombre." };

            try
            {
                int id = await _productoRepo.CrearAsync(dto, tenantId);
                return new ServiceResult { Success = true, Data = id };
            }
            catch (Exception ex)
            {
                return new ServiceResult { Success = false, Message = "Error al crear producto: " + ex.Message };
            }
        }

        public async Task<ServiceResult> ActualizarAsync(UpdateProductoDto dto, int tenantId)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return new ServiceResult { Success = false, Message = "El nombre es obligatorio." };

            if (await _productoRepo.ExisteNombreAsync(dto.Nombre, dto.ProductoID, tenantId))
                return new ServiceResult { Success = false, Message = "Ya existe otro producto con este nombre." };

            try
            {
                await _productoRepo.ActualizarAsync(dto, tenantId);
                return new ServiceResult { Success = true };
            }
            catch (Exception ex)
            {
                return new ServiceResult { Success = false, Message = "Error al actualizar producto: " + ex.Message };
            }
        }

        public async Task EliminarAsync(int id, int tenantId)
        {
            await _productoRepo.EliminarAsync(id, tenantId);
        }

        public async Task ReactivarAsync(int id, int tenantId)
        {
            await _productoRepo.ReactivarAsync(id, tenantId);
        }

        public async Task<IEnumerable<MarcaDto>> ObtenerMarcasAsync(int tenantId)
        {
            return await _productoRepo.ObtenerMarcasAsync(tenantId);
        }

        public async Task<IEnumerable<CategoriaDto>> ObtenerCategoriasAsync(int tenantId)
        {
            return await _productoRepo.ObtenerCategoriasAsync(tenantId);
        }

        public async Task<IEnumerable<ProductoDto>> ObtenerCargadoresYAccesoriosAsync(int tenantId)
        {
            return await _productoRepo.ObtenerCargadoresYAccesoriosAsync(tenantId);
        }

        public async Task<ServiceResult> CrearMarcaRapidaAsync(string nombre, int tenantId)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return new ServiceResult { Success = false, Message = "Nombre requerido." };
            
            if (await _productoRepo.ExisteMarcaAsync(nombre, tenantId))
                return new ServiceResult { Success = false, Message = "Esta marca ya existe." };

            var id = await _productoRepo.CrearMarcaRapidaAsync(nombre, tenantId);
            return new ServiceResult { Success = true, Data = new { id, nombre } };
        }

        public async Task<ServiceResult> CrearCategoriaRapidaAsync(string nombre, int tenantId)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return new ServiceResult { Success = false, Message = "Nombre requerido." };
            
            if (await _productoRepo.ExisteCategoriaAsync(nombre, tenantId))
                return new ServiceResult { Success = false, Message = "Esta categoría ya existe." };

            var id = await _productoRepo.CrearCategoriaRapidaAsync(nombre, tenantId);
            return new ServiceResult { Success = true, Data = new { id, nombre } };
        }

        public async Task<bool> ExisteNombreAsync(string nombre, int? excludeId, int tenantId)
        {
            return await _productoRepo.ExisteNombreAsync(nombre, excludeId, tenantId);
        }
    }
}

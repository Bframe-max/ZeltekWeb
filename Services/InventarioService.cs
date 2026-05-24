using ZeltekWeb.Models;
using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Repositories.Interfaces;
using ZeltekWeb.Services.Interfaces;

namespace ZeltekWeb.Services
{
    public class InventarioService : IInventarioService
    {
        private readonly IInventarioRepository _inventarioRepo;

        public InventarioService(IInventarioRepository inventarioRepo)
        {
            _inventarioRepo = inventarioRepo;
        }

        public async Task<(IEnumerable<InventarioItemDto> Items, int TotalLaptops, int TotalAccesorios)> ObtenerInventarioAsync(string? buscar, string filtro)
            => await _inventarioRepo.ObtenerInventarioAsync(buscar, filtro);

        public async Task<InventarioItemDto?> ObtenerPorIdAsync(int id)
            => await _inventarioRepo.ObtenerPorIdAsync(id);

        public async Task<ServiceResult> RecibirProductoAsync(RecepcionProductoDto dto)
        {
            try
            {
                bool existsAndBelongs = await _inventarioRepo.ExisteItemEnCompraAsync(dto.ItemID, dto.CompraID);
                if (!existsAndBelongs)
                    return new ServiceResult { Success = false, Message = "El ítem no existe o no pertenece a esta compra." };

                dto.Usuario = dto.Usuario ?? "System";
                await _inventarioRepo.RecibirProductoAsync(dto);
                await _inventarioRepo.ActualizarEstadoCompraSiCompletaAsync(dto.CompraID);
                return new ServiceResult { Success = true, Message = "Producto recibido correctamente" };
            }
            catch (Exception ex)
            {
                return new ServiceResult { Success = false, Message = "Error interno: " + ex.Message };
            }
        }

        public async Task MarcarCompraRecibidaManualAsync(int compraId, bool omitirRevision)
            => await _inventarioRepo.MarcarCompraRecibidaManualAsync(compraId, omitirRevision);

        public async Task<IEnumerable<ItemPorRevisarViewModel>> ObtenerPorRevisarAsync()
            => await _inventarioRepo.ObtenerPorRevisarAsync();

        public async Task ProcesarInspeccionAsync(InspeccionCalidadDto dto)
            => await _inventarioRepo.ProcesarInspeccionAsync(dto);

        public async Task<IEnumerable<ItemEnReparacionViewModel>> ObtenerReparacionesActivasAsync()
            => await _inventarioRepo.ObtenerReparacionesActivasAsync();

        public async Task FinalizarReparacionAsync(FinalizarReparacionDto dto)
            => await _inventarioRepo.FinalizarReparacionAsync(dto);

        public async Task RegistrarPerdidaReparacionAsync(int itemId, string motivo)
            => await _inventarioRepo.RegistrarPerdidaReparacionAsync(itemId, motivo);

        public async Task<ServiceResult> ActualizarPrecioAsync(int itemId, decimal nuevoPrecio)
        {
            if (itemId <= 0 || nuevoPrecio < 0)
                return new ServiceResult { Success = false, Message = "Datos inválidos." };

            bool updated = await _inventarioRepo.ActualizarPrecioAsync(itemId, nuevoPrecio);
            return updated
                ? new ServiceResult { Success = true, Message = "Precio actualizado correctamente." }
                : new ServiceResult { Success = false, Message = "No se encontró el ítem." };
        }
    }
}

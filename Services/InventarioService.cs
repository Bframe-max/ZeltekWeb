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

        public async Task<(IEnumerable<InventarioItemDto> Items, int TotalLaptops, int TotalAccesorios)> ObtenerInventarioAsync(string? buscar, string filtro, int tenantId)
        {
            return await _inventarioRepo.ObtenerInventarioAsync(buscar, filtro, tenantId);
        }

        public async Task<InventarioItemDto?> ObtenerPorIdAsync(int id, int tenantId)
        {
            return await _inventarioRepo.ObtenerPorIdAsync(id, tenantId);
        }

        public async Task<ServiceResult> RecibirProductoAsync(RecepcionProductoDto dto, int tenantId)
        {
            try
            {
                // Validate ownership
                bool existsAndBelongs = await _inventarioRepo.ExisteItemEnCompraAsync(dto.ItemID, dto.CompraID, tenantId);
                if (!existsAndBelongs)
                {
                    return new ServiceResult { Success = false, Message = "El ítem no existe o no pertenece a esta compra." };
                }

                // Business Logic: Condition check?
                // The repository handles the update.
                
                dto.Usuario = dto.Usuario ?? "System"; // Ensure user
                await _inventarioRepo.RecibirProductoAsync(dto, tenantId);

                // Auto-update purchase status if all items received?
                // The repository method `RecibirProductoAsync` doesn't do it implicitly for simplicity, or does it?
                // In my implementation of `InventarioRepository.RecibirProductoAsync`, I removed the explicit "Update Compra" check I wrote in plan notes?
                // Let me check repo code I just wrote...
                // I see I didn't include `ActualizarEstadoCompraSiCompletaAsync` call inside `RecibirProductoAsync`.
                
                await _inventarioRepo.ActualizarEstadoCompraSiCompletaAsync(dto.CompraID, tenantId);

                return new ServiceResult { Success = true, Message = "Producto recibido correctamente" };
            }
            catch (Exception ex)
            {
                return new ServiceResult { Success = false, Message = "Error interno: " + ex.Message };
            }
        }

        public async Task MarcarCompraRecibidaManualAsync(int compraId, bool omitirRevision, int tenantId)
        {
            await _inventarioRepo.MarcarCompraRecibidaManualAsync(compraId, omitirRevision, tenantId);
        }

        public async Task<IEnumerable<ItemPorRevisarViewModel>> ObtenerPorRevisarAsync(int tenantId)
        {
            return await _inventarioRepo.ObtenerPorRevisarAsync(tenantId);
        }

        public async Task ProcesarInspeccionAsync(InspeccionCalidadDto dto, int tenantId)
        {
            await _inventarioRepo.ProcesarInspeccionAsync(dto, tenantId);
        }

        public async Task<IEnumerable<ItemEnReparacionViewModel>> ObtenerReparacionesActivasAsync(int tenantId)
        {
            return await _inventarioRepo.ObtenerReparacionesActivasAsync(tenantId);
        }

        public async Task FinalizarReparacionAsync(FinalizarReparacionDto dto, int tenantId)
        {
            await _inventarioRepo.FinalizarReparacionAsync(dto, tenantId);
        }

        public async Task RegistrarPerdidaReparacionAsync(int itemId, string motivo, int tenantId)
        {
            await _inventarioRepo.RegistrarPerdidaReparacionAsync(itemId, motivo, tenantId);
        }

        public async Task<ServiceResult> ActualizarPrecioAsync(int itemId, decimal nuevoPrecio, int tenantId)
        {
            if (itemId <= 0 || nuevoPrecio < 0)
                return new ServiceResult { Success = false, Message = "Datos inválidos." };

            bool updated = await _inventarioRepo.ActualizarPrecioAsync(itemId, nuevoPrecio, tenantId);
            if (updated)
                return new ServiceResult { Success = true, Message = "Precio actualizado correctamente." };
            else
                return new ServiceResult { Success = false, Message = "No se encontró el ítem." };
        }
    }
}

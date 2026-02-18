using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Repositories;

namespace ZeltekWeb.Services
{
    public class CompraService : ICompraService
    {
        private readonly ICompraRepository _compraRepo;

        public CompraService(ICompraRepository compraRepo)
        {
            _compraRepo = compraRepo;
        }

        public async Task<(IEnumerable<CompraListDto> Items, int Activos, decimal InversionFlotante)> ObtenerHistorialAsync(string? buscar, string viewType, int tenantId)
        {
            return await _compraRepo.ObtenerHistorialAsync(buscar, viewType, tenantId);
        }

        public async Task<CompraDto?> ObtenerPorIdAsync(int id, int tenantId)
        {
            return await _compraRepo.ObtenerPorIdAsync(id, tenantId);
        }

        public async Task<IEnumerable<ItemCompraDto>> ObtenerItemsPorCompraAsync(int compraId, int tenantId)
        {
            return await _compraRepo.ObtenerItemsPorCompraAsync(compraId, tenantId);
        }

        public async Task<int> CrearCompraAsync(CreateCompraDto dto, int tenantId)
        {
            // Optional: Business validation here
            return await _compraRepo.CrearCompraAsync(dto, tenantId);
        }

        public async Task ActualizarCompraAsync(UpdateCompraDto dto, int tenantId)
        {
            await _compraRepo.ActualizarCompraAsync(dto, tenantId);
        }

        public async Task EliminarCompraAsync(int id, int tenantId)
        {
            var compra = await _compraRepo.ObtenerPorIdAsync(id, tenantId);
            if (compra != null && (compra.EstadoCompra == "Completado" || compra.EstadoCompra == "Recibido" || compra.EstadoCompra == "Cancelado"))
            {
                throw new InvalidOperationException("No se puede eliminar una compra ya completada o cancelada.");
            }
            await _compraRepo.EliminarCompraAsync(id, tenantId);
        }

        public async Task ForzarCierreAsync(int id, int tenantId)
        {
            await _compraRepo.ForzarCierreAsync(id, tenantId);
        }

        public async Task<bool> VerificarTrackingAsync(string tracking, int? excluirCompraId, int tenantId)
        {
            if (string.IsNullOrWhiteSpace(tracking)) return false;
            return await _compraRepo.VerificarTrackingAsync(tracking, excluirCompraId, tenantId);
        }

        public async Task<bool> VerificarSerialAsync(string serial, int? excluirItemId, int tenantId)
        {
            if (string.IsNullOrWhiteSpace(serial)) return false;
            return await _compraRepo.VerificarSerialAsync(serial, excluirItemId, tenantId);
        }

        public async Task<RecibirItemResult> RecibirItemAsync(RecibirItemDto dto, int tenantId)
        {
             // 1. Get Item Info to determine Category (Accessory vs Device)
             var info = await _compraRepo.GetItemInfoAsync(dto.ItemID, tenantId);
             bool esAccesorio = info.Categoria.IndexOf("Cargador", StringComparison.OrdinalIgnoreCase) >= 0 
                             || info.Categoria.IndexOf("Accesorio", StringComparison.OrdinalIgnoreCase) >= 0;

             string serieFinal = (dto.SerieReal ?? "").Trim();

             if (esAccesorio)
             {
                 if (string.IsNullOrEmpty(serieFinal))
                     serieFinal = "ACC-" + dto.ItemID + "-" + DateTime.Now.ToString("HHmm");
                 
                 dto.Condicion = "Nuevo/Accesorio";
                 dto.Bateria = "N/A";
                 dto.Ram = null;
                 dto.Almacenamiento = null;
                 dto.Procesador = null;
             }
             else
             {
                 if (string.IsNullOrEmpty(serieFinal))
                     serieFinal = "SIN-SERIE-" + dto.ItemID;
                 else
                 {
                     // Validate Duplicate Serial
                     bool exists = await _compraRepo.VerificarSerialAsync(serieFinal, dto.ItemID, tenantId);
                     if (exists)
                     {
                         return new RecibirItemResult { Success = false, Message = "⛔ Serial ya existe en inventario." };
                     }
                 }

                 // Validation Logic
                 if (!string.IsNullOrWhiteSpace(dto.Bateria) && dto.Bateria.Trim() != "N/A")
                 {
                    var batStr = dto.Bateria.Trim().TrimEnd('%');
                    if (int.TryParse(batStr, out int pct) && (pct <= 0 || pct > 100))
                    {
                        return new RecibirItemResult { Success = false, Message = "Porcentaje de batería debe ser mayor a 0 y menor o igual a 100." };
                    }
                 }
                 if (dto.Almacenamiento.HasValue && dto.Almacenamiento.Value <= 0)
                 {
                     return new RecibirItemResult { Success = false, Message = "Almacenamiento debe ser mayor a 0." };
                 }
             }
             
             // Update DTO with final serial
             dto.SerieReal = serieFinal;
             
             await _compraRepo.RecibirItemAsync(dto, tenantId);
             
             return new RecibirItemResult { Success = true, Message = "Item recibido correctamente." };
        }

        public async Task<(bool EsAccesorio, string ProductoNombre)> GetItemInfoAsync(int itemId, int tenantId)
        {
            if (itemId <= 0) return (false, "");
            var info = await _compraRepo.GetItemInfoAsync(itemId, tenantId);
            bool esAccesorio = info.Categoria.IndexOf("Cargador", StringComparison.OrdinalIgnoreCase) >= 0 
                            || info.Categoria.IndexOf("Accesorio", StringComparison.OrdinalIgnoreCase) >= 0;
            return (esAccesorio, info.Nombre);
        }
    }
}

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

        public async Task<(IEnumerable<CompraListDto> Items, int Activos, decimal InversionFlotante)> ObtenerHistorialAsync(string? buscar, string viewType)
            => await _compraRepo.ObtenerHistorialAsync(buscar, viewType);

        public async Task<CompraDto?> ObtenerPorIdAsync(int id)
            => await _compraRepo.ObtenerPorIdAsync(id);

        public async Task<IEnumerable<ItemCompraDto>> ObtenerItemsPorCompraAsync(int compraId)
            => await _compraRepo.ObtenerItemsPorCompraAsync(compraId);

        public async Task<int> CrearCompraAsync(CreateCompraDto dto)
            => await _compraRepo.CrearCompraAsync(dto);

        public async Task ActualizarCompraAsync(UpdateCompraDto dto)
            => await _compraRepo.ActualizarCompraAsync(dto);

        public async Task EliminarCompraAsync(int id)
        {
            var compra = await _compraRepo.ObtenerPorIdAsync(id);
            if (compra != null && (compra.EstadoCompra == "Completado" || compra.EstadoCompra == "Recibido" || compra.EstadoCompra == "Cancelado"))
                throw new InvalidOperationException("No se puede eliminar una compra ya completada o cancelada.");
            await _compraRepo.EliminarCompraAsync(id);
        }

        public async Task ForzarCierreAsync(int id)
            => await _compraRepo.ForzarCierreAsync(id);

        public async Task<bool> VerificarTrackingAsync(string tracking, int? excluirCompraId)
        {
            if (string.IsNullOrWhiteSpace(tracking)) return false;
            return await _compraRepo.VerificarTrackingAsync(tracking, excluirCompraId);
        }

        public async Task<bool> VerificarSerialAsync(string serial, int? excluirItemId)
        {
            if (string.IsNullOrWhiteSpace(serial)) return false;
            return await _compraRepo.VerificarSerialAsync(serial, excluirItemId);
        }

        public async Task<RecibirItemResult> RecibirItemAsync(RecibirItemDto dto)
        {
            var info = await _compraRepo.GetItemInfoAsync(dto.ItemID);
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
                    bool exists = await _compraRepo.VerificarSerialAsync(serieFinal, dto.ItemID);
                    if (exists) return new RecibirItemResult { Success = false, Message = "⛔ Serial ya existe en inventario." };
                }

                if (!string.IsNullOrWhiteSpace(dto.Bateria) && dto.Bateria.Trim() != "N/A")
                {
                    var batStr = dto.Bateria.Trim().TrimEnd('%');
                    if (int.TryParse(batStr, out int pct) && (pct <= 0 || pct > 100))
                        return new RecibirItemResult { Success = false, Message = "Porcentaje de batería debe ser mayor a 0 y menor o igual a 100." };
                }
                if (dto.Almacenamiento.HasValue && dto.Almacenamiento.Value <= 0)
                    return new RecibirItemResult { Success = false, Message = "Almacenamiento debe ser mayor a 0." };
            }

            dto.SerieReal = serieFinal;
            await _compraRepo.RecibirItemAsync(dto);
            return new RecibirItemResult { Success = true, Message = "Item recibido correctamente." };
        }

        public async Task<(bool EsAccesorio, string ProductoNombre)> GetItemInfoAsync(int itemId)
        {
            if (itemId <= 0) return (false, "");
            var info = await _compraRepo.GetItemInfoAsync(itemId);
            bool esAccesorio = info.Categoria.IndexOf("Cargador", StringComparison.OrdinalIgnoreCase) >= 0
                            || info.Categoria.IndexOf("Accesorio", StringComparison.OrdinalIgnoreCase) >= 0;
            return (esAccesorio, info.Nombre);
        }
    }
}

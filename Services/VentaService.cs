using System.Data;
using System.Transactions;
using ZeltekWeb.Data;
using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Repositories;
using ZeltekWeb.Services.Interfaces;
using ZeltekWeb.Models; 

namespace ZeltekWeb.Services
{
    public class VentaService : IVentaService
    {
        private readonly IVentaRepository _ventaRepo;
        private readonly IClienteRepository _clienteRepo;
        private readonly IDbConnectionFactory _connectionFactory;

        public VentaService(IVentaRepository ventaRepo, IClienteRepository clienteRepo, IDbConnectionFactory connectionFactory)
        {
            _ventaRepo = ventaRepo;
            _clienteRepo = clienteRepo;
            _connectionFactory = connectionFactory;
        }

        public async Task<VentaHistorialResumenDto> ObtenerHistorialAsync(string? buscar, DateTime? fechaInicio, DateTime? fechaFin, int tenantId)
        {
            var ventas = (await _ventaRepo.ObtenerHistorialAsync(buscar, fechaInicio, fechaFin, tenantId)).ToList();

            return new VentaHistorialResumenDto
            {
                Ventas = ventas,
                TotalVendido = ventas.Sum(x => x.VentaTotal),
                TotalGanancia = ventas.Sum(x => x.Ganancia),
                TotalEquipos = ventas.Count
            };
        }

        public async Task<IEnumerable<dynamic>> BuscarClientesAsync(string termino, int tenantId)
        {
            return await _clienteRepo.BuscarClientesAsync(termino, tenantId);
        }

        public async Task<ProductoInfoDTO?> ObtenerProductoInfoAsync(int itemId, int tenantId)
        {
            var info = await _ventaRepo.ObtenerInfoItemAsync(itemId, tenantId);
            if (info.ProductoId == 0) return null;

            return new ProductoInfoDTO
            {
                CostoTotal = info.CostoTotal,
                Nombre = info.Nombre,
                PrecioSugerido = Math.Round(info.CostoTotal * 1.30m, 2)
            };
        }

        public async Task RegistrarVentaRapidaAsync(CreateVentaDto dto, int tenantId)
        {
             // 1. Validation Logic
            if (dto.PrecioFinal <= 0) throw new ArgumentException("El precio debe ser mayor a 0.");
            if (dto.FechaVenta.Date > DateTime.Now.Date) throw new ArgumentException("No puedes registrar ventas en el futuro.");
            
            // Allow older sales? Logic said -5 days.
            if (dto.FechaVenta.Date < DateTime.Now.Date.AddDays(-5)) throw new ArgumentException("No se permiten ventas con más de 5 días de antigüedad.");

            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 2. Client Logic
                int clienteId = 0;
                if (dto.ClienteIdExistente > 0)
                {
                    clienteId = dto.ClienteIdExistente;
                }
                else
                {
                    // Create minimal client
                    // Note: IClienteRepository now takes IDbTransaction? = null. 
                    // To include in this transaction, we pass it.
                    clienteId = await _clienteRepo.CrearClienteAsync(
                        dto.ClienteNombre, 
                        dto.ClienteCelular, 
                        dto.ClienteCedula, 
                        "", "", 
                        dto.TipoVenta, 
                        tenantId, 
                        transaction);
                }

                // 3. Item Info & Verification
                 var itemInfo = await _ventaRepo.ObtenerInfoItemAsync(dto.ItemId, tenantId);
                if (itemInfo.ProductoId == 0) throw new Exception("Ítem no encontrado.");

                int cantidad = dto.CantidadAVender;
                if (cantidad < 1) cantidad = 1;
                
                // If unique, force 1
                if (itemInfo.EsUnico) cantidad = 1;
                else if (cantidad > itemInfo.CantidadActual)
                {
                    throw new Exception($"Stock insuficiente. Disponible: {itemInfo.CantidadActual}, Solicitado: {cantidad}");
                }

                // 4. Create Venta Header
                // DTO probably just needs to carry the data. Total is calculated here or in Repo?
                // Repo logic uses dto.PrecioFinal * dto.CantidadAVender.
                // We trust Repo logic or update DTO if needed.
                int ventaId = await _ventaRepo.CrearVentaAsync(dto, clienteId, tenantId, transaction);

                // 5. Create Details
                // If Quantity > 1 (Non-unique items like accessories), we insert 1 detail line? or N lines?
                // Zeltek usually manages Unique Items (Phylosoraptor/Laptop). 
                // "Accesories" logic might be different.
                // Based on "CrearDetalleVentaAsync" taking "itemId", it implies linking specific ItemID.
                // If it's a bulk item, ItemID is the lot?
                // Let's assume 1 Detail Row for the bulk.
                // Wait, previous logic looped:
                // for (int i = 0; i < cantidad; i++) await _ventaRepo.CrearDetalleVentaAsync(...)
                // This implies creating N detail rows? 
                // If it's a bulk item, usually we create 1 detail row with Quantity.
                // But `CrearDetalleVentaAsync` implementation: INSERT INTO DetalleVenta.
                // Does DetalleVenta have Quantity? 
                // Previous check on `VentaRepository.cs`: 
                // INSERT INTO DetalleVenta (VentaID, ItemID, ProductoID, PrecioUnitario) VALUES ...
                // No Quantity column visible in that snippet. 
                // So it must be 1 row per unit, OR the snippet was simplified.
                // Reverting to loop logic to be safe/consistent with previous "VentaService.cs" view.
                
                for (int i = 0; i < cantidad; i++)
                {
                    await _ventaRepo.CrearDetalleVentaAsync(ventaId, dto.ItemId, itemInfo.ProductoId, dto.PrecioFinal, tenantId, transaction);
                }

                // 6. Update Stock
                bool stockUpdated = await _ventaRepo.ActualizarStockItemAsync(dto.ItemId, cantidad, itemInfo.EsUnico, tenantId, transaction);
                if (!stockUpdated) throw new Exception("El producto ya fue vendido o no hay stock suficiente.");

                // 7. Warranties
                if (dto.GarantiaMeses > 0)
                {
                     var inicio = dto.FechaVenta;
                     var fin = inicio.AddMonths(dto.GarantiaMeses);
                     await _ventaRepo.CrearGarantiaAsync(ventaId, itemInfo.ProductoId, dto.ItemId, inicio, fin, tenantId, transaction);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}

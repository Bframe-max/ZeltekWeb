using System.Data;
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

        public async Task<VentaHistorialResumenDto> ObtenerHistorialAsync(string? buscar, DateTime? fechaInicio, DateTime? fechaFin)
        {
            var ventas = (await _ventaRepo.ObtenerHistorialAsync(buscar, fechaInicio, fechaFin)).ToList();
            return new VentaHistorialResumenDto
            {
                Ventas = ventas,
                TotalVendido = ventas.Sum(x => x.VentaTotal),
                TotalGanancia = ventas.Sum(x => x.Ganancia),
                TotalEquipos = ventas.Count
            };
        }

        public async Task<IEnumerable<dynamic>> BuscarClientesAsync(string termino)
            => await _clienteRepo.BuscarClientesAsync(termino);

        public async Task<ProductoInfoDTO?> ObtenerProductoInfoAsync(int itemId)
        {
            var info = await _ventaRepo.ObtenerInfoItemAsync(itemId);
            if (info.ProductoId == 0) return null;
            return new ProductoInfoDTO
            {
                CostoTotal = info.CostoTotal,
                Nombre = info.Nombre,
                PrecioSugerido = Math.Round(info.CostoTotal * 1.30m, 2)
            };
        }

        public async Task RegistrarVentaRapidaAsync(CreateVentaDto dto)
        {
            if (dto.PrecioFinal <= 0) throw new ArgumentException("El precio debe ser mayor a 0.");
            if (dto.FechaVenta.Date > DateTime.Now.Date) throw new ArgumentException("No puedes registrar ventas en el futuro.");
            if (dto.FechaVenta.Date < DateTime.Now.Date.AddDays(-5)) throw new ArgumentException("No se permiten ventas con más de 5 días de antigüedad.");

            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                int clienteId = 0;
                if (dto.ClienteIdExistente > 0)
                {
                    clienteId = dto.ClienteIdExistente;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(dto.ClienteNombre))
                        throw new ArgumentException("El nombre del cliente es obligatorio.");

                    var telefono = (dto.ClienteCelular ?? "").Trim();
                    if (!System.Text.RegularExpressions.Regex.IsMatch(telefono, @"^[2578]\d{7}$"))
                        throw new ArgumentException("El teléfono debe tener 8 dígitos y comenzar con 2, 5, 7 u 8 (ej: 88123456).");

                    clienteId = await _clienteRepo.CrearClienteAsync(
                        dto.ClienteNombre, telefono, dto.ClienteCedula,
                        "", "", dto.TipoVenta, transaction);
                }

                var itemInfo = await _ventaRepo.ObtenerInfoItemAsync(dto.ItemId);
                if (itemInfo.ProductoId == 0) throw new Exception("Ítem no encontrado.");

                int cantidad = dto.CantidadAVender;
                if (cantidad < 1) cantidad = 1;
                if (itemInfo.EsUnico) cantidad = 1;
                else if (cantidad > itemInfo.CantidadActual)
                    throw new Exception($"Stock insuficiente. Disponible: {itemInfo.CantidadActual}, Solicitado: {cantidad}");

                int ventaId = await _ventaRepo.CrearVentaAsync(dto, clienteId, transaction);

                for (int i = 0; i < cantidad; i++)
                    await _ventaRepo.CrearDetalleVentaAsync(ventaId, dto.ItemId, itemInfo.ProductoId, dto.PrecioFinal, transaction);

                bool stockUpdated = await _ventaRepo.ActualizarStockItemAsync(dto.ItemId, cantidad, itemInfo.EsUnico, transaction);
                if (!stockUpdated) throw new Exception("El producto ya fue vendido o no hay stock suficiente.");

                if (dto.GarantiaMeses > 0)
                {
                    var inicio = dto.FechaVenta;
                    var fin = inicio.AddMonths(dto.GarantiaMeses);
                    await _ventaRepo.CrearGarantiaAsync(ventaId, itemInfo.ProductoId, dto.ItemId, inicio, fin, transaction);
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

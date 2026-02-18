using ZeltekWeb.Constants;
using ZeltekWeb.Models;
using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Repositories;
using ZeltekWeb.Services.Interfaces;

namespace ZeltekWeb.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _repository;

        public DashboardService(IDashboardRepository repository)
        {
            _repository = repository;
        }

        public async Task<DashboardViewModel> GetDashboardMetricsAsync(int tenantId)
        {
            var model = new DashboardViewModel();

            // 1. Ventas de Hoy
            model.VentasHoy = await _repository.GetVentasHoyAsync(tenantId);
            
            // 2. En Camino Metrics
            var excludedStatuses = new[] 
            { 
                PurchaseStatus.Completado, 
                PurchaseStatus.Recibida, 
                "Recibido", 
                PurchaseStatus.Cancelada, 
                "Cancelado"
            };
            var enCamino = await _repository.GetEnCaminoMetricsAsync(excludedStatuses, tenantId);
            model.EnCaminoCantidad = enCamino.Cantidad;
            model.EnCaminoMonto = enCamino.Monto;

            // 3. Por Revisar Count
            model.PorRevisarCantidad = await _repository.GetPorRevisarCountAsync(InventoryStatus.PorRevisar, tenantId); 

            // 4. Stock Metrics
            var stock = await _repository.GetStockMetricsAsync(InventoryStatus.EnStock, tenantId);
            model.StockCantidad = stock.Cantidad;
            model.StockValor = stock.Valor;

            // 5. Actividad Reciente
            var actividadDtos = await _repository.GetRecienteActividadAsync(5, tenantId);
            model.ActividadReciente = actividadDtos.Select(dto => new ActividadRecienteViewModel
            {
                Tipo = dto.Tipo,
                Descripcion = dto.Descripcion,
                Fecha = dto.Fecha,
                Monto = dto.Monto
            }).ToList();

            return model;
        }

        public async Task<BusquedaGlobalViewModel> BuscarGlobalAsync(string query, int tenantId)
        {
            var viewModel = new BusquedaGlobalViewModel { Query = query ?? "" };
            if (string.IsNullOrWhiteSpace(viewModel.Query)) return viewModel;

            var term = viewModel.Query.Trim();

            // Execute in parallel
            var taskVentas = _repository.BuscarEnVentasAsync(term, tenantId);
            var taskCompras = _repository.BuscarEnComprasAsync(term, tenantId);
            var taskInventario = _repository.BuscarEnInventarioAsync(term, tenantId);

            await Task.WhenAll(taskVentas, taskCompras, taskInventario);

            // Map DTOs to ViewModels
            viewModel.Ventas = (await taskVentas).Select(v => new ResultadoBusquedaVenta 
            { 
                VentaID = v.Id, 
                ClienteNombre = v.Header, 
                Fecha = v.Date ?? DateTime.MinValue, 
                Total = v.Amount ?? 0 
            }).ToList();

            viewModel.Compras = (await taskCompras).Select(c => new ResultadoBusquedaCompra
            {
                CompraID = c.Id,
                Tracking = c.Header,
                ProductoPrincipal = c.SubHeader,
                Fecha = c.Date ?? DateTime.MinValue,
                Total = c.Amount ?? 0
            }).ToList();

            viewModel.Inventario = (await taskInventario).Select(i => new ResultadoBusquedaInventario
            {
                ItemID = i.Id,
                ProductoNombre = i.Header,
                NumeroSerie = i.SubHeader,
                Estado = i.Status
            }).ToList();

            return viewModel;
        }
    }
}

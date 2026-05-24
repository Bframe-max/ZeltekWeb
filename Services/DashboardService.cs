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

        public async Task<DashboardViewModel> GetDashboardMetricsAsync()
        {
            var model = new DashboardViewModel();
            model.VentasHoy = await _repository.GetVentasHoyAsync();

            var excludedStatuses = new[] { PurchaseStatus.Completado, PurchaseStatus.Recibida, "Recibido", PurchaseStatus.Cancelada, "Cancelado" };
            var enCamino = await _repository.GetEnCaminoMetricsAsync(excludedStatuses);
            model.EnCaminoCantidad = enCamino.Cantidad;
            model.EnCaminoMonto = enCamino.Monto;

            model.PorRevisarCantidad = await _repository.GetPorRevisarCountAsync(InventoryStatus.PorRevisar);

            var stock = await _repository.GetStockMetricsAsync(InventoryStatus.EnStock);
            model.StockCantidad = stock.Cantidad;
            model.StockValor = stock.Valor;

            var actividadDtos = await _repository.GetRecienteActividadAsync(5);
            model.ActividadReciente = actividadDtos.Select(dto => new ActividadRecienteViewModel
            {
                Tipo = dto.Tipo,
                Descripcion = dto.Descripcion,
                Fecha = dto.Fecha,
                Monto = dto.Monto
            }).ToList();

            return model;
        }

        public async Task<BusquedaGlobalViewModel> BuscarGlobalAsync(string query)
        {
            var viewModel = new BusquedaGlobalViewModel { Query = query ?? "" };
            if (string.IsNullOrWhiteSpace(viewModel.Query)) return viewModel;

            var term = viewModel.Query.Trim();

            var taskVentas = _repository.BuscarEnVentasAsync(term);
            var taskCompras = _repository.BuscarEnComprasAsync(term);
            var taskInventario = _repository.BuscarEnInventarioAsync(term);
            await Task.WhenAll(taskVentas, taskCompras, taskInventario);

            viewModel.Ventas = (await taskVentas).Select(v => new ResultadoBusquedaVenta
            {
                VentaID = v.Id, ClienteNombre = v.Header, Fecha = v.Date ?? DateTime.MinValue, Total = v.Amount ?? 0
            }).ToList();

            viewModel.Compras = (await taskCompras).Select(c => new ResultadoBusquedaCompra
            {
                CompraID = c.Id, Tracking = c.Header, ProductoPrincipal = c.SubHeader, Fecha = c.Date ?? DateTime.MinValue, Total = c.Amount ?? 0
            }).ToList();

            viewModel.Inventario = (await taskInventario).Select(i => new ResultadoBusquedaInventario
            {
                ItemID = i.Id, ProductoNombre = i.Header, NumeroSerie = i.SubHeader, Estado = i.Status
            }).ToList();

            return viewModel;
        }
    }
}

using System.Linq;
using System.Threading.Tasks;
using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Repositories.Interfaces;
using ZeltekWeb.Services.Interfaces;

namespace ZeltekWeb.Services
{
    public class ReporteService : IReporteService
    {
        private readonly IReporteRepository _reporteRepo;

        public ReporteService(IReporteRepository reporteRepo)
        {
            _reporteRepo = reporteRepo;
        }

        public async Task<ReporteDashboardDto> ObtenerDashboardAsync(int mesesTendencia, int periodoGananciaMeses)
        {
            var dashboard = new ReporteDashboardDto
            {
                MesesTendencia = mesesTendencia,
                PeriodoGananciaMeses = periodoGananciaMeses
            };

            dashboard.Tendencia = (await _reporteRepo.ObtenerTendenciaVentasAsync(mesesTendencia)).ToList();
            dashboard.TopProductos = (await _reporteRepo.ObtenerTopProductosAsync()).ToList();
            dashboard.ResumenFinanciero = await _reporteRepo.ObtenerResumenFinancieroAsync(periodoGananciaMeses);

            decimal valorInventario = await _reporteRepo.ObtenerValorInventarioActualAsync();
            dashboard.ValorInventario = new ValorInventarioDto { TotalValor = valorInventario };

            dashboard.InventarioDormido = (await _reporteRepo.ObtenerInventarioDormidoAsync()).ToList();
            dashboard.TopRevendedores = (await _reporteRepo.ObtenerTopRevendedoresAsync()).ToList();

            return dashboard;
        }
    }
}

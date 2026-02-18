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

        public async Task<ReporteDashboardDto> ObtenerDashboardAsync(int mesesTendencia, int periodoGananciaMeses, int tenantId)
        {
            var dashboard = new ReporteDashboardDto
            {
                MesesTendencia = mesesTendencia,
                PeriodoGananciaMeses = periodoGananciaMeses
            };

            // Aggregating data.
            // If any of these fail (e.g. missing Gasto table), we might want to catch it individually if we want partial dashboard.
            // For now, let it bubble and Controller catches it.
            
            dashboard.Tendencia = (await _reporteRepo.ObtenerTendenciaVentasAsync(mesesTendencia, tenantId)).ToList();
            dashboard.TopProductos = (await _reporteRepo.ObtenerTopProductosAsync(tenantId)).ToList();
            dashboard.ResumenFinanciero = await _reporteRepo.ObtenerResumenFinancieroAsync(periodoGananciaMeses, tenantId);
            
            decimal valorInventario = await _reporteRepo.ObtenerValorInventarioActualAsync(tenantId);
            dashboard.ValorInventario = new ValorInventarioDto { TotalValor = valorInventario };
            
            dashboard.InventarioDormido = (await _reporteRepo.ObtenerInventarioDormidoAsync(tenantId)).ToList();
            dashboard.TopRevendedores = (await _reporteRepo.ObtenerTopRevendedoresAsync(tenantId)).ToList();

            return dashboard;
        }
    }
}

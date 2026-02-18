using System.Threading.Tasks;
using ZeltekWeb.Models.DTOs;

namespace ZeltekWeb.Services.Interfaces
{
    public interface IReporteService
    {
        Task<ReporteDashboardDto> ObtenerDashboardAsync(int mesesTendencia, int periodoGananciaMeses, int tenantId);
    }
}

using ZeltekWeb.Models;

namespace ZeltekWeb.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardMetricsAsync(int tenantId);
        Task<BusquedaGlobalViewModel> BuscarGlobalAsync(string query, int tenantId);
    }
}

using ZeltekWeb.Models;

namespace ZeltekWeb.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardMetricsAsync();
        Task<BusquedaGlobalViewModel> BuscarGlobalAsync(string query);
    }
}

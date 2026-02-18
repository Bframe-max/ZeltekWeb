using ZeltekWeb.Models.DTOs;

namespace ZeltekWeb.Repositories
{
    public interface IDashboardRepository
    {
        Task<decimal> GetVentasHoyAsync(int tenantId);
        Task<(int Cantidad, decimal Monto)> GetEnCaminoMetricsAsync(string[] excludedStatuses, int tenantId);
        Task<int> GetPorRevisarCountAsync(string status, int tenantId);
        Task<(int Cantidad, decimal Valor)> GetStockMetricsAsync(string status, int tenantId);
        Task<List<ActividadRecienteDto>> GetRecienteActividadAsync(int top, int tenantId);

        // Global Search
        Task<List<SearchResultDto>> BuscarEnVentasAsync(string query, int tenantId);
        Task<List<SearchResultDto>> BuscarEnComprasAsync(string query, int tenantId);
        Task<List<SearchResultDto>> BuscarEnInventarioAsync(string query, int tenantId);
    }
}

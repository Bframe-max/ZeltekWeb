using ZeltekWeb.Models.DTOs;

namespace ZeltekWeb.Repositories
{
    public interface IDashboardRepository
    {
        Task<decimal> GetVentasHoyAsync();
        Task<(int Cantidad, decimal Monto)> GetEnCaminoMetricsAsync(string[] excludedStatuses);
        Task<int> GetPorRevisarCountAsync(string status);
        Task<(int Cantidad, decimal Valor)> GetStockMetricsAsync(string status);
        Task<List<ActividadRecienteDto>> GetRecienteActividadAsync(int top);
        Task<List<SearchResultDto>> BuscarEnVentasAsync(string query);
        Task<List<SearchResultDto>> BuscarEnComprasAsync(string query);
        Task<List<SearchResultDto>> BuscarEnInventarioAsync(string query);
    }
}

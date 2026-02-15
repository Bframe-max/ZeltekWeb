using ZeltekWeb.Models;

namespace ZeltekWeb.Repositories
{
    public interface IVentaRepository
    {
        Task<IEnumerable<VentaListViewModel>> ObtenerHistorialAsync(string? buscar, DateTime? fechaInicio, DateTime? fechaFin);
    }
}

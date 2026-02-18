using System.Collections.Generic;
using System.Threading.Tasks;
using ZeltekWeb.Models.DTOs;

namespace ZeltekWeb.Repositories.Interfaces
{
    public interface IReporteRepository
    {
        Task<IEnumerable<TendenciaVentasDto>> ObtenerTendenciaVentasAsync(int meses, int tenantId);
        Task<IEnumerable<TopProductoDto>> ObtenerTopProductosAsync(int tenantId);
        Task<ResumenFinancieroDto> ObtenerResumenFinancieroAsync(int periodoMeses, int tenantId);
        Task<decimal> ObtenerValorInventarioActualAsync(int tenantId);
        Task<IEnumerable<InventarioDormidoDto>> ObtenerInventarioDormidoAsync(int tenantId);
        Task<IEnumerable<TopRevendedorDto>> ObtenerTopRevendedoresAsync(int tenantId);
    }
}

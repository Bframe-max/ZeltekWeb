using System.Collections.Generic;
using System.Threading.Tasks;
using ZeltekWeb.Models.DTOs;

namespace ZeltekWeb.Repositories.Interfaces
{
    public interface IReporteRepository
    {
        Task<IEnumerable<TendenciaVentasDto>> ObtenerTendenciaVentasAsync(int meses);
        Task<IEnumerable<TopProductoDto>> ObtenerTopProductosAsync();
        Task<ResumenFinancieroDto> ObtenerResumenFinancieroAsync(int periodoMeses);
        Task<decimal> ObtenerValorInventarioActualAsync();
        Task<IEnumerable<InventarioDormidoDto>> ObtenerInventarioDormidoAsync();
        Task<IEnumerable<TopRevendedorDto>> ObtenerTopRevendedoresAsync();
    }
}

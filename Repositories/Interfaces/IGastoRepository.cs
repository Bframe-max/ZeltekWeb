using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZeltekWeb.Models.DTOs;

namespace ZeltekWeb.Repositories.Interfaces
{
    public interface IGastoRepository
    {
        Task<IEnumerable<GastoListDto>> ObtenerGastosAsync(DateTime? desde, DateTime? hasta, int tenantId);
        Task<GastoResumenDto> ObtenerResumenAsync(DateTime? desde, DateTime? hasta, int tenantId);
        Task<IEnumerable<GastoPorCategoriaDto>> ObtenerGastosPorCategoriaAsync(DateTime? desde, DateTime? hasta, int tenantId);
        Task<GastoEditDto?> ObtenerPorIdAsync(int id, int tenantId);
        Task<int> CrearAsync(GastoCreateDto gasto, int tenantId);
        Task ActualizarAsync(GastoEditDto gasto, int tenantId);
        Task EliminarAsync(int id, int tenantId);
    }
}

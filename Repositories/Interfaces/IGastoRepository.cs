using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZeltekWeb.Models.DTOs;

namespace ZeltekWeb.Repositories.Interfaces
{
    public interface IGastoRepository
    {
        Task<IEnumerable<GastoListDto>> ObtenerGastosAsync(DateTime? desde, DateTime? hasta);
        Task<GastoResumenDto> ObtenerResumenAsync(DateTime? desde, DateTime? hasta);
        Task<IEnumerable<GastoPorCategoriaDto>> ObtenerGastosPorCategoriaAsync(DateTime? desde, DateTime? hasta);
        Task<GastoEditDto?> ObtenerPorIdAsync(int id);
        Task<int> CrearAsync(GastoCreateDto gasto);
        Task ActualizarAsync(GastoEditDto gasto);
        Task EliminarAsync(int id);
    }
}

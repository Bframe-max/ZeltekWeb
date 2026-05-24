using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZeltekWeb.Models.DTOs;

namespace ZeltekWeb.Services.Interfaces
{
    public interface IGastoService
    {
        Task<IEnumerable<GastoListDto>> ObtenerListadoAsync(DateTime? desde, DateTime? hasta);
        Task<GastoResumenDto> ObtenerResumenAsync(DateTime? desde, DateTime? hasta);
        Task<IEnumerable<GastoPorCategoriaDto>> ObtenerPorCategoriaAsync(DateTime? desde, DateTime? hasta);
        Task<GastoEditDto?> ObtenerPorIdAsync(int id);
        Task<int> CrearAsync(GastoCreateDto dto);
        Task ActualizarAsync(GastoEditDto dto);
        Task EliminarAsync(int id);
    }
}

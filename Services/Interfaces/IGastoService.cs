using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZeltekWeb.Models.DTOs;

namespace ZeltekWeb.Services.Interfaces
{
    public interface IGastoService
    {
        Task<IEnumerable<GastoListDto>> ObtenerListadoAsync(DateTime? desde, DateTime? hasta, int tenantId);
        Task<GastoResumenDto> ObtenerResumenAsync(DateTime? desde, DateTime? hasta, int tenantId);
        Task<IEnumerable<GastoPorCategoriaDto>> ObtenerPorCategoriaAsync(DateTime? desde, DateTime? hasta, int tenantId);
        Task<GastoEditDto?> ObtenerPorIdAsync(int id, int tenantId);
        Task<int> CrearAsync(GastoCreateDto dto, int tenantId);
        Task ActualizarAsync(GastoEditDto dto, int tenantId);
        Task EliminarAsync(int id, int tenantId);
    }
}

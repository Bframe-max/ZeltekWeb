using System.Collections.Generic;
using System.Threading.Tasks;
using ZeltekWeb.Models.DTOs;

namespace ZeltekWeb.Services.Interfaces
{
    public interface IGarantiaService
    {
        Task<IEnumerable<GarantiaVigenteDto>> ObtenerGarantiasVigentesAsync(string? buscar, int tenantId);
    }
}

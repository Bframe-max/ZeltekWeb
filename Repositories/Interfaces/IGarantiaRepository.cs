using System.Collections.Generic;
using System.Threading.Tasks;
using ZeltekWeb.Models.DTOs;

namespace ZeltekWeb.Repositories.Interfaces
{
    public interface IGarantiaRepository
    {
        Task<IEnumerable<GarantiaVigenteDto>> ObtenerGarantiasVigentesAsync(string? buscar);
    }
}

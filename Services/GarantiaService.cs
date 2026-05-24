using System.Collections.Generic;
using System.Threading.Tasks;
using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Repositories.Interfaces;
using ZeltekWeb.Services.Interfaces;

namespace ZeltekWeb.Services
{
    public class GarantiaService : IGarantiaService
    {
        private readonly IGarantiaRepository _garantiaRepo;

        public GarantiaService(IGarantiaRepository garantiaRepo)
        {
            _garantiaRepo = garantiaRepo;
        }

        public async Task<IEnumerable<GarantiaVigenteDto>> ObtenerGarantiasVigentesAsync(string? buscar)
            => await _garantiaRepo.ObtenerGarantiasVigentesAsync(buscar);
    }
}

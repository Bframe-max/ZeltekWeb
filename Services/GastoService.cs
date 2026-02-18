using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Repositories.Interfaces;
using ZeltekWeb.Services.Interfaces;

namespace ZeltekWeb.Services
{
    public class GastoService : IGastoService
    {
        private readonly IGastoRepository _gastoRepo;

        public GastoService(IGastoRepository gastoRepo)
        {
            _gastoRepo = gastoRepo;
        }

        public Task<IEnumerable<GastoListDto>> ObtenerListadoAsync(DateTime? desde, DateTime? hasta, int tenantId)
        {
            return _gastoRepo.ObtenerGastosAsync(desde, hasta, tenantId);
        }

        public Task<GastoResumenDto> ObtenerResumenAsync(DateTime? desde, DateTime? hasta, int tenantId)
        {
            return _gastoRepo.ObtenerResumenAsync(desde, hasta, tenantId);
        }

        public Task<IEnumerable<GastoPorCategoriaDto>> ObtenerPorCategoriaAsync(DateTime? desde, DateTime? hasta, int tenantId)
        {
            return _gastoRepo.ObtenerGastosPorCategoriaAsync(desde, hasta, tenantId);
        }

        public Task<GastoEditDto?> ObtenerPorIdAsync(int id, int tenantId)
        {
            return _gastoRepo.ObtenerPorIdAsync(id, tenantId);
        }

        public Task<int> CrearAsync(GastoCreateDto dto, int tenantId)
        {
            // Business logic for validation could go here
            if (dto.Monto <= 0)
                throw new ArgumentException("El monto debe ser positivo.");

            return _gastoRepo.CrearAsync(dto, tenantId);
        }

        public Task ActualizarAsync(GastoEditDto dto, int tenantId)
        {
            if (dto.Monto <= 0)
                throw new ArgumentException("El monto debe ser positivo.");

            return _gastoRepo.ActualizarAsync(dto, tenantId);
        }

        public Task EliminarAsync(int id, int tenantId)
        {
            return _gastoRepo.EliminarAsync(id, tenantId);
        }
    }
}

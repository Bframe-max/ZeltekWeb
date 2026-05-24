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

        public Task<IEnumerable<GastoListDto>> ObtenerListadoAsync(DateTime? desde, DateTime? hasta)
            => _gastoRepo.ObtenerGastosAsync(desde, hasta);

        public Task<GastoResumenDto> ObtenerResumenAsync(DateTime? desde, DateTime? hasta)
            => _gastoRepo.ObtenerResumenAsync(desde, hasta);

        public Task<IEnumerable<GastoPorCategoriaDto>> ObtenerPorCategoriaAsync(DateTime? desde, DateTime? hasta)
            => _gastoRepo.ObtenerGastosPorCategoriaAsync(desde, hasta);

        public Task<GastoEditDto?> ObtenerPorIdAsync(int id)
            => _gastoRepo.ObtenerPorIdAsync(id);

        public Task<int> CrearAsync(GastoCreateDto dto)
        {
            if (dto.Monto <= 0) throw new ArgumentException("El monto debe ser positivo.");
            return _gastoRepo.CrearAsync(dto);
        }

        public Task ActualizarAsync(GastoEditDto dto)
        {
            if (dto.Monto <= 0) throw new ArgumentException("El monto debe ser positivo.");
            return _gastoRepo.ActualizarAsync(dto);
        }

        public Task EliminarAsync(int id)
            => _gastoRepo.EliminarAsync(id);
    }
}

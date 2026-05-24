using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using ZeltekWeb.Data;
using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Repositories.Interfaces;

namespace ZeltekWeb.Repositories
{
    public class GastoRepository : IGastoRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public GastoRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<GastoListDto>> ObtenerGastosAsync(DateTime? desde, DateTime? hasta)
        {
            using var db = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT GastoID, FechaGasto, Descripcion, CategoriaGasto, Monto
                FROM GastoOperativo
                WHERE (@Desde IS NULL OR FechaGasto >= @Desde)
                  AND (@Hasta IS NULL OR FechaGasto <= @Hasta)
                ORDER BY FechaGasto DESC";

            return await db.QueryAsync<GastoListDto>(sql, new { Desde = desde, Hasta = hasta });
        }

        public async Task<GastoResumenDto> ObtenerResumenAsync(DateTime? desde, DateTime? hasta)
        {
            using var db = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT COUNT(*) AS CantidadGastos, ISNULL(SUM(Monto), 0) AS TotalGastos
                FROM GastoOperativo
                WHERE (@Desde IS NULL OR FechaGasto >= @Desde)
                  AND (@Hasta IS NULL OR FechaGasto <= @Hasta)";

            return await db.QuerySingleOrDefaultAsync<GastoResumenDto>(sql, new { Desde = desde, Hasta = hasta })
                   ?? new GastoResumenDto();
        }

        public async Task<IEnumerable<GastoPorCategoriaDto>> ObtenerGastosPorCategoriaAsync(DateTime? desde, DateTime? hasta)
        {
            using var db = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT ISNULL(CategoriaGasto, 'Sin categoría') AS Categoria, SUM(Monto) AS Total
                FROM GastoOperativo
                WHERE (@Desde IS NULL OR FechaGasto >= @Desde)
                  AND (@Hasta IS NULL OR FechaGasto <= @Hasta)
                GROUP BY CategoriaGasto
                ORDER BY SUM(Monto) DESC";

            return await db.QueryAsync<GastoPorCategoriaDto>(sql, new { Desde = desde, Hasta = hasta });
        }

        public async Task<GastoEditDto?> ObtenerPorIdAsync(int id)
        {
            using var db = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT GastoID, FechaGasto, Descripcion, CategoriaGasto, Monto
                FROM GastoOperativo
                WHERE GastoID = @ID";

            return await db.QuerySingleOrDefaultAsync<GastoEditDto>(sql, new { ID = id });
        }

        public async Task<int> CrearAsync(GastoCreateDto gasto)
        {
            using var db = _connectionFactory.CreateConnection();
            var sql = @"
                INSERT INTO GastoOperativo (FechaGasto, Descripcion, CategoriaGasto, Monto)
                VALUES (@FechaGasto, @Descripcion, @CategoriaGasto, @Monto);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            return await db.ExecuteScalarAsync<int>(sql, new
            {
                gasto.FechaGasto,
                gasto.Descripcion,
                gasto.CategoriaGasto,
                gasto.Monto
            });
        }

        public async Task ActualizarAsync(GastoEditDto gasto)
        {
            using var db = _connectionFactory.CreateConnection();
            var sql = @"
                UPDATE GastoOperativo
                SET FechaGasto = @FechaGasto,
                    Descripcion = @Descripcion,
                    CategoriaGasto = @CategoriaGasto,
                    Monto = @Monto
                WHERE GastoID = @GastoID";

            await db.ExecuteAsync(sql, new
            {
                gasto.FechaGasto,
                gasto.Descripcion,
                gasto.CategoriaGasto,
                gasto.Monto,
                gasto.GastoID
            });
        }

        public async Task EliminarAsync(int id)
        {
            using var db = _connectionFactory.CreateConnection();
            await db.ExecuteAsync("DELETE FROM GastoOperativo WHERE GastoID = @ID", new { ID = id });
        }
    }
}

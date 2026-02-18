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

        public async Task<IEnumerable<GastoListDto>> ObtenerGastosAsync(DateTime? desde, DateTime? hasta, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT
                    GastoID,
                    FechaGasto,
                    Descripcion,
                    CategoriaGasto,
                    Monto
                FROM GastoOperativo
                WHERE TenantID = @TenantId
                  AND (@Desde IS NULL OR FechaGasto >= @Desde)
                  AND (@Hasta IS NULL OR FechaGasto <= @Hasta)
                ORDER BY FechaGasto DESC";

            return await db.QueryAsync<GastoListDto>(sql, new { TenantId = tenantId, Desde = desde, Hasta = hasta });
        }

        public async Task<GastoResumenDto> ObtenerResumenAsync(DateTime? desde, DateTime? hasta, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT
                    COUNT(*) AS CantidadGastos,
                    ISNULL(SUM(Monto), 0) AS TotalGastos
                FROM GastoOperativo
                WHERE TenantID = @TenantId
                  AND (@Desde IS NULL OR FechaGasto >= @Desde)
                  AND (@Hasta IS NULL OR FechaGasto <= @Hasta)";

            return await db.QuerySingleOrDefaultAsync<GastoResumenDto>(sql, new { TenantId = tenantId, Desde = desde, Hasta = hasta }) 
                   ?? new GastoResumenDto();
        }

        public async Task<IEnumerable<GastoPorCategoriaDto>> ObtenerGastosPorCategoriaAsync(DateTime? desde, DateTime? hasta, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT
                    ISNULL(CategoriaGasto, 'Sin categoría') AS Categoria,
                    SUM(Monto) AS Total
                FROM GastoOperativo
                WHERE TenantID = @TenantId
                  AND (@Desde IS NULL OR FechaGasto >= @Desde)
                  AND (@Hasta IS NULL OR FechaGasto <= @Hasta)
                GROUP BY CategoriaGasto
                ORDER BY SUM(Monto) DESC";

            return await db.QueryAsync<GastoPorCategoriaDto>(sql, new { TenantId = tenantId, Desde = desde, Hasta = hasta });
        }

        public async Task<GastoEditDto?> ObtenerPorIdAsync(int id, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT
                    GastoID,
                    FechaGasto,
                    Descripcion,
                    CategoriaGasto,
                    Monto
                FROM GastoOperativo
                WHERE GastoID = @ID AND TenantID = @TenantId";

            return await db.QuerySingleOrDefaultAsync<GastoEditDto>(sql, new { ID = id, TenantId = tenantId });
        }

        public async Task<int> CrearAsync(GastoCreateDto gasto, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            var sql = @"
                INSERT INTO GastoOperativo
                (FechaGasto, Descripcion, CategoriaGasto, Monto, TenantID)
                VALUES
                (@FechaGasto, @Descripcion, @CategoriaGasto, @Monto, @TenantId);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            return await db.ExecuteScalarAsync<int>(sql, new 
            { 
                gasto.FechaGasto, 
                gasto.Descripcion, 
                gasto.CategoriaGasto, 
                gasto.Monto, 
                TenantId = tenantId 
            });
        }

        public async Task ActualizarAsync(GastoEditDto gasto, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            var sql = @"
                UPDATE GastoOperativo
                SET
                    FechaGasto = @FechaGasto,
                    Descripcion = @Descripcion,
                    CategoriaGasto = @CategoriaGasto,
                    Monto = @Monto
                WHERE GastoID = @GastoID AND TenantID = @TenantId";

            await db.ExecuteAsync(sql, new 
            { 
                gasto.FechaGasto, 
                gasto.Descripcion, 
                gasto.CategoriaGasto, 
                gasto.Monto, 
                gasto.GastoID, 
                TenantId = tenantId 
            });
        }

        public async Task EliminarAsync(int id, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            var sql = "DELETE FROM GastoOperativo WHERE GastoID = @ID AND TenantID = @TenantId";
            await db.ExecuteAsync(sql, new { ID = id, TenantId = tenantId });
        }
    }
}

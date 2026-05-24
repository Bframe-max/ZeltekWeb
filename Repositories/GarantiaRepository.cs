using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using ZeltekWeb.Data;
using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Repositories.Interfaces;

namespace ZeltekWeb.Repositories
{
    public class GarantiaRepository : IGarantiaRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public GarantiaRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<GarantiaVigenteDto>> ObtenerGarantiasVigentesAsync(string? buscar)
        {
            using var db = _connectionFactory.CreateConnection();

            var sql = @"
                SELECT
                    Cliente,
                    Telefono,
                    Producto,
                    NumeroSerie,
                    FechaVenta,
                    GarantiaMeses,
                    FechaVenceGarantia,
                    DiasRestantes,
                    Procesador,
                    RAM_GB,
                    Almacenamiento_GB,
                    Estado
                FROM vw_GarantiasVigentes
                WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                sql += @" AND (
                    Cliente LIKE '%' + @Buscar + '%' OR
                    Producto LIKE '%' + @Buscar + '%' OR
                    NumeroSerie LIKE '%' + @Buscar + '%'
                )";
            }

            sql += " ORDER BY DiasRestantes ASC";

            return await db.QueryAsync<GarantiaVigenteDto>(sql, new { Buscar = buscar });
        }
    }
}

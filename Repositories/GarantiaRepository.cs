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

        public async Task<IEnumerable<GarantiaVigenteDto>> ObtenerGarantiasVigentesAsync(string? buscar, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();

            // ? SOLUCIÓN: Usamos la VISTA 'vw_GarantiasVigentes'.
            // Esta vista ya tiene los Joins hechos, los nombres corregidos (Cliente, Producto)
            // y la lógica de fecha de vencimiento real.
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
                WHERE TenantId = @TenantId
                AND IsDeleted = 0";

            // Lógica de Búsqueda
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                sql += @" AND (
                    Cliente LIKE '%' + @Buscar + '%' OR 
                    Producto LIKE '%' + @Buscar + '%' OR 
                    NumeroSerie LIKE '%' + @Buscar + '%'
                )";
            }

            // Ordenamos por las que están por vencer primero
            sql += " ORDER BY DiasRestantes ASC";

            return await db.QueryAsync<GarantiaVigenteDto>(sql, new { TenantId = tenantId, Buscar = buscar });
        }
    }
}
using Dapper;
using System.Data;
using ZeltekWeb.Models;
using ZeltekWeb.Services;

namespace ZeltekWeb.Repositories
{
    public class VentaRepository : IVentaRepository
    {
        private readonly IConnectionFactory _connectionFactory;

        public VentaRepository(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<VentaListViewModel>> ObtenerHistorialAsync(string? buscar, DateTime? fechaInicio, DateTime? fechaFin)
        {
            using var db = _connectionFactory.CreateConnection();
            
            var sql = @"
                SELECT 
                    VentaID, FechaVenta, Cliente, TipoCliente, 
                    Producto, NumeroSerie, Especificaciones, 
                    CostoTotalVenta, VentaTotal, Ganancia, MetodoPago
                FROM vw_ReporteVentas
                WHERE (@Buscar IS NULL 
                        OR Cliente LIKE '%' + @Buscar + '%' 
                        OR Producto LIKE '%' + @Buscar + '%'
                        OR NumeroSerie LIKE '%' + @Buscar + '%')
                AND (@FInicio IS NULL OR CAST(FechaVenta AS DATE) >= @FInicio)
                AND (@FFin IS NULL OR CAST(FechaVenta AS DATE) <= @FFin)
                ORDER BY FechaVenta DESC";

            var parameters = new
            {
                Buscar = buscar,
                FInicio = fechaInicio,
                FFin = fechaFin
            };

            return await db.QueryAsync<VentaListViewModel>(sql, parameters);
        }
    }
}

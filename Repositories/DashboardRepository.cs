using Dapper;
using System.Data;
using ZeltekWeb.Constants;
using ZeltekWeb.Data;
using ZeltekWeb.Models.DTOs;

namespace ZeltekWeb.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public DashboardRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<decimal> GetVentasHoyAsync(int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT ISNULL(SUM(TotalVenta), 0)
                FROM Venta
                WHERE CAST(FechaVenta AS DATE) = CAST(GETDATE() AS DATE)";
                // AND TenantID = @TenantId
            return await db.ExecuteScalarAsync<decimal>(sql, new { TenantId = tenantId });
        }

        public async Task<(int Cantidad, decimal Monto)> GetEnCaminoMetricsAsync(string[] excludedStatuses, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT COUNT(*) as Count,
                       ISNULL(SUM(ISNULL(CostoTotal, 0) + ISNULL(CostoEnvioTotal, 0)), 0) as Amount
                FROM Compra
                WHERE EstadoCompra NOT IN @ExcludedStatuses";
            
             var result = await db.QueryFirstOrDefaultAsync<(int Count, decimal Amount)>(sql, new { ExcludedStatuses = excludedStatuses, TenantId = tenantId });
             return result;
        }

        public async Task<int> GetPorRevisarCountAsync(string status, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT COUNT(*)
                FROM ItemInventario
                WHERE Estado = @Status";
            return await db.ExecuteScalarAsync<int>(sql, new { Status = status, TenantId = tenantId });
        }

        public async Task<(int Cantidad, decimal Valor)> GetStockMetricsAsync(string status, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT COUNT(*) as Count,
                       ISNULL(SUM(CostoCompra + ISNULL(CostoEnvio, 0) + ISNULL(CostoReparacion, 0)), 0) as Value
                FROM ItemInventario
                WHERE Estado = @Status";
             var result = await db.QueryFirstOrDefaultAsync<(int Count, decimal Value)>(sql, new { Status = status, TenantId = tenantId });
             return result;
        }

        public async Task<List<ActividadRecienteDto>> GetRecienteActividadAsync(int top, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT TOP (@Top) Tipo, Descripcion, Fecha, Monto FROM (
                    SELECT 'Venta' AS Tipo,
                            'Venta #' + CAST(v.VentaID AS VARCHAR(20)) + ' · ' + ISNULL(c.NombreCompleto, 'Sin cliente') AS Descripcion,
                            v.FechaVenta AS Fecha,
                            v.TotalVenta AS Monto
                    FROM Venta v
                    LEFT JOIN Cliente c ON v.ClienteID = c.ClienteID
                    UNION ALL
                    SELECT 'Compra' AS Tipo,
                            'Compra #' + CAST(comp.CompraID AS VARCHAR(20)) + ' · ' + ISNULL((SELECT TOP 1 p.Nombre FROM ItemInventario i INNER JOIN Producto p ON i.ProductoID = p.ProductoID WHERE i.CompraID = comp.CompraID), 'Varios') AS Descripcion,
                            comp.FechaCompra AS Fecha,
                            comp.CostoTotal AS Monto
                    FROM Compra comp
                ) u
                ORDER BY Fecha DESC";
            
            var result = await db.QueryAsync<ActividadRecienteDto>(sql, new { Top = top, TenantId = tenantId });
            return result.ToList();
        }

        public async Task<List<SearchResultDto>> BuscarEnVentasAsync(string query, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT v.VentaID as Id, 
                       ISNULL(c.NombreCompleto, 'Cliente General') as Header,
                       'Venta #' + CAST(v.VentaID as varchar) as SubHeader,
                       v.FechaVenta as Date, 
                       v.TotalVenta as Amount,
                       'Completado' as Status,
                       'Venta' as Type
                FROM Venta v
                LEFT JOIN Cliente c ON v.ClienteID = c.ClienteID
                LEFT JOIN DetalleVenta d ON d.VentaID = v.VentaID
                LEFT JOIN ItemInventario i ON i.ItemID = d.ItemID
                WHERE (c.NombreCompleto LIKE '%' + @Buscar + '%'
                    OR i.NumeroSerie LIKE '%' + @Buscar + '%')
                GROUP BY v.VentaID, c.NombreCompleto, v.FechaVenta, v.TotalVenta";
            
            var result = await db.QueryAsync<SearchResultDto>(sql, new { Buscar = query, TenantId = tenantId });
            return result.ToList();
        }

        public async Task<List<SearchResultDto>> BuscarEnComprasAsync(string query, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT c.CompraID as Id,
                       ISNULL(c.TrackingProveedor, 'Sin Tracking') as Header,
                       ISNULL((SELECT TOP 1 p.Nombre FROM ItemInventario i INNER JOIN Producto p ON i.ProductoID = p.ProductoID WHERE i.CompraID = c.CompraID), 'Varios') as SubHeader,
                       c.FechaCompra as Date,
                       c.CostoTotal as Amount,
                       c.EstadoCompra as Status,
                       'Compra' as Type
                FROM Compra c
                LEFT JOIN ItemInventario i ON i.CompraID = c.CompraID
                LEFT JOIN Producto p ON i.ProductoID = p.ProductoID
                WHERE (c.TrackingProveedor LIKE '%' + @Buscar + '%'
                    OR c.VendedorOnline LIKE '%' + @Buscar + '%'
                    OR p.Nombre LIKE '%' + @Buscar + '%'
                    OR i.NumeroSerie LIKE '%' + @Buscar + '%')
                GROUP BY c.CompraID, c.TrackingProveedor, c.FechaCompra, c.CostoTotal, c.EstadoCompra";
            
            var result = await db.QueryAsync<SearchResultDto>(sql, new { Buscar = query, TenantId = tenantId });
            return result.ToList();
        }

        public async Task<List<SearchResultDto>> BuscarEnInventarioAsync(string query, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT i.ItemID as Id,
                       p.Nombre as Header,
                       ISNULL(i.NumeroSerie, 'S/N') as SubHeader,
                       i.FechaRegistro as Date,
                       NULL as Amount,
                       i.Estado as Status,
                       'Inventario' as Type
                FROM ItemInventario i
                INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                WHERE i.NumeroSerie LIKE '%' + @Buscar + '%' OR p.Nombre LIKE '%' + @Buscar + '%'
                ORDER BY i.ItemID DESC";
            
            var result = await db.QueryAsync<SearchResultDto>(sql, new { Buscar = query, TenantId = tenantId });
            return result.ToList();
        }
    }
}

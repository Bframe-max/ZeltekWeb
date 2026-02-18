using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZeltekWeb.Data;
using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Repositories.Interfaces;

namespace ZeltekWeb.Repositories
{
    public class ReporteRepository : IReporteRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ReporteRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<TendenciaVentasDto>> ObtenerTendenciaVentasAsync(int meses, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();

            // Calculamos la fecha de inicio basándonos en meses completos (1ro del mes)
            // Ejemplo: Si estamos en Feb y pides 3 meses, queremos desde el 1ro de Diciembre.
            var fechaInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-(meses - 1));

            string sqlVentas = @"
                SELECT
                    FORMAT(v.FechaVenta, 'yyyy-MM') AS Mes,
                    FORMAT(MIN(v.FechaVenta), 'MMM yy') AS MesLabel,
                    SUM(d.PrecioUnitario) AS TotalVentas,
                    COUNT(DISTINCT v.VentaID) AS CantidadVentas
                FROM Venta v
                INNER JOIN DetalleVenta d ON v.VentaID = d.VentaID
                WHERE v.IsDeleted = 0
                AND v.TenantID = @TenantId
                AND v.FechaVenta >= @FechaInicio 
                GROUP BY FORMAT(v.FechaVenta, 'yyyy-MM')
                ORDER BY FORMAT(v.FechaVenta, 'yyyy-MM')";

            var tendencias = (await db.QueryAsync<TendenciaVentasDto>(sqlVentas, new { FechaInicio = fechaInicio, TenantId = tenantId })).ToList();

            string sqlEquipos = @"
                SELECT 
                    FORMAT(v.FechaVenta, 'yyyy-MM') AS Mes,
                    COUNT(*) AS Cantidad
                FROM Venta v
                INNER JOIN DetalleVenta d ON v.VentaID = d.VentaID
                WHERE v.IsDeleted = 0
                AND v.TenantID = @TenantId
                AND v.FechaVenta >= @FechaInicio
                GROUP BY FORMAT(v.FechaVenta, 'yyyy-MM')";

            var equipos = await db.QueryAsync<(string Mes, int Cantidad)>(sqlEquipos, new { FechaInicio = fechaInicio, TenantId = tenantId });
            var equiposDict = equipos.ToDictionary(k => k.Mes, v => v.Cantidad);

            foreach (var t in tendencias)
            {
                if (equiposDict.TryGetValue(t.Mes, out int cant))
                {
                    t.CantidadEquipos = cant;
                }
            }

            return tendencias;
        }

        public async Task<IEnumerable<TopProductoDto>> ObtenerTopProductosAsync(int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT TOP 10 
                    p.Nombre AS ProductoNombre,
                    COUNT(*) AS CantidadVentas,
                    SUM(d.PrecioUnitario) AS TotalVendido
                FROM DetalleVenta d
                INNER JOIN Venta v ON d.VentaID = v.VentaID
                INNER JOIN ItemInventario i ON d.ItemID = i.ItemID
                INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                WHERE v.IsDeleted = 0
                AND v.TenantID = @TenantId
                GROUP BY p.ProductoID, p.Nombre
                ORDER BY COUNT(*) DESC";

            return await db.QueryAsync<TopProductoDto>(sql, new { TenantId = tenantId });
        }

        // 🔥 MÉTODO CORREGIDO: Lógica de Fechas Exacta conectada a la Vista
        public async Task<ResumenFinancieroDto> ObtenerResumenFinancieroAsync(int periodoMeses, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();

            // Calculamos el inicio del periodo
            var fechaInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-(periodoMeses - 1));

            // 1. Ingresos y Costos (Ventas)
            string sqlVentas = @"
                SELECT 
                    ISNULL(SUM(d.PrecioUnitario), 0) as IngresoTotal,
                    ISNULL(SUM(d.PrecioUnitario - (i.CostoCompra + ISNULL(i.CostoEnvio, 0) + ISNULL(i.CostoReparacion, 0))), 0) as GananciaBruta
                FROM Venta v
                INNER JOIN DetalleVenta d ON v.VentaID = d.VentaID
                INNER JOIN ItemInventario i ON d.ItemID = i.ItemID
                WHERE v.IsDeleted = 0
                AND v.TenantID = @TenantId
                AND v.FechaVenta >= @FechaInicio";

            var ventasResult = await db.QueryFirstOrDefaultAsync<(decimal IngresoTotal, decimal GananciaBruta)>(sqlVentas, new { FechaInicio = fechaInicio, TenantId = tenantId });

            // 2. Gastos Operativos
            string sqlGastos = @"
                SELECT ISNULL(SUM(Monto), 0)
                FROM GastoOperativo
                WHERE TenantID = @TenantId
                AND FechaGasto >= @FechaInicio";

            decimal gastosOperativos = await db.ExecuteScalarAsync<decimal>(sqlGastos, new { FechaInicio = fechaInicio, TenantId = tenantId });

            return new ResumenFinancieroDto
            {
                IngresoTotal = ventasResult.IngresoTotal,
                GananciaBruta = ventasResult.GananciaBruta,
                GastosOperativos = gastosOperativos
            };
        }

        public async Task<decimal> ObtenerValorInventarioActualAsync(int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();

            string sql = @"
                SELECT ISNULL(SUM(i.CostoCompra + ISNULL(i.CostoEnvio, 0) + ISNULL(i.CostoReparacion, 0)), 0)
                FROM ItemInventario i
                WHERE i.Estado = 'En Stock'
                AND i.TenantID = @TenantId";

            return await db.ExecuteScalarAsync<decimal>(sql, new { TenantId = tenantId });
        }

        public async Task<IEnumerable<InventarioDormidoDto>> ObtenerInventarioDormidoAsync(int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT
                    i.ItemID,
                    p.Nombre AS ProductoNombre,
                    ISNULL(i.NumeroSerie, '') AS NumeroSerie,
                    DATEDIFF(day, c.FechaCompra, GETDATE()) AS DiasEnStock,
                    (i.CostoCompra + ISNULL(i.CostoEnvio, 0) + ISNULL(i.CostoReparacion, 0)) AS CostoTotal,
                    ISNULL(i.PrecioVenta, 0) AS PrecioVenta,
                    i.Procesador,
                    ISNULL(i.RAM_GB, 0) AS RAM_GB,
                    ISNULL(i.Almacenamiento_GB, 0) AS Almacenamiento_GB
                FROM ItemInventario i
                INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                LEFT JOIN Compra c ON i.CompraID = c.CompraID
                WHERE i.Estado = 'En Stock'
                AND DATEDIFF(day, c.FechaCompra, GETDATE()) >= 60
                AND i.TenantID = @TenantId
                ORDER BY DiasEnStock DESC";

            return await db.QueryAsync<InventarioDormidoDto>(sql, new { TenantId = tenantId });
        }

        public async Task<IEnumerable<TopRevendedorDto>> ObtenerTopRevendedoresAsync(int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();

            string sql = @"
                SELECT 
                    c.NombreCompleto AS NombreCliente,
                    c.TipoCliente,
                    COUNT(v.VentaID) AS CantidadCompras,
                    ISNULL(SUM(d.PrecioUnitario), 0) AS TotalComprado,
                    ISNULL(SUM(d.PrecioUnitario - (i.CostoCompra + ISNULL(i.CostoEnvio, 0) + ISNULL(i.CostoReparacion, 0))), 0) AS GananciaGenerada,
                    MAX(v.FechaVenta) AS UltimaCompra
                FROM Cliente c
                INNER JOIN Venta v ON c.ClienteID = v.ClienteID
                INNER JOIN DetalleVenta d ON v.VentaID = d.VentaID
                INNER JOIN ItemInventario i ON d.ItemID = i.ItemID
                WHERE v.IsDeleted = 0
                AND v.TenantID = @TenantId
                AND LOWER(LTRIM(RTRIM(ISNULL(c.TipoCliente,'')))) = 'revendedor'
                GROUP BY c.ClienteID, c.NombreCompleto, c.TipoCliente
                ORDER BY TotalComprado DESC";

            return await db.QueryAsync<TopRevendedorDto>(sql, new { TenantId = tenantId });
        }
    }
}
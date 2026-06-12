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

        public async Task<IEnumerable<TendenciaVentasDto>> ObtenerTendenciaVentasAsync(int meses)
        {
            using var db = _connectionFactory.CreateConnection();
            var fechaInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-(meses - 1));

            string sqlVentas = @"
                SELECT
                    FORMAT(v.FechaVenta, 'yyyy-MM') AS Mes,
                    FORMAT(MIN(v.FechaVenta), 'MMM yy') AS MesLabel,
                    SUM(d.PrecioUnitario * d.Cantidad) AS TotalVentas,
                    COUNT(DISTINCT v.VentaID) AS CantidadVentas
                FROM Venta v
                INNER JOIN DetalleVenta d ON v.VentaID = d.VentaID
                WHERE v.FechaVenta >= @FechaInicio
                GROUP BY FORMAT(v.FechaVenta, 'yyyy-MM')
                ORDER BY FORMAT(v.FechaVenta, 'yyyy-MM')";

            var tendencias = (await db.QueryAsync<TendenciaVentasDto>(sqlVentas, new { FechaInicio = fechaInicio })).ToList();

            string sqlEquipos = @"
                SELECT FORMAT(v.FechaVenta, 'yyyy-MM') AS Mes, SUM(d.Cantidad) AS Cantidad
                FROM Venta v
                INNER JOIN DetalleVenta d ON v.VentaID = d.VentaID
                WHERE v.FechaVenta >= @FechaInicio
                GROUP BY FORMAT(v.FechaVenta, 'yyyy-MM')";

            var equipos = await db.QueryAsync<(string Mes, int Cantidad)>(sqlEquipos, new { FechaInicio = fechaInicio });
            var equiposDict = equipos.ToDictionary(k => k.Mes, v => v.Cantidad);

            foreach (var t in tendencias)
                if (equiposDict.TryGetValue(t.Mes, out int cant))
                    t.CantidadEquipos = cant;

            return tendencias;
        }

        public async Task<IEnumerable<TopProductoDto>> ObtenerTopProductosAsync()
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT TOP 10
                    p.Nombre AS ProductoNombre,
                    SUM(d.Cantidad) AS CantidadVentas,
                    SUM(d.PrecioUnitario * d.Cantidad) AS TotalVendido
                FROM DetalleVenta d
                INNER JOIN Venta v ON d.VentaID = v.VentaID
                INNER JOIN ItemInventario i ON d.ItemID = i.ItemID
                INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                GROUP BY p.ProductoID, p.Nombre
                ORDER BY SUM(d.Cantidad) DESC";
            return await db.QueryAsync<TopProductoDto>(sql);
        }

        public async Task<ResumenFinancieroDto> ObtenerResumenFinancieroAsync(int periodoMeses)
        {
            using var db = _connectionFactory.CreateConnection();
            var fechaInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-(periodoMeses - 1));

            string sqlVentas = @"
                SELECT
                    ISNULL(SUM(d.PrecioUnitario * d.Cantidad), 0) as IngresoTotal,
                    ISNULL(SUM((d.PrecioUnitario * d.Cantidad) -
                        (CASE WHEN ISNULL(p.EsUnico, 1) = 1
                              THEN (i.CostoCompra + ISNULL(i.CostoEnvio, 0) + ISNULL(i.CostoReparacion, 0))
                              ELSE (i.CostoCompra / NULLIF(i.CantidadInicial, 0)) * d.Cantidad
                         END)), 0) as GananciaBruta
                FROM Venta v
                INNER JOIN DetalleVenta d ON v.VentaID = d.VentaID
                INNER JOIN ItemInventario i ON d.ItemID = i.ItemID
                INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                WHERE v.FechaVenta >= @FechaInicio";

            var ventasResult = await db.QueryFirstOrDefaultAsync<(decimal IngresoTotal, decimal GananciaBruta)>(sqlVentas, new { FechaInicio = fechaInicio });

            string sqlGastos = @"
                SELECT ISNULL(SUM(Monto), 0)
                FROM GastoOperativo
                WHERE FechaGasto >= @FechaInicio";

            decimal gastosOperativos = await db.ExecuteScalarAsync<decimal>(sqlGastos, new { FechaInicio = fechaInicio });

            return new ResumenFinancieroDto
            {
                IngresoTotal = ventasResult.IngresoTotal,
                GananciaBruta = ventasResult.GananciaBruta,
                GastosOperativos = gastosOperativos
            };
        }

        public async Task<decimal> ObtenerValorInventarioActualAsync()
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT ISNULL(SUM(i.CostoCompra + ISNULL(i.CostoEnvio, 0) + ISNULL(i.CostoReparacion, 0)), 0)
                FROM ItemInventario i
                WHERE i.Estado = 'En Stock'";
            return await db.ExecuteScalarAsync<decimal>(sql);
        }

        public async Task<IEnumerable<InventarioDormidoDto>> ObtenerInventarioDormidoAsync()
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT
                    i.ItemID, p.Nombre AS ProductoNombre, ISNULL(i.NumeroSerie, '') AS NumeroSerie,
                    DATEDIFF(day, c.FechaCompra, GETDATE()) AS DiasEnStock,
                    (i.CostoCompra + ISNULL(i.CostoEnvio, 0) + ISNULL(i.CostoReparacion, 0)) AS CostoTotal,
                    ISNULL(i.PrecioVenta, 0) AS PrecioVenta, i.Procesador,
                    ISNULL(i.RAM_GB, 0) AS RAM_GB, ISNULL(i.Almacenamiento_GB, 0) AS Almacenamiento_GB
                FROM ItemInventario i
                INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                LEFT JOIN Compra c ON i.CompraID = c.CompraID
                WHERE i.Estado = 'En Stock' AND DATEDIFF(day, c.FechaCompra, GETDATE()) >= 60
                ORDER BY DiasEnStock DESC";
            return await db.QueryAsync<InventarioDormidoDto>(sql);
        }

        public async Task<IEnumerable<TopRevendedorDto>> ObtenerTopRevendedoresAsync()
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT
                    c.NombreCompleto AS NombreCliente, c.TipoCliente,
                    COUNT(DISTINCT v.VentaID) AS CantidadCompras,
                    ISNULL(SUM(d.PrecioUnitario * d.Cantidad), 0) AS TotalComprado,
                    ISNULL(SUM((d.PrecioUnitario * d.Cantidad) - (i.CostoCompra + ISNULL(i.CostoEnvio, 0) + ISNULL(i.CostoReparacion, 0))), 0) AS GananciaGenerada,
                    MAX(v.FechaVenta) AS UltimaCompra
                FROM Cliente c
                INNER JOIN Venta v ON c.ClienteID = v.ClienteID
                INNER JOIN DetalleVenta d ON v.VentaID = d.VentaID
                INNER JOIN ItemInventario i ON d.ItemID = i.ItemID
                WHERE LOWER(LTRIM(RTRIM(ISNULL(c.TipoCliente,'')))) = 'revendedor'
                GROUP BY c.ClienteID, c.NombreCompleto, c.TipoCliente
                ORDER BY TotalComprado DESC";
            return await db.QueryAsync<TopRevendedorDto>(sql);
        }
    }
}

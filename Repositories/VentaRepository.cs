using Dapper;
using System.Data;
using ZeltekWeb.Models;
using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Data;

namespace ZeltekWeb.Repositories
{
    public class VentaRepository : IVentaRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public VentaRepository(IDbConnectionFactory connectionFactory)
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

            return await db.QueryAsync<VentaListViewModel>(sql, new { Buscar = buscar, FInicio = fechaInicio, FFin = fechaFin });
        }

        public async Task<int> CrearVentaAsync(CreateVentaDto dto, int clienteId, IDbTransaction transaction)
        {
            var sql = @"
                INSERT INTO Venta (ClienteID, FechaVenta, TotalVenta, MetodoPago, GarantiaMeses, NotasVenta, CreatedBy, TipoVenta)
                OUTPUT INSERTED.VentaID
                VALUES (@ClienteID, @FechaVenta, @Total, @Metodo, @Garantia, @Notas, @Usuario, @Tipo)";

            decimal total = dto.PrecioFinal * dto.CantidadAVender;

            return await transaction.Connection.ExecuteScalarAsync<int>(sql, new
            {
                ClienteID = clienteId,
                FechaVenta = dto.FechaVenta,
                Total = total,
                Metodo = dto.MetodoPago,
                Garantia = dto.GarantiaMeses,
                Notas = dto.Notas,
                Usuario = dto.Usuario,
                Tipo = dto.TipoVenta
            }, transaction);
        }

        public async Task CrearDetalleVentaAsync(int ventaId, int itemId, int productoId, decimal precio, IDbTransaction transaction)
        {
            var sql = "INSERT INTO DetalleVenta (VentaID, ItemID, ProductoID, PrecioUnitario) VALUES (@VentaID, @ItemID, @ProductoID, @Precio)";
            await transaction.Connection.ExecuteAsync(sql, new { VentaID = ventaId, ItemID = itemId, ProductoID = productoId, Precio = precio }, transaction);
        }

        public async Task<bool> ActualizarStockItemAsync(int itemId, int cantidad, bool esUnico, IDbTransaction transaction)
        {
            if (esUnico)
            {
                var sql = "UPDATE ItemInventario SET Estado = 'Vendido' WHERE ItemID = @ItemID AND Estado = 'En Stock'";
                var rows = await transaction.Connection.ExecuteAsync(sql, new { ItemID = itemId }, transaction);
                return rows > 0;
            }
            else
            {
                var sql = "UPDATE ItemInventario SET CantidadActual = CantidadActual - @Cantidad WHERE ItemID = @ItemID AND Estado = 'En Stock' AND CantidadActual >= @Cantidad";
                var rows = await transaction.Connection.ExecuteAsync(sql, new { Cantidad = cantidad, ItemID = itemId }, transaction);
                return rows > 0;
            }
        }

        public async Task CrearGarantiaAsync(int ventaId, int productoId, int itemId, DateTime inicio, DateTime fin, IDbTransaction transaction)
        {
            var sql = @"
                INSERT INTO Garantias (VentaID, ProductoID, ItemID, FechaInicio, FechaFin, Estado)
                VALUES (@VentaID, @ProductoID, @ItemID, @FechaInicio, @FechaFin, 'Activa')";

            await transaction.Connection.ExecuteAsync(sql, new
            {
                VentaID = ventaId,
                ProductoID = productoId,
                ItemID = itemId,
                FechaInicio = inicio,
                FechaFin = fin
            }, transaction);
        }

        public async Task<(int ProductoId, bool EsUnico, int CantidadActual, decimal CostoTotal, string Nombre)> ObtenerInfoItemAsync(int itemId)
        {
            using var db = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT
                    i.ProductoID,
                    ISNULL(p.EsUnico, 1),
                    ISNULL(i.CantidadActual, 0),
                    (i.CostoCompra + ISNULL(i.CostoEnvio, 0) + ISNULL(i.CostoReparacion, 0)) as CostoTotal,
                    p.Nombre
                FROM ItemInventario i
                INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                WHERE i.ItemID = @ItemID";

            return await db.QueryFirstOrDefaultAsync<(int, bool, int, decimal, string)>(sql, new { ItemID = itemId });
        }
    }
}

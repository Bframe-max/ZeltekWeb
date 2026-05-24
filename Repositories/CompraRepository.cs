using Dapper;
using System.Data;
using ZeltekWeb.Data;
using ZeltekWeb.Models.DTOs;

namespace ZeltekWeb.Repositories
{
    public class CompraRepository : ICompraRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public CompraRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<(IEnumerable<CompraListDto> Items, int Activos, decimal InversionFlotante)> ObtenerHistorialAsync(string? buscar, string viewType)
        {
            using var db = _connectionFactory.CreateConnection();

            string sqlKPI = @"
                SELECT COUNT(*), ISNULL(SUM(CostoTotal), 0)
                FROM Compra
                WHERE EstadoCompra NOT IN ('Recibido', 'Completado', 'Cancelado')";

            int activos = 0;
            decimal inversion = 0;

            using (var kpiReader = await db.ExecuteReaderAsync(sqlKPI))
            {
                if (kpiReader.Read())
                {
                    activos = kpiReader.GetInt32(0);
                    inversion = kpiReader.GetDecimal(1);
                }
            }

            string sql = @"
                SELECT
                    c.CompraID,
                    c.FechaCompra,
                    c.VendedorOnline,
                    c.TrackingProveedor,
                    c.CostoTotal,
                    c.EstadoCompra,
                    (SELECT COUNT(*) FROM ItemInventario WHERE CompraID = c.CompraID) as CantidadItems,
                    (SELECT COUNT(*) FROM ItemInventario WHERE CompraID = c.CompraID AND Estado NOT IN ('En Stock', 'Vendido', 'Por Revisar')) as ItemsPendientes,
                    ISNULL((SELECT TOP 1 p.Nombre
                        FROM ItemInventario i
                        INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                        WHERE i.CompraID = c.CompraID), 'Varios / Desconocido') as ProductoPrincipal
                FROM Compra c
                WHERE 1=1 ";

            var parameters = new DynamicParameters();

            if (!string.IsNullOrEmpty(buscar))
            {
                sql += @" AND (
                    c.VendedorOnline LIKE '%' + @Buscar + '%'
                    OR c.TrackingProveedor LIKE '%' + @Buscar + '%'
                    OR c.EstadoCompra LIKE '%' + @Buscar + '%'
                    OR EXISTS (
                        SELECT 1 FROM ItemInventario i
                        INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                        WHERE i.CompraID = c.CompraID AND p.Nombre LIKE '%' + @Buscar + '%'
                    )
                )";
                parameters.Add("Buscar", buscar);
            }

            if (viewType == "history")
                sql += " AND c.EstadoCompra IN ('Recibido', 'Completado', 'Cancelado')";
            else
                sql += " AND c.EstadoCompra NOT IN ('Recibido', 'Completado', 'Cancelado')";

            sql += " ORDER BY c.FechaCompra DESC";

            var items = await db.QueryAsync<CompraListDto>(sql, parameters);
            return (items, activos, inversion);
        }

        public async Task<CompraDto?> ObtenerPorIdAsync(int id)
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT CompraID, FechaCompra, VendedorOnline, TrackingProveedor, CostoTotal, CostoEnvioTotal, EstadoCompra
                FROM Compra
                WHERE CompraID = @ID";
            return await db.QueryFirstOrDefaultAsync<CompraDto>(sql, new { ID = id });
        }

        public async Task<IEnumerable<ItemCompraDto>> ObtenerItemsPorCompraAsync(int compraId)
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT
                    i.ItemID,
                    i.ProductoID,
                    p.Nombre as ProductoNombre,
                    i.CostoCompra,
                    i.Estado,
                    i.NumeroSerie,
                    CASE WHEN (ISNULL(c.Nombre,'') LIKE '%Cargador%' OR ISNULL(c.Nombre,'') LIKE '%Accesorio%') THEN 1 ELSE 0 END as EsAccesorio,
                    ISNULL(p.EsUnico, 1) as EsUnico,
                    i.Procesador,
                    i.RAM_GB as Ram,
                    i.Almacenamiento_GB as Almacenamiento
                FROM ItemInventario i
                INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                LEFT JOIN Categoria c ON p.CategoriaID = c.CategoriaID
                WHERE i.CompraID = @ID";

            return await db.QueryAsync<ItemCompraDto>(sql, new { ID = compraId });
        }

        public async Task<bool> VerificarTrackingAsync(string tracking, int? excluirCompraId)
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = "SELECT 1 FROM Compra WHERE TrackingProveedor = @Tracking";
            var parameters = new DynamicParameters();
            parameters.Add("Tracking", tracking);

            if (excluirCompraId.HasValue)
            {
                sql += " AND CompraID <> @ExcludedID";
                parameters.Add("ExcludedID", excluirCompraId.Value);
            }

            return await db.ExecuteScalarAsync<int?>(sql, parameters) != null;
        }

        public async Task<int> CrearCompraAsync(CreateCompraDto dto)
        {
            using var db = _connectionFactory.CreateConnection();
            db.Open();
            using var tx = db.BeginTransaction();

            try
            {
                var sqlCompra = @"
                    INSERT INTO Compra (ProveedorID, VendedorOnline, TrackingProveedor, FechaCompra, CostoTotal, CostoEnvioTotal, EstadoCompra)
                    OUTPUT INSERTED.CompraID
                    VALUES (1, @Vendedor, @Tracking, @Fecha, @CostoTotal, @CostoEnvio, 'En Transito')";

                var compraId = await db.ExecuteScalarAsync<int>(sqlCompra, new
                {
                    Vendedor = dto.Vendedor,
                    Tracking = dto.TrackingProveedor ?? "",
                    Fecha = dto.FechaCompra,
                    CostoTotal = dto.CostoTotalCompra,
                    CostoEnvio = dto.CostoEnvioTotal ?? 0
                }, tx);

                if (dto.Cantidad > 0)
                {
                    decimal costoUnitario = dto.CostoTotalCompra / dto.Cantidad;
                    var sqlItem = @"
                        INSERT INTO ItemInventario (ProductoID, CompraID, Estado, CostoCompra, FechaRegistro)
                        VALUES (@ProductoID, @CompraID, 'En Transito', @CostoUnitario, GETDATE())";

                    for (int i = 0; i < dto.Cantidad; i++)
                    {
                        await db.ExecuteAsync(sqlItem, new
                        {
                            ProductoID = dto.ProductoID,
                            CompraID = compraId,
                            CostoUnitario = costoUnitario
                        }, tx);
                    }
                }

                tx.Commit();
                return compraId;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task ActualizarCompraAsync(UpdateCompraDto dto)
        {
            using var db = _connectionFactory.CreateConnection();
            db.Open();
            using var tx = db.BeginTransaction();

            try
            {
                var sql = @"
                    UPDATE Compra
                    SET TrackingProveedor = @Tracking,
                        EstadoCompra = @Estado,
                        CostoTotal = @CostoTotal
                    WHERE CompraID = @ID";

                await db.ExecuteAsync(sql, new
                {
                    Tracking = dto.TrackingProveedor ?? "",
                    Estado = dto.EstadoCompra,
                    CostoTotal = dto.CostoTotal,
                    ID = dto.CompraID
                }, tx);

                int cantidadItems = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM ItemInventario WHERE CompraID = @ID", new { ID = dto.CompraID }, tx);

                if (cantidadItems > 0)
                {
                    decimal costoPorItem = dto.CostoTotal / cantidadItems;
                    await db.ExecuteAsync("UPDATE ItemInventario SET CostoCompra = @Costo WHERE CompraID = @ID",
                        new { Costo = costoPorItem, ID = dto.CompraID }, tx);
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task ActualizarCostosItemsAsync(int compraId, decimal costoUnitario, IDbTransaction? transaction = null)
        {
            var sql = "UPDATE ItemInventario SET CostoCompra = @Costo WHERE CompraID = @ID";
            if (transaction != null)
                await transaction.Connection.ExecuteAsync(sql, new { Costo = costoUnitario, ID = compraId }, transaction);
            else
            {
                using var db = _connectionFactory.CreateConnection();
                await db.ExecuteAsync(sql, new { Costo = costoUnitario, ID = compraId });
            }
        }

        public async Task EliminarCompraAsync(int id)
        {
            using var db = _connectionFactory.CreateConnection();
            db.Open();
            using var tx = db.BeginTransaction();
            try
            {
                await db.ExecuteAsync("DELETE FROM ItemInventario WHERE CompraID = @ID", new { ID = id }, tx);
                await db.ExecuteAsync("DELETE FROM Compra WHERE CompraID = @ID", new { ID = id }, tx);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task ForzarCierreAsync(int id)
        {
            using var db = _connectionFactory.CreateConnection();
            await db.ExecuteAsync("UPDATE Compra SET EstadoCompra = 'Completado' WHERE CompraID = @ID", new { ID = id });
        }

        public async Task<bool> VerificarSerialAsync(string serial, int? excluirItemId)
        {
            using var db = _connectionFactory.CreateConnection();
            var sql = "SELECT 1 FROM ItemInventario WHERE NumeroSerie = @Serial";
            var p = new DynamicParameters();
            p.Add("Serial", serial);

            if (excluirItemId.HasValue)
            {
                sql += " AND ItemID <> @ExcludeID";
                p.Add("ExcludeID", excluirItemId.Value);
            }

            return await db.ExecuteScalarAsync<int?>(sql, p) != null;
        }

        public async Task RecibirItemAsync(RecibirItemDto dto)
        {
            using var db = _connectionFactory.CreateConnection();
            db.Open();
            using var tx = db.BeginTransaction();

            try
            {
                string estadoFinal = dto.ProductoNuevo ? "En Stock" : "Por Revisar";

                string sql = @"
                    UPDATE ItemInventario
                    SET Estado = @Estado,
                        NumeroSerie = @Serie,
                        CostoEnvio = @Envio,
                        CostoReparacion = @Reparacion,
                        Condicion = @Condicion,
                        SaludBateria = @Bateria,
                        RAM_GB = @Ram,
                        Almacenamiento_GB = @Rom,
                        Procesador = @Cpu,
                        FechaRecepcion = GETDATE()
                    WHERE ItemID = @ItemID";

                await db.ExecuteAsync(sql, new
                {
                    Estado = estadoFinal,
                    Serie = dto.SerieReal,
                    Envio = dto.CostoEnvio,
                    Reparacion = dto.CostoReparacion,
                    Condicion = dto.Condicion,
                    Bateria = dto.Bateria,
                    Ram = dto.Ram,
                    Rom = dto.Almacenamiento,
                    Cpu = dto.Procesador,
                    ItemID = dto.ItemID
                }, tx);

                string sqlCheck = @"
                    UPDATE Compra
                    SET EstadoCompra = 'Recibida'
                    WHERE CompraID = @CompraID
                    AND NOT EXISTS (SELECT * FROM ItemInventario WHERE CompraID = @CompraID AND Estado NOT IN ('En Stock', 'Vendido', 'Por Revisar'))";

                await db.ExecuteAsync(sqlCheck, new { CompraID = dto.CompraID }, tx);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task<(string Nombre, string Categoria)> GetItemInfoAsync(int itemId)
        {
            using var db = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT p.Nombre, ISNULL(c.Nombre, '') AS Categoria
                FROM ItemInventario i
                INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                LEFT JOIN Categoria c ON p.CategoriaID = c.CategoriaID
                WHERE i.ItemID = @ItemID";

            return await db.QueryFirstOrDefaultAsync<(string Nombre, string Categoria)>(sql, new { ItemID = itemId });
        }
    }
}

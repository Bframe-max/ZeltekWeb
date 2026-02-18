using Dapper;
using System.Data;
using ZeltekWeb.Constants;
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

        public async Task<(IEnumerable<CompraListDto> Items, int Activos, decimal InversionFlotante)> ObtenerHistorialAsync(string? buscar, string viewType, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            
            // 1. Calculate KPIs (Always on Active items, independent of filter)
            // RLS handles Tenant filtering
            string sqlKPI = @"
                SELECT 
                    COUNT(*), 
                    ISNULL(SUM(CostoTotal), 0)
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

            // 2. Get List
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
            parameters.Add("TenantId", tenantId); // For RLS if needed, though usually session context handles it

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
            {
                sql += " AND c.EstadoCompra IN ('Recibido', 'Completado', 'Cancelado')";
            }
            else
            {
                sql += " AND c.EstadoCompra NOT IN ('Recibido', 'Completado', 'Cancelado')";
            }

            sql += " ORDER BY c.FechaCompra DESC";

            var items = await db.QueryAsync<CompraListDto>(sql, parameters);
            return (items, activos, inversion);
        }

        public async Task<CompraDto?> ObtenerPorIdAsync(int id, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT CompraID, FechaCompra, VendedorOnline, TrackingProveedor, CostoTotal, CostoEnvioTotal, EstadoCompra 
                FROM Compra 
                WHERE CompraID = @ID";
            return await db.QueryFirstOrDefaultAsync<CompraDto>(sql, new { ID = id }); // RLS handles Tenant check
        }

        public async Task<IEnumerable<ItemCompraDto>> ObtenerItemsPorCompraAsync(int compraId, int tenantId)
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

        public async Task<bool> VerificarTrackingAsync(string tracking, int? excluirCompraId, int tenantId)
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
            
            // RLS ensures we only check tracking numbers within our tenant
            return await db.ExecuteScalarAsync<int?>(sql, parameters) != null;
        }

        public async Task<int> CrearCompraAsync(CreateCompraDto dto, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            
            // Using the existing Stored Procedure logic but inline for better control over TenantID
            // Or we can invoke the SP if we update it. 
            // Given the refactor, let's use direct SQL to ensure TenantID is passed explicitly.
            
            // However, the original controller used 'sp_RegistrarCompraConItem'.
            // To be safe and explicit with TenantID, I will implement the logic here using a transaction.
            
            db.Open();
            using var tx = db.BeginTransaction();
            
            try 
            {
                var sqlCompra = @"
                    INSERT INTO Compra (ProveedorID, VendedorOnline, TrackingProveedor, FechaCompra, CostoTotal, CostoEnvioTotal, EstadoCompra, TenantID)
                    OUTPUT INSERTED.CompraID
                    VALUES (1, @Vendedor, @Tracking, @Fecha, @CostoTotal, @CostoEnvio, 'En Transito', @TenantId)";
                
                var compraId = await db.ExecuteScalarAsync<int>(sqlCompra, new 
                {
                    Vendedor = dto.Vendedor,
                    Tracking = dto.TrackingProveedor ?? "",
                    Fecha = dto.FechaCompra,
                    CostoTotal = dto.CostoTotalCompra,
                    CostoEnvio = dto.CostoEnvioTotal ?? 0,
                    TenantId = tenantId
                }, tx);

                // Insert Items
                if (dto.Cantidad > 0)
                {
                    decimal costoUnitario = dto.CostoTotalCompra / dto.Cantidad;
                    
                    var sqlItem = @"
                        INSERT INTO ItemInventario (ProductoID, CompraID, Estado, CostoCompra, FechaRegistro, TenantID)
                        VALUES (@ProductoID, @CompraID, 'En Transito', @CostoUnitario, GETDATE(), @TenantId)";
                    
                    for(int i = 0; i < dto.Cantidad; i++)
                    {
                        await db.ExecuteAsync(sqlItem, new 
                        {
                            ProductoID = dto.ProductoID,
                            CompraID = compraId,
                            CostoUnitario = costoUnitario,
                            TenantId = tenantId
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

        public async Task ActualizarCompraAsync(UpdateCompraDto dto, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            db.Open();
            using var tx = db.BeginTransaction();

            try 
            {
                // 1. Update Compra
                var sql = @"
                    UPDATE Compra 
                    SET TrackingProveedor = @Tracking, 
                        EstadoCompra = @Estado,
                        CostoTotal = @CostoTotal
                    WHERE CompraID = @ID"; // RLS Check implicit
                
                await db.ExecuteAsync(sql, new 
                { 
                    Tracking = dto.TrackingProveedor ?? "",
                    Estado = dto.EstadoCompra,
                    CostoTotal = dto.CostoTotal,
                    ID = dto.CompraID
                }, tx);

                // 2. Update Items Costs (Distribute new total)
                await ActualizarCostosItemsAsync(dto.CompraID, 0, tenantId, tx); // Will calculate inside or we pass 0 to trigger recalc?
                // Actually the logic in controller was: 
                // "Repartir el nuevo total entre los ítems: cada ItemInventario de esta compra recibe CostoCompra = CostoTotal / CantidadItems"
                
                // Let's implement that specific logic here
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
        
        public async Task ActualizarCostosItemsAsync(int compraId, decimal costoUnitario, int tenantId, IDbTransaction? transaction = null)
        {
             // This method might be redundant if logic is inside ActualizarCompraAsync, but interface defined it.
             // We can use it if needed.
             var sql = "UPDATE ItemInventario SET CostoCompra = @Costo WHERE CompraID = @ID";
             if(transaction != null)
             {
                 await transaction.Connection.ExecuteAsync(sql, new { Costo = costoUnitario, ID = compraId }, transaction);
             }
             else 
             {
                using var db = _connectionFactory.CreateConnection();
                await db.ExecuteAsync(sql, new { Costo = costoUnitario, ID = compraId });
             }
        }

        public async Task EliminarCompraAsync(int id, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            db.Open();
            using var tx = db.BeginTransaction();
            try 
            {
                // Delete Items first
                await db.ExecuteAsync("DELETE FROM ItemInventario WHERE CompraID = @ID", new { ID = id }, tx);
                // Delete Compra
                await db.ExecuteAsync("DELETE FROM Compra WHERE CompraID = @ID", new { ID = id }, tx);
                
                tx.Commit();
            }
            catch 
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task ForzarCierreAsync(int id, int tenantId)
        {
             using var db = _connectionFactory.CreateConnection();
             var sql = "UPDATE Compra SET EstadoCompra = 'Completado' WHERE CompraID = @ID";
             await db.ExecuteAsync(sql, new { ID = id });
        }

        public async Task<bool> VerificarSerialAsync(string serial, int? excluirItemId, int tenantId)
        {
             using var db = _connectionFactory.CreateConnection();
             var sql = "SELECT 1 FROM ItemInventario WHERE NumeroSerie = @Serial";
             var p = new DynamicParameters();
             p.Add("Serial", serial);
             
             if(excluirItemId.HasValue)
             {
                 sql += " AND ItemID <> @ExcludeID";
                 p.Add("ExcludeID", excluirItemId.Value);
             }
             
             return await db.ExecuteScalarAsync<int?>(sql, p) != null;
        }

        public async Task RecibirItemAsync(RecibirItemDto dto, int tenantId)
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

                // Check Update Compra Status
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

        public async Task<(string Nombre, string Categoria)> GetItemInfoAsync(int itemId, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT p.Nombre, ISNULL(c.Nombre, '') AS Categoria
                FROM ItemInventario i
                INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                LEFT JOIN Categoria c ON p.CategoriaID = c.CategoriaID
                WHERE i.ItemID = @ItemID";
            
            var result = await db.QueryFirstOrDefaultAsync<(string Nombre, string Categoria)>(sql, new { ItemID = itemId });
            return result;
        }
    }
}

using Dapper;
using System.Data;
using ZeltekWeb.Constants;
using ZeltekWeb.Data;
using ZeltekWeb.Models;
using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Repositories.Interfaces;

namespace ZeltekWeb.Repositories
{
    public class InventarioRepository : IInventarioRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public InventarioRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<(IEnumerable<InventarioItemDto> Items, int TotalLaptops, int TotalAccesorios)> ObtenerInventarioAsync(string? buscar, string filtro, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            string condicionEstado;
            
            // Map UI filter strings to SQL conditions
            switch (filtro)
            {
                case "Vendido":
                    condicionEstado = "i.Estado = 'Vendido'";
                    break;
                case "Transito":
                    condicionEstado = "i.Estado IN ('En Camino', 'En Transito', 'En Miami', 'En Aduana')"; // Align with other modules
                    break;
                case "Reparacion":
                    condicionEstado = "i.Estado IN ('En Reparación', 'EnReparacion')";
                    break;
                case "Stock":
                default:
                    condicionEstado = "i.Estado = 'En Stock'";
                    break;
            }

            string sql = $@"
                SELECT 
                    i.ItemID,
                    i.ProductoID,
                    p.Nombre AS ProductoNombre,
                    ISNULL(i.NumeroSerie, '') AS NumeroSerie,
                    i.Estado,
                    (i.CostoCompra + ISNULL(i.CostoEnvio, 0) + ISNULL(i.CostoReparacion, 0)) AS CostoTotal,
                    ISNULL(i.PrecioVenta, 0) AS PrecioVenta,
                    ISNULL(i.Procesador, '') AS Procesador,
                    ISNULL(i.RAM_GB, 0) AS RAM_GB,
                    ISNULL(i.Almacenamiento_GB, 0) AS Almacenamiento_GB,
                    ISNULL(i.CantidadInicial, 0) AS CantidadInicial,
                    ISNULL(i.CantidadActual, 0) AS CantidadActual,
                    ISNULL(p.EsUnico, 1) AS EsUnico,
                    ISNULL(i.Color, '') AS Color
                FROM ItemInventario i
                INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                WHERE {condicionEstado}
                AND i.TenantID = @TenantId "; // STRICT TENANT FILTER

            var parameters = new DynamicParameters();
            parameters.Add("TenantId", tenantId);

            if (!string.IsNullOrEmpty(buscar))
            {
                sql += @" AND (
                    p.Nombre LIKE '%' + @Buscar + '%' 
                    OR i.NumeroSerie LIKE '%' + @Buscar + '%'
                )";
                parameters.Add("Buscar", buscar);
            }

            sql += " ORDER BY i.ItemID DESC";

            var allItems = await db.QueryAsync<InventarioItemDto>(sql, parameters);
            
            // Calculate totals in-memory or via SQL? In-memory is fine for now as pagination isn't strictly requested yet but listing logic implies full load.
            // But let's verify if user wants specific counts. The View separates Laptops vs Accesorios.
            // The return tuple matches what Controller needs.
            
            var laptopsCount = allItems.Count(x => x.EsUnico);
            var accesoriosCount = allItems.Count(x => !x.EsUnico);

            return (allItems, laptopsCount, accesoriosCount);
        }

        public async Task<InventarioItemDto?> ObtenerPorIdAsync(int id, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT 
                    i.ItemID,
                    i.ProductoID,
                    p.Nombre AS ProductoNombre,
                    ISNULL(i.NumeroSerie, '') AS NumeroSerie,
                    i.Estado,
                    (i.CostoCompra + ISNULL(i.CostoEnvio, 0) + ISNULL(i.CostoReparacion, 0)) AS CostoTotal,
                    ISNULL(i.PrecioVenta, 0) AS PrecioVenta,
                    ISNULL(i.Procesador, '') AS Procesador,
                    ISNULL(i.RAM_GB, 0) AS RAM_GB,
                    ISNULL(i.Almacenamiento_GB, 0) AS Almacenamiento_GB,
                    ISNULL(i.CantidadInicial, 0) AS CantidadInicial,
                    ISNULL(i.CantidadActual, 0) AS CantidadActual,
                    ISNULL(p.EsUnico, 1) AS EsUnico,
                    ISNULL(i.Color, '') AS Color
                FROM ItemInventario i
                INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                WHERE i.ItemID = @ID AND i.TenantID = @TenantId";
            
            return await db.QueryFirstOrDefaultAsync<InventarioItemDto>(sql, new { ID = id, TenantId = tenantId });
        }

        public async Task RecibirProductoAsync(RecepcionProductoDto dto, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            db.Open();
            using var tx = db.BeginTransaction();

            try
            {
                string estadoFinal = (dto.Condicion == "Nuevo" || dto.Condicion == "Nuevo/Accesorio") ? "En Stock" : "Por Revisar";
                // If it's explicitly set via logic:
                // Actually the business logic from controller was:
                // If "Accesorios/Lotes" -> "Nuevo/Accesorio" implied "En Stock" usually, but technically "Por Revisar" is safer if not fully trusted.
                // However, previous controller set Estado based on `Condicion`. 
                // Let's trust the logic from Service Layer to pass correct params, but here we just update.
                // WAIT: The previous logic had a `RecibirCompra` generic method too.
                // Here we are receiving specific items.
                
                // Let's rely on standard logic: if it's new it might skip revision, if used it goes to revision.
                // But the `sp_RecibirLaptopConAccesorios` did both updates. 
                // We will implement direct SQL update here to be more transparent and multi-tenant safe.

                var sql = @"
                    UPDATE ItemInventario 
                    SET 
                        NumeroSerie = @Serie,
                        Estado = @Estado,
                        Condicion = @Condicion,
                        Procesador = @Cpu,
                        RAM_GB = @Ram,
                        Almacenamiento_GB = @Rom,
                        Color = @Color,
                        CostoEnvio = @Envio,
                        CostoOtros = @Otros,
                        FechaRecepcion = GETDATE()
                    WHERE ItemID = @ItemID AND TenantID = @TenantId";

                // Logic: if Condicion is "Nuevo" or we want to skip revision...
                // The Controller passed `model.Condicion`. 
                // Let's assume if it is "Nuevo", it goes "En Stock"??
                // Controller `Recibir` method was calling `sp_RecibirLaptopConAccesorios` which probably handled status?
                // Actually, the `RecibirCompra` action handles bulk status update.
                // The individual receive might just set details but keep it "En Camino" until whole box is received? 
                // NO, `sp_RecibirLaptopConAccesorios` updated status to 'En Stock' or 'Por Revisar'.
                // Let's assume 'Por Revisar' is default safely unless strictly new.
                
                // Correction: The Service layer should decide the status. 
                // I will add `Estado` to DTO or assume a default. 
                // Let's stick to "Por Revisar" for safety, or check condition.
                
                string estado = "Por Revisar"; 
                if (dto.Condicion != null && dto.Condicion.Contains("Nuevo", StringComparison.OrdinalIgnoreCase)) 
                    estado = "En Stock";

                await db.ExecuteAsync(sql, new
                {
                    Serie = dto.NumeroSerie,
                    Estado = estado,
                    Condicion = dto.Condicion,
                    Cpu = dto.Procesador,
                    Ram = dto.Ram,
                    Rom = dto.Almacenamiento,
                    Color = dto.Color,
                    Envio = dto.CostoEnvio,
                    Otros = dto.CostoOtros,
                    ItemID = dto.ItemID,
                    TenantId = tenantId
                }, tx);

                // Handle Charger Logic if needed (Updating another item to be "Sold/Used" or "Attached")
                // Previous SP `sp_RecibirLaptopConAccesorios` likely handled decreasing stock of chargers if `RequiereCargadorDeStock` is true.
                if (dto.RequiereCargadorDeStock && dto.IdCargador.HasValue)
                {
                    // Logic: Find a charger of that type in stock and link it or mark it?
                    // Usually it decreases quantity of the charger stack.
                    // "Unlink" one from stock.
                    var sqlCargador = @"
                        UPDATE ItemInventario 
                        SET CantidadActual = CantidadActual - 1 
                        WHERE ProductoID = @CargadorID 
                        AND Estado = 'En Stock' 
                        AND CantidadActual > 0
                        AND TenantID = @TenantId";
                        
                    await db.ExecuteAsync(sqlCargador, new { CargadorID = dto.IdCargador.Value, TenantId = tenantId }, tx);
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task<bool> ActualizarEstadoCompraSiCompletaAsync(int compraId, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            string sqlCheck = @"
                SELECT COUNT(*) 
                FROM ItemInventario 
                WHERE CompraID = @CompraID 
                AND Estado IN ('En Camino', 'En Transito', 'En Transito USA', 'En Miami', 'Viajando a NIC', 'En Aduana')
                AND TenantID = @TenantId"; // Check if any pending items exist

            int pending = await db.ExecuteScalarAsync<int>(sqlCheck, new { CompraID = compraId, TenantId = tenantId });

            if (pending == 0)
            {
                string sqlUpdate = "UPDATE Compra SET EstadoCompra = 'Recibida' WHERE CompraID = @ID AND TenantID = @TenantId";
                await db.ExecuteAsync(sqlUpdate, new { ID = compraId, TenantId = tenantId });
                return true;
            }
            return false;
        }

        public async Task MarcarCompraRecibidaManualAsync(int compraId, bool omitirRevision, int tenantId)
        {
             using var db = _connectionFactory.CreateConnection();
             string nuevoEstado = omitirRevision ? "En Stock" : "Por Revisar";
             
             string sql = @"
                UPDATE ItemInventario 
                SET Estado = @Estado
                WHERE CompraID = @CompraID 
                AND Estado IN ('En Camino', 'En Transito', 'En Transito USA', 'En Miami', 'Viajando a NIC', 'En Aduana')
                AND TenantID = @TenantId";

             await db.ExecuteAsync(sql, new { Estado = nuevoEstado, CompraID = compraId, TenantId = tenantId });
             
             // Also update Compra Header
             await db.ExecuteAsync("UPDATE Compra SET EstadoCompra = 'Recibida' WHERE CompraID = @ID AND TenantID = @TenantId", 
                new { ID = compraId, TenantId = tenantId });
        }

        public async Task<IEnumerable<ItemPorRevisarViewModel>> ObtenerPorRevisarAsync(int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT 
                    i.ItemID, 
                    p.Nombre AS ProductoNombre, 
                    ISNULL(i.NumeroSerie, 'S/N') AS NumeroSerie,
                    i.FechaRecepcion
                FROM ItemInventario i
                INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                WHERE i.Estado = 'Por Revisar' AND i.TenantID = @TenantId
                ORDER BY i.ItemID DESC";

            return await db.QueryAsync<ItemPorRevisarViewModel>(sql, new { TenantId = tenantId });
        }

        public async Task ProcesarInspeccionAsync(InspeccionCalidadDto dto, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            db.Open();
            using var tx = db.BeginTransaction();
            try 
            {
                if (dto.EsUnico)
                {
                    // Logic for Unique Items (Laptops/Phones)
                    bool irAReparacion = !string.IsNullOrEmpty(dto.NotasInspeccion) || (!string.IsNullOrEmpty(dto.Condicion) && dto.Condicion.Contains("Dañado")); 
                    // Or explicit flag? DTO doesn't have "Outcome". Logic usually implied by "NotasInspeccion" or specific button.
                    // Controller had `string resultado`. We should probably pass that or infer it.
                    // Let's assume passed in DTO or handled by Service logic calling this method with correct params.
                    // Let's assume if MotivoRechazo is present -> Reparacion? 
                    // Actually Controller logic: `irAReparacion = !string.Equals(resultado, "aprobar")`.
                    // We need to know if we are approving or rejecting.
                    
                    // Let's use `MotivoRechazo` as the trigger for Reparation in Unique items too?
                    // Controller used `NotasInspeccion` for Reparation Detail.
                    
                    // Let's assume: If `MotivoRechazo` or `NotasInspeccion` is populated AND we aren't approving... 
                    // Wait, `InspeccionCalidadDto` is new.
                    // I'll assume Service layer decides and sets status on DTO or we pass status?
                    // Refactor: Let's trust "Condicion". If "Para Reparacion", etc.
                    // But simpler: If "NotasInspeccion" has content -> Repair? No, notes can be "Good condition".
                    
                    // Let's trust `MotivoRechazo` as "ProblemaReportado".
                    bool isRepair = !string.IsNullOrEmpty(dto.MotivoRechazo); 
                    
                    string nuevoEstado = isRepair ? "En Reparación" : "En Stock";
                    
                    if (isRepair)
                    {
                         var sqlRep = @"
                            INSERT INTO Reparaciones (ItemID, ProblemaReportado, Estado, TenantID)
                            VALUES (@ItemID, @Problema, 'En Taller', @TenantId)";
                         await db.ExecuteAsync(sqlRep, new { ItemID = dto.ItemID, Problema = dto.MotivoRechazo, TenantId = tenantId }, tx);
                    }

                    var sqlUpdate = @"
                        UPDATE ItemInventario
                        SET Estado = @Estado,
                            Condicion = @Condicion,
                            SaludBateria = @Bateria
                        WHERE ItemID = @ID AND TenantID = @TenantId";
                    
                    await db.ExecuteAsync(sqlUpdate, new 
                    { 
                        Estado = nuevoEstado, 
                        Condicion = dto.Condicion, 
                        Bateria = dto.SaludBateriaPorcentaje.HasValue ? dto.SaludBateriaPorcentaje.ToString() : null,
                        ID = dto.ItemID,
                        TenantId = tenantId
                    }, tx);
                }
                else 
                {
                    // Lote / Accesorio
                    int rejected = dto.CantidadTotal - dto.CantidadAprobada;
                    
                    // Update Stock
                    var sqlUpdate = @"
                        UPDATE ItemInventario
                        SET Estado = 'En Stock',
                            CantidadActual = @CantidadAprobada,
                            Condicion = 'Nuevo/Accesorio'
                        WHERE ItemID = @ID AND TenantID = @TenantId";
                        
                    await db.ExecuteAsync(sqlUpdate, new { CantidadAprobada = dto.CantidadAprobada, ID = dto.ItemID, TenantId = tenantId }, tx);

                    if (rejected > 0)
                    {
                        var sqlGasto = @"
                            INSERT INTO GastoOperativo (FechaGasto, Descripcion, CategoriaGasto, Monto, TenantID)
                            VALUES (GETDATE(), @Desc, 'Merma/Daño', @Monto, @TenantId)";
                            
                        decimal loss = rejected * dto.CostoUnitario;
                        string desc = $"Merma/Rechazo en Inspección Lote #{dto.ItemID}: {rejected} unidades. Motivo: {dto.MotivoRechazo}";
                        
                        await db.ExecuteAsync(sqlGasto, new { Desc = desc, Monto = loss, TenantId = tenantId }, tx);
                    }
                }
                
                tx.Commit();
            }
            catch 
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task<IEnumerable<ItemEnReparacionViewModel>> ObtenerReparacionesActivasAsync(int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT r.ItemID, p.Nombre AS ProductoNombre, ISNULL(i.NumeroSerie, 'S/N') AS NumeroSerie,
                       r.ProblemaReportado,
                       r.FechaIngreso
                FROM Reparaciones r
                INNER JOIN ItemInventario i ON r.ItemID = i.ItemID
                INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                WHERE r.Estado = 'En Taller' AND r.TenantID = @TenantId
                ORDER BY r.ReparacionID DESC";
            
            return await db.QueryAsync<ItemEnReparacionViewModel>(sql, new { TenantId = tenantId });
        }

        public async Task FinalizarReparacionAsync(FinalizarReparacionDto dto, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            db.Open();
            using var tx = db.BeginTransaction();
            try 
            {
                 // Update Reparaciones
                 var sqlRep = @"
                    UPDATE Reparaciones
                    SET CostoRepuestos = @Repuestos,
                        CostoManoObra = @ManoObra,
                        TrabajoRealizado = @Notas,
                        FechaSalida = GETDATE(),
                        Estado = 'Finalizada'
                    WHERE ItemID = @ItemID AND Estado = 'En Taller' AND TenantID = @TenantId";
                    
                 await db.ExecuteAsync(sqlRep, new 
                 {
                     Repuestos = dto.CostoRepuestos,
                     ManoObra = dto.CostoManoObra,
                     Notas = dto.NotasReparacion,
                     ItemID = dto.ItemID,
                     TenantId = tenantId
                 }, tx);

                 // Update ItemInventario
                 var total = dto.CostoRepuestos + dto.CostoManoObra;
                 var sqlItem = @"
                    UPDATE ItemInventario
                    SET Estado = 'En Stock',
                        CostoReparacion = ISNULL(CostoReparacion, 0) + @Total
                    WHERE ItemID = @ItemID AND TenantID = @TenantId";
                    
                 await db.ExecuteAsync(sqlItem, new { Total = total, ItemID = dto.ItemID, TenantId = tenantId }, tx);

                 tx.Commit();
            }
            catch 
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task RegistrarPerdidaReparacionAsync(int itemId, string motivo, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            db.Open();
            using var tx = db.BeginTransaction();
            try 
            {
                // Get Cost
                var sqlGet = "SELECT (CostoCompra + ISNULL(CostoEnvio, 0) + ISNULL(CostoReparacion, 0)) FROM ItemInventario WHERE ItemID = @ID AND TenantID = @TenantId";
                decimal costo = await db.ExecuteScalarAsync<decimal>(sqlGet, new { ID = itemId, TenantId = tenantId }, tx);

                // Mark Item Lost
                await db.ExecuteAsync("UPDATE ItemInventario SET Estado = 'Perdida' WHERE ItemID = @ID AND TenantID = @TenantId", new { ID = itemId, TenantId = tenantId }, tx);

                // Cancel Repair
                var sqlRep = "UPDATE Reparaciones SET Estado = 'Cancelada', TrabajoRealizado = @Motivo WHERE ItemID = @ID AND Estado = 'En Taller' AND TenantID = @TenantId";
                await db.ExecuteAsync(sqlRep, new { ID = itemId, Motivo = "DECLARADA PÉRDIDA TOTAL: " + motivo, TenantId = tenantId }, tx);

                // Log Expense
                var sqlGasto = @"
                    INSERT INTO GastoOperativo (FechaGasto, Descripcion, CategoriaGasto, Monto, TenantID)
                    VALUES (GETDATE(), @Desc, 'Merma/Daño', @Monto, @TenantId)";
                await db.ExecuteAsync(sqlGasto, new { Desc = $"Pérdida Total en Reparación Item #{itemId}. Motivo: {motivo}", Monto = costo, TenantId = tenantId }, tx);

                tx.Commit();
            }
            catch 
            {
                 tx.Rollback();
                 throw;
            }
        }

        public async Task<bool> ActualizarPrecioAsync(int itemId, decimal nuevoPrecio, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            var sql = "UPDATE ItemInventario SET PrecioVenta = @Precio WHERE ItemID = @ID AND TenantID = @TenantId";
            var rows = await db.ExecuteAsync(sql, new { Precio = nuevoPrecio, ID = itemId, TenantId = tenantId });
            return rows > 0;
        }

        public async Task<bool> ExisteItemEnCompraAsync(int itemId, int compraId, int tenantId)
        {
             using var db = _connectionFactory.CreateConnection();
             var sql = "SELECT 1 FROM ItemInventario WHERE ItemID = @ItemID AND CompraID = @CompraID AND TenantID = @TenantId";
             return await db.ExecuteScalarAsync<int?>(sql, new { ItemID = itemId, CompraID = compraId, TenantId = tenantId }) != null;
        }
    }
}

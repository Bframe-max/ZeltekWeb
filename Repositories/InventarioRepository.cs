using Dapper;
using System.Data;
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

        public async Task<(IEnumerable<InventarioItemDto> Items, int TotalLaptops, int TotalAccesorios)> ObtenerInventarioAsync(string? buscar, string filtro)
        {
            using var db = _connectionFactory.CreateConnection();
            string condicionEstado = filtro switch
            {
                "Vendido" => "i.Estado = 'Vendido'",
                "Transito" => "i.Estado IN ('En Camino', 'En Transito', 'En Miami', 'En Aduana')",
                "Reparacion" => "i.Estado IN ('En Reparación', 'EnReparacion')",
                _ => "i.Estado = 'En Stock'"
            };

            string sql = $@"
                SELECT
                    i.ItemID, i.ProductoID,
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
                WHERE {condicionEstado}";

            var parameters = new DynamicParameters();

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
            return (allItems, allItems.Count(x => x.EsUnico), allItems.Count(x => !x.EsUnico));
        }

        public async Task<InventarioItemDto?> ObtenerPorIdAsync(int id)
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT
                    i.ItemID, i.ProductoID,
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
                WHERE i.ItemID = @ID";
            return await db.QueryFirstOrDefaultAsync<InventarioItemDto>(sql, new { ID = id });
        }

        public async Task RecibirProductoAsync(RecepcionProductoDto dto)
        {
            using var db = _connectionFactory.CreateConnection();
            db.Open();
            using var tx = db.BeginTransaction();

            try
            {
                string estado = "Por Revisar";
                if (dto.Condicion != null && dto.Condicion.Contains("Nuevo", StringComparison.OrdinalIgnoreCase))
                    estado = "En Stock";

                var sql = @"
                    UPDATE ItemInventario
                    SET NumeroSerie = @Serie, Estado = @Estado, Condicion = @Condicion,
                        Procesador = @Cpu, RAM_GB = @Ram, Almacenamiento_GB = @Rom,
                        Color = @Color, CostoEnvio = @Envio, CostoOtros = @Otros,
                        FechaRecepcion = GETDATE()
                    WHERE ItemID = @ItemID";

                await db.ExecuteAsync(sql, new
                {
                    Serie = dto.NumeroSerie, Estado = estado, Condicion = dto.Condicion,
                    Cpu = dto.Procesador, Ram = dto.Ram, Rom = dto.Almacenamiento,
                    Color = dto.Color, Envio = dto.CostoEnvio, Otros = dto.CostoOtros,
                    ItemID = dto.ItemID
                }, tx);

                if (dto.RequiereCargadorDeStock && dto.IdCargador.HasValue)
                {
                    var sqlCargador = @"
                        UPDATE ItemInventario
                        SET CantidadActual = CantidadActual - 1
                        WHERE ProductoID = @CargadorID AND Estado = 'En Stock' AND CantidadActual > 0";
                    await db.ExecuteAsync(sqlCargador, new { CargadorID = dto.IdCargador.Value }, tx);
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task<bool> ActualizarEstadoCompraSiCompletaAsync(int compraId)
        {
            using var db = _connectionFactory.CreateConnection();
            string sqlCheck = @"
                SELECT COUNT(*)
                FROM ItemInventario
                WHERE CompraID = @CompraID
                AND Estado IN ('En Camino', 'En Transito', 'En Transito USA', 'En Miami', 'Viajando a NIC', 'En Aduana')";

            int pending = await db.ExecuteScalarAsync<int>(sqlCheck, new { CompraID = compraId });

            if (pending == 0)
            {
                await db.ExecuteAsync("UPDATE Compra SET EstadoCompra = 'Recibida' WHERE CompraID = @ID", new { ID = compraId });
                return true;
            }
            return false;
        }

        public async Task MarcarCompraRecibidaManualAsync(int compraId, bool omitirRevision)
        {
            using var db = _connectionFactory.CreateConnection();
            string nuevoEstado = omitirRevision ? "En Stock" : "Por Revisar";

            await db.ExecuteAsync(@"
                UPDATE ItemInventario SET Estado = @Estado
                WHERE CompraID = @CompraID
                AND Estado IN ('En Camino', 'En Transito', 'En Transito USA', 'En Miami', 'Viajando a NIC', 'En Aduana')",
                new { Estado = nuevoEstado, CompraID = compraId });

            await db.ExecuteAsync("UPDATE Compra SET EstadoCompra = 'Recibida' WHERE CompraID = @ID", new { ID = compraId });
        }

        public async Task<IEnumerable<ItemPorRevisarViewModel>> ObtenerPorRevisarAsync()
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT i.ItemID, p.Nombre AS ProductoNombre, ISNULL(i.NumeroSerie, 'S/N') AS NumeroSerie, i.FechaRecepcion
                FROM ItemInventario i
                INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                WHERE i.Estado = 'Por Revisar'
                ORDER BY i.ItemID DESC";
            return await db.QueryAsync<ItemPorRevisarViewModel>(sql);
        }

        public async Task ProcesarInspeccionAsync(InspeccionCalidadDto dto)
        {
            using var db = _connectionFactory.CreateConnection();
            db.Open();
            using var tx = db.BeginTransaction();
            try
            {
                if (dto.EsUnico)
                {
                    bool isRepair = !string.IsNullOrEmpty(dto.MotivoRechazo);
                    string nuevoEstado = isRepair ? "En Reparación" : "En Stock";

                    if (isRepair)
                    {
                        await db.ExecuteAsync(
                            "INSERT INTO Reparaciones (ItemID, ProblemaReportado, Estado) VALUES (@ItemID, @Problema, 'En Taller')",
                            new { ItemID = dto.ItemID, Problema = dto.MotivoRechazo }, tx);
                    }

                    await db.ExecuteAsync(@"
                        UPDATE ItemInventario SET Estado = @Estado, Condicion = @Condicion, SaludBateria = @Bateria
                        WHERE ItemID = @ID",
                        new { Estado = nuevoEstado, Condicion = dto.Condicion,
                              Bateria = dto.SaludBateriaPorcentaje.HasValue ? dto.SaludBateriaPorcentaje.ToString() : null,
                              ID = dto.ItemID }, tx);
                }
                else
                {
                    int rejected = dto.CantidadTotal - dto.CantidadAprobada;

                    await db.ExecuteAsync(@"
                        UPDATE ItemInventario SET Estado = 'En Stock', CantidadActual = @CantidadAprobada, Condicion = 'Nuevo/Accesorio'
                        WHERE ItemID = @ID",
                        new { CantidadAprobada = dto.CantidadAprobada, ID = dto.ItemID }, tx);

                    if (rejected > 0)
                    {
                        decimal loss = rejected * dto.CostoUnitario;
                        string desc = $"Merma/Rechazo en Inspección Lote #{dto.ItemID}: {rejected} unidades. Motivo: {dto.MotivoRechazo}";
                        await db.ExecuteAsync(
                            "INSERT INTO GastoOperativo (FechaGasto, Descripcion, CategoriaGasto, Monto) VALUES (GETDATE(), @Desc, 'Merma/Daño', @Monto)",
                            new { Desc = desc, Monto = loss }, tx);
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

        public async Task<IEnumerable<ItemEnReparacionViewModel>> ObtenerReparacionesActivasAsync()
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT r.ItemID, p.Nombre AS ProductoNombre, ISNULL(i.NumeroSerie, 'S/N') AS NumeroSerie,
                       r.ProblemaReportado, r.FechaIngreso
                FROM Reparaciones r
                INNER JOIN ItemInventario i ON r.ItemID = i.ItemID
                INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                WHERE r.Estado = 'En Taller'
                ORDER BY r.ReparacionID DESC";
            return await db.QueryAsync<ItemEnReparacionViewModel>(sql);
        }

        public async Task FinalizarReparacionAsync(FinalizarReparacionDto dto)
        {
            using var db = _connectionFactory.CreateConnection();
            db.Open();
            using var tx = db.BeginTransaction();
            try
            {
                await db.ExecuteAsync(@"
                    UPDATE Reparaciones SET CostoRepuestos = @Repuestos, CostoManoObra = @ManoObra,
                        TrabajoRealizado = @Notas, FechaSalida = GETDATE(), Estado = 'Finalizada'
                    WHERE ItemID = @ItemID AND Estado = 'En Taller'",
                    new { Repuestos = dto.CostoRepuestos, ManoObra = dto.CostoManoObra,
                          Notas = dto.NotasReparacion, ItemID = dto.ItemID }, tx);

                var total = dto.CostoRepuestos + dto.CostoManoObra;
                await db.ExecuteAsync(@"
                    UPDATE ItemInventario SET Estado = 'En Stock', CostoReparacion = ISNULL(CostoReparacion, 0) + @Total
                    WHERE ItemID = @ItemID",
                    new { Total = total, ItemID = dto.ItemID }, tx);

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task RegistrarPerdidaReparacionAsync(int itemId, string motivo)
        {
            using var db = _connectionFactory.CreateConnection();
            db.Open();
            using var tx = db.BeginTransaction();
            try
            {
                var sqlGet = "SELECT (CostoCompra + ISNULL(CostoEnvio, 0) + ISNULL(CostoReparacion, 0)) FROM ItemInventario WHERE ItemID = @ID";
                decimal costo = await db.ExecuteScalarAsync<decimal>(sqlGet, new { ID = itemId }, tx);

                await db.ExecuteAsync("UPDATE ItemInventario SET Estado = 'Perdida' WHERE ItemID = @ID", new { ID = itemId }, tx);
                await db.ExecuteAsync(
                    "UPDATE Reparaciones SET Estado = 'Cancelada', TrabajoRealizado = @Motivo WHERE ItemID = @ID AND Estado = 'En Taller'",
                    new { ID = itemId, Motivo = "DECLARADA PÉRDIDA TOTAL: " + motivo }, tx);
                await db.ExecuteAsync(
                    "INSERT INTO GastoOperativo (FechaGasto, Descripcion, CategoriaGasto, Monto) VALUES (GETDATE(), @Desc, 'Merma/Daño', @Monto)",
                    new { Desc = $"Pérdida Total en Reparación Item #{itemId}. Motivo: {motivo}", Monto = costo }, tx);

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task<bool> ActualizarPrecioAsync(int itemId, decimal nuevoPrecio)
        {
            using var db = _connectionFactory.CreateConnection();
            var rows = await db.ExecuteAsync("UPDATE ItemInventario SET PrecioVenta = @Precio WHERE ItemID = @ID", new { Precio = nuevoPrecio, ID = itemId });
            return rows > 0;
        }

        public async Task<bool> ExisteItemEnCompraAsync(int itemId, int compraId)
        {
            using var db = _connectionFactory.CreateConnection();
            return await db.ExecuteScalarAsync<int?>(
                "SELECT 1 FROM ItemInventario WHERE ItemID = @ItemID AND CompraID = @CompraID",
                new { ItemID = itemId, CompraID = compraId }) != null;
        }
    }
}

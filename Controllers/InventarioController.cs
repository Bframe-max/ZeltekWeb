using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ZeltekWeb.Models;
using ZeltekWeb.Repositories;
using System.Data;

namespace ZeltekWeb.Controllers
{
    [Authorize]
    public class InventarioController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IInventarioRepository _inventarioRepo;

        public InventarioController(IConfiguration config, IInventarioRepository inventarioRepo)
        {
            _config = config;
            _inventarioRepo = inventarioRepo;
        }

        /// <summary>
        /// Index con tabs: Laptops (EsUnico == true, 1 fila = 1 equipo) y Accesorios (EsUnico == false, con CantidadActual).
        /// </summary>
        public IActionResult Index(string buscar, string filtro = "Stock")
        {
            var lista = new List<ItemInventarioViewModel>();
            string cadena = _config.GetConnectionString("ZeltekDB");

            string condicionEstado = "i.Estado = 'En Stock'";
            if (filtro == "Vendido")
                condicionEstado = "i.Estado = 'Vendido'";
            else if (filtro == "Transito")
                condicionEstado = "i.Estado IN ('En Camino', 'En Transito', 'En Miami', 'En Aduana')";

            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();
                string sql = $@"
                    SELECT 
                        i.ItemID AS ItemID,
                        i.ProductoID AS ProductoID,
                        p.Nombre AS Nombre,
                        ISNULL(i.NumeroSerie, '') AS NumeroSerie,
                        i.Estado AS Estado,
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
                    AND (@Buscar IS NULL 
                         OR p.Nombre LIKE '%' + @Buscar + '%' 
                         OR i.NumeroSerie LIKE '%' + @Buscar + '%')
                    ORDER BY i.ItemID DESC";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Buscar", string.IsNullOrEmpty(buscar) ? DBNull.Value : buscar);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new ItemInventarioViewModel
                            {
                                ItemID = Convert.ToInt32(dr["ItemID"]),
                                ProductoID = Convert.ToInt32(dr["ProductoID"]),
                                ProductoNombre = dr["Nombre"].ToString(),
                                NumeroSerie = dr["NumeroSerie"] == DBNull.Value ? "S/N" : dr["NumeroSerie"].ToString(),
                                Estado = dr["Estado"].ToString(),
                                CostoTotal = Convert.ToDecimal(dr["CostoTotal"]),
                                PrecioVenta = Convert.ToDecimal(dr["PrecioVenta"]),
                                Procesador = dr["Procesador"] == DBNull.Value ? "" : dr["Procesador"].ToString(),
                                RAM_GB = Convert.ToInt32(dr["RAM_GB"]),
                                Almacenamiento_GB = Convert.ToInt32(dr["Almacenamiento_GB"]),
                                CantidadInicial = Convert.ToInt32(dr["CantidadInicial"]),
                                CantidadActual = Convert.ToInt32(dr["CantidadActual"]),
                                EsUnico = dr["EsUnico"] == DBNull.Value ? true : Convert.ToBoolean(dr["EsUnico"]),
                                Color = dr["Color"] == DBNull.Value ? "" : dr["Color"].ToString()
                            });
                        }
                    }
                }
            }

            var laptops = lista.Where(x => x.EsUnico).ToList();
            var accesorios = lista.Where(x => !x.EsUnico).ToList();
            var model = new InventarioIndexViewModel
            {
                Laptops = laptops,
                Accesorios = accesorios,
                Busqueda = buscar,
                FiltroActual = filtro
            };
            return View(model);
        }

        /// <summary>
        /// Recibe un producto (laptop) con opción de asignar cargador del stock. Usa sp_RecibirLaptopConAccesorios.
        /// </summary>
        /// <summary>
        /// Recibe un producto (laptop o accesorio) usando sp_RecibirLaptopConAccesorios.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Recibir(RecepcionProductoViewModel model)
        {
            if (model.ItemID <= 0 || model.CompraID <= 0)
                return BadRequest(new { success = false, message = "Datos inválidos" });

            string cadena = _config.GetConnectionString("ZeltekDB");
            int productoId;
            bool esUnico = true;
            using (var con = new SqlConnection(cadena))
            {
                con.Open();
                using (var cmd = new SqlCommand("SELECT i.ProductoID, i.CompraID, ISNULL(p.EsUnico, 1) FROM ItemInventario i INNER JOIN Producto p ON i.ProductoID = p.ProductoID WHERE i.ItemID = @ID", con))
                {
                    cmd.Parameters.AddWithValue("@ID", model.ItemID);
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read())
                            return NotFound(new { success = false, message = "Item no encontrado" });
                        productoId = dr.GetInt32(0);
                        var compraId = dr.GetInt32(1);
                        // Safe conversion: handle BIT (bool) or INT (0/1)
                        var valEsUnico = dr.GetValue(2);
                        esUnico = (valEsUnico is bool b) ? b : Convert.ToBoolean(valEsUnico);
                        if (compraId != model.CompraID)
                            return BadRequest(new { success = false, message = "El ítem no pertenece a esta compra" });
                    }
                }
            }

            var numeroSerie = model.NumeroSerie?.Trim() ?? "";
            var usuario = User.Identity?.Name ?? "System";

            // Lógica para Specs: Si es accesorio, enviar NULLs
            string? procesador = model.Procesador;
            int? ram = model.Ram;
            int? almacenamiento = model.Almacenamiento;
            string? color = model.Color;
            string condicion = model.Condicion;
            bool requiereCargador = model.RequiereCargadorDeStock;
            int? idCargador = model.IdCargador;

            if (!esUnico) // Accesorio / Lote
            {
                if (string.IsNullOrEmpty(numeroSerie))
                    numeroSerie = "ACC-" + model.ItemID + "-" + DateTime.Now.ToString("HHmm");
                
                condicion = "Nuevo/Accesorio";
                procesador = null;
                ram = null;
                almacenamiento = null;
                color = null; // SP espera null si no aplica
                requiereCargador = false;
                idCargador = null;
            }
            else // Laptop / Unico
            {
                 if (string.IsNullOrEmpty(numeroSerie))
                    numeroSerie = "SIN-SERIE-" + model.ItemID;
            }

            try 
            {
                await _inventarioRepo.RecibirProductoAsync(
                    model.ItemID,
                    model.CompraID,
                    productoId,
                    numeroSerie,
                    condicion ?? "",
                    procesador ?? "",
                    ram,
                    almacenamiento,
                    color,
                    model.CostoEnvio,
                    model.CostoOtros,
                    usuario,
                    requiereCargador,
                    requiereCargador ? idCargador : null);

                // Check final Compra state (Recibida)
                // El SP podría hacerlo, pero mantenemos esta logica auxiliar por seguridad
                using (var con = new SqlConnection(cadena))
                {
                    con.Open();
                    using (var cmd = new SqlCommand(@"UPDATE Compra SET EstadoCompra = 'Recibida' WHERE CompraID = @CompraID AND NOT EXISTS (SELECT 1 FROM ItemInventario WHERE CompraID = @CompraID AND Estado = 'En Camino')", con))
                    {
                        cmd.Parameters.AddWithValue("@CompraID", model.CompraID);
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Producto recibido correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error interno: " + ex.Message });
            }
        }

        /// <summary>
        /// Marca una compra como recibida.
        /// omitirRevision = true (producto nuevo): ítems pasan directo a 'En Stock'.
        /// omitirRevision = false (usado): ítems pasan a 'Por Revisar' (mesa de calidad).
        /// </summary>
        [HttpPost]
        public IActionResult RecibirCompra(int compraId, bool omitirRevision = false)
        {
            var nuevoEstado = omitirRevision ? "En Stock" : "Por Revisar";
            var cadena = _config.GetConnectionString("ZeltekDB");
            using (var con = new SqlConnection(cadena))
            {
                con.Open();
                var sql = @"
                    UPDATE ItemInventario
                    SET Estado = @Estado
                    WHERE CompraID = @CompraID
                      AND Estado IN ('En Camino', 'En Transito', 'En Transito USA', 'En Miami', 'Viajando a NIC', 'En Aduana')";
                using (var cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@CompraID", compraId);
                    cmd.Parameters.AddWithValue("@Estado", nuevoEstado);
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Index", "Compras");
        }

        /// <summary>
        /// Mesa de calidad: lista de ítems en estado "Por Revisar".
        /// </summary>
        public IActionResult Calidad()
        {
            var lista = new List<ItemPorRevisarViewModel>();
            var cadena = _config.GetConnectionString("ZeltekDB");
            using (var con = new SqlConnection(cadena))
            {
                con.Open();
                var sql = @"
                    SELECT i.ItemID, p.Nombre, ISNULL(i.NumeroSerie, 'S/N')
                    FROM ItemInventario i
                    INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                    WHERE i.Estado = 'Por Revisar'
                    ORDER BY i.ItemID DESC";
                using (var cmd = new SqlCommand(sql, con))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new ItemPorRevisarViewModel
                        {
                            ItemID = dr.GetInt32(0),
                            ProductoNombre = dr.GetString(1),
                            NumeroSerie = dr.GetString(2)
                        });
                    }
                }
            }
            return View(lista);
        }

        /// <summary>
        /// Formulario de inspección para un ítem (checklist y aprobar / enviar a reparación).
        /// </summary>
        [HttpGet]
        public IActionResult Inspeccion(int id)
        {
            var cadena = _config.GetConnectionString("ZeltekDB");
            using (var con = new SqlConnection(cadena))
            {
                con.Open();
                var sql = @"
                    SELECT 
                        i.ItemID, 
                        p.Nombre, 
                        ISNULL(i.NumeroSerie, 'S/N'),
                        ISNULL(p.EsUnico, 1),
                        ISNULL(i.CantidadActual, 0),
                        (i.CostoCompra + ISNULL(i.CostoEnvio, 0) + ISNULL(i.CostoReparacion, 0)) AS CostoTotal,
                        ISNULL(i.CantidadInicial, 1)
                    FROM ItemInventario i 
                    INNER JOIN Producto p ON i.ProductoID = p.ProductoID 
                    WHERE i.ItemID = @ID AND i.Estado = 'Por Revisar'";
                
                using (var cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read()) return NotFound();
                        
                        var valEsUnico = dr.GetValue(3);
                        var esUnico = (valEsUnico is bool b) ? b : Convert.ToBoolean(valEsUnico);
                        var cantidadActual = dr.GetInt32(4);
                        var costoTotal = dr.GetDecimal(5);
                        var cantidadInicial = dr.GetInt32(6); // Usada como divisor para costo unitario si fuera necesario, pero CostoTotal en ItemInventario suele ser TOTAL de la linea?
                                                              // En Zeltek, ItemInventario para lotes: CostoCompra es UNITARIO o TOTAL? 
                                                              // Generalmente CostoCompra es por la linea. Si compré 10 a $100 total, c/u $10.
                                                              // Asumamos que CostoTotal es el valor total del ItemInventario record.
                                                              // Costo Unitario = CostoTotal / CantidadInicial (o Actual si no ha mermado antes).
                                                              // UPDATE: En ComprasController, al crear item: CostoCompra = costo * cantidad.
                                                              // Entonces CostoUnitario = CostoTotal / CantidadInicial (o Actual).
                        
                        var cantidadParaCalculo = cantidadActual > 0 ? cantidadActual : 1;
                        var costoUnitario = costoTotal / cantidadParaCalculo;

                        var model = new InspeccionCalidadViewModel
                        {
                            ItemID = dr.GetInt32(0),
                            ProductoNombre = dr.GetString(1),
                            NumeroSerie = dr.GetString(2),
                            EsUnico = esUnico,
                            CantidadTotal = cantidadActual,
                            CantidadAprobada = cantidadActual, // Por defecto aprueba todo
                            CostoUnitario = costoUnitario
                        };
                        return View(model);
                    }
                }
            }
        }

        /// <summary>
        /// Guarda resultado de inspección: Aprobar → En Stock, o Enviar a Reparación → crea registro en Reparaciones y estado En Reparación.
        /// </summary>
        [HttpPost]
        public IActionResult Inspeccion(int id, string resultado, InspeccionCalidadViewModel model)
        {
            if (model.ItemID != id) return BadRequest();
            
            var cadena = _config.GetConnectionString("ZeltekDB");
            using (var con = new SqlConnection(cadena))
            {
                con.Open();

                if (model.EsUnico)
                {
                    // LÓGICA PARA ITEMS ÚNICOS (LAPTOPS/CELULARES)
                    var irAReparacion = !string.Equals(resultado, "aprobar", StringComparison.OrdinalIgnoreCase);

                    if (irAReparacion)
                    {
                        // Crear registro en tabla Reparaciones (estado 'En Taller') y actualizar ítem a En Reparación
                        var sqlInsert = @"
                            INSERT INTO Reparaciones (ItemID, ProblemaReportado, Estado)
                            VALUES (@ItemID, @Problema, 'En Taller')";
                        using (var cmd = new SqlCommand(sqlInsert, con))
                        {
                            cmd.Parameters.AddWithValue("@ItemID", id);
                            cmd.Parameters.AddWithValue("@Problema", string.IsNullOrWhiteSpace(model.NotasInspeccion) ? (object)DBNull.Value : model.NotasInspeccion.Trim());
                            cmd.ExecuteNonQuery();
                        }
                    }

                    var nuevoEstado = irAReparacion ? "En Reparación" : "En Stock";
                    var sql = @"
                        UPDATE ItemInventario
                        SET Estado = @Estado,
                            Condicion = @Condicion,
                            SaludBateria = @SaludBateria
                        WHERE ItemID = @ID AND Estado = 'Por Revisar'";
                    using (var cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@ID", id);
                        cmd.Parameters.AddWithValue("@Estado", nuevoEstado);
                        cmd.Parameters.AddWithValue("@Condicion", (object?)model.Condicion ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SaludBateria", model.SaludBateriaPorcentaje.HasValue ? model.SaludBateriaPorcentaje.Value.ToString() : (object)DBNull.Value);
                        if (cmd.ExecuteNonQuery() == 0) return NotFound();
                    }
                }
                else
                {
                    // LÓGICA PARA LOTES / ACCESORIOS
                    // 1. Calcular rechazados
                    int cantidadRechazada = model.CantidadTotal - model.CantidadAprobada;
                    if (cantidadRechazada < 0) cantidadRechazada = 0;

                    // 2. Actualizar stock con la cantidad aprobada y marcar En Stock
                    var sqlUpdate = @"
                        UPDATE ItemInventario
                        SET Estado = 'En Stock',
                            CantidadActual = @CantidadAprobada,
                            Condicion = 'Nuevo/Accesorio'
                        WHERE ItemID = @ID";
                    
                    using (var cmd = new SqlCommand(sqlUpdate, con))
                    {
                        cmd.Parameters.AddWithValue("@ID", id);
                        cmd.Parameters.AddWithValue("@CantidadAprobada", model.CantidadAprobada);
                        cmd.ExecuteNonQuery();
                    }

                    // 3. Si hay rechazos, registrar GastoOperativo (Pérdida)
                    if (cantidadRechazada > 0)
                    {
                        decimal montoPerdida = cantidadRechazada * model.CostoUnitario;
                        var descripcion = $"Merma/Rechazo en Inspección Lote #{id}: {cantidadRechazada} unidades. Motivo: {model.MotivoRechazo}";
                        
                        var sqlGasto = @"
                            INSERT INTO GastoOperativo (FechaGasto, Descripcion, CategoriaGasto, Monto)
                            VALUES (GETDATE(), @Desc, 'Merma/Daño', @Monto)";
                        
                        using (var cmdG = new SqlCommand(sqlGasto, con))
                        {
                            cmdG.Parameters.AddWithValue("@Desc", descripcion);
                            cmdG.Parameters.AddWithValue("@Monto", montoPerdida);
                            cmdG.ExecuteNonQuery();
                        }
                    }
                }
            }
            return RedirectToAction(nameof(Calidad));
        }

        /// <summary>
        /// Lista de reparaciones abiertas (Estado = 'En Taller') desde la tabla Reparaciones.
        /// </summary>
        public IActionResult Reparaciones()
        {
            var lista = new List<ItemEnReparacionViewModel>();
            var cadena = _config.GetConnectionString("ZeltekDB");
            using (var con = new SqlConnection(cadena))
            {
                con.Open();
                var sql = @"
                    SELECT r.ItemID, p.Nombre, ISNULL(i.NumeroSerie, 'S/N'),
                           r.ProblemaReportado,
                           CAST(NULL AS DATETIME) AS FechaIngreso
                    FROM Reparaciones r
                    INNER JOIN ItemInventario i ON r.ItemID = i.ItemID
                    INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                    WHERE r.Estado = 'En Taller'
                    ORDER BY r.ReparacionID DESC";
                using (var cmd = new SqlCommand(sql, con))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new ItemEnReparacionViewModel
                        {
                            ItemID = dr.GetInt32(0),
                            ProductoNombre = dr.GetString(1),
                            NumeroSerie = dr.GetString(2),
                            ProblemaReportado = dr.IsDBNull(3) ? null : dr.GetString(3),
                            FechaIngreso = dr.IsDBNull(4) ? null : dr.GetDateTime(4)
                        });
                    }
                }
            }
            return View(lista);
        }

        /// <summary>
        /// Finaliza la reparación: actualiza Reparaciones (solo CostoRepuestos, CostoManoObra, TrabajoRealizado, FechaSalida, Estado).
        /// CostoTotal es columna calculada en SQL, no se escribe. Luego actualiza el ítem: estado a En Stock y suma el costo de reparación.
        /// </summary>
        [HttpPost]
        public IActionResult FinalizarReparacion(int itemId, decimal costoRepuestos, decimal costoManoObra, string? notasReparacion)
        {
            var totalReparacion = costoRepuestos + costoManoObra;
            var cadena = _config.GetConnectionString("ZeltekDB");
            using (var con = new SqlConnection(cadena))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        // 1. Actualizar Reparaciones (NO tocar CostoTotal: es columna computada)
                        var sqlRep = @"
                            UPDATE Reparaciones
                            SET CostoRepuestos = @CostoRepuestos,
                                CostoManoObra = @CostoManoObra,
                                TrabajoRealizado = @Notas,
                                FechaSalida = GETDATE(),
                                Estado = 'Finalizada'
                            WHERE ItemID = @ItemID AND Estado = 'En Taller'";
                        using (var cmd = new SqlCommand(sqlRep, con, tx))
                        {
                            cmd.Parameters.AddWithValue("@ItemID", itemId);
                            cmd.Parameters.AddWithValue("@CostoRepuestos", costoRepuestos);
                            cmd.Parameters.AddWithValue("@CostoManoObra", costoManoObra);
                            cmd.Parameters.AddWithValue("@Notas", string.IsNullOrWhiteSpace(notasReparacion) ? (object)DBNull.Value : notasReparacion.Trim());
                            if (cmd.ExecuteNonQuery() == 0)
                            {
                                tx.Rollback();
                                return NotFound();
                            }
                        }

                        // 2. Buscar y actualizar el ítem: estado a En Stock y sumar costo de reparación al activo
                        // Acepta 'En Reparación' o 'EnReparacion' por si la BD usa uno u otro
                        var sqlItem = @"
                            UPDATE ItemInventario
                            SET Estado = 'En Stock',
                                CostoReparacion = ISNULL(CostoReparacion, 0) + @Total
                            WHERE ItemID = @ItemID
                                AND Estado IN ('En Reparación', 'EnReparacion')";
                        using (var cmd = new SqlCommand(sqlItem, con, tx))
                        {
                            cmd.Parameters.AddWithValue("@ItemID", itemId);
                            cmd.Parameters.AddWithValue("@Total", totalReparacion);
                            var rows = cmd.ExecuteNonQuery();
                            if (rows == 0)
                            {
                                tx.Rollback();
                                return NotFound();
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
            }
            return RedirectToAction(nameof(Reparaciones));
        }

        [HttpPost]
        public IActionResult DeclararPerdidaReparacion(int itemId, string motivo)
        {
            var cadena = _config.GetConnectionString("ZeltekDB");
            using (var con = new SqlConnection(cadena))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        // 1. Obtener Costo Total del ítem para registrar la pérdida
                        decimal costoTotalItem = 0;
                        string nombreProducto = "";
                        var sqlGet = "SELECT (CostoCompra + ISNULL(CostoEnvio, 0) + ISNULL(CostoReparacion, 0)), ProductoID FROM ItemInventario WHERE ItemID = @ID";
                        using(var cmdGet = new SqlCommand(sqlGet, con, tx))
                        {
                            cmdGet.Parameters.AddWithValue("@ID", itemId);
                            using(var dr = cmdGet.ExecuteReader())
                            {
                                if(dr.Read())
                                {
                                    costoTotalItem = dr.GetDecimal(0);
                                }
                                else {
                                    tx.Rollback();
                                    return NotFound();
                                }
                            }
                        }

                        // 2. Marcar ItemInventario como 'Perdida'
                        var sqlItem = "UPDATE ItemInventario SET Estado = 'Perdida' WHERE ItemID = @ID";
                        using (var cmd = new SqlCommand(sqlItem, con, tx))
                        {
                            cmd.Parameters.AddWithValue("@ID", itemId);
                            cmd.ExecuteNonQuery();
                        }

                        // 3. Cancelar la reparación
                        var sqlRep = "UPDATE Reparaciones SET Estado = 'Cancelada', TrabajoRealizado = @Motivo WHERE ItemID = @ID AND Estado = 'En Taller'";
                        using (var cmd = new SqlCommand(sqlRep, con, tx))
                        {
                            cmd.Parameters.AddWithValue("@ID", itemId);
                            cmd.Parameters.AddWithValue("@Motivo", "DECLARADA PÉRDIDA TOTAL: " + motivo);
                            cmd.ExecuteNonQuery();
                        }

                        // 4. Registrar Gasto
                        var sqlGasto = @"
                            INSERT INTO GastoOperativo (FechaGasto, Descripcion, CategoriaGasto, Monto)
                            VALUES (GETDATE(), @Desc, 'Merma/Daño', @Monto)";
                        using (var cmd = new SqlCommand(sqlGasto, con, tx))
                        {
                            cmd.Parameters.AddWithValue("@Desc", $"Pérdida Total en Reparación Item #{itemId}. Motivo: {motivo}");
                            cmd.Parameters.AddWithValue("@Monto", costoTotalItem);
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
            return RedirectToAction(nameof(Reparaciones));
        }
    }
}
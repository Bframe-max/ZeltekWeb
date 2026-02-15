using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using ZeltekWeb.Models;
using System.Data;

namespace ZeltekWeb.Controllers
{
    [Authorize]
    public class ComprasController : Controller
    {
        private readonly IConfiguration _config;

        public ComprasController(IConfiguration config)
        {
            _config = config;
        }

        // ==========================================
        // 1. LISTADO (Index)
        // ==========================================
        public IActionResult Index(string buscar)
        {
            var lista = new List<CompraListViewModel>();
            string cadena = _config.GetConnectionString("ZeltekDB");

            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();

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
                    WHERE (@Buscar IS NULL 
                           OR c.VendedorOnline LIKE '%' + @Buscar + '%' 
                           OR c.TrackingProveedor LIKE '%' + @Buscar + '%'
                           OR c.EstadoCompra LIKE '%' + @Buscar + '%')
                    ORDER BY c.FechaCompra DESC";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Buscar", string.IsNullOrEmpty(buscar) ? DBNull.Value : buscar);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new CompraListViewModel
                            {
                                CompraID = dr.GetInt32(0),
                                Fecha = dr.GetDateTime(1),
                                Vendedor = dr.IsDBNull(2) ? "-" : dr.GetString(2),
                                Tracking = dr.IsDBNull(3) ? "-" : dr.GetString(3),
                                Total = dr.GetDecimal(4),
                                Estado = dr.GetString(5),
                                CantidadItems = dr.GetInt32(6),
                                ItemsPendientes = dr.GetInt32(7),
                                ProductoPrincipal = dr.GetString(8)
                            });
                        }
                    }
                }
            }

            ViewData["BusquedaActual"] = buscar;
            return View(lista);
        }

        // ==========================================
        // API: VERIFICAR SI TRACKING YA EXISTE
        // ==========================================
        /// <summary>
        /// Retorna true si el tracking ya está registrado, false si está libre.
        /// </summary>
        [HttpGet]
        public IActionResult VerificarTracking(string tracking)
        {
            var t = tracking?.Trim() ?? "";
            if (string.IsNullOrEmpty(t))
            {
                return Json(false);
            }

            string cadena = _config.GetConnectionString("ZeltekDB");
            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();
                string sql = "SELECT 1 FROM Compra WHERE TrackingProveedor = @T";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@T", t);
                    var existe = cmd.ExecuteScalar() != null;
                    return Json(existe);
                }
            }
        }

        /// <summary>
        /// Devuelve si el ítem es accesorio/cargador (por categoría del producto) para ocultar detalles técnicos.
        /// </summary>
        [HttpGet]
        public IActionResult GetItemInfo(int itemId)
        {
            if (itemId <= 0) return Json(new { esAccesorio = false, productoNombre = "" });
            string cadena = _config.GetConnectionString("ZeltekDB");
            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();
                string sql = @"
                    SELECT p.Nombre, ISNULL(c.Nombre, '') AS Categoria
                    FROM ItemInventario i
                    INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                    LEFT JOIN Categoria c ON p.CategoriaID = c.CategoriaID
                    WHERE i.ItemID = @ItemID";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@ItemID", itemId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read())
                            return Json(new { esAccesorio = false, productoNombre = "" });
                        string nombre = dr.GetString(0);
                        string cat = dr.IsDBNull(1) ? "" : dr.GetString(1);
                        bool esAccesorio = cat.IndexOf("Cargador", StringComparison.OrdinalIgnoreCase) >= 0
                                        || cat.IndexOf("Accesorio", StringComparison.OrdinalIgnoreCase) >= 0;
                        return Json(new { esAccesorio, productoNombre = nombre });
                    }
                }
            }
        }

        /// <summary>
        /// Retorna true si el serial ya existe en ItemInventario (evitar duplicados).
        /// </summary>
        [HttpGet]
        public IActionResult VerificarSerial(string serial)
        {
            var s = serial?.Trim() ?? "";
            if (string.IsNullOrEmpty(s))
                return Json(false);
            string cadena = _config.GetConnectionString("ZeltekDB");
            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();
                string sql = "SELECT 1 FROM ItemInventario WHERE NumeroSerie = @S";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@S", s);
                    var existe = cmd.ExecuteScalar() != null;
                    return Json(existe);
                }
            }
        }

        // ==========================================
        // 2. CREAR (GET)
        // ==========================================
        public IActionResult Create()
        {
            CargarListaProductos();
            return View(new CompraCreateViewModel());
        }

        // ==========================================
        // 3. GUARDAR (POST)
        // ==========================================
        [HttpPost]
        public IActionResult Create(CompraCreateViewModel model)
        {
            // Validación explícita de lote (por si se omite validación en cliente)
            if (model.Cantidad <= 0)
            {
                ModelState.AddModelError("Cantidad", "La cantidad debe ser mayor a 0.");
            }
            if (model.CostoTotalCompra <= 0)
            {
                ModelState.AddModelError("CostoTotalCompra", "El costo total de la compra debe ser mayor a 0.");
            }
            var costoEnvio = model.CostoEnvioTotal ?? 0;
            if (costoEnvio < 0)
            {
                ModelState.AddModelError("CostoEnvioTotal", "El costo de envío no puede ser negativo.");
            }

            if (!ModelState.IsValid)
            {
                CargarListaProductos();
                return View(model);
            }

            var tracking = model.TrackingProveedor?.Trim() ?? "";
            if (!string.IsNullOrEmpty(tracking))
            {
                string cadenaCheck = _config.GetConnectionString("ZeltekDB");
                using (SqlConnection conCheck = new SqlConnection(cadenaCheck))
                {
                    conCheck.Open();
                    using (SqlCommand cmdCheck = new SqlCommand("SELECT 1 FROM Compra WHERE TrackingProveedor = @T", conCheck))
                    {
                        cmdCheck.Parameters.AddWithValue("@T", tracking);
                        if (cmdCheck.ExecuteScalar() != null)
                        {
                            ModelState.AddModelError("TrackingProveedor", "Este tracking ya fue registrado.");
                            CargarListaProductos();
                            return View(model);
                        }
                    }
                }
            }

            var trackingVal = model.TrackingProveedor?.Trim();
            var trackingParaBD = string.IsNullOrEmpty(trackingVal) ? "" : trackingVal;
            string cadena = _config.GetConnectionString("ZeltekDB");

            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();
                try
                {
                    using (SqlCommand cmd = new SqlCommand("sp_RegistrarCompraConItem", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ProveedorID", 1);
                        cmd.Parameters.AddWithValue("@VendedorOnline", model.Vendedor);
                        cmd.Parameters.AddWithValue("@TrackingProveedor", trackingParaBD);
                        cmd.Parameters.AddWithValue("@FechaCompra", model.FechaCompra);
                        cmd.Parameters.AddWithValue("@ProductoID", model.ProductoID);
                        cmd.Parameters.AddWithValue("@Cantidad", model.Cantidad);
                        cmd.Parameters.AddWithValue("@CostoTotalLote", model.CostoTotalCompra);
                        cmd.Parameters.AddWithValue("@CostoEnvioTotal", model.CostoEnvioTotal ?? 0);
                        cmd.ExecuteNonQuery();
                    }
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al registrar la compra: " + ex.Message);
                    CargarListaProductos();
                    return View(model);
                }
            }
        }

        // ==========================================
        // 4. VER DETALLE (Details)
        // ==========================================
        public IActionResult Details(int id)
        {
            var modelo = new CompraDetalleViewModel();
            modelo.CompraID = id;

            string cadena = _config.GetConnectionString("ZeltekDB");
            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();

                string sqlCabecera = "SELECT VendedorOnline, TrackingProveedor, FechaCompra FROM Compra WHERE CompraID = @ID";
                using (SqlCommand cmd = new SqlCommand(sqlCabecera, con))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            modelo.Vendedor = dr.GetString(0);
                            modelo.Tracking = dr.IsDBNull(1) ? "-" : dr.GetString(1);
                            modelo.Fecha = dr.GetDateTime(2);
                        }
                    }
                }

                    string sqlItems = @"
                    SELECT 
                        i.ItemID, 
                        p.Nombre, 
                        i.CostoCompra, 
                        i.Estado, 
                        i.NumeroSerie, 
                        ISNULL(p.EsUnico, 1),
                        i.Procesador,
                        i.RAM_GB,
                        i.Almacenamiento_GB
                    FROM ItemInventario i
                    INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                    WHERE i.CompraID = @ID";

                using (SqlCommand cmd = new SqlCommand(sqlItems, con))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            modelo.Items.Add(new ItemEnCompra
                            {
                                ItemID = dr.GetInt32(0),
                                ProductoNombre = dr.GetString(1),
                                Costo = dr.GetDecimal(2),
                                Estado = dr.GetString(3),
                                NumeroSerie = dr.IsDBNull(4) ? "-" : dr.GetString(4),
                                EsAccesorio = !dr.GetBoolean(5),
                                Procesador = dr.IsDBNull(6) ? null : dr.GetString(6),
                                Ram = dr.IsDBNull(7) ? null : dr.GetInt32(7),
                                Almacenamiento = dr.IsDBNull(8) ? null : dr.GetInt32(8)
                            });
                        }
                    }
                }

                var cargadores = new List<SelectListItem>();
                string sqlCarg = @"
                    SELECT p.ProductoID, p.Nombre FROM Producto p
                    LEFT JOIN Categoria c ON p.CategoriaID = c.CategoriaID
                    WHERE p.IsDeleted = 0 AND p.EsUnico = 0
                    AND (ISNULL(c.Nombre,'') LIKE '%Cargador%' OR ISNULL(c.Nombre,'') LIKE '%Accesorio%')
                    ORDER BY p.Nombre";
                using (SqlCommand cmdC = new SqlCommand(sqlCarg, con))
                using (SqlDataReader drC = cmdC.ExecuteReader())
                {
                    while (drC.Read())
                        cargadores.Add(new SelectListItem { Value = drC.GetInt32(0).ToString(), Text = drC.GetString(1) });
                }
                ViewBag.Cargadores = cargadores;
            }
            return View(modelo);
        }

        // ==========================================
        // 5. ACCIÓN: RECIBIR ITEM (CORREGIDO)
        // ==========================================
        [HttpPost]
        public IActionResult RecibirItem(
            int itemId,
            int compraIdRetorno,
            bool productoNuevo,
            string serieReal,
            decimal costoEnvio,
            decimal costoReparacion,
            string condicion,
            string bateria,
            int? ram,
            int? almacenamiento,
            string procesador
        )
        {
            string cadena = _config.GetConnectionString("ZeltekDB");

            // Determinar esAccesorio por categoría del producto (Cargador/Accesorio) — no por checkbox
            bool esAccesorio = false;
            using (SqlConnection conCat = new SqlConnection(cadena))
            {
                conCat.Open();
                string sqlCat = @"
                    SELECT ISNULL(c.Nombre, '') FROM ItemInventario i
                    INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                    LEFT JOIN Categoria c ON p.CategoriaID = c.CategoriaID
                    WHERE i.ItemID = @ItemID";
                using (SqlCommand cmdCat = new SqlCommand(sqlCat, conCat))
                {
                    cmdCat.Parameters.AddWithValue("@ItemID", itemId);
                    var catObj = cmdCat.ExecuteScalar();
                    var cat = (catObj?.ToString() ?? "");
                    esAccesorio = cat.IndexOf("Cargador", StringComparison.OrdinalIgnoreCase) >= 0
                               || cat.IndexOf("Accesorio", StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }

            var estadoFinal = productoNuevo ? "En Stock" : "Por Revisar";
            string serieFinal = (serieReal ?? "").Trim();

            if (esAccesorio)
            {
                if (string.IsNullOrEmpty(serieFinal))
                    serieFinal = "ACC-" + itemId + "-" + DateTime.Now.ToString("HHmm");
                condicion = "Nuevo/Accesorio";
                bateria = "N/A";
                ram = null;
                almacenamiento = null;
                procesador = null;
            }
            else
            {
                if (string.IsNullOrEmpty(serieFinal))
                    serieFinal = "SIN-SERIE-" + itemId;
                else
                {
                    // Validar serial duplicado (excluyendo el item actual)
                    using (SqlConnection conDup = new SqlConnection(cadena))
                    {
                        conDup.Open();
                        using (SqlCommand cmdDup = new SqlCommand("SELECT 1 FROM ItemInventario WHERE NumeroSerie = @S AND ItemID <> @CurrentID", conDup))
                        {
                            cmdDup.Parameters.AddWithValue("@S", serieFinal);
                            cmdDup.Parameters.AddWithValue("@CurrentID", itemId);
                            if (cmdDup.ExecuteScalar() != null)
                            {
                                TempData["ErrorRecibir"] = "⛔ Serial ya existe en inventario.";
                                return RedirectToAction("Details", new { id = compraIdRetorno });
                            }
                        }
                    }
                }

                // Validación Laptop/Celular: Batería 1-100, Almacenamiento > 0
                if (!string.IsNullOrWhiteSpace(bateria) && bateria.Trim() != "N/A")
                {
                    var batStr = bateria.Trim().TrimEnd('%');
                    if (int.TryParse(batStr, out int pct) && (pct <= 0 || pct > 100))
                    {
                        TempData["ErrorRecibir"] = "Porcentaje de batería debe ser mayor a 0 y menor o igual a 100.";
                        return RedirectToAction("Details", new { id = compraIdRetorno });
                    }
                }
                if (almacenamiento.HasValue && almacenamiento.Value <= 0)
                {
                    TempData["ErrorRecibir"] = "Almacenamiento debe ser mayor a 0.";
                    return RedirectToAction("Details", new { id = compraIdRetorno });
                }
            }


            // Detectar AJAX
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();

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

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Estado", estadoFinal);
                    cmd.Parameters.AddWithValue("@Serie", serieFinal);
                    cmd.Parameters.AddWithValue("@Envio", costoEnvio);
                    cmd.Parameters.AddWithValue("@Reparacion", costoReparacion);
                    cmd.Parameters.AddWithValue("@Condicion", condicion ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Bateria", bateria ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Ram", ram ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Rom", almacenamiento ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Cpu", procesador ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ItemID", itemId);

                    cmd.ExecuteNonQuery();
                }

                string sqlCheck = @"
                   UPDATE Compra 
                   SET EstadoCompra = 'Recibida' 
                   WHERE CompraID = @CompraID 
                   AND NOT EXISTS (SELECT * FROM ItemInventario WHERE CompraID = @CompraID AND Estado NOT IN ('En Stock', 'Vendido', 'Por Revisar'))";

                using (SqlCommand cmdCheck = new SqlCommand(sqlCheck, con))
                {
                    cmdCheck.Parameters.AddWithValue("@CompraID", compraIdRetorno);
                    cmdCheck.ExecuteNonQuery();
                }
            }

            if (isAjax)
            {
                return Json(new { success = true, message = "Item recibido correctamente." });
            }

            return RedirectToAction("Details", new { id = compraIdRetorno });
        }

        // ==========================================
        // 6. EDITAR SEGUIMIENTO (GET)
        // ==========================================
        public IActionResult Edit(int id)
        {
            var model = new CompraCreateViewModel();
            string cadena = _config.GetConnectionString("ZeltekDB");

            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();
                string sql = "SELECT VendedorOnline, TrackingProveedor, EstadoCompra, CostoTotal FROM Compra WHERE CompraID = @ID";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            ViewBag.CompraID = id;
                            model.Vendedor = dr.GetString(0);
                            model.TrackingProveedor = dr.IsDBNull(1) ? "" : dr.GetString(1);
                            ViewBag.EstadoActual = dr.GetString(2);
                            ViewBag.CostoTotal = dr.IsDBNull(3) ? 0m : dr.GetDecimal(3);
                        }
                    }
                }
            }
            return View(model);
        }

        // ==========================================
        // 7. GUARDAR EDICIÓN (POST)
        // ==========================================
        [HttpPost]
        public IActionResult Edit(int id, string? tracking, string estado, decimal costoTotal)
        {
            var trackingTrim = tracking?.Trim() ?? "";
            if (!string.IsNullOrEmpty(trackingTrim))
            {
                string cadenaCheck = _config.GetConnectionString("ZeltekDB");
                using (SqlConnection conCheck = new SqlConnection(cadenaCheck))
                {
                    conCheck.Open();
                    using (SqlCommand cmdCheck = new SqlCommand("SELECT 1 FROM Compra WHERE TrackingProveedor = @T AND CompraID <> @ID", conCheck))
                    {
                        cmdCheck.Parameters.AddWithValue("@T", trackingTrim);
                        cmdCheck.Parameters.AddWithValue("@ID", id);
                        if (cmdCheck.ExecuteScalar() != null)
                        {
                            var model = new CompraCreateViewModel
                            {
                                TrackingProveedor = trackingTrim,
                                Vendedor = ""
                            };
                            ViewBag.CompraID = id;
                            ViewBag.EstadoActual = estado;
                            ViewBag.CostoTotal = costoTotal;
                            ModelState.AddModelError("TrackingProveedor", "Este tracking ya fue registrado en otra compra.");
                            return View(model);
                        }
                    }
                }
            }

            string cadena = _config.GetConnectionString("ZeltekDB");
            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();

                // 1. Actualizar Compra (Tracking, Estado, CostoTotal)
                string sql = @"
                    UPDATE Compra 
                    SET TrackingProveedor = @Tracking, 
                        EstadoCompra = @Estado,
                        CostoTotal = @CostoTotal
                    WHERE CompraID = @ID";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Tracking", string.IsNullOrEmpty(trackingTrim) ? "" : trackingTrim);
                    cmd.Parameters.AddWithValue("@Estado", estado);
                    cmd.Parameters.AddWithValue("@CostoTotal", costoTotal);
                    cmd.Parameters.AddWithValue("@ID", id);
                    cmd.ExecuteNonQuery();
                }

                // 2. Repartir el nuevo total entre los ítems: cada ItemInventario de esta compra recibe CostoCompra = CostoTotal / CantidadItems
                int cantidadItems = 0;
                using (SqlCommand cmdCount = new SqlCommand("SELECT COUNT(*) FROM ItemInventario WHERE CompraID = @ID", con))
                {
                    cmdCount.Parameters.AddWithValue("@ID", id);
                    cantidadItems = Convert.ToInt32(cmdCount.ExecuteScalar());
                }
                if (cantidadItems > 0)
                {
                    decimal costoPorItem = costoTotal / cantidadItems;
                    using (SqlCommand cmdItems = new SqlCommand("UPDATE ItemInventario SET CostoCompra = @Costo WHERE CompraID = @ID", con))
                    {
                        cmdItems.Parameters.AddWithValue("@Costo", costoPorItem);
                        cmdItems.Parameters.AddWithValue("@ID", id);
                        cmdItems.ExecuteNonQuery();
                    }
                }
            }
            return RedirectToAction("Index");
        }

        // ==========================================
        // 8. ELIMINAR COMPRA (solo En Transito o Pagado)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Eliminar(int id)
        {
            string cadena = _config.GetConnectionString("ZeltekDB");
            string? estado = null;
            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT EstadoCompra FROM Compra WHERE CompraID = @ID", con))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    var obj = cmd.ExecuteScalar();
                    if (obj == null)
                    {
                        TempData["ErrorEliminar"] = "Compra no encontrada.";
                        return RedirectToAction("Index");
                    }
                    estado = obj.ToString();
                }
            }

            if (estado != "En Transito" && estado != "Pagado")
            {
                TempData["ErrorEliminar"] = "⛔ No se puede eliminar una compra ya procesada. Debes revertir el inventario primero.";
                return RedirectToAction("Index");
            }

            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();
                using (SqlTransaction tx = con.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmdItems = new SqlCommand("DELETE FROM ItemInventario WHERE CompraID = @ID", con, tx))
                        {
                            cmdItems.Parameters.AddWithValue("@ID", id);
                            cmdItems.ExecuteNonQuery();
                        }
                        using (SqlCommand cmdCompra = new SqlCommand("DELETE FROM Compra WHERE CompraID = @ID", con, tx))
                        {
                            cmdCompra.Parameters.AddWithValue("@ID", id);
                            cmdCompra.ExecuteNonQuery();
                        }
                        tx.Commit();
                        TempData["SuccessEliminar"] = "Compra eliminada correctamente.";
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        TempData["ErrorEliminar"] = "Error al eliminar: " + ex.Message;
                    }
                }
            }
            return RedirectToAction("Index");
        }

        private void CargarListaProductos()
        {
            var productos = new List<SelectListItem>();
            string cadena = _config.GetConnectionString("ZeltekDB");

            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();
                string sql = "SELECT ProductoID, Nombre FROM Producto ORDER BY Nombre";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            productos.Add(new SelectListItem
                            {
                                Value = dr.GetInt32(0).ToString(),
                                Text = dr.GetString(1)
                            });
                        }
                    }
                }
            }
            ViewBag.Productos = productos;
        }
    }
}
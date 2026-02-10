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
                                ProductoPrincipal = dr.GetString(7)
                            });
                        }
                    }
                }
            }

            ViewData["BusquedaActual"] = buscar;
            return View(lista);
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
            if (!ModelState.IsValid)
            {
                CargarListaProductos();
                return View(model);
            }

            string cadena = _config.GetConnectionString("ZeltekDB");

            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();
                using (SqlTransaction tx = con.BeginTransaction())
                {
                    try
                    {
                        string sqlCompra = @"
                            INSERT INTO Compra 
                            (ProveedorID, VendedorOnline, TrackingProveedor, FechaCompra, CostoTotal, EstadoCompra)
                            OUTPUT INSERTED.CompraID
                            VALUES 
                            (1, @Vendedor, @Tracking, @Fecha, @Total, 'En Transito')";

                        int compraId;
                        using (SqlCommand cmd = new SqlCommand(sqlCompra, con, tx))
                        {
                            cmd.Parameters.AddWithValue("@Vendedor", model.Vendedor);
                            cmd.Parameters.AddWithValue("@Tracking", model.TrackingProveedor);
                            cmd.Parameters.AddWithValue("@Fecha", model.FechaCompra);
                            cmd.Parameters.AddWithValue("@Total", model.Cantidad * model.CostoPorUnidad);

                            compraId = (int)cmd.ExecuteScalar();
                        }

                        string sqlItem = @"
                            INSERT INTO ItemInventario 
                            (ProductoID, CompraID, CostoCompra, PrecioVenta, Estado, NumeroSerie)
                            VALUES 
                            (@ProdID, @CompraID, @Costo, 0.00, 'En Camino', @SerieTemp)";

                        using (SqlCommand cmdItem = new SqlCommand(sqlItem, con, tx))
                        {
                            cmdItem.Parameters.AddWithValue("@ProdID", model.ProductoID);
                            cmdItem.Parameters.AddWithValue("@CompraID", compraId);
                            cmdItem.Parameters.AddWithValue("@Costo", model.CostoPorUnidad);

                            SqlParameter pSerie = cmdItem.Parameters.Add("@SerieTemp", SqlDbType.NVarChar);

                            for (int i = 1; i <= model.Cantidad; i++)
                            {
                                pSerie.Value = $"PEND-{model.TrackingProveedor}-{i}";
                                cmdItem.ExecuteNonQuery();
                            }
                        }

                        tx.Commit();
                        return RedirectToAction("Index");
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        ModelState.AddModelError("", "Error: " + ex.Message);
                        CargarListaProductos();
                        return View(model);
                    }
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
                    SELECT i.ItemID, p.Nombre, i.CostoCompra, i.Estado, i.NumeroSerie
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
                                NumeroSerie = dr.IsDBNull(4) ? "-" : dr.GetString(4)
                            });
                        }
                    }
                }
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
            bool esAccesorio,
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
            // Producto nuevo (checkbox marcado) → En Stock. Usado (desmarcado) → Por Revisar (mesa de calidad).
            var estadoFinal = productoNuevo ? "En Stock" : "Por Revisar";

            string cadena = _config.GetConnectionString("ZeltekDB");
            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();

                string serieFinal = serieReal;

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
                    if (string.IsNullOrEmpty(serieFinal)) serieFinal = "SIN-SERIE-" + itemId;
                }

                // FechaRecepcion: si la columna no existe en ItemInventario, quita esa línea o agrega: ALTER TABLE ItemInventario ADD FechaRecepcion DATETIME NULL
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
                   AND NOT EXISTS (SELECT * FROM ItemInventario WHERE CompraID = @CompraID AND Estado = 'En Camino')";

                using (SqlCommand cmdCheck = new SqlCommand(sqlCheck, con))
                {
                    cmdCheck.Parameters.AddWithValue("@CompraID", compraIdRetorno);
                    cmdCheck.ExecuteNonQuery();
                }
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
        public IActionResult Edit(int id, string tracking, string estado, decimal costoTotal)
        {
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
                    cmd.Parameters.AddWithValue("@Tracking", tracking ?? "");
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
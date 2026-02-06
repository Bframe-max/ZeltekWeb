using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using ZeltekWeb.Filters;
using ZeltekWeb.Models;

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

        // =========================
        // 📋 LISTA DE COMPRAS
        // =========================
        public IActionResult Index()
        {
            var lista = new List<CompraViewModel>();
            var cadena = _config.GetConnectionString("ZeltekDB");

            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();

                var sql = @"
                    SELECT 
                        c.CompraID,
                        p.Nombre AS Proveedor,
                        ISNULL(c.VendedorOnline, '') AS VendedorOnline,
                        c.FechaCompra,
                        ISNULL(c.CostoTotal, 0) AS CostoTotal,
                        ISNULL(c.TrackingProveedor, '') AS TrackingProveedor,
                        ISNULL(c.EstadoCompra, 'Creada') AS EstadoCompra
                    FROM Compra c
                    LEFT JOIN Proveedor p ON c.ProveedorID = p.ProveedorID
                    ORDER BY c.FechaCompra DESC";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new CompraViewModel
                        {
                            CompraID = dr.GetInt32(0),
                            Proveedor = dr.IsDBNull(1) ? "Sin proveedor" : dr.GetString(1),
                            VendedorOnline = dr.GetString(2),
                            FechaCompra = dr.GetDateTime(3),
                            CostoTotal = dr.GetDecimal(4),
                            TrackingProveedor = dr.GetString(5),
                            EstadoCompra = dr.GetString(6)
                        });
                    }
                }
            }

            return View(lista);
        }

        // =========================
        // 🆕 CREAR COMPRA (GET)
        // =========================
        public IActionResult Create()
        {
            ViewBag.Proveedores = ObtenerProveedores();

            return View(new CompraViewModel
            {
                FechaCompra = DateTime.Today
            });
        }

        // =========================
        // 💾 CREAR COMPRA (POST)
        // =========================
        [HttpPost]
        public IActionResult Create(CompraViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Proveedores = ObtenerProveedores();
                return View(model);
            }

            var cadena = _config.GetConnectionString("ZeltekDB");

            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();

                var sql = @"
                    INSERT INTO Compra
                    (ProveedorID, VendedorOnline, FechaCompra, TrackingProveedor, CostoTotal, EstadoCompra)
                    VALUES
                    (@ProveedorID, @VendedorOnline, @FechaCompra, @TrackingProveedor, 0, 'Creada')";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@ProveedorID", (object?)model.ProveedorID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@VendedorOnline", (object?)model.VendedorOnline ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FechaCompra", model.FechaCompra);
                    cmd.Parameters.AddWithValue("@TrackingProveedor", (object?)model.TrackingProveedor ?? DBNull.Value);

                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index");
        }

        // =========================
        // ✏️ EDITAR COMPRA (GET)
        // =========================
        public IActionResult Edit(int id)
        {
            var cadena = _config.GetConnectionString("ZeltekDB");

            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();

                var sql = @"
                    SELECT 
                        CompraID,
                        ProveedorID,
                        ISNULL(VendedorOnline, '') AS VendedorOnline,
                        FechaCompra,
                        ISNULL(TrackingProveedor, '') AS TrackingProveedor,
                        ISNULL(EstadoCompra, 'Creada') AS EstadoCompra
                    FROM Compra
                    WHERE CompraID = @ID";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@ID", id);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read())
                            return NotFound();

                        var model = new CompraViewModel
                        {
                            CompraID = dr.GetInt32(0),
                            ProveedorID = dr.IsDBNull(1) ? null : dr.GetInt32(1),
                            VendedorOnline = dr.GetString(2),
                            FechaCompra = dr.GetDateTime(3),
                            TrackingProveedor = dr.GetString(4),
                            EstadoCompra = dr.GetString(5)
                        };

                        ViewBag.Proveedores = ObtenerProveedores();
                        return View(model);
                    }
                }
            }
        }

        // =========================
        // 💾 EDITAR COMPRA (POST)
        // =========================
        [HttpPost]
        public IActionResult Edit(CompraViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Proveedores = ObtenerProveedores();
                return View(model);
            }

            var cadena = _config.GetConnectionString("ZeltekDB");

            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();

                var sql = @"
                    UPDATE Compra SET
                        ProveedorID = @ProveedorID,
                        VendedorOnline = @VendedorOnline,
                        FechaCompra = @FechaCompra,
                        TrackingProveedor = @TrackingProveedor
                    WHERE CompraID = @ID";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@ProveedorID", (object?)model.ProveedorID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@VendedorOnline", (object?)model.VendedorOnline ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FechaCompra", model.FechaCompra);
                    cmd.Parameters.AddWithValue("@TrackingProveedor", (object?)model.TrackingProveedor ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ID", model.CompraID);

                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index");
        }

        // =========================
        // 🔧 HELPERS
        // =========================
        private List<SelectListItem> ObtenerProveedores()
        {
            var lista = new List<SelectListItem>();
            var cadena = _config.GetConnectionString("ZeltekDB");

            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();

                var sql = "SELECT ProveedorID, Nombre FROM Proveedor ORDER BY Nombre";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new SelectListItem
                        {
                            Value = dr.GetInt32(0).ToString(),
                            Text = dr.GetString(1)
                        });
                    }
                }
            }

            return lista;
        }
    }
}

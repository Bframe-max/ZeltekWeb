using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ZeltekWeb.Filters;
using ZeltekWeb.Models;

namespace ZeltekWeb.Controllers
{
    [Authorize]
    public class ItemsController : BaseController
    {
        private readonly IConfiguration _config;

        public ItemsController(IConfiguration config)
        {
            _config = config;
        }

        // =========================
        // LISTA DE ITEMS POR COMPRA
        // =========================
        public IActionResult Index(int compraId)
        {
            var lista = new List<ItemListViewModel>();
            var cadena = _config.GetConnectionString("ZeltekDB");
            int tenantId = GetTenantId();

            using SqlConnection con = new SqlConnection(cadena);
            con.Open();

            // 🔒 Security: Added TenantID filter
            var sql = @"
                SELECT 
                    i.ItemID,
                    p.Nombre AS Producto,
                    i.NumeroSerie,
                    i.CostoCompra,
                    i.PrecioVenta,
                    i.Estado
                FROM ItemInventario i
                INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                WHERE i.CompraID = @CompraID AND i.TenantID = @TenantID
                ORDER BY i.ItemID DESC";

            using SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@CompraID", compraId);
            cmd.Parameters.AddWithValue("@TenantID", tenantId);

            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new ItemListViewModel
                {
                    ItemID = dr.GetInt32(0),
                    Producto = dr.GetString(1),
                    NumeroSerie = dr.IsDBNull(2) ? null : dr.GetString(2),
                    CostoCompra = dr.GetDecimal(3),
                    PrecioVenta = dr.GetDecimal(4),
                    Estado = dr.GetString(5)
                });
            }

            ViewBag.CompraID = compraId;
            return View(lista);
        }

        // =========================
        // FORMULARIO NUEVO ITEM
        // =========================
        public IActionResult Create(int compraId)
        {
            var productos = new List<ProductoSelectViewModel>();
            var cadena = _config.GetConnectionString("ZeltekDB");
            int tenantId = GetTenantId();

            using SqlConnection con = new SqlConnection(cadena);
            con.Open();

            // 🔒 Security: Added TenantID filter
            var sql = @"
                SELECT ProductoID, Nombre
                FROM Producto
                WHERE IsDeleted = 0 AND TenantID = @TenantID
                ORDER BY Nombre";

            using SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@TenantID", tenantId);
            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                productos.Add(new ProductoSelectViewModel
                {
                    ProductoID = dr.GetInt32(0),
                    Nombre = dr.GetString(1)
                });
            }

            ViewBag.Productos = productos;

            return View(new ItemViewModel
            {
                CompraID = compraId,
                Estado = "En Stock"
            });
        }

        // =========================
        // GUARDAR ITEM
        // =========================
        [HttpPost]
        public IActionResult Create(ItemViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var cadena = _config.GetConnectionString("ZeltekDB");
            int tenantId = GetTenantId();

            using SqlConnection con = new SqlConnection(cadena);
            con.Open();

            // 🔒 Validar serial duplicado (Tenant check implied by uniqueness?)
            // Ideally explicit tenant check or unique index per tenant.
            if (!string.IsNullOrWhiteSpace(model.NumeroSerie))
            {
                using var checkCmd = new SqlCommand(
                    "SELECT COUNT(1) FROM ItemInventario WHERE NumeroSerie = @NumeroSerie AND TenantID = @TenantID", con);

                checkCmd.Parameters.AddWithValue("@NumeroSerie", model.NumeroSerie);
                checkCmd.Parameters.AddWithValue("@TenantID", tenantId);

                if ((int)checkCmd.ExecuteScalar() > 0)
                {
                    ModelState.AddModelError("", "Ese número de serie ya existe.");
                    return View(model);
                }
            }

            // 🔒 Security: Ensure CompraID belongs to Tenant
            using var checkCompraCmd = new SqlCommand("SELECT COUNT(1) FROM Compra WHERE CompraID = @CompraID AND TenantID = @TenantID", con);
            checkCompraCmd.Parameters.AddWithValue("@CompraID", model.CompraID);
            checkCompraCmd.Parameters.AddWithValue("@TenantID", tenantId);
             
            if ((int)checkCompraCmd.ExecuteScalar() == 0)
            {
                 return Forbid(); // Or NotFound
            }

            // 1️⃣ Insertar Item
            var sqlInsert = @"
                INSERT INTO ItemInventario
                (
                    CompraID,
                    ProductoID,
                    NumeroSerie,
                    CostoCompra,
                    PrecioVenta,
                    Estado,
                    TenantID
                )
                VALUES
                (
                    @CompraID,
                    @ProductoID,
                    @NumeroSerie,
                    @CostoCompra,
                    @PrecioVenta,
                    @Estado,
                    @TenantID
                )";

            using SqlCommand cmd = new SqlCommand(sqlInsert, con);
            cmd.Parameters.AddWithValue("@CompraID", model.CompraID);
            cmd.Parameters.AddWithValue("@ProductoID", model.ProductoID);
            cmd.Parameters.AddWithValue("@NumeroSerie", (object?)model.NumeroSerie ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CostoCompra", model.CostoCompra);
            cmd.Parameters.AddWithValue("@PrecioVenta", model.PrecioVenta);
            cmd.Parameters.AddWithValue("@Estado", model.Estado);
            cmd.Parameters.AddWithValue("@TenantID", tenantId);
            cmd.ExecuteNonQuery();

            // 2️⃣ Recalcular total de la compra
            RecalcularTotalCompra(con, model.CompraID, tenantId);

            // 3️⃣ Cambiar estado a Confirmada si estaba Creada
            ConfirmarCompraSiTieneItems(con, model.CompraID, tenantId);

            return RedirectToAction("Index", new { compraId = model.CompraID });
        }

        // =========================
        // EDITAR ITEM (GET)
        // =========================
        public IActionResult Edit(int id)
        {
            var cadena = _config.GetConnectionString("ZeltekDB");
            int tenantId = GetTenantId();
            using SqlConnection con = new SqlConnection(cadena);
            con.Open();

            var sql = @"
                SELECT 
                    i.ItemID,
                    i.CompraID,
                    p.Nombre,
                    i.NumeroSerie,
                    i.CostoCompra,
                    i.PrecioVenta,
                    i.Estado
                FROM ItemInventario i
                INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                WHERE i.ItemID = @ItemID AND i.TenantID = @TenantID";

            using SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@ItemID", id);
            cmd.Parameters.AddWithValue("@TenantID", tenantId);

            using SqlDataReader dr = cmd.ExecuteReader();
            if (!dr.Read())
                return NotFound();

            var model = new ItemEditViewModel
            {
                ItemID = dr.GetInt32(0),
                CompraID = dr.GetInt32(1),
                Producto = dr.GetString(2),
                NumeroSerie = dr.IsDBNull(3) ? null : dr.GetString(3),
                CostoCompra = dr.GetDecimal(4),
                PrecioVenta = dr.GetDecimal(5),
                Estado = dr.GetString(6)
            };

            return View(model);
        }

        // =========================
        // EDITAR ITEM (POST)
        // =========================
        [HttpPost]
        public IActionResult Edit(ItemEditViewModel model)
        {
            var cadena = _config.GetConnectionString("ZeltekDB");
            int tenantId = GetTenantId();
            using SqlConnection con = new SqlConnection(cadena);
            con.Open();

            var sql = @"
                UPDATE ItemInventario
                SET
                    NumeroSerie = @NumeroSerie,
                    CostoCompra = @CostoCompra,
                    PrecioVenta = @PrecioVenta,
                    Estado = @Estado
                WHERE ItemID = @ItemID AND TenantID = @TenantID";

            using SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@NumeroSerie", (object?)model.NumeroSerie ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CostoCompra", model.CostoCompra);
            cmd.Parameters.AddWithValue("@PrecioVenta", model.PrecioVenta);
            cmd.Parameters.AddWithValue("@Estado", model.Estado);
            cmd.Parameters.AddWithValue("@ItemID", model.ItemID);
            cmd.Parameters.AddWithValue("@TenantID", tenantId);
            int rows = cmd.ExecuteNonQuery();
            
            if (rows == 0) return NotFound();

            // 🔄 Recalcular total por si cambió el costo
            RecalcularTotalCompra(con, model.CompraID, tenantId);

            return RedirectToAction("Index", new { compraId = model.CompraID });
        }

        // =========================
        // 🔧 HELPERS
        // =========================
        private void RecalcularTotalCompra(SqlConnection con, int compraId, int tenantId)
        {
            var sql = @"
                UPDATE Compra
                SET CostoTotal = (
                    SELECT ISNULL(SUM(CostoCompra), 0)
                    FROM ItemInventario
                    WHERE CompraID = @CompraID AND TenantID = @TenantID
                )
                WHERE CompraID = @CompraID AND TenantID = @TenantID";

            using SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@CompraID", compraId);
            cmd.Parameters.AddWithValue("@TenantID", tenantId);
            cmd.ExecuteNonQuery();
        }

        private void ConfirmarCompraSiTieneItems(SqlConnection con, int compraId, int tenantId)
        {
            var sql = @"
                UPDATE Compra
                SET EstadoCompra = 'Confirmada'
                WHERE CompraID = @CompraID
                  AND EstadoCompra = 'Creada'
                  AND TenantID = @TenantID";

            using SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@CompraID", compraId);
            cmd.Parameters.AddWithValue("@TenantID", tenantId);
            cmd.ExecuteNonQuery();
        }
    }
}

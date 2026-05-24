using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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

            using SqlConnection con = new SqlConnection(cadena);
            con.Open();

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
                WHERE i.CompraID = @CompraID
                ORDER BY i.ItemID DESC";

            using SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@CompraID", compraId);

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

            using SqlConnection con = new SqlConnection(cadena);
            con.Open();

            var sql = @"
                SELECT ProductoID, Nombre
                FROM Producto
                WHERE IsDeleted = 0
                ORDER BY Nombre";

            using SqlCommand cmd = new SqlCommand(sql, con);
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

            using SqlConnection con = new SqlConnection(cadena);
            con.Open();

            if (!string.IsNullOrWhiteSpace(model.NumeroSerie))
            {
                using var checkCmd = new SqlCommand(
                    "SELECT COUNT(1) FROM ItemInventario WHERE NumeroSerie = @NumeroSerie", con);

                checkCmd.Parameters.AddWithValue("@NumeroSerie", model.NumeroSerie);

                if ((int)checkCmd.ExecuteScalar() > 0)
                {
                    ModelState.AddModelError("", "Ese número de serie ya existe.");
                    return View(model);
                }
            }

            using var checkCompraCmd = new SqlCommand("SELECT COUNT(1) FROM Compra WHERE CompraID = @CompraID", con);
            checkCompraCmd.Parameters.AddWithValue("@CompraID", model.CompraID);

            if ((int)checkCompraCmd.ExecuteScalar() == 0)
                return NotFound();

            var sqlInsert = @"
                INSERT INTO ItemInventario
                (
                    CompraID,
                    ProductoID,
                    NumeroSerie,
                    CostoCompra,
                    PrecioVenta,
                    Estado
                )
                VALUES
                (
                    @CompraID,
                    @ProductoID,
                    @NumeroSerie,
                    @CostoCompra,
                    @PrecioVenta,
                    @Estado
                )";

            using SqlCommand cmd = new SqlCommand(sqlInsert, con);
            cmd.Parameters.AddWithValue("@CompraID", model.CompraID);
            cmd.Parameters.AddWithValue("@ProductoID", model.ProductoID);
            cmd.Parameters.AddWithValue("@NumeroSerie", (object?)model.NumeroSerie ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CostoCompra", model.CostoCompra);
            cmd.Parameters.AddWithValue("@PrecioVenta", model.PrecioVenta);
            cmd.Parameters.AddWithValue("@Estado", model.Estado);
            cmd.ExecuteNonQuery();

            RecalcularTotalCompra(con, model.CompraID);
            ConfirmarCompraSiTieneItems(con, model.CompraID);

            return RedirectToAction("Index", new { compraId = model.CompraID });
        }

        // =========================
        // EDITAR ITEM (GET)
        // =========================
        public IActionResult Edit(int id)
        {
            var cadena = _config.GetConnectionString("ZeltekDB");
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
                WHERE i.ItemID = @ItemID";

            using SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@ItemID", id);

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
            using SqlConnection con = new SqlConnection(cadena);
            con.Open();

            var sql = @"
                UPDATE ItemInventario
                SET
                    NumeroSerie = @NumeroSerie,
                    CostoCompra = @CostoCompra,
                    PrecioVenta = @PrecioVenta,
                    Estado = @Estado
                WHERE ItemID = @ItemID";

            using SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@NumeroSerie", (object?)model.NumeroSerie ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CostoCompra", model.CostoCompra);
            cmd.Parameters.AddWithValue("@PrecioVenta", model.PrecioVenta);
            cmd.Parameters.AddWithValue("@Estado", model.Estado);
            cmd.Parameters.AddWithValue("@ItemID", model.ItemID);
            int rows = cmd.ExecuteNonQuery();

            if (rows == 0) return NotFound();

            RecalcularTotalCompra(con, model.CompraID);

            return RedirectToAction("Index", new { compraId = model.CompraID });
        }

        // =========================
        // HELPERS
        // =========================
        private void RecalcularTotalCompra(SqlConnection con, int compraId)
        {
            var sql = @"
                UPDATE Compra
                SET CostoTotal = (
                    SELECT ISNULL(SUM(CostoCompra), 0)
                    FROM ItemInventario
                    WHERE CompraID = @CompraID
                )
                WHERE CompraID = @CompraID";

            using SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@CompraID", compraId);
            cmd.ExecuteNonQuery();
        }

        private void ConfirmarCompraSiTieneItems(SqlConnection con, int compraId)
        {
            var sql = @"
                UPDATE Compra
                SET EstadoCompra = 'Confirmada'
                WHERE CompraID = @CompraID
                  AND EstadoCompra = 'Creada'";

            using SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@CompraID", compraId);
            cmd.ExecuteNonQuery();
        }
    }
}

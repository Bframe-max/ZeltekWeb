using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ZeltekWeb.Filters;
using ZeltekWeb.Models;

namespace ZeltekWeb.Controllers
{
    [Authorize]
    public class InventarioController : Controller
    {
        private readonly IConfiguration _config;

        public InventarioController(IConfiguration config)
        {
            _config = config;
        }

        // =========================
        // LISTADO INVENTARIO
        // =========================
        public IActionResult Index()
        {
            var lista = new List<InventarioViewModel>();
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
                ORDER BY i.ItemID DESC";

            using SqlCommand cmd = new SqlCommand(sql, con);
            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new InventarioViewModel
                {
                    ItemID = dr.GetInt32(0),
                    Producto = dr.GetString(1),
                    NumeroSerie = dr.IsDBNull(2) ? null : dr.GetString(2),
                    CostoCompra = dr.GetDecimal(3),
                    PrecioVenta = dr.GetDecimal(4),
                    Estado = dr.GetString(5)
                });
            }

            return View(lista);
        }
    }
}

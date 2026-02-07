using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ZeltekWeb.Models;
using System.Linq;

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
        public IActionResult Index(string buscar)
        {
            var lista = new List<InventarioViewModel>();
            var cadena = _config.GetConnectionString("ZeltekDB");

            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();

                // CORRECCIÓN: Los datos técnicos están en 'i' (ItemInventario), no en 'p' (Producto)
                // Y usamos los nombres reales de tus columnas: RAM_GB, Almacenamiento_GB
                string sql = @"
                    SELECT 
                        i.ItemID,
                        p.Nombre AS Producto,
                        i.NumeroSerie,
                        i.CostoCompra,
                        i.PrecioVenta,
                        i.Estado,
                        i.Procesador,         -- Índice 6 (Viene de ItemInventario)
                        i.RAM_GB,             -- Índice 7 (Viene de ItemInventario)
                        i.Almacenamiento_GB   -- Índice 8 (Viene de ItemInventario)
                    FROM ItemInventario i
                    INNER JOIN Producto p ON i.ProductoID = p.ProductoID";

                if (!string.IsNullOrEmpty(buscar))
                {
                    sql += " WHERE p.Nombre LIKE @buscar OR i.NumeroSerie LIKE @buscar";
                }

                sql += " ORDER BY i.ItemID DESC";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    if (!string.IsNullOrEmpty(buscar))
                    {
                        cmd.Parameters.AddWithValue("@buscar", "%" + buscar + "%");
                    }

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new InventarioViewModel
                            {
                                ItemID = dr.GetInt32(0),
                                Producto = dr.GetString(1),
                                NumeroSerie = dr.IsDBNull(2) ? null : dr.GetString(2),
                                CostoCompra = dr.GetDecimal(3),
                                PrecioVenta = dr.GetDecimal(4),
                                Estado = dr.GetString(5),

                                // Construimos las especificaciones
                                // Usamos .ToString() para asegurar que funcione si la RAM es número o texto
                                Especificaciones = ConstruirSpecs(
                                    dr.IsDBNull(6) ? "" : dr.GetValue(6).ToString(), // Procesador
                                    dr.IsDBNull(7) ? "" : dr.GetValue(7).ToString(), // RAM_GB
                                    dr.IsDBNull(8) ? "" : dr.GetValue(8).ToString()  // Almacenamiento_GB
                                )
                            });
                        }
                    }
                }
            }

            // Ordenamos visualmente: Stock primero
            var listaOrdenada = lista.OrderBy(x => x.Estado == "Vendido").ToList();

            ViewData["BusquedaActual"] = buscar;
            return View(listaOrdenada);
        }

        // Método auxiliar para formatear bonito
        private string ConstruirSpecs(string proc, string ram, string disco)
        {
            var partes = new List<string>();

            if (!string.IsNullOrEmpty(proc)) partes.Add(proc);

            // Si el valor ya trae "GB" no lo repetimos, si es solo un número "16", le pegamos "GB"
            if (!string.IsNullOrEmpty(ram))
                partes.Add(ram.ToUpper().Contains("GB") ? ram : ram + "GB");

            if (!string.IsNullOrEmpty(disco))
                partes.Add(disco.ToUpper().Contains("GB") || disco.ToUpper().Contains("TB") ? disco : disco + "GB");

            return string.Join(" • ", partes);
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ZeltekWeb.Models; // Importante
using System.Data;

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

        // Modificación: Agregamos el parámetro 'filtro'
        // Modificación: Agregamos lógica para "Transito"
        public IActionResult Index(string buscar, string filtro = "Stock")
        {
            var lista = new List<ItemInventarioViewModel>();
            string cadena = _config.GetConnectionString("ZeltekDB");

            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();

                // 1. Lógica del filtro de Estados
                // "Stock" -> En Stock
                // "Vendido" -> Vendido
                // "Transito" -> Todo lo que viene viajando (creado en Compras)

                string condicionEstado = "i.Estado = 'En Stock'"; // Default

                if (filtro == "Vendido")
                    condicionEstado = "i.Estado = 'Vendido'";
                else if (filtro == "Transito")
                    condicionEstado = "i.Estado IN ('En Camino', 'En Transito', 'En Miami', 'En Aduana')";

                string sql = $@"
                    SELECT 
                        i.ItemID,
                        p.Nombre,
                        i.NumeroSerie,
                        i.Estado,
                        -- Calculamos costo acumulado
                        (i.CostoCompra + ISNULL(i.CostoEnvio, 0) + ISNULL(i.CostoReparacion, 0)) as CostoTotal,
                        i.PrecioVenta,
                        i.Procesador,
                        ISNULL(i.RAM_GB, 0),
                        ISNULL(i.Almacenamiento_GB, 0)
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
                                ItemID = dr.GetInt32(0),
                                ProductoNombre = dr.GetString(1),
                                NumeroSerie = dr.IsDBNull(2) ? "S/N" : dr.GetString(2),
                                Estado = dr.GetString(3),
                                CostoTotal = dr.GetDecimal(4),
                                PrecioVenta = dr.GetDecimal(5),
                                Procesador = dr.IsDBNull(6) ? "" : dr.GetString(6),
                                RAM_GB = dr.GetInt32(7),
                                Almacenamiento_GB = dr.GetInt32(8)
                            });
                        }
                    }
                }
            }

            ViewData["Busqueda"] = buscar;
            ViewData["FiltroActual"] = filtro;
            return View(lista);
        }
    }
}
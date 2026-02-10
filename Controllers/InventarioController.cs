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
                var sql = "SELECT i.ItemID, p.Nombre, ISNULL(i.NumeroSerie, 'S/N') FROM ItemInventario i INNER JOIN Producto p ON i.ProductoID = p.ProductoID WHERE i.ItemID = @ID AND i.Estado = 'Por Revisar'";
                using (var cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read()) return NotFound();
                        var model = new InspeccionCalidadViewModel
                        {
                            ItemID = dr.GetInt32(0),
                            ProductoNombre = dr.GetString(1),
                            NumeroSerie = dr.GetString(2)
                        };
                        return View(model);
                    }
                }
            }
        }

        /// <summary>
        /// Guarda resultado de inspección: Aprobar → En Stock, o Enviar a Reparación → En Reparación.
        /// </summary>
        [HttpPost]
        public IActionResult Inspeccion(int id, string resultado, InspeccionCalidadViewModel model)
        {
            if (model.ItemID != id) return BadRequest();
            var nuevoEstado = string.Equals(resultado, "aprobar", StringComparison.OrdinalIgnoreCase) ? "En Stock" : "En Reparación";

            var cadena = _config.GetConnectionString("ZeltekDB");
            using (var con = new SqlConnection(cadena))
            {
                con.Open();
                // Actualizar Estado y opcionalmente Condicion (cosmética), SaludBateria
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
            return RedirectToAction(nameof(Calidad));
        }
    }
}
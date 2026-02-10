using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ZeltekWeb.Models;

namespace ZeltekWeb.Controllers
{
    [Authorize]
    public class GarantiasController : Controller
    {
        private readonly IConfiguration _config;

        public GarantiasController(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Lista garantías vigentes. Opcional: filtrar por nombre cliente, teléfono, producto o serie.
        /// </summary>
        public IActionResult Index(string? buscar)
        {
            var lista = new List<GarantiaVigenteViewModel>();
            var cadena = _config.GetConnectionString("ZeltekDB");

            const string sql = @"
                SELECT
                    c.NombreCompleto AS Cliente,
                    ISNULL(c.Telefono, '') AS Telefono,
                    p.Nombre AS Producto,
                    ISNULL(i.NumeroSerie, '') AS NumeroSerie,
                    v.FechaVenta,
                    dv.GarantiaMeses,
                    DATEADD(month, dv.GarantiaMeses, v.FechaVenta) AS FechaVenceGarantia,
                    DATEDIFF(day, CAST(GETDATE() AS DATE), DATEADD(month, dv.GarantiaMeses, v.FechaVenta)) AS DiasRestantes,
                    i.Procesador,
                    ISNULL(i.RAM_GB, 0) AS RAM_GB,
                    ISNULL(i.Almacenamiento_GB, 0) AS Almacenamiento_GB
                FROM DetalleVenta dv
                JOIN Venta v ON dv.VentaID = v.VentaID
                JOIN Cliente c ON v.ClienteID = c.ClienteID
                JOIN ItemInventario i ON dv.ItemID = i.ItemID
                JOIN Producto p ON i.ProductoID = p.ProductoID
                WHERE dv.GarantiaMeses > 0
                  AND DATEADD(month, dv.GarantiaMeses, v.FechaVenta) >= CAST(GETDATE() AS DATE)
                  AND (@Buscar IS NULL OR @Buscar = ''
                       OR c.NombreCompleto LIKE '%' + @Buscar + '%'
                       OR c.Telefono LIKE '%' + @Buscar + '%'
                       OR p.Nombre LIKE '%' + @Buscar + '%'
                       OR i.NumeroSerie LIKE '%' + @Buscar + '%')
                ORDER BY FechaVenceGarantia ASC";

            using (var con = new SqlConnection(cadena))
            {
                con.Open();
                using (var cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Buscar", string.IsNullOrWhiteSpace(buscar) ? (object)DBNull.Value : buscar.Trim());
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new GarantiaVigenteViewModel
                            {
                                Cliente = dr.GetString(0),
                                Telefono = dr.GetString(1),
                                Producto = dr.GetString(2),
                                NumeroSerie = dr.GetString(3),
                                FechaVenta = dr.GetDateTime(4),
                                GarantiaMeses = dr.GetInt32(5),
                                FechaVenceGarantia = dr.GetDateTime(6),
                                DiasRestantes = dr.GetInt32(7),
                                Procesador = dr.IsDBNull(8) ? null : dr.GetString(8),
                                RAM_GB = dr.GetInt32(9),
                                Almacenamiento_GB = dr.GetInt32(10)
                            });
                        }
                    }
                }
            }

            ViewData["Busqueda"] = buscar;
            return View(lista);
        }
    }
}

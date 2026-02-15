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
        private readonly ILogger<GarantiasController> _logger;

        public GarantiasController(IConfiguration config, ILogger<GarantiasController> logger)
        {
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Lista garantías vigentes desde la vista vw_GarantiasVigentes.
        /// Opcional: filtrar por nombre cliente, teléfono, producto o serie.
        /// </summary>
        public IActionResult Index(string? buscar)
        {
            var lista = new List<GarantiaVigenteViewModel>();
            var cadena = _config.GetConnectionString("ZeltekDB");

            // Leer desde la VISTA (tabla Garantias), no desde DetalleVenta.
            // La vista debe devolver columnas en este orden: Cliente, Telefono, Producto, NumeroSerie,
            // FechaVenta, GarantiaMeses, FechaVenceGarantia, DiasRestantes, Procesador, RAM_GB, Almacenamiento_GB
            const string sql = @"
                SELECT
                    Cliente,
                    ISNULL(Telefono, '') AS Telefono,
                    Producto,
                    ISNULL(NumeroSerie, '') AS NumeroSerie,
                    FechaInicio AS FechaVenta,
                    DATEDIFF(month, FechaInicio, FechaFin) AS GarantiaMeses,
                    FechaFin AS FechaVenceGarantia,
                    DATEDIFF(day, CAST(GETDATE() AS DATE), FechaFin) AS DiasRestantes,
                    Procesador,
                    ISNULL(RAM_GB, 0) AS RAM_GB,
                    ISNULL(Almacenamiento_GB, 0) AS Almacenamiento_GB
                FROM vw_GarantiasVigentes
                WHERE Estado = 'Activa'
                  AND FechaFin >= CAST(GETDATE() AS DATE)
                  AND (@Buscar IS NULL OR @Buscar = ''
                       OR Cliente LIKE '%' + @Buscar + '%'
                       OR Telefono LIKE '%' + @Buscar + '%'
                       OR Producto LIKE '%' + @Buscar + '%'
                       OR NumeroSerie LIKE '%' + @Buscar + '%')
                ORDER BY FechaFin ASC";

            try
            {
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
                                    GarantiaMeses = dr.IsDBNull(5) ? 0 : dr.GetInt32(5),
                                    FechaVenceGarantia = dr.GetDateTime(6),
                                    DiasRestantes = dr.GetInt32(7),
                                    Procesador = dr.IsDBNull(8) ? null : dr.GetString(8),
                                    RAM_GB = dr.IsDBNull(9) ? 0 : dr.GetInt32(9),
                                    Almacenamiento_GB = dr.IsDBNull(10) ? 0 : dr.GetInt32(10)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar garantías vigentes desde vw_GarantiasVigentes");
                throw;
            }

            _logger.LogInformation("Garantías vigentes recuperadas: {Count} (filtro buscar='{Buscar}')", lista.Count, buscar ?? "");
            ViewData["Busqueda"] = buscar;
            return View(lista);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ZeltekWeb.Models;

namespace ZeltekWeb.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IConfiguration _config;

        public DashboardController(IConfiguration config)
        {
            _config = config;
        }

        public IActionResult Index()
        {
            var model = new DashboardViewModel();
            var cadena = _config.GetConnectionString("ZeltekDB");

            using (var con = new SqlConnection(cadena))
            {
                con.Open();

                // Ventas de hoy
                var sqlVentasHoy = @"
                    SELECT ISNULL(SUM(TotalVenta), 0)
                    FROM Venta
                    WHERE CAST(FechaVenta AS DATE) = CAST(GETDATE() AS DATE)";
                using (var cmd = new SqlCommand(sqlVentasHoy, con))
                {
                    var obj = cmd.ExecuteScalar();
                    if (obj != null && obj != DBNull.Value) model.VentasHoy = Convert.ToDecimal(obj);
                }

                // En camino: cantidad y monto en vuelo (ItemInventario en tránsito)
                var sqlEnCamino = @"
                    SELECT COUNT(*),
                           ISNULL(SUM(CostoCompra + ISNULL(CostoEnvio, 0) + ISNULL(CostoReparacion, 0)), 0)
                    FROM ItemInventario
                    WHERE Estado IN ('En Camino', 'En Transito', 'En Transito USA', 'En Miami', 'Viajando a NIC', 'En Aduana')";
                using (var cmd = new SqlCommand(sqlEnCamino, con))
                using (var dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        model.EnCaminoCantidad = dr.GetInt32(0);
                        model.EnCaminoMonto = dr.GetDecimal(1);
                    }
                }

                // Por revisar: equipos que no han pasado test de calidad (estado "Por Revisar")
                var sqlPorRevisar = @"
                    SELECT COUNT(*)
                    FROM ItemInventario
                    WHERE Estado = 'Por Revisar'";
                using (var cmd = new SqlCommand(sqlPorRevisar, con))
                {
                    var obj = cmd.ExecuteScalar();
                    if (obj != null && obj != DBNull.Value) model.PorRevisarCantidad = Convert.ToInt32(obj);
                }

                // En Bodega (Stock): cantidad y valor total (CostoCompra + CostoEnvio + CostoReparacion)
                var sqlStock = @"
                    SELECT COUNT(*),
                           ISNULL(SUM(CostoCompra + ISNULL(CostoEnvio, 0) + ISNULL(CostoReparacion, 0)), 0)
                    FROM ItemInventario
                    WHERE Estado = 'En Stock'";
                using (var cmd = new SqlCommand(sqlStock, con))
                using (var dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        model.StockCantidad = dr.GetInt32(0);
                        model.StockValor = dr.GetDecimal(1);
                    }
                }

                // Actividad reciente: últimas 5 transacciones (Ventas y Compras mezcladas por fecha)
                var sqlActividad = @"
                    SELECT TOP 5 Tipo, Descripcion, Fecha, Monto FROM (
                        SELECT 'Venta' AS Tipo, 'Venta #' + CAST(v.VentaID AS VARCHAR(20)) AS Descripcion, v.FechaVenta AS Fecha, v.TotalVenta AS Monto FROM Venta v
                        UNION ALL
                        SELECT 'Compra' AS Tipo, 'Compra #' + CAST(c.CompraID AS VARCHAR(20)) AS Descripcion, c.FechaCompra AS Fecha, c.CostoTotal AS Monto FROM Compra c
                    ) u
                    ORDER BY Fecha DESC";
                using (var cmd = new SqlCommand(sqlActividad, con))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        model.ActividadReciente.Add(new ActividadRecienteViewModel
                        {
                            Tipo = dr.GetString(0),
                            Descripcion = dr.GetString(1),
                            Fecha = dr.GetDateTime(2),
                            Monto = dr.GetDecimal(3)
                        });
                    }
                }
            }

            return View(model);
        }
    }
}

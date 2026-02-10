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

                // Actividad reciente: últimas 5 transacciones con descripción cliente/producto (JOIN)
                var sqlActividad = @"
                    SELECT TOP 5 Tipo, Descripcion, Fecha, Monto FROM (
                        SELECT 'Venta' AS Tipo,
                               'Venta #' + CAST(v.VentaID AS VARCHAR(20)) + ' · ' + ISNULL(c.NombreCompleto, 'Sin cliente') AS Descripcion,
                               v.FechaVenta AS Fecha,
                               v.TotalVenta AS Monto
                        FROM Venta v
                        LEFT JOIN Cliente c ON v.ClienteID = c.ClienteID
                        UNION ALL
                        SELECT 'Compra' AS Tipo,
                               'Compra #' + CAST(comp.CompraID AS VARCHAR(20)) + ' · ' + ISNULL((SELECT TOP 1 p.Nombre FROM ItemInventario i INNER JOIN Producto p ON i.ProductoID = p.ProductoID WHERE i.CompraID = comp.CompraID), 'Varios') AS Descripcion,
                               comp.FechaCompra AS Fecha,
                               comp.CostoTotal AS Monto
                        FROM Compra comp
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

        /// <summary>
        /// Búsqueda global por serie, nombre o tracking en Ventas, Compras e Inventario.
        /// </summary>
        [HttpGet]
        public IActionResult Buscar(string q)
        {
            var model = new BusquedaGlobalViewModel { Query = q ?? "" };
            if (string.IsNullOrWhiteSpace(model.Query))
                return View(model);

            var cadena = _config.GetConnectionString("ZeltekDB");
            using (var con = new SqlConnection(cadena))
            {
                con.Open();
                var buscar = model.Query.Trim();

                // Ventas: por nombre cliente o por número de serie (ítem vendido)
                var sqlVentas = @"
                    SELECT DISTINCT v.VentaID, v.FechaVenta, v.TotalVenta, ISNULL(c.NombreCompleto, '') AS ClienteNombre
                    FROM Venta v
                    LEFT JOIN Cliente c ON v.ClienteID = c.ClienteID
                    LEFT JOIN DetalleVenta d ON d.VentaID = v.VentaID
                    LEFT JOIN ItemInventario i ON i.ItemID = d.ItemID
                    WHERE c.NombreCompleto LIKE '%' + @Buscar + '%'
                       OR i.NumeroSerie LIKE '%' + @Buscar + '%'";
                using (var cmd = new SqlCommand(sqlVentas, con))
                {
                    cmd.Parameters.AddWithValue("@Buscar", buscar);
                    using (var dr = cmd.ExecuteReader())
                        while (dr.Read())
                            model.Ventas.Add(new ResultadoBusquedaVenta
                            {
                                VentaID = dr.GetInt32(0),
                                Fecha = dr.GetDateTime(1),
                                Total = dr.GetDecimal(2),
                                ClienteNombre = dr.IsDBNull(3) ? "" : dr.GetString(3)
                            });
                }

                // Compras: por tracking, vendedor o nombre producto / serie
                var sqlCompras = @"
                    SELECT DISTINCT c.CompraID, c.FechaCompra, c.CostoTotal,
                           ISNULL(c.TrackingProveedor, '') AS Tracking,
                           ISNULL((SELECT TOP 1 p.Nombre FROM ItemInventario i INNER JOIN Producto p ON i.ProductoID = p.ProductoID WHERE i.CompraID = c.CompraID), '') AS ProductoPrincipal
                    FROM Compra c
                    LEFT JOIN ItemInventario i ON i.CompraID = c.CompraID
                    LEFT JOIN Producto p ON i.ProductoID = p.ProductoID
                    WHERE c.TrackingProveedor LIKE '%' + @Buscar + '%'
                       OR c.VendedorOnline LIKE '%' + @Buscar + '%'
                       OR p.Nombre LIKE '%' + @Buscar + '%'
                       OR i.NumeroSerie LIKE '%' + @Buscar + '%'";
                using (var cmd = new SqlCommand(sqlCompras, con))
                {
                    cmd.Parameters.AddWithValue("@Buscar", buscar);
                    using (var dr = cmd.ExecuteReader())
                        while (dr.Read())
                            model.Compras.Add(new ResultadoBusquedaCompra
                            {
                                CompraID = dr.GetInt32(0),
                                Fecha = dr.GetDateTime(1),
                                Total = dr.GetDecimal(2),
                                Tracking = dr.IsDBNull(3) ? "" : dr.GetString(3),
                                ProductoPrincipal = dr.IsDBNull(4) ? "" : dr.GetString(4)
                            });
                }

                // Inventario: por serie o nombre producto
                var sqlInv = @"
                    SELECT i.ItemID, p.Nombre, ISNULL(i.NumeroSerie, 'S/N'), i.Estado
                    FROM ItemInventario i
                    INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                    WHERE i.NumeroSerie LIKE '%' + @Buscar + '%' OR p.Nombre LIKE '%' + @Buscar + '%'
                    ORDER BY i.ItemID DESC";
                using (var cmd = new SqlCommand(sqlInv, con))
                {
                    cmd.Parameters.AddWithValue("@Buscar", buscar);
                    using (var dr = cmd.ExecuteReader())
                        while (dr.Read())
                            model.Inventario.Add(new ResultadoBusquedaInventario
                            {
                                ItemID = dr.GetInt32(0),
                                ProductoNombre = dr.GetString(1),
                                NumeroSerie = dr.IsDBNull(2) ? "S/N" : dr.GetString(2),
                                Estado = dr.GetString(3)
                            });
                }
            }

            return View(model);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ZeltekWeb.Models;

namespace ZeltekWeb.Controllers
{
    [Authorize]
    public class ReportesController : Controller
    {
        private readonly IConfiguration _config;

        public ReportesController(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Dashboard de reportes: tendencia, top productos, inventario dormido, valor inventario.
        /// </summary>
        /// <param name="meses">6 o 12 meses para el gráfico de tendencia</param>
        /// <param name="periodoGanancia">1, 3 o 12 meses para el cálculo de Ganancia Neta</param>
        public IActionResult Index(int meses = 12, int periodoGanancia = 1)
        {
            if (meses != 6 && meses != 12) meses = 12;
            if (periodoGanancia != 1 && periodoGanancia != 3 && periodoGanancia != 12) periodoGanancia = 1;

            var model = new ReporteIndexViewModel { MesesTendencia = meses, PeriodoGananciaMeses = periodoGanancia };
            var cadena = _config.GetConnectionString("ZeltekDB");

            using (var con = new SqlConnection(cadena))
            {
                con.Open();

                // Fecha inicio = día 1 del mes que está N meses atrás (incluir el mes completo)
                // Ej: hoy 9-Feb → 6 meses atrás día 1 = 1-Ago (no 9-Ago), así el primer mes cuenta todo.
                var sqlTendencia = @"
                    SELECT
                        FORMAT(v.FechaVenta, 'yyyy-MM') AS Mes,
                        FORMAT(MIN(v.FechaVenta), 'MMM yy') AS MesLabel,
                        SUM(d.PrecioUnitario) AS TotalVentas,
                        COUNT(*) AS CantidadVentas
                    FROM Venta v
                    INNER JOIN DetalleVenta d ON v.VentaID = d.VentaID
                    WHERE v.FechaVenta >= DATEADD(MONTH, -@Meses, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1))
                    GROUP BY FORMAT(v.FechaVenta, 'yyyy-MM')
                    ORDER BY FORMAT(v.FechaVenta, 'yyyy-MM')";

                using (var cmd = new SqlCommand(sqlTendencia, con))
                {
                    cmd.Parameters.AddWithValue("@Meses", meses);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            model.TendenciaMeses.Add(new ReporteTendenciaMensualViewModel
                            {
                                Mes = dr.GetString(0),
                                MesLabel = dr.GetString(1),
                                TotalVentas = dr.GetDecimal(2),
                                CantidadVentas = dr.GetInt32(3)
                            });
                        }
                    }
                }

                // 1.1 CantidadEquipos por mes desde vw_ResumenFinanciero (misma fecha inicio = día 1 del mes)
                var sqlCantidadEquipos = @"
                    SELECT Anio, Mes, ISNULL(CantidadEquipos, 0)
                    FROM vw_ResumenFinanciero
                    WHERE DATEFROMPARTS(Anio, Mes, 1) >= DATEADD(MONTH, -@Meses, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1))
                    ORDER BY Anio, Mes";
                var dictCantidadEquipos = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                using (var cmd = new SqlCommand(sqlCantidadEquipos, con))
                {
                    cmd.Parameters.AddWithValue("@Meses", meses);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var anio = dr.GetInt32(0);
                            var mes = dr.GetInt32(1);
                            var key = $"{anio}-{mes:D2}";
                            dictCantidadEquipos[key] = dr.GetInt32(2);
                        }
                    }
                }
                foreach (var t in model.TendenciaMeses)
                {
                    if (dictCantidadEquipos.TryGetValue(t.Mes, out var cant))
                        t.CantidadEquipos = cant;
                }

                // 2. Top productos (más vendidos)
                var sqlTop = @"
                    SELECT
                        p.Nombre AS ProductoNombre,
                        COUNT(*) AS CantidadVentas,
                        SUM(d.PrecioUnitario) AS TotalVendido
                    FROM DetalleVenta d
                    INNER JOIN ItemInventario i ON d.ItemID = i.ItemID
                    INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                    GROUP BY p.ProductoID, p.Nombre
                    ORDER BY COUNT(*) DESC";

                using (var cmd = new SqlCommand(sqlTop, con))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        model.TopProductos.Add(new ReporteTopProductoViewModel
                        {
                            ProductoNombre = dr.GetString(0),
                            CantidadVentas = dr.GetInt32(1),
                            TotalVendido = dr.GetDecimal(2)
                        });
                    }
                }

                // 2.5 Ganancia Neta desde vw_ResumenFinanciero (GananciaBruta - GastosOperativos = GananciaNeta)
                var sqlResumenFinanciero = @"
                    SELECT
                        ISNULL(SUM(GananciaBruta), 0),
                        ISNULL(SUM(GastosOperativos), 0)
                    FROM vw_ResumenFinanciero
                    WHERE DATEFROMPARTS(Anio, Mes, 1) >= DATEADD(month, -@PeriodoGanancia, CAST(GETDATE() AS DATE))";
                using (var cmd = new SqlCommand(sqlResumenFinanciero, con))
                {
                    cmd.Parameters.AddWithValue("@PeriodoGanancia", periodoGanancia);
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            model.GananciaBrutaVentas = dr.GetDecimal(0);
                            model.TotalGastosOperativos = dr.GetDecimal(1);
                        }
                    }
                }

                // 3. Valor del inventario actual (items En Stock)
                var sqlValor = @"
                    SELECT SUM(i.CostoCompra + ISNULL(i.CostoEnvio, 0) + ISNULL(i.CostoReparacion, 0))
                    FROM ItemInventario i
                    WHERE i.Estado = 'En Stock'";

                using (var cmd = new SqlCommand(sqlValor, con))
                {
                    var obj = cmd.ExecuteScalar();
                    if (obj != null && obj != DBNull.Value)
                        model.ValorInventarioActual = Convert.ToDecimal(obj);
                }

                // 4. Inventario dormido (> 60 días en stock, desde FechaCompra de la compra)
                var sqlDormido = @"
                    SELECT
                        i.ItemID,
                        p.Nombre AS ProductoNombre,
                        ISNULL(i.NumeroSerie, '') AS NumeroSerie,
                        DATEDIFF(day, c.FechaCompra, GETDATE()) AS DiasEnStock,
                        (i.CostoCompra + ISNULL(i.CostoEnvio, 0) + ISNULL(i.CostoReparacion, 0)) AS CostoTotal,
                        i.PrecioVenta,
                        i.Procesador,
                        ISNULL(i.RAM_GB, 0) AS RAM_GB,
                        ISNULL(i.Almacenamiento_GB, 0) AS Almacenamiento_GB
                    FROM ItemInventario i
                    INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                    INNER JOIN Compra c ON i.CompraID = c.CompraID
                    WHERE i.Estado = 'En Stock'
                      AND DATEDIFF(day, c.FechaCompra, GETDATE()) >= 60
                    ORDER BY DiasEnStock DESC";

                using (var cmd = new SqlCommand(sqlDormido, con))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        model.InventarioDormido.Add(new ReporteInventarioDormidoViewModel
                        {
                            ItemID = dr.GetInt32(0),
                            ProductoNombre = dr.GetString(1),
                            NumeroSerie = dr.GetString(2),
                            DiasEnStock = dr.GetInt32(3),
                            CostoTotal = dr.GetDecimal(4),
                            PrecioVenta = dr.GetDecimal(5),
                            Procesador = dr.IsDBNull(6) ? null : dr.GetString(6),
                            RAM_GB = dr.GetInt32(7),
                            Almacenamiento_GB = dr.GetInt32(8)
                        });
                    }
                }

                // 5. Top clientes revendedores desde vw_TopClientes (quién más compra)
                var sqlTopRevendedores = @"
                    SELECT
                        Cliente,
                        TipoCliente,
                        CantidadCompras,
                        TotalComprado,
                        GananciaGenerada,
                        UltimaCompra
                    FROM vw_TopClientes
                    WHERE LOWER(LTRIM(RTRIM(ISNULL(TipoCliente,'')))) = 'revendedor'
                      AND Cliente IS NOT NULL AND LTRIM(RTRIM(Cliente)) <> ''
                    ORDER BY TotalComprado DESC";

                using (var cmd = new SqlCommand(sqlTopRevendedores, con))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        model.TopRevendedores.Add(new TopRevendedorViewModel
                        {
                            NombreCliente = dr.GetString(0),
                            TipoCliente = dr.IsDBNull(1) ? "" : dr.GetString(1),
                            CantidadCompras = dr.GetInt32(2),
                            TotalComprado = dr.GetDecimal(3),
                            GananciaGenerada = dr.GetDecimal(4),
                            UltimaCompra = dr.IsDBNull(5) ? null : dr.GetDateTime(5)
                        });
                    }
                }
            }

            return View(model);
        }
    }
}

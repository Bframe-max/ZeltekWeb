using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ZeltekWeb.Filters;
using ZeltekWeb.Models;

namespace ZeltekWeb.Controllers
{
    [AuthFilter]
    public class ReportesController : Controller

    {
        public IActionResult Index()
        {
            return View();
        }



        private readonly IConfiguration _config;

        public ReportesController(IConfiguration config)
        {
            _config = config;
        }

        public IActionResult GananciaNeta(DateTime? desde, DateTime? hasta)
        {
            var model = new ReporteGananciaNetaViewModel
            {
                Desde = desde,
                Hasta = hasta
            };

            var cadena = _config.GetConnectionString("ZeltekDB");
            using SqlConnection con = new SqlConnection(cadena);
            con.Open();

            // =========================
            // VENTAS
            // =========================
            var sqlVentas = @"
                SELECT
                    COUNT(*) ,
                    SUM(d.PrecioUnitario),
                    SUM(i.CostoCompra)
                FROM Venta v
                INNER JOIN DetalleVenta d ON v.VentaID = d.VentaID
                INNER JOIN ItemInventario i ON d.ItemID = i.ItemID
                WHERE (@Desde IS NULL OR v.FechaVenta >= @Desde)
                  AND (@Hasta IS NULL OR v.FechaVenta <= @Hasta)
            ";

            using (var cmd = new SqlCommand(sqlVentas, con))
            {
                cmd.Parameters.AddWithValue("@Desde", (object?)desde ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Hasta", (object?)hasta ?? DBNull.Value);

                using var dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    model.CantidadVentas = dr.GetInt32(0);
                    model.TotalVentas = dr.IsDBNull(1) ? 0 : dr.GetDecimal(1);
                    model.TotalCosto = dr.IsDBNull(2) ? 0 : dr.GetDecimal(2);
                }
            }

            // =========================
            // GASTOS OPERATIVOS
            // =========================
            var sqlGastos = @"
                SELECT
                    COUNT(*),
                    SUM(Monto)
                FROM GastoOperativo
                WHERE (@Desde IS NULL OR FechaGasto >= @Desde)
                  AND (@Hasta IS NULL OR FechaGasto <= @Hasta)
            ";

            using (var cmd = new SqlCommand(sqlGastos, con))
            {
                cmd.Parameters.AddWithValue("@Desde", (object?)desde ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Hasta", (object?)hasta ?? DBNull.Value);

                using var dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    model.CantidadGastos = dr.GetInt32(0);
                    model.TotalGastos = dr.IsDBNull(1) ? 0 : dr.GetDecimal(1);
                }
            }

            // =========================
            // GANANCIA NETA
            // =========================
            model.GananciaNeta = model.TotalVentas - model.TotalCosto - model.TotalGastos;

            return View(model);




        }
        public IActionResult GraficaGanancia(DateTime? desde, DateTime? hasta)
        {
            var lista = new List<ReporteGraficaMensualViewModel>();

            var cadena = _config.GetConnectionString("ZeltekDB");
            using SqlConnection con = new SqlConnection(cadena);
            con.Open();

   


            var sql = @"
SELECT
    v.Mes,
    v.Ventas,
    v.Costos,
    ISNULL(g.Gastos, 0) AS Gastos,
    (v.Ventas - v.Costos - ISNULL(g.Gastos, 0)) AS Ganancia
FROM
(
    SELECT
        FORMAT(v.FechaVenta, 'yyyy-MM') AS Mes,
        SUM(d.PrecioUnitario) AS Ventas,
        SUM(i.CostoCompra) AS Costos
    FROM Venta v
    INNER JOIN DetalleVenta d ON v.VentaID = d.VentaID
    INNER JOIN ItemInventario i ON d.ItemID = i.ItemID
    WHERE (@Desde IS NULL OR v.FechaVenta >= @Desde)
      AND (@Hasta IS NULL OR v.FechaVenta <= @Hasta)
    GROUP BY FORMAT(v.FechaVenta, 'yyyy-MM')
) v
LEFT JOIN
(
    SELECT
        FORMAT(FechaGasto, 'yyyy-MM') AS Mes,
        SUM(Monto) AS Gastos
    FROM GastoOperativo
    WHERE (@Desde IS NULL OR FechaGasto >= @Desde)
      AND (@Hasta IS NULL OR FechaGasto <= @Hasta)
    GROUP BY FORMAT(FechaGasto, 'yyyy-MM')
) g ON v.Mes = g.Mes
ORDER BY v.Mes";


            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Desde", (object?)desde ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Hasta", (object?)hasta ?? DBNull.Value);

            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new ReporteGraficaMensualViewModel
                {
                    Mes = dr.GetString(0),
                    Ventas = dr.IsDBNull(1) ? 0 : dr.GetDecimal(1),
                    Costos = dr.IsDBNull(2) ? 0 : dr.GetDecimal(2),
                    Gastos = dr.IsDBNull(3) ? 0 : dr.GetDecimal(3)
                    // ❌ NO asignar Ganancia
                });
            }

            return Json(new
            {
                labels = lista.Select(x => x.Mes),
                ventas = lista.Select(x => x.Ventas),
                gastos = lista.Select(x => x.Gastos),
                ganancia = lista.Select(x => x.Ganancia)
            });
        }

        


    }
}

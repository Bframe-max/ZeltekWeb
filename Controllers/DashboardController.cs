using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ZeltekWeb.Models;
using ZeltekWeb.Filters;

namespace ZeltekWeb.Controllers
{
    [AuthFilter]
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

            using SqlConnection con = new SqlConnection(cadena);
            con.Open();

            // 1️⃣ Capital invertido
            model.CapitalInvertido = ExecuteScalarDecimal(con, @"
                SELECT ISNULL(SUM(CostoCompra + CostoReparacion + CostoEnvio),0)
                FROM ItemInventario
                WHERE Estado = 'En Stock'
            ");

            // 2️⃣ Valor inventario
            model.ValorInventario = ExecuteScalarDecimal(con, @"
                SELECT ISNULL(SUM(PrecioVenta),0)
                FROM ItemInventario
                WHERE Estado = 'En Stock'
            ");

            // 3️⃣ Ventas del mes
            model.VentasMes = ExecuteScalarDecimal(con, @"
                SELECT ISNULL(SUM(TotalVenta),0)
                FROM Venta
                WHERE FORMAT(FechaVenta,'yyyy-MM') = FORMAT(GETDATE(),'yyyy-MM')
            ");

            // 4️⃣ Ganancia del mes
            model.GananciaMes = ExecuteScalarDecimal(con, @"
                SELECT ISNULL(SUM(
                    dv.PrecioUnitario -
                    (i.CostoCompra + ISNULL(i.CostoReparacion,0) + ISNULL(i.CostoEnvio,0))
                ),0)
                FROM DetalleVenta dv
                JOIN ItemInventario i ON dv.ItemID = i.ItemID
                JOIN Venta v ON dv.VentaID = v.VentaID
                WHERE FORMAT(v.FechaVenta,'yyyy-MM') = FORMAT(GETDATE(),'yyyy-MM')
            ");

            // 5️⃣ Garantías activas
            model.GarantiasActivas = ExecuteScalarInt(con, @"
                SELECT COUNT(*)
                FROM DetalleVenta dv
                JOIN Venta v ON dv.VentaID = v.VentaID
                WHERE dv.GarantiaMeses > 0
                AND DATEADD(month, dv.GarantiaMeses, v.FechaVenta) >= GETDATE()
            ");

            // 6️⃣ Equipos en camino
            model.EquiposEnCamino = ExecuteScalarInt(con, @"
    SELECT COUNT(*)
    FROM ItemInventario i
    JOIN Compra c ON i.CompraID = c.CompraID
    JOIN Envio e ON c.EnvioID = e.EnvioID
    WHERE e.FechaRecibidoNicaragua IS NULL
");


            return View(model);
        }

        private decimal ExecuteScalarDecimal(SqlConnection con, string sql)
        {
            using var cmd = new SqlCommand(sql, con);
            return Convert.ToDecimal(cmd.ExecuteScalar());
        }

        private int ExecuteScalarInt(SqlConnection con, string sql)
        {
            using var cmd = new SqlCommand(sql, con);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }
}

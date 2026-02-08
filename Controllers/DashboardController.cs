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
            string connectionString = _config.GetConnectionString("ZeltekDB");

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                // Usamos la nueva vista operativa
                string sql = "SELECT * FROM vw_DashboardOperativo";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            model.ValorInventario = dr.GetDecimal(dr.GetOrdinal("ValorInventario"));
                            model.EnCamino = dr.GetDecimal(dr.GetOrdinal("EnCamino"));
                            model.PorCobrar = dr.GetDecimal(dr.GetOrdinal("PorCobrar"));
                            model.CantidadVendidos = dr.GetInt32(dr.GetOrdinal("CantidadVendidos"));
                            model.GananciaTotal = dr.GetDecimal(dr.GetOrdinal("GananciaTotal"));
                        }
                    }
                }
            }
            return View(model);
        }
    }
}
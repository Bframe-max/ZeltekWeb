using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ZeltekWeb.Controllers
{
    [AllowAnonymous]
    public class HealthController : Controller
    {
        private readonly IConfiguration _config;

        public HealthController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet("/health")]
        public IActionResult Index()
        {
            try
            {
                using var con = new SqlConnection(_config.GetConnectionString("ZeltekDB"));
                con.Open();

                using var cmd = new SqlCommand("SELECT 1", con);
                cmd.ExecuteScalar();

                return Ok("OK");
            }
            catch
            {
                return StatusCode(503, "DB OFFLINE");
            }
        }
    }
}

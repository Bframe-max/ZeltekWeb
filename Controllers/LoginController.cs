using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;





namespace ZeltekWeb.Controllers
{
    public class LoginController : Controller
    {
        private readonly IConfiguration _config;

        public LoginController(IConfiguration config)
        {
            _config = config;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(string usuario, string password)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(password));

            using SqlConnection con = new SqlConnection(
                _config.GetConnectionString("ZeltekDB"));

            con.Open();

            var cmd = new SqlCommand(@"
                SELECT UsuarioID
                FROM UsuarioSistema
                WHERE Usuario = @u
                  AND PasswordHash = @p
                  AND Activo = 1", con);

            cmd.Parameters.AddWithValue("@u", usuario);
            cmd.Parameters.AddWithValue("@p", hash);

            var result = cmd.ExecuteScalar();

            if (result != null)
            {
                HttpContext.Session.SetInt32("UsuarioID", (int)result);
                HttpContext.Session.SetString("Usuario", usuario);

                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Error = "Usuario o contraseña incorrectos";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }
}

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ZeltekWeb.Models;
using ZeltekWeb.Data;
using Dapper;

namespace ZeltekWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DapperContext _context;

        public HomeController(ILogger<HomeController> logger, DapperContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> TestConnection()
        {
            using var connection = await _context.CreateConnectionAsync();
            var productos = await connection.QueryAsync("SELECT * FROM Producto");
            return Ok(productos);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}

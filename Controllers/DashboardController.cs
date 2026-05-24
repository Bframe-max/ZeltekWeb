using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZeltekWeb.Services.Interfaces;

namespace ZeltekWeb.Controllers
{
    [Authorize]
    public class DashboardController : BaseController
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            var metrics = await _dashboardService.GetDashboardMetricsAsync();
            return View(metrics);
        }

        [HttpGet]
        public async Task<IActionResult> Buscar(string query)
        {
            var resultados = await _dashboardService.BuscarGlobalAsync(query);
            return Json(resultados);
        }
    }
}

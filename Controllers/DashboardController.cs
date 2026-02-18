using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZeltekWeb.Models;
using ZeltekWeb.Repositories;

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
            int tenantId = GetTenantId();
            var metrics = await _dashboardService.GetDashboardMetricsAsync(tenantId);
            return View(metrics);
        }

        /// <summary>
        /// Búsqueda global por serie, nombre o tracking en Ventas, Compras e Inventario.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Buscar(string query)
        {
            int tenantId = GetTenantId();
            var resultados = await _dashboardService.BuscarGlobalAsync(query, tenantId);
            return Json(resultados);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Repositories; // For IVentaRepository (History)
using ZeltekWeb.Services.Interfaces; // For IVentaService

namespace ZeltekWeb.Controllers
{
    [Authorize]
    public class VentasController : BaseController
    {
        private readonly IVentaService _ventaService;

        public VentasController(IVentaService ventaService)
        {
            _ventaService = ventaService;
        }

        public async Task<IActionResult> Index(string buscar, DateTime? fechaInicio, DateTime? fechaFin)
        {
            if (!fechaInicio.HasValue) fechaInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            if (!fechaFin.HasValue) fechaFin = DateTime.Now;

            int tenantId = GetTenantId();

            var resumen = await _ventaService.ObtenerHistorialAsync(buscar, fechaInicio, fechaFin, tenantId);
            
            ViewBag.TotalVendido = resumen.TotalVendido;
            ViewBag.TotalGanancia = resumen.TotalGanancia;
            ViewBag.TotalEquipos = resumen.TotalEquipos;

            ViewData["Busqueda"] = buscar;
            ViewData["FechaInicio"] = fechaInicio.Value.ToString("yyyy-MM-dd");
            ViewData["FechaFin"] = fechaFin.Value.ToString("yyyy-MM-dd");

            return View(resumen.Ventas);
        }

        [HttpPost]
        public async Task<IActionResult> CrearVentaRapida([FromBody] CreateVentaDto dto)
        {
            if (!ModelState.IsValid)
            {
                 var errors = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                 return Json(new { success = false, message = "⚠️ " + errors });
            }

            dto.Usuario = User.Identity?.Name ?? "Sistema";
            int tenantId = GetTenantId();

            try
            {
                await _ventaService.RegistrarVentaRapidaAsync(dto, tenantId);
                return Json(new { success = true, message = "✅ Venta registrada correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error Técnico: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProductoInfo(int id)
        {
            if (id <= 0) return Json(null);
            int tenantId = GetTenantId();
            var info = await _ventaService.ObtenerProductoInfoAsync(id, tenantId);
            return Json(info);
        }

        [HttpGet]
        public async Task<IActionResult> BuscarClientes(string termino)
        {
            int tenantId = GetTenantId();
            var clientes = await _ventaService.BuscarClientesAsync(termino, tenantId);
            return Json(clientes);
        }
    }
}
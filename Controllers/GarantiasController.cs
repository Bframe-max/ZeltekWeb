using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZeltekWeb.Models;
using ZeltekWeb.Services.Interfaces;

namespace ZeltekWeb.Controllers
{
    [Authorize]
    public class GarantiasController : BaseController
    {
        private readonly IGarantiaService _garantiaService;

        public GarantiasController(IGarantiaService garantiaService)
        {
            _garantiaService = garantiaService;
        }

        public async Task<IActionResult> Index(string? buscar)
        {
            try
            {
                var dtos = await _garantiaService.ObtenerGarantiasVigentesAsync(buscar);

                var viewModels = dtos.Select(d => new GarantiaVigenteViewModel
                {
                    Cliente = d.Cliente, Telefono = d.Telefono, Producto = d.Producto,
                    NumeroSerie = d.NumeroSerie, FechaVenta = d.FechaVenta, GarantiaMeses = d.GarantiaMeses,
                    FechaVenceGarantia = d.FechaVenceGarantia, DiasRestantes = d.DiasRestantes,
                    Procesador = d.Procesador, RAM_GB = d.RAM_GB, Almacenamiento_GB = d.Almacenamiento_GB
                }).ToList();

                ViewData["Busqueda"] = buscar;
                return View(viewModels);
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel { RequestId = "Error al cargar garantías: " + ex.Message });
            }
        }
    }
}

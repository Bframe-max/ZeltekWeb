using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZeltekWeb.Models;
using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Services.Interfaces;

namespace ZeltekWeb.Controllers
{
    [Authorize]
    public class GastosController : BaseController
    {
        private readonly IGastoService _gastoService;
        private readonly ILogger<GastosController> _logger;

        public GastosController(IGastoService gastoService, ILogger<GastosController> logger)
        {
            _gastoService = gastoService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta)
        {
            try
            {
                var resumenDto = await _gastoService.ObtenerResumenAsync(desde, hasta);
                var listadoDto = await _gastoService.ObtenerListadoAsync(desde, hasta);
                var categoriasDto = await _gastoService.ObtenerPorCategoriaAsync(desde, hasta);

                var resumen = new GastosResumenViewModel { CantidadGastos = resumenDto.CantidadGastos, TotalGastos = resumenDto.TotalGastos };

                var lista = listadoDto.Select(g => new GastoListViewModel
                {
                    GastoID = g.GastoID, FechaGasto = g.FechaGasto,
                    Descripcion = g.Descripcion, CategoriaGasto = g.CategoriaGasto, Monto = g.Monto
                }).ToList();

                var gastosPorCategoria = categoriasDto.Select(c => new GastoPorCategoriaViewModel { Categoria = c.Categoria, Total = c.Total }).ToList();

                ViewBag.Resumen = resumen;
                ViewBag.Desde = desde;
                ViewBag.Hasta = hasta;
                ViewBag.GastosPorCategoria = gastosPorCategoria;
                return View(lista);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Gastos Index");
                return View("Error", new ErrorViewModel { RequestId = "Error al cargar gastos: " + ex.Message });
            }
        }

        public IActionResult Create()
        {
            ViewBag.Categorias = ObtenerCategorias();
            return View(new GastoCreateViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(GastoCreateViewModel model)
        {
            if (!ModelState.IsValid) { ViewBag.Categorias = ObtenerCategorias(); return View(model); }

            try
            {
                var dto = new GastoCreateDto { FechaGasto = model.FechaGasto, Descripcion = model.Descripcion, CategoriaGasto = model.CategoriaGasto, Monto = model.Monto };
                await _gastoService.CrearAsync(dto);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al crear gasto: " + ex.Message);
                ViewBag.Categorias = ObtenerCategorias();
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var dto = await _gastoService.ObtenerPorIdAsync(id);
                if (dto == null) return NotFound();

                var model = new GastoEditViewModel
                {
                    GastoID = dto.GastoID, FechaGasto = dto.FechaGasto,
                    Descripcion = dto.Descripcion, CategoriaGasto = dto.CategoriaGasto, Monto = dto.Monto
                };
                ViewBag.Categorias = ObtenerCategorias();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Gasto for Edit");
                return View("Error", new ErrorViewModel { RequestId = "Error al cargar gasto." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(GastoEditViewModel model)
        {
            if (!ModelState.IsValid) { ViewBag.Categorias = ObtenerCategorias(); return View(model); }

            try
            {
                var dto = new GastoEditDto
                {
                    GastoID = model.GastoID, FechaGasto = model.FechaGasto,
                    Descripcion = model.Descripcion, CategoriaGasto = model.CategoriaGasto, Monto = model.Monto
                };
                await _gastoService.ActualizarAsync(dto);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar gasto: " + ex.Message);
                ViewBag.Categorias = ObtenerCategorias();
                return View(model);
            }
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            try { await _gastoService.EliminarAsync(id); }
            catch (Exception ex) { _logger.LogError(ex, "Error deleting Gasto"); }
            return RedirectToAction(nameof(Index));
        }

        private static string[] ObtenerCategorias() => new[]
        {
            "Logística", "Servicios", "Marketing", "Insumos", "Mantenimiento", "Comisiones", "Impuestos", "Otros"
        };
    }
}

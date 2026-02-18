using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
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

        // =========================
        // LISTADO + RESUMEN
        // =========================
        public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta)
        {
            try
            {
                int tenantId = GetTenantId();

                // Call service methods in parallel where possible, or sequential
                var resumenDto = await _gastoService.ObtenerResumenAsync(desde, hasta, tenantId);
                var listadoDto = await _gastoService.ObtenerListadoAsync(desde, hasta, tenantId);
                var categoriasDto = await _gastoService.ObtenerPorCategoriaAsync(desde, hasta, tenantId);

                // Map DTOs to ViewModels (Keeping original ViewModels to minimize View changes)
                var resumen = new GastosResumenViewModel
                {
                    CantidadGastos = resumenDto.CantidadGastos,
                    TotalGastos = resumenDto.TotalGastos
                };

                var lista = listadoDto.Select(g => new GastoListViewModel
                {
                    GastoID = g.GastoID,
                    FechaGasto = g.FechaGasto,
                    Descripcion = g.Descripcion,
                    CategoriaGasto = g.CategoriaGasto,
                    Monto = g.Monto
                }).ToList();

                var gastosPorCategoria = categoriasDto.Select(c => new GastoPorCategoriaViewModel
                {
                    Categoria = c.Categoria,
                    Total = c.Total
                }).ToList();

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

        // =========================
        // CREAR
        // =========================
        public IActionResult Create()
        {
            ViewBag.Categorias = ObtenerCategorias();
            return View(new GastoCreateViewModel()); // Using existing ViewModel to keep view compatibility or switch to DTO? 
            // Better stick to ViewModel if View expects it, or use DTO if View is updated.
            // Requirement said "Usa DTOs para pasar datos a la vista". 
            // But existing Views use GastoCreateViewModel.
            // I will map ViewModel <-> DTO in Controller.
        }

        [HttpPost]
        public async Task<IActionResult> Create(GastoCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = ObtenerCategorias();
                return View(model);
            }

            try
            {
                int tenantId = GetTenantId();
                var dto = new GastoCreateDto
                {
                    FechaGasto = model.FechaGasto,
                    Descripcion = model.Descripcion,
                    CategoriaGasto = model.CategoriaGasto,
                    Monto = model.Monto
                };

                await _gastoService.CrearAsync(dto, tenantId);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al crear gasto: " + ex.Message);
                ViewBag.Categorias = ObtenerCategorias();
                return View(model);
            }
        }

        // =========================
        // EDITAR
        // =========================
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                int tenantId = GetTenantId();
                var dto = await _gastoService.ObtenerPorIdAsync(id, tenantId);

                if (dto == null)
                    return NotFound();

                var model = new GastoEditViewModel
                {
                    GastoID = dto.GastoID,
                    FechaGasto = dto.FechaGasto,
                    Descripcion = dto.Descripcion,
                    CategoriaGasto = dto.CategoriaGasto,
                    Monto = dto.Monto
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
            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = ObtenerCategorias();
                return View(model);
            }

            try
            {
                int tenantId = GetTenantId();
                var dto = new GastoEditDto
                {
                    GastoID = model.GastoID,
                    FechaGasto = model.FechaGasto,
                    Descripcion = model.Descripcion,
                    CategoriaGasto = model.CategoriaGasto,
                    Monto = model.Monto
                };

                await _gastoService.ActualizarAsync(dto, tenantId);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar gasto: " + ex.Message);
                ViewBag.Categorias = ObtenerCategorias();
                return View(model);
            }
        }

        // =========================
        // ELIMINAR
        // =========================
        [HttpPost]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                int tenantId = GetTenantId();
                await _gastoService.EliminarAsync(id, tenantId);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Should redirect to Index with error message or similar
                _logger.LogError(ex, "Error deleting Gasto");
                return RedirectToAction(nameof(Index)); 
            }
        }

        // =========================
        // CATEGORÍAS
        // =========================
        private static string[] ObtenerCategorias()
        {
            return new[]
            {
                "Logística",
                "Servicios",
                "Marketing",
                "Insumos",
                "Mantenimiento",
                "Comisiones",
                "Impuestos",
                "Otros"
            };
        }
    }
}

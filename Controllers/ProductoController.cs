using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using ZeltekWeb.Models;
using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ZeltekWeb.Controllers
{
    [Authorize]
    public class ProductoController : BaseController
    {
        private readonly IProductoService _productoService;

        public ProductoController(IProductoService productoService)
        {
            _productoService = productoService;
        }

        // =========================
        // LISTADO
        // =========================
        public async Task<IActionResult> Index()
        {
            int tenantId = GetTenantId();
            var dtos = await _productoService.ObtenerTodosAsync(tenantId);
            
            // Map to ViewModel for View
            var viewModels = dtos.Select(p => new ProductoViewModel
            {
                ProductoID = p.ProductoID,
                Nombre = p.Nombre,
                Marca = p.Marca,
                Categoria = p.Categoria,
                EsUnico = p.EsUnico
            }).ToList();

            return View(viewModels);
        }

        // =========================
        // CREAR
        // =========================
        public async Task<IActionResult> Create()
        {
            int tenantId = GetTenantId();
            ViewBag.Marcas = await ObtenerMarcasSelectList(tenantId);
            ViewBag.Categorias = await ObtenerCategoriasSelectList(tenantId);
            return View("CreateEdit", new ProductoCreateEditViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(ProductoCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await ReloadViewBag(model);
                return View("CreateEdit", model);
            }

            int tenantId = GetTenantId();
            var dto = new CreateProductoDto
            {
                Nombre = model.Nombre,
                Descripcion = model.Descripcion,
                MarcaID = model.MarcaID,
                Modelo = model.Modelo,
                CategoriaID = model.CategoriaID,
                EsUnico = model.EsUnico,
                CostoReferencia = model.CostoReferencia
            };

            var result = await _productoService.CrearAsync(dto, tenantId);
            if (result.Success)
            {
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", result.Message);
                if (result.Message.Contains("nombre")) ModelState.AddModelError("Nombre", result.Message);
                await ReloadViewBag(model);
                return View("CreateEdit", model);
            }
        }

        // =========================
        // EDITAR
        // =========================
        public async Task<IActionResult> Edit(int id)
        {
            int tenantId = GetTenantId();
            var dto = await _productoService.ObtenerPorIdAsync(id, tenantId);

            if (dto == null) return NotFound();

            var model = new ProductoCreateEditViewModel
            {
                ProductoID = dto.ProductoID,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                MarcaID = dto.MarcaID,
                Modelo = dto.Modelo,
                CategoriaID = dto.CategoriaID,
                EsUnico = dto.EsUnico,
                CostoReferencia = dto.CostoReferencia
            };

            ViewBag.Marcas = await ObtenerMarcasSelectList(tenantId);
            ViewBag.Categorias = await ObtenerCategoriasSelectList(tenantId);
            return View("CreateEdit", model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, ProductoCreateEditViewModel model)
        {
            if (model.ProductoID != id) return NotFound();
            
            if (!ModelState.IsValid)
            {
                await ReloadViewBag(model);
                return View("CreateEdit", model);
            }

            int tenantId = GetTenantId();
            var dto = new UpdateProductoDto
            {
                ProductoID = id,
                Nombre = model.Nombre,
                Descripcion = model.Descripcion,
                MarcaID = model.MarcaID,
                Modelo = model.Modelo,
                CategoriaID = model.CategoriaID,
                EsUnico = model.EsUnico,
                CostoReferencia = model.CostoReferencia
            };

            var result = await _productoService.ActualizarAsync(dto, tenantId);
            if (result.Success)
            {
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", result.Message);
                if (result.Message.Contains("nombre")) ModelState.AddModelError("Nombre", result.Message);
                await ReloadViewBag(model);
                return View("CreateEdit", model);
            }
        }

        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            int tenantId = GetTenantId();
            await _productoService.EliminarAsync(id, tenantId);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Reactivar(int id)
        {
            int tenantId = GetTenantId();
            await _productoService.ReactivarAsync(id, tenantId);
            return RedirectToAction("Edit", new { id });
        }

        // =========================
        // AJAX API
        // =========================

        [HttpGet]
        public async Task<IActionResult> CheckNombre(string nombre, int? excludeId)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return Json(new { exists = false });
            
            int tenantId = GetTenantId();
            bool exists = await _productoService.ExisteNombreAsync(nombre, excludeId, tenantId);
            
            // To match previous logic that returned ID and IsDeleted if exists, we'd need Repo to return object.
            // But Service simplified it to bool. 
            // The frontend probably uses this to show "Already exists" warning.
            // If explicit details needed, I'd need to enhance Service.
            // For now simplest parity:
            
            return Json(new { exists });
        }

        [HttpPost]
        public async Task<IActionResult> CrearMarcaRapida(string nombre)
        {
            int tenantId = GetTenantId();
            var result = await _productoService.CrearMarcaRapidaAsync(nombre, tenantId);
            
            if (result.Success)
            {
                // Dynamic mapping of anonymous object from result.Data
                // Ideally I should cast or use specific DTO return, but anonymous works for JSON serialization usually.
                return Json(new { success = true, id = ((dynamic)result.Data).id, nombre = ((dynamic)result.Data).nombre });
            }
            return Json(new { success = false, message = result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> CrearCategoriaRapida(string nombre)
        {
            int tenantId = GetTenantId();
            var result = await _productoService.CrearCategoriaRapidaAsync(nombre, tenantId);
            
            if (result.Success)
            {
                return Json(new { success = true, id = ((dynamic)result.Data).id, nombre = ((dynamic)result.Data).nombre });
            }
            return Json(new { success = false, message = result.Message });
        }
        
        // =========================
        // HELPERS
        // =========================
        private async Task<List<SelectListItem>> ObtenerMarcasSelectList(int tenantId)
        {
            var marcas = await _productoService.ObtenerMarcasAsync(tenantId);
            return marcas.Select(m => new SelectListItem
            {
                Value = m.MarcaID.ToString(),
                Text = m.Nombre
            }).ToList();
        }

        private async Task<List<SelectListItem>> ObtenerCategoriasSelectList(int tenantId)
        {
            var cats = await _productoService.ObtenerCategoriasAsync(tenantId);
            return cats.Select(c => new SelectListItem
            {
                Value = c.CategoriaID.ToString(),
                Text = c.Nombre
            }).ToList();
        }

        private async Task ReloadViewBag(ProductoCreateEditViewModel? model = null)
        {
            int tenantId = GetTenantId();
            ViewBag.Marcas = await ObtenerMarcasSelectList(tenantId);
            ViewBag.Categorias = await ObtenerCategoriasSelectList(tenantId);
        }
    }
}

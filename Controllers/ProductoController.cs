using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ZeltekWeb.Models;
using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Services.Interfaces;

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

        public async Task<IActionResult> Index()
        {
            var dtos = await _productoService.ObtenerTodosAsync();
            var viewModels = dtos.Select(p => new ProductoViewModel
            {
                ProductoID = p.ProductoID, Nombre = p.Nombre, Marca = p.Marca,
                Categoria = p.Categoria, EsUnico = p.EsUnico
            }).ToList();
            return View(viewModels);
        }

        public async Task<IActionResult> Create()
        {
            await ReloadViewBag();
            return View("CreateEdit", new ProductoCreateEditViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(ProductoCreateEditViewModel model)
        {
            if (!ModelState.IsValid) { await ReloadViewBag(); return View("CreateEdit", model); }

            var dto = new CreateProductoDto
            {
                Nombre = model.Nombre, Descripcion = model.Descripcion, MarcaID = model.MarcaID,
                Modelo = model.Modelo, CategoriaID = model.CategoriaID, EsUnico = model.EsUnico,
                CostoReferencia = model.CostoReferencia
            };

            var result = await _productoService.CrearAsync(dto);
            if (result.Success) return RedirectToAction("Index");

            ModelState.AddModelError("", result.Message);
            if (result.Message.Contains("nombre")) ModelState.AddModelError("Nombre", result.Message);
            await ReloadViewBag();
            return View("CreateEdit", model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var dto = await _productoService.ObtenerPorIdAsync(id);
            if (dto == null) return NotFound();

            var model = new ProductoCreateEditViewModel
            {
                ProductoID = dto.ProductoID, Nombre = dto.Nombre, Descripcion = dto.Descripcion,
                MarcaID = dto.MarcaID, Modelo = dto.Modelo, CategoriaID = dto.CategoriaID,
                EsUnico = dto.EsUnico, CostoReferencia = dto.CostoReferencia
            };

            await ReloadViewBag();
            return View("CreateEdit", model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, ProductoCreateEditViewModel model)
        {
            if (model.ProductoID != id) return NotFound();
            if (!ModelState.IsValid) { await ReloadViewBag(); return View("CreateEdit", model); }

            var dto = new UpdateProductoDto
            {
                ProductoID = id, Nombre = model.Nombre, Descripcion = model.Descripcion,
                MarcaID = model.MarcaID, Modelo = model.Modelo, CategoriaID = model.CategoriaID,
                EsUnico = model.EsUnico, CostoReferencia = model.CostoReferencia
            };

            var result = await _productoService.ActualizarAsync(dto);
            if (result.Success) return RedirectToAction("Index");

            ModelState.AddModelError("", result.Message);
            if (result.Message.Contains("nombre")) ModelState.AddModelError("Nombre", result.Message);
            await ReloadViewBag();
            return View("CreateEdit", model);
        }

        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            await _productoService.EliminarAsync(id);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Reactivar(int id)
        {
            await _productoService.ReactivarAsync(id);
            return RedirectToAction("Edit", new { id });
        }

        [HttpGet]
        public async Task<IActionResult> CheckNombre(string nombre, int? excludeId)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return Json(new { exists = false });
            bool exists = await _productoService.ExisteNombreAsync(nombre, excludeId);
            return Json(new { exists });
        }

        [HttpPost]
        public async Task<IActionResult> CrearMarcaRapida(string nombre)
        {
            var result = await _productoService.CrearMarcaRapidaAsync(nombre);
            if (result.Success)
                return Json(new { success = true, id = ((dynamic)result.Data).id, nombre = ((dynamic)result.Data).nombre });
            return Json(new { success = false, message = result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> CrearCategoriaRapida(string nombre)
        {
            var result = await _productoService.CrearCategoriaRapidaAsync(nombre);
            if (result.Success)
                return Json(new { success = true, id = ((dynamic)result.Data).id, nombre = ((dynamic)result.Data).nombre });
            return Json(new { success = false, message = result.Message });
        }

        private async Task ReloadViewBag(ProductoCreateEditViewModel? model = null)
        {
            var marcas = await _productoService.ObtenerMarcasAsync();
            ViewBag.Marcas = marcas.Select(m => new SelectListItem { Value = m.MarcaID.ToString(), Text = m.Nombre }).ToList();
            var cats = await _productoService.ObtenerCategoriasAsync();
            ViewBag.Categorias = cats.Select(c => new SelectListItem { Value = c.CategoriaID.ToString(), Text = c.Nombre }).ToList();
        }
    }
}

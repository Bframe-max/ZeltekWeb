using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZeltekWeb.Models;
using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Services.Interfaces;

namespace ZeltekWeb.Controllers
{
    [Authorize]
    public class InventarioController : BaseController
    {
        private readonly IInventarioService _inventarioService;

        public InventarioController(IInventarioService inventarioService)
        {
            _inventarioService = inventarioService;
        }

        public async Task<IActionResult> Index(string buscar, string filtro = "Stock")
        {
            var (items, totalLaptops, totalAccesorios) = await _inventarioService.ObtenerInventarioAsync(buscar, filtro);

            var viewModels = items.Select(dto => new ItemInventarioViewModel
            {
                ItemID = dto.ItemID, ProductoID = dto.ProductoID, ProductoNombre = dto.ProductoNombre,
                NumeroSerie = dto.NumeroSerie, Estado = dto.Estado, CostoTotal = dto.CostoTotal,
                PrecioVenta = dto.PrecioVenta, Procesador = dto.Procesador ?? "", RAM_GB = dto.RAM_GB,
                Almacenamiento_GB = dto.Almacenamiento_GB, CantidadInicial = dto.CantidadInicial,
                CantidadActual = dto.CantidadActual, EsUnico = dto.EsUnico, Color = dto.Color ?? ""
            }).ToList();

            var model = new InventarioIndexViewModel
            {
                Laptops = viewModels.Where(x => x.EsUnico).ToList(),
                Accesorios = viewModels.Where(x => !x.EsUnico).ToList(),
                Busqueda = buscar,
                FiltroActual = filtro
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Recibir(RecepcionProductoViewModel model)
        {
            if (model.ItemID <= 0 || model.CompraID <= 0)
                return BadRequest(new { success = false, message = "Datos inválidos" });

            var dto = new RecepcionProductoDto
            {
                ItemID = model.ItemID, CompraID = model.CompraID, NumeroSerie = model.NumeroSerie,
                Condicion = model.Condicion, Procesador = model.Procesador, Ram = model.Ram,
                Almacenamiento = model.Almacenamiento, Color = model.Color, CostoEnvio = model.CostoEnvio,
                CostoOtros = model.CostoOtros, RequiereCargadorDeStock = model.RequiereCargadorDeStock,
                IdCargador = model.IdCargador, Usuario = User.Identity?.Name ?? "System"
            };

            var result = await _inventarioService.RecibirProductoAsync(dto);
            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> RecibirCompra(int compraId, bool omitirRevision = false)
        {
            await _inventarioService.MarcarCompraRecibidaManualAsync(compraId, omitirRevision);
            return RedirectToAction("Index", "Compras");
        }

        public async Task<IActionResult> Calidad()
        {
            var lista = await _inventarioService.ObtenerPorRevisarAsync();
            return View(lista);
        }

        [HttpGet]
        public async Task<IActionResult> Inspeccion(int id)
        {
            var item = await _inventarioService.ObtenerPorIdAsync(id);
            if (item == null || item.Estado != "Por Revisar") return NotFound();

            decimal costoUnitario = item.CostoTotal;
            if (!item.EsUnico && item.CantidadActual > 0)
                costoUnitario = item.CostoTotal / item.CantidadActual;

            var model = new InspeccionCalidadViewModel
            {
                ItemID = item.ItemID, ProductoNombre = item.ProductoNombre, NumeroSerie = item.NumeroSerie,
                EsUnico = item.EsUnico, CantidadTotal = item.CantidadActual,
                CantidadAprobada = item.CantidadActual, CostoUnitario = costoUnitario
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Inspeccion(int id, string resultado, InspeccionCalidadViewModel model)
        {
            if (model.ItemID != id) return BadRequest();

            bool isApproved = string.Equals(resultado, "aprobar", StringComparison.OrdinalIgnoreCase);
            string? motivo = isApproved ? null : (model.MotivoRechazo ?? "Rechazado en inspección");
            if (model.EsUnico && !isApproved) motivo = model.NotasInspeccion;

            var dto = new InspeccionCalidadDto
            {
                ItemID = model.ItemID, ProductoNombre = model.ProductoNombre, NumeroSerie = model.NumeroSerie,
                EsUnico = model.EsUnico, CantidadTotal = model.CantidadTotal, CantidadAprobada = model.CantidadAprobada,
                CostoUnitario = model.CostoUnitario, Condicion = model.Condicion,
                SaludBateriaPorcentaje = model.SaludBateriaPorcentaje, NotasInspeccion = model.NotasInspeccion,
                MotivoRechazo = motivo
            };

            await _inventarioService.ProcesarInspeccionAsync(dto);
            return RedirectToAction(nameof(Calidad));
        }

        public async Task<IActionResult> Reparaciones()
        {
            var lista = await _inventarioService.ObtenerReparacionesActivasAsync();
            return View(lista);
        }

        [HttpPost]
        public async Task<IActionResult> FinalizarReparacion(int itemId, decimal costoRepuestos, decimal costoManoObra, string? notasReparacion)
        {
            var dto = new FinalizarReparacionDto
            {
                ItemID = itemId, CostoRepuestos = costoRepuestos,
                CostoManoObra = costoManoObra, NotasReparacion = notasReparacion
            };
            await _inventarioService.FinalizarReparacionAsync(dto);
            return RedirectToAction(nameof(Reparaciones));
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> DeclararPerdidaReparacion(int itemId, string motivo)
        {
            await _inventarioService.RegistrarPerdidaReparacionAsync(itemId, motivo);
            return RedirectToAction(nameof(Reparaciones));
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> ActualizarPrecio(int itemId, decimal nuevoPrecio)
        {
            var result = await _inventarioService.ActualizarPrecioAsync(itemId, nuevoPrecio);
            return Json(result);
        }
    }
}

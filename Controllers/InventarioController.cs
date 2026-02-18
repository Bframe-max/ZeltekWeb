using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ZeltekWeb.Models;
using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Services.Interfaces;

namespace ZeltekWeb.Controllers
{
    [Authorize]
    [Authorize]
    public class InventarioController : BaseController
    {
        private readonly IInventarioService _inventarioService;

        public InventarioController(IInventarioService inventarioService)
        {
            _inventarioService = inventarioService;
        }

        /// <summary>
        /// Index con tabs: Laptops (EsUnico == true) y Accesorios (EsUnico == false).
        /// </summary>
        public async Task<IActionResult> Index(string buscar, string filtro = "Stock")
        {
            int tenantId = GetTenantId();
            var (items, totalLaptops, totalAccesorios) = await _inventarioService.ObtenerInventarioAsync(buscar, filtro, tenantId);

            // Map DTOs to ViewModel
            var viewModels = items.Select(dto => new ItemInventarioViewModel
            {
                ItemID = dto.ItemID,
                ProductoID = dto.ProductoID,
                ProductoNombre = dto.ProductoNombre,
                NumeroSerie = dto.NumeroSerie,
                Estado = dto.Estado,
                CostoTotal = dto.CostoTotal,
                PrecioVenta = dto.PrecioVenta,
                Procesador = dto.Procesador ?? "",
                RAM_GB = dto.RAM_GB,
                Almacenamiento_GB = dto.Almacenamiento_GB,
                CantidadInicial = dto.CantidadInicial,
                CantidadActual = dto.CantidadActual,
                EsUnico = dto.EsUnico,
                Color = dto.Color ?? ""
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

            int tenantId = GetTenantId();
            var usuario = User.Identity?.Name ?? "System";

            var dto = new RecepcionProductoDto
            {
                ItemID = model.ItemID,
                CompraID = model.CompraID,
                NumeroSerie = model.NumeroSerie,
                Condicion = model.Condicion,
                Procesador = model.Procesador,
                Ram = model.Ram,
                Almacenamiento = model.Almacenamiento,
                Color = model.Color,
                CostoEnvio = model.CostoEnvio,
                CostoOtros = model.CostoOtros,
                RequiereCargadorDeStock = model.RequiereCargadorDeStock,
                IdCargador = model.IdCargador,
                Usuario = usuario
            };

            var result = await _inventarioService.RecibirProductoAsync(dto, tenantId);

            return Json(result); // ServiceResult has Success and Message matching frontend expectation
        }

        [HttpPost]
        public async Task<IActionResult> RecibirCompra(int compraId, bool omitirRevision = false)
        {
            int tenantId = GetTenantId();
            await _inventarioService.MarcarCompraRecibidaManualAsync(compraId, omitirRevision, tenantId);
            return RedirectToAction("Index", "Compras");
        }

        public async Task<IActionResult> Calidad()
        {
            int tenantId = GetTenantId();
            var lista = await _inventarioService.ObtenerPorRevisarAsync(tenantId);
            return View(lista);
        }

        [HttpGet]
        public async Task<IActionResult> Inspeccion(int id)
        {
            int tenantId = GetTenantId();
            var item = await _inventarioService.ObtenerPorIdAsync(id, tenantId);

            if (item == null || item.Estado != "Por Revisar") return NotFound();

            // Calculate Unit Cost roughly if needed or assume Total is Unit for Unicos
            decimal costoUnitario = item.CostoTotal;
            if (!item.EsUnico && item.CantidadActual > 0)
                costoUnitario = item.CostoTotal / item.CantidadActual; // Approximate logic matching controller

            var model = new InspeccionCalidadViewModel
            {
                ItemID = item.ItemID,
                ProductoNombre = item.ProductoNombre,
                NumeroSerie = item.NumeroSerie,
                EsUnico = item.EsUnico,
                CantidadTotal = item.CantidadActual, // Or Inicial? Controller used Actual.
                CantidadAprobada = item.CantidadActual,
                CostoUnitario = costoUnitario
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Inspeccion(int id, string resultado, InspeccionCalidadViewModel model)
        {
            if (model.ItemID != id) return BadRequest();
            int tenantId = GetTenantId();

            // Map Logic
            // If result != "aprobar" -> Reject/Repair
            // Controller logic: irAReparacion = !string.Equals(resultado, "aprobar", StringComparison.OrdinalIgnoreCase);
            
            // We pass data to DTO. Service handles "Outcome".
            // But DTO didn't have "Outcome" field...
            // "MotivoRechazo" was used for Lotes.
            // "NotasInspeccion" used for Repairs.
            
            // Let's populate MotivoRechazo if result is not aprobar?
            // Or add explicit field to DTO. 
            // I'll reuse fields.
            
            bool isApproved = string.Equals(resultado, "aprobar", StringComparison.OrdinalIgnoreCase);
            string? motivo = isApproved ? null : (model.MotivoRechazo ?? "Rechazado en inspección");
            if (model.EsUnico && !isApproved) motivo = model.NotasInspeccion; // Map Notes to Motivo for Repair logic if needed or just pass Notes

            var dto = new InspeccionCalidadDto
            {
                ItemID = model.ItemID,
                ProductoNombre = model.ProductoNombre, // Not strictly needed by Repo
                NumeroSerie = model.NumeroSerie, // Not strictly needed
                EsUnico = model.EsUnico,
                CantidadTotal = model.CantidadTotal,
                CantidadAprobada = model.CantidadAprobada,
                CostoUnitario = model.CostoUnitario,
                Condicion = model.Condicion,
                SaludBateriaPorcentaje = model.SaludBateriaPorcentaje,
                NotasInspeccion = model.NotasInspeccion,
                MotivoRechazo = motivo 
            };
            
            // If Unique and Not Approved -> Ensure Motivo/Notes are set so Service triggers Repair.
            // Service logic: "bool irAReparacion = !string.IsNullOrEmpty(dto.NotasInspeccion) || ..."
            // So copying Notes is good.

            await _inventarioService.ProcesarInspeccionAsync(dto, tenantId);
            return RedirectToAction(nameof(Calidad));
        }

        public async Task<IActionResult> Reparaciones()
        {
            int tenantId = GetTenantId();
            var lista = await _inventarioService.ObtenerReparacionesActivasAsync(tenantId);
            return View(lista);
        }

        [HttpPost]
        public async Task<IActionResult> FinalizarReparacion(int itemId, decimal costoRepuestos, decimal costoManoObra, string? notasReparacion)
        {
            int tenantId = GetTenantId();
            var dto = new FinalizarReparacionDto
            {
                ItemID = itemId,
                CostoRepuestos = costoRepuestos,
                CostoManoObra = costoManoObra,
                NotasReparacion = notasReparacion
            };
            await _inventarioService.FinalizarReparacionAsync(dto, tenantId);
            return RedirectToAction(nameof(Reparaciones));
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> DeclararPerdidaReparacion(int itemId, string motivo)
        {
            int tenantId = GetTenantId();
            await _inventarioService.RegistrarPerdidaReparacionAsync(itemId, motivo, tenantId);
            return RedirectToAction(nameof(Reparaciones));
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> ActualizarPrecio(int itemId, decimal nuevoPrecio)
        {
            int tenantId = GetTenantId();
            var result = await _inventarioService.ActualizarPrecioAsync(itemId, nuevoPrecio, tenantId);
            return Json(result);
        }
    }
}
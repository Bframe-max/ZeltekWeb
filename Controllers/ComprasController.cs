using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ZeltekWeb.Models;
using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Services;
using ZeltekWeb.Services.Interfaces;

namespace ZeltekWeb.Controllers
{
    [Authorize]
    public class ComprasController : BaseController
    {
        private readonly ICompraService _compraService;
        private readonly IProductoService _productoService;
        
        public ComprasController(ICompraService compraService, IProductoService productoService)
        {
            _compraService = compraService;
            _productoService = productoService;
        }

        // ==========================================
        // 1. LISTADO (Index)
        // ==========================================
        public async Task<IActionResult> Index(string buscar, string viewType = "transit")
        {
            int tenantId = GetTenantId();
            var (items, kpiActivos, kpiInversion) = await _compraService.ObtenerHistorialAsync(buscar, viewType, tenantId);

            var viewModels = items.Select(dto => new CompraListViewModel
            {
                CompraID = dto.CompraID,
                Fecha = dto.FechaCompra,
                Vendedor = string.IsNullOrEmpty(dto.VendedorOnline) ? "-" : dto.VendedorOnline,
                Tracking = string.IsNullOrEmpty(dto.TrackingProveedor) ? "-" : dto.TrackingProveedor,
                Total = dto.CostoTotal,
                Estado = dto.EstadoCompra,
                CantidadItems = dto.CantidadItems,
                ItemsPendientes = dto.ItemsPendientes,
                ProductoPrincipal = dto.ProductoPrincipal
            }).ToList();

            ViewData["BusquedaActual"] = buscar;
            ViewBag.CurrentFilter = viewType;
            ViewBag.KpiActivos = kpiActivos;
            ViewBag.KpiInversion = kpiInversion;

            return View(viewModels);
        }

        // ==========================================
        // API: VERIFICAR SI TRACKING YA EXISTE
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> VerificarTracking(string tracking)
        {
            int tenantId = GetTenantId();
            bool existe = await _compraService.VerificarTrackingAsync(tracking, null, tenantId);
            return Json(existe);
        }

        // ==========================================
        // API: GET ITEM INFO
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetItemInfo(int itemId)
        {
            int tenantId = GetTenantId();
            var (esAccesorio, nombre) = await _compraService.GetItemInfoAsync(itemId, tenantId);
            return Json(new { esAccesorio, productoNombre = nombre });
        }

        // ==========================================
        // API: VERIFICAR SERIAL
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> VerificarSerial(string serial)
        {
            int tenantId = GetTenantId();
            bool existe = await _compraService.VerificarSerialAsync(serial, null, tenantId);
            return Json(existe);
        }

        // ==========================================
        // 2. CREAR (GET)
        // ==========================================
        public async Task<IActionResult> Create()
        {
            await CargarListaProductos();
            return View(new CompraCreateViewModel());
        }

        // ==========================================
        // 3. GUARDAR (POST)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> Create(CompraCreateViewModel model)
        {
            int tenantId = GetTenantId();

            if (model.Cantidad <= 0) ModelState.AddModelError("Cantidad", "La cantidad debe ser mayor a 0.");
            if (model.CostoTotalCompra <= 0) ModelState.AddModelError("CostoTotalCompra", "El costo total de la compra debe ser mayor a 0.");
            if ((model.CostoEnvioTotal ?? 0) < 0) ModelState.AddModelError("CostoEnvioTotal", "El costo de envío no puede ser negativo.");

            if (!ModelState.IsValid)
            {
                await CargarListaProductos();
                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(model.TrackingProveedor))
            {
                if (await _compraService.VerificarTrackingAsync(model.TrackingProveedor, null, tenantId))
                {
                    ModelState.AddModelError("TrackingProveedor", "Este tracking ya fue registrado.");
                    await CargarListaProductos();
                    return View(model);
                }
            }

            try
            {
                var dto = new CreateCompraDto
                {
                    Vendedor = model.Vendedor,
                    TrackingProveedor = model.TrackingProveedor,
                    FechaCompra = model.FechaCompra,
                    ProductoID = model.ProductoID,
                    Cantidad = model.Cantidad,
                    CostoTotalCompra = model.CostoTotalCompra,
                    CostoEnvioTotal = model.CostoEnvioTotal,
                    Usuario = User.Identity?.Name ?? "System"
                };

                await _compraService.CrearCompraAsync(dto, tenantId);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al registrar la compra: " + ex.Message);
                await CargarListaProductos();
                return View(model);
            }
        }

        // ==========================================
        // 4. VER DETALLE (Details)
        // ==========================================
        public async Task<IActionResult> Details(int id)
        {
            int tenantId = GetTenantId();
            var compra = await _compraService.ObtenerPorIdAsync(id, tenantId);
            
            if (compra == null) return NotFound();

            var modelo = new CompraDetalleViewModel
            {
                CompraID = compra.CompraID,
                Vendedor = compra.VendedorOnline,
                Tracking = string.IsNullOrEmpty(compra.TrackingProveedor) ? "-" : compra.TrackingProveedor,
                Fecha = compra.FechaCompra,
                CostoEnvio = compra.CostoEnvioTotal,
                CostoTotal = compra.CostoTotal,
                EstadoCompra = compra.EstadoCompra
            };

            var items = await _compraService.ObtenerItemsPorCompraAsync(id, tenantId);
            foreach(var item in items)
            {
                modelo.Items.Add(new ItemEnCompra
                {
                    ItemID = item.ItemID,
                    ProductoNombre = item.ProductoNombre,
                    Costo = item.CostoCompra,
                    Estado = item.Estado,
                    NumeroSerie = string.IsNullOrEmpty(item.NumeroSerie) ? "-" : item.NumeroSerie,
                    EsAccesorio = item.EsAccesorio,
                    EsUnico = item.EsUnico,
                    Procesador = item.Procesador,
                    Ram = item.Ram,
                    Almacenamiento = item.Almacenamiento
                });
            }

            await CargarCargadores();
            return View(modelo);
        }

        // ==========================================
        // 5. ACCIÓN: RECIBIR ITEM
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> RecibirItem(
            int itemId,
            int compraIdRetorno,
            bool productoNuevo,
            string serieReal,
            decimal costoEnvio,
            decimal costoReparacion,
            string condicion,
            string bateria,
            int? ram,
            int? almacenamiento,
            string procesador
        )
        {
            int tenantId = GetTenantId();
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            var dto = new RecibirItemDto
            {
                ItemID = itemId,
                CompraID = compraIdRetorno,
                ProductoNuevo = productoNuevo,
                SerieReal = serieReal,
                CostoEnvio = costoEnvio,
                CostoReparacion = costoReparacion,
                Condicion = condicion,
                Bateria = bateria,
                Ram = ram,
                Almacenamiento = almacenamiento,
                Procesador = procesador
            };

            var result = await _compraService.RecibirItemAsync(dto, tenantId);

            if (!result.Success)
            {
                if (isAjax) return Json(new { success = false, message = result.Message });
                TempData["ErrorRecibir"] = result.Message;
                return RedirectToAction("Details", new { id = compraIdRetorno });
            }

            if (isAjax) return Json(new { success = true, message = result.Message });
            return RedirectToAction("Details", new { id = compraIdRetorno });
        }

        // ==========================================
        // 6. EDITAR SEGUIMIENTO (GET)
        // ==========================================
        public async Task<IActionResult> Edit(int id)
        {
            int tenantId = GetTenantId();
            var compra = await _compraService.ObtenerPorIdAsync(id, tenantId);
            if (compra == null) return NotFound();

            var model = new CompraCreateViewModel
            {
                Vendedor = compra.VendedorOnline,
                TrackingProveedor = compra.TrackingProveedor ?? ""
            };
            
            ViewBag.CompraID = id;
            ViewBag.EstadoActual = compra.EstadoCompra;
            ViewBag.CostoTotal = compra.CostoTotal;
            
            return View(model);
        }

        // ==========================================
        // 7. GUARDAR EDICIÓN (POST)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> Edit(int id, string? tracking, string estado, decimal costoTotal)
        {
            int tenantId = GetTenantId();
            var trackingTrim = tracking?.Trim() ?? "";

            if (!string.IsNullOrEmpty(trackingTrim))
            {
                if (await _compraService.VerificarTrackingAsync(trackingTrim, id, tenantId))
                {
                     var model = new CompraCreateViewModel
                    {
                        TrackingProveedor = trackingTrim,
                        Vendedor = ""
                    };
                    ViewBag.CompraID = id;
                    ViewBag.EstadoActual = estado;
                    ViewBag.CostoTotal = costoTotal;
                    ModelState.AddModelError("TrackingProveedor", "Este tracking ya fue registrado en otra compra.");
                    return View(model);
                }
            }

            var updateDto = new UpdateCompraDto
            {
                CompraID = id,
                TrackingProveedor = trackingTrim,
                EstadoCompra = estado,
                CostoTotal = costoTotal
            };

            await _compraService.ActualizarCompraAsync(updateDto, tenantId);
            return RedirectToAction("Index");
        }

        // ==========================================
        // 8. ELIMINAR COMPRA
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Eliminar(int id)
        {
            int tenantId = GetTenantId();
            try
            {
                await _compraService.EliminarCompraAsync(id, tenantId);
                TempData["SuccessEliminar"] = "Compra eliminada correctamente.";
            }
            catch (Exception ex)
            {
                TempData["ErrorEliminar"] = "Error al eliminar: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        // ==========================================
        // 9. FORZAR CIERRE
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> ForzarCierre(int id)
        {
            int tenantId = GetTenantId();
            await _compraService.ForzarCierreAsync(id, tenantId);
            TempData["SuccessEliminar"] = "Compra cerrada manualmente (marcada como Completada)."; 
            return RedirectToAction("Index");
        }

        // ==========================================
        // PRIVATE HELPERS
        // ==========================================
        private async Task CargarListaProductos()
        {
            int tenantId = GetTenantId();
            var productos = await _productoService.ObtenerTodosAsync(tenantId);
            
            ViewBag.Productos = productos.Select(p => new SelectListItem
            {
                Value = p.ProductoID.ToString(),
                Text = p.Nombre
            }).ToList();
        }

        private async Task CargarCargadores()
        {
            int tenantId = GetTenantId();
            var cargadores = await _productoService.ObtenerCargadoresYAccesoriosAsync(tenantId);
            
            ViewBag.Cargadores = cargadores.Select(c => new SelectListItem
            {
                Value = c.ProductoID.ToString(),
                Text = c.Nombre
            }).ToList();
        }
    }
}
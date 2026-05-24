using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ZeltekWeb.Models;
using ZeltekWeb.Services.Interfaces;

namespace ZeltekWeb.Controllers
{
    [Authorize]
    public class ReportesController : BaseController
    {
        private readonly IReporteService _reporteService;
        private readonly ILogger<ReportesController> _logger;

        // Constructor injection of Service and Logger
        public ReportesController(IReporteService reporteService, ILogger<ReportesController> logger)
        {
            _reporteService = reporteService;
            _logger = logger;
        }

        /// <summary>
        /// Dashboard de reportes: tendencia, top productos, inventario dormido, valor inventario.
        /// </summary>
        public async Task<IActionResult> Index(int meses = 12, int periodoGanancia = 1)
        {
            if (meses != 6 && meses != 12) meses = 12;
            if (periodoGanancia != 1 && periodoGanancia != 3 && periodoGanancia != 12) periodoGanancia = 1;

            try
            {
                var dashDto = await _reporteService.ObtenerDashboardAsync(meses, periodoGanancia);
                
                // Manual mapping to ViewModel to decouple View from DTOs
                var model = new ReporteIndexViewModel
                {
                    MesesTendencia = meses,
                    PeriodoGananciaMeses = periodoGanancia,
                    TendenciaMeses = dashDto.Tendencia.Select(t => new ReporteTendenciaMensualViewModel
                    {
                        Mes = t.Mes,
                        MesLabel = t.MesLabel,
                        TotalVentas = t.TotalVentas,
                        CantidadVentas = t.CantidadVentas,
                        CantidadEquipos = t.CantidadEquipos
                    }).ToList(),
                    TopProductos = dashDto.TopProductos.Select(p => new ReporteTopProductoViewModel
                    {
                        ProductoNombre = p.ProductoNombre,
                        CantidadVentas = p.CantidadVentas,
                        TotalVendido = p.TotalVendido
                    }).ToList(),
                    IngresoTotal = dashDto.ResumenFinanciero.IngresoTotal,
                    GananciaBrutaReal = dashDto.ResumenFinanciero.GananciaBruta,
                    TotalGastosOperativos = dashDto.ResumenFinanciero.GastosOperativos,
                    ValorInventarioActual = dashDto.ValorInventario.TotalValor,
                    InventarioDormido = dashDto.InventarioDormido.Select(i => new ReporteInventarioDormidoViewModel
                    {
                        ItemID = i.ItemID,
                        ProductoNombre = i.ProductoNombre,
                        NumeroSerie = i.NumeroSerie,
                        DiasEnStock = i.DiasEnStock,
                        CostoTotal = i.CostoTotal,
                        PrecioVenta = i.PrecioVenta,
                        Procesador = i.Procesador,
                        RAM_GB = i.RAM_GB,
                        Almacenamiento_GB = i.Almacenamiento_GB
                    }).ToList(),
                    TopRevendedores = dashDto.TopRevendedores.Select(r => new TopRevendedorViewModel
                    {
                        NombreCliente = r.NombreCliente,
                        TipoCliente = r.TipoCliente,
                        CantidadCompras = r.CantidadCompras,
                        TotalComprado = r.TotalComprado,
                        GananciaGenerada = r.GananciaGenerada,
                        UltimaCompra = r.UltimaCompra
                    }).ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report dashboard");
                // Return a friendly error view instead of crash
                return View("Error", new ErrorViewModel { RequestId = "Error loading reports: " + ex.Message });
            }
        }
    }
}

namespace ZeltekWeb.Models
{
    /// <summary>
    /// Modelo principal de la página Index de Reportes: tendencia, top productos, inventario dormido, valor y ganancia neta.
    /// </summary>
    public class ReporteIndexViewModel
    {
        public List<ReporteTendenciaMensualViewModel> TendenciaMeses { get; set; } = new();
        public List<ReporteTopProductoViewModel> TopProductos { get; set; } = new();
        public List<ReporteInventarioDormidoViewModel> InventarioDormido { get; set; } = new();
        public List<TopRevendedorViewModel> TopRevendedores { get; set; } = new();
        public decimal ValorInventarioActual { get; set; }
        public int MesesTendencia { get; set; } = 12; // 6 o 12 meses para el gráfico
        /// <summary>Período para Ganancia Neta: 1, 3 o 12 meses.</summary>
        public int PeriodoGananciaMeses { get; set; } = 1;

        /// <summary>Ganancia Bruta = Total ventas del período (últimos N meses).</summary>
        public decimal GananciaBrutaVentas { get; set; }
        /// <summary>Total gastos operativos del mismo período.</summary>
        public decimal TotalGastosOperativos { get; set; }
        /// <summary>Ganancia Neta = Ganancia Bruta - Gastos Operativos.</summary>
        public decimal GananciaNeta => GananciaBrutaVentas - TotalGastosOperativos;
    }
}

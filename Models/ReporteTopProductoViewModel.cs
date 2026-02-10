namespace ZeltekWeb.Models
{
    /// <summary>
    /// Producto más vendido para el reporte "Qué comprar en eBay".
    /// </summary>
    public class ReporteTopProductoViewModel
    {
        public string ProductoNombre { get; set; } = "";
        public int CantidadVentas { get; set; }
        public decimal TotalVendido { get; set; }
    }
}

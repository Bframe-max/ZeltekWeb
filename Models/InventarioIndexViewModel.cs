namespace ZeltekWeb.Models
{
    /// <summary>
    /// Vista Index de Inventario: Laptops (serializados) y Accesorios (por stock/lote).
    /// </summary>
    public class InventarioIndexViewModel
    {
        public List<ItemInventarioViewModel> Laptops { get; set; } = new();
        public List<ItemInventarioViewModel> Accesorios { get; set; } = new();
        public string? Busqueda { get; set; }
        public string FiltroActual { get; set; } = "Stock";
    }
}

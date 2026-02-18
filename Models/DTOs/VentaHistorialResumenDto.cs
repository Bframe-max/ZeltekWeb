namespace ZeltekWeb.Models.DTOs
{
    public class VentaHistorialResumenDto
    {
        public IEnumerable<VentaListViewModel> Ventas { get; set; } = new List<VentaListViewModel>();
        public decimal TotalVendido { get; set; }
        public decimal TotalGanancia { get; set; }
        public int TotalEquipos { get; set; }
    }
}

namespace ZeltekWeb.Models
{
    public class VentaRapidaViewModel
    {
        // ===== CLIENTE =====
        public string NombreCliente { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string TipoCliente { get; set; } = "Regular"; // Regular | Revendedor

        // ===== VENTA =====
        public DateTime FechaVenta { get; set; } = DateTime.Today;
        public string MetodoPago { get; set; } = "Efectivo";

        // ===== PRODUCTO =====
        public string? NumeroSerie { get; set; }
        public int? ProductoID { get; set; }
        public decimal PrecioUnitario { get; set; }
        public int GarantiaMeses { get; set; } = 0;
    }
}

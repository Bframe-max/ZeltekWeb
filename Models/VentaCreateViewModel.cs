namespace ZeltekWeb.Models
{
    public class VentaCreateViewModel
    {
        public int ItemID { get; set; }
        public int ProductoID { get; set; }

        public string? Producto { get; set; }
        public string? NumeroSerie { get; set; }

        public decimal PrecioUnitario { get; set; }

        public int GarantiaMeses { get; set; } = 0;

        // ===== CLIENTE =====
        public int? ClienteID { get; set; }
        public string? Cedula { get; set; }
        public string? NombreCompleto { get; set; }
        public string? Telefono { get; set; }
        public string TipoCliente { get; set; } = "Regular"; // Regular | Revendedor

        // ===== VENTA =====
        public string? MetodoPago { get; set; }
    }
}

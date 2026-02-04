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

        public int? ClienteID { get; set; }
        public string? MetodoPago { get; set; }
    }
}

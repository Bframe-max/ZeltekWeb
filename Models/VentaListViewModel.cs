namespace ZeltekWeb.Models
{
    public class VentaListViewModel
    {
        public int VentaID { get; set; }
        public DateTime FechaVenta { get; set; }

        public string Producto { get; set; } = "";
        public string? NumeroSerie { get; set; }

        // 🔹 COSTOS Y PRECIOS
        public decimal Costo { get; set; }
        public decimal PrecioVenta { get; set; }

        // 🔹 GANANCIA (ahora editable)
        public decimal Ganancia { get; set; }

        public string MetodoPago { get; set; } = "";
    }
}

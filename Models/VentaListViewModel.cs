namespace ZeltekWeb.Models
{
    public class VentaListViewModel
    {
        public int VentaID { get; set; }
        public DateTime FechaVenta { get; set; }
        public string Producto { get; set; } = "";
        public string? NumeroSerie { get; set; }

        // COSTOS Y PRECIOS
        public decimal Costo { get; set; }
        public decimal PrecioVenta { get; set; }
        public decimal Ganancia { get; set; } // Esta es la clave 💰

        public string MetodoPago { get; set; } = "";
        public string Especificaciones { get; set; }
        public string Cliente { get; set; }

        // --- AGREGA SOLO ESTA LÍNEA PARA EL BADGE AMARILLO ---
        public string TipoCliente { get; set; }
    }
}
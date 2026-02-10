namespace ZeltekWeb.Models
{
    /// <summary>
    /// Ítem en stock hace más de 60 días (alerta de aging).
    /// </summary>
    public class ReporteInventarioDormidoViewModel
    {
        public int ItemID { get; set; }
        public string ProductoNombre { get; set; } = "";
        public string NumeroSerie { get; set; } = "";
        public int DiasEnStock { get; set; }
        public decimal CostoTotal { get; set; }
        public decimal PrecioVenta { get; set; }
        public string? Procesador { get; set; }
        public int RAM_GB { get; set; }
        public int Almacenamiento_GB { get; set; }
    }
}

namespace ZeltekWeb.Models
{
    public class ReporteGananciaNetaViewModel
    {
        public decimal TotalVentas { get; set; }
        public decimal TotalCosto { get; set; }
        public decimal TotalGastos { get; set; }
        public decimal GananciaNeta { get; set; }

        public int CantidadVentas { get; set; }
        public int CantidadGastos { get; set; }

        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
    }
}

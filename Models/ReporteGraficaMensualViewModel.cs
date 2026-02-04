namespace ZeltekWeb.Models
{
    public class ReporteGraficaMensualViewModel
    {
        public string? Mes { get; set; }

        public decimal Ventas { get; set; }

        public decimal Costos { get; set; }   // 👈 ESTE FALTABA

        public decimal Gastos { get; set; }

        public decimal Ganancia
        {
            get { return Ventas - Costos - Gastos; }
        }
    }
}

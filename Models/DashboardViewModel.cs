namespace ZeltekWeb.Models
{
    public class DashboardViewModel
    {
        public decimal CapitalInvertido { get; set; }
        public decimal ValorInventario { get; set; }
        public decimal VentasMes { get; set; }
        public decimal GananciaMes { get; set; }

        public int GarantiasActivas { get; set; }
        public int EquiposEnCamino { get; set; }
    }
}

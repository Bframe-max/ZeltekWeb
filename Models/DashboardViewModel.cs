namespace ZeltekWeb.Models
{
    public class DashboardViewModel
    {
        public decimal ValorInventario { get; set; }
        public decimal EnCamino { get; set; }
        public decimal PorCobrar { get; set; }

        public int CantidadVendidos { get; set; }
        public decimal GananciaTotal { get; set; }

        // Un total simple de tus ACTIVOS (Mercadería + Deudas)
        // OJO: No sumamos caja aquí porque no la estamos trayendo.
        public decimal TotalActivos => ValorInventario + EnCamino + PorCobrar;
    }
}
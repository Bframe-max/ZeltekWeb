namespace ZeltekWeb.Models
{
    /// <summary>
    /// Dashboard principal operativo: pulso del día, stock, accesos rápidos, actividad reciente.
    /// </summary>
    public class DashboardViewModel
    {
        // A. El Pulso del Día (KPIs diarios)
        public decimal VentasHoy { get; set; }
        public int EnCaminoCantidad { get; set; }   // Equipos en camino (eBay)
        public decimal EnCaminoMonto { get; set; }   // $ en vuelo
        public int PorRevisarCantidad { get; set; } // Equipos por revisar
        public int StockCantidad { get; set; }      // Ítems en bodega (Estado = 'En Stock')
        public decimal StockValor { get; set; }     // Valor total en bodega (CostoCompra + gastos)

        public List<ActividadRecienteViewModel> ActividadReciente { get; set; } = new();
    }
}
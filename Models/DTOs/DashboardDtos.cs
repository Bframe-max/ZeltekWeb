namespace ZeltekWeb.Models.DTOs
{
    public class DashboardKpiDto
    {
        public decimal VentasHoy { get; set; }
        public int EnCaminoCount { get; set; }
        public decimal EnCaminoAmount { get; set; }
        public int PorRevisarCount { get; set; }
        public int StockCount { get; set; }
        public decimal StockValue { get; set; }
    }

    public class ActividadRecienteDto
    {
        public string Tipo { get; set; } = string.Empty; // "Venta" or "Compra"
        public string Descripcion { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public decimal Monto { get; set; }
    }

    public class SearchResultDto
    {
        public int Id { get; set; }
        public string Header { get; set; } = string.Empty; // e.g., Name, Tracking
        public string SubHeader { get; set; } = string.Empty; // e.g., Serial, Client
        public DateTime? Date { get; set; }
        public decimal? Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "Venta", "Compra", "Inventario"
    }
}

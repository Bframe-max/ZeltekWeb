namespace ZeltekWeb.Models
{
    /// <summary>
    /// Un mes en el gráfico de tendencia de crecimiento (ventas por mes).
    /// </summary>
    public class ReporteTendenciaMensualViewModel
    {
        public string Mes { get; set; } = "";       // ej. "2024-01" o "Ene 24"
        public string MesLabel { get; set; } = "";  // etiqueta corta para el eje
        public decimal TotalVentas { get; set; }
        public int CantidadVentas { get; set; }
        /// <summary>Unidades vendidas por mes (desde vw_ResumenFinanciero).</summary>
        public int CantidadEquipos { get; set; }
    }
}

namespace ZeltekWeb.Models
{
    /// <summary>
    /// Una fila de actividad reciente (Venta o Compra) para el Dashboard.
    /// </summary>
    public class ActividadRecienteViewModel
    {
        public string Tipo { get; set; } = "";       // "Venta" | "Compra"
        public string Descripcion { get; set; } = "";
        public DateTime Fecha { get; set; }
        public decimal Monto { get; set; }
    }
}

namespace ZeltekWeb.Models
{
    /// <summary>
    /// Ítem en estado "En Reparación" para la vista de gestión de reparaciones.
    /// </summary>
    public class ItemEnReparacionViewModel
    {
        public int ItemID { get; set; }
        public string ProductoNombre { get; set; } = "";
        public string NumeroSerie { get; set; } = "";
        public string? ProblemaReportado { get; set; }
        public DateTime? FechaIngreso { get; set; }
    }
}

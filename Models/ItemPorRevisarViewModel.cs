namespace ZeltekWeb.Models
{
    /// <summary>
    /// Ítem en estado "Por Revisar" para la mesa de calidad.
    /// </summary>
    public class ItemPorRevisarViewModel
    {
        public int ItemID { get; set; }
        public string ProductoNombre { get; set; } = "";
        public string NumeroSerie { get; set; } = "";
        public DateTime? FechaRecepcion { get; set; }
    }
}

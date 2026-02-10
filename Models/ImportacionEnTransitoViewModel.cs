namespace ZeltekWeb.Models
{
    /// <summary>
    /// Fila para la tabla "En Tránsito" del Dashboard (rastreo de importaciones).
    /// </summary>
    public class ImportacionEnTransitoViewModel
    {
        public int CompraID { get; set; }
        public string Producto { get; set; } = "";
        public string Tracking { get; set; } = "";
        public string Estado { get; set; } = "";  // ej. "En Miami", "Llega: Jueves"
    }
}
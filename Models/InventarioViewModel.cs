namespace ZeltekWeb.Models
{
    public class InventarioViewModel
    {
        public int ItemID { get; set; }
        public string Producto { get; set; } = "";

        // 👇 ESTA ES LA NUEVA PROPIEDAD QUE FALTABA
        public string Especificaciones { get; set; } = "";

        public string? NumeroSerie { get; set; }
        public decimal CostoCompra { get; set; }
        public decimal PrecioVenta { get; set; }
        public string Estado { get; set; } = "";
    }
}
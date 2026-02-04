namespace ZeltekWeb.Models
{
    public class ItemEditViewModel
    {
        public int ItemID { get; set; }
        public int CompraID { get; set; }
        public int ProductoID { get; set; }

        public string Producto { get; set; } = "";

        public string? NumeroSerie { get; set; }

        public decimal CostoCompra { get; set; }
        public decimal PrecioVenta { get; set; }

        public string Estado { get; set; } = "En Stock";
    }
}

namespace ZeltekWeb.Models
{
    public class CompraViewModel
    {
        public int CompraID { get; set; }

        public int? ProveedorID { get; set; }
        public string? Proveedor { get; set; }

        public string? VendedorOnline { get; set; }
        public DateTime FechaCompra { get; set; }

        public decimal CostoTotal { get; set; }

        public string? TrackingProveedor { get; set; }
        public string? URLCompra { get; set; }

        public string? EstadoCompra { get; set; }

    }
}

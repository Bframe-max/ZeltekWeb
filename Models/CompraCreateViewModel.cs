using System.ComponentModel.DataAnnotations;

namespace ZeltekWeb.Models
{
    public class CompraCreateViewModel
    {
        [Required(ErrorMessage = "El vendedor es obligatorio")]
        public string Vendedor { get; set; } // En BD se llama VendedorOnline

        [Required(ErrorMessage = "El Tracking es obligatorio")]
        public string TrackingProveedor { get; set; } // En BD se llama TrackingProveedor

        [Required]
        public DateTime FechaCompra { get; set; } = DateTime.Now;

        // --- DETALLE DEL PRODUCTO ---

        [Required(ErrorMessage = "Selecciona un producto")]
        public int ProductoID { get; set; }

        [Required]
        [Range(1, 100, ErrorMessage = "Mínimo 1 unidad")]
        public int Cantidad { get; set; }

        [Required]
        public decimal CostoPorUnidad { get; set; }
    }
}
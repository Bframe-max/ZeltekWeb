using System.ComponentModel.DataAnnotations;

namespace ZeltekWeb.Models
{
    public class CompraCreateViewModel
    {
        [Required(ErrorMessage = "El vendedor es obligatorio")]
        public string Vendedor { get; set; } // En BD se llama VendedorOnline

        /// <summary>Opcional. Si se ingresa, debe ser único en la base de datos.</summary>
        public string? TrackingProveedor { get; set; } // En BD: TrackingProveedor

        [Required]
        public DateTime FechaCompra { get; set; } = DateTime.Now;

        // --- DETALLE DEL PRODUCTO ---

        [Required(ErrorMessage = "Selecciona un producto")]
        [Range(1, int.MaxValue, ErrorMessage = "Selecciona un producto")]
        public int ProductoID { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria")]
        [Range(1, 100, ErrorMessage = "Mínimo 1 unidad")]
        public int Cantidad { get; set; }

        /// <summary>Costo total del lote (productos), sin envío. El unitario lo calcula el SP.</summary>
        [Required(ErrorMessage = "El costo total de la compra es obligatorio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Debe ser mayor a 0")]
        public decimal CostoTotalCompra { get; set; }

        /// <summary>Costo total de envío (opcional). Si no se envía o está vacío, se usa 0.</summary>
        [Range(0, double.MaxValue, ErrorMessage = "El envío no puede ser negativo")]
        public decimal? CostoEnvioTotal { get; set; }
    }
}
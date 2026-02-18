using System.ComponentModel.DataAnnotations;

namespace ZeltekWeb.Models.DTOs
{
    public class CreateVentaDto
    {
        [Required]
        public int ItemId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal PrecioFinal { get; set; }

        public DateTime FechaVenta { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "El método de pago es obligatorio")]
        public string MetodoPago { get; set; } = string.Empty;

        // Cliente Info
        [Required(ErrorMessage = "El nombre del cliente es obligatorio")]
        public string ClienteNombre { get; set; } = string.Empty;
        public string? ClienteCedula { get; set; }
        public string? ClienteCelular { get; set; }
        public int ClienteIdExistente { get; set; } // 0 si es nuevo o no seleccionado de la lista

        // Venta Info
        public int GarantiaMeses { get; set; }
        public string? Notas { get; set; }
        public string TipoVenta { get; set; } = "Normal"; // Normal, Revendedor

        // Detalles Extra
        public int? ProductoId { get; set; } // Opcional, se puede inferir del ItemId pero a veces viene del front
        public int CantidadAVender { get; set; } = 1;
        
        // Cargador
        public bool IncluirCargador { get; set; }
        public int? IdCargador { get; set; }

        // Multi-tenancy (Futuro)
        public int TenantId { get; set; } = 1;
        public string Usuario { get; set; } = "Sistema";
    }

    public class ProductoInfoDTO
    {
        public decimal CostoTotal { get; set; }
        public decimal PrecioSugerido { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    public class ClienteLookupDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string UltimaCompra { get; set; } = "Nuevo";
    }
}

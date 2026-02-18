using System;

namespace ZeltekWeb.Models.DTOs
{
    public class GarantiaVigenteDto
    {
        public string Cliente { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Producto { get; set; } = string.Empty;
        public string NumeroSerie { get; set; } = string.Empty;
        public DateTime FechaVenta { get; set; }
        public int GarantiaMeses { get; set; }
        public DateTime FechaVenceGarantia { get; set; }
        public int DiasRestantes { get; set; }
        public string? Procesador { get; set; }
        public int RAM_GB { get; set; }
        public int Almacenamiento_GB { get; set; }
    }
}

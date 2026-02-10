namespace ZeltekWeb.Models
{
    /// <summary>
    /// Representa una garantía vigente para mostrar en el módulo Garantías.
    /// Incluye datos del cliente, producto vendido y especificaciones del ítem.
    /// </summary>
    public class GarantiaVigenteViewModel
    {
        // Cliente
        public string Cliente { get; set; } = "";
        public string Telefono { get; set; } = "";

        // Producto e ítem
        public string Producto { get; set; } = "";
        public string NumeroSerie { get; set; } = "";

        // Especificaciones del equipo (ItemInventario)
        public string? Procesador { get; set; }
        public int RAM_GB { get; set; }
        public int Almacenamiento_GB { get; set; }

        // Garantía
        public DateTime FechaVenta { get; set; }
        public int GarantiaMeses { get; set; }
        public DateTime FechaVenceGarantia { get; set; }
        public int DiasRestantes { get; set; }
    }
}

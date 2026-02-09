namespace ZeltekWeb.Models
{
    public class ItemInventarioViewModel
    {
        public int ItemID { get; set; }
        public string ProductoNombre { get; set; } // OJO: Antes se llamaba 'Producto', ahora 'ProductoNombre'
        public string NumeroSerie { get; set; }
        public string Estado { get; set; }

        // Costos y Precios
        public decimal CostoTotal { get; set; }
        public decimal PrecioVenta { get; set; }

        // Especificaciones
        public string Procesador { get; set; }
        public int RAM_GB { get; set; }
        public int Almacenamiento_GB { get; set; }
    }
}
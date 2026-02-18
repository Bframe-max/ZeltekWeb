namespace ZeltekWeb.Models
{
    public class CompraDetalleViewModel
    {
        // Datos de la Cabecera
        public int CompraID { get; set; }
        public string Vendedor { get; set; }
        public string Tracking { get; set; }
        public DateTime Fecha { get; set; }
        public decimal CostoEnvio { get; set; }
        public decimal CostoTotal { get; set; }
        public string EstadoCompra { get; set; }

        // La lista de equipos que vienen en esa caja
        public List<ItemEnCompra> Items { get; set; } = new List<ItemEnCompra>();
    }

    public class ItemEnCompra
    {
        public int ItemID { get; set; }
        public string ProductoNombre { get; set; }
        public decimal Costo { get; set; }
        public string Estado { get; set; }
        public string NumeroSerie { get; set; }
        public bool EsAccesorio { get; set; }
        public bool EsUnico { get; set; }

        public string? Procesador { get; set; }
        public int? Ram { get; set; }
        public int? Almacenamiento { get; set; }
        public string? Color { get; set; }  // UI Only, not preserved in DB yet
    }
}
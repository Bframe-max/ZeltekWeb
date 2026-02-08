namespace ZeltekWeb.Models
{
    public class CompraDetalleViewModel
    {
        // Datos de la Cabecera
        public int CompraID { get; set; }
        public string Vendedor { get; set; }
        public string Tracking { get; set; }
        public DateTime Fecha { get; set; }

        // La lista de equipos que vienen en esa caja
        public List<ItemEnCompra> Items { get; set; } = new List<ItemEnCompra>();
    }

    public class ItemEnCompra
    {
        public int ItemID { get; set; }
        public string ProductoNombre { get; set; }
        public decimal Costo { get; set; }
        public string Estado { get; set; }     // 'En Camino' o 'En Stock'
        public string NumeroSerie { get; set; } // Puede ser 'PEND-TRACK...' o la real
    }
}
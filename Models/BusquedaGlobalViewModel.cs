namespace ZeltekWeb.Models
{
    /// <summary>
    /// Resultados de búsqueda global (serie, nombre, tracking) en Ventas, Compras e Inventario.
    /// </summary>
    public class BusquedaGlobalViewModel
    {
        public string Query { get; set; } = "";
        public List<ResultadoBusquedaVenta> Ventas { get; set; } = new();
        public List<ResultadoBusquedaCompra> Compras { get; set; } = new();
        public List<ResultadoBusquedaInventario> Inventario { get; set; } = new();
    }

    public class ResultadoBusquedaVenta
    {
        public int VentaID { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
        public string ClienteNombre { get; set; } = "";
    }

    public class ResultadoBusquedaCompra
    {
        public int CompraID { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
        public string Tracking { get; set; } = "";
        public string ProductoPrincipal { get; set; } = "";
    }

    public class ResultadoBusquedaInventario
    {
        public int ItemID { get; set; }
        public string ProductoNombre { get; set; } = "";
        public string NumeroSerie { get; set; } = "";
        public string Estado { get; set; } = "";
    }
}

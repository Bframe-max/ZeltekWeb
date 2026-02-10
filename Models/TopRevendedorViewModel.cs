namespace ZeltekWeb.Models
{
    /// <summary>
    /// Fila de vw_TopClientes: cliente rankeado por compras (ventas que nos ha hecho).
    /// </summary>
    public class TopRevendedorViewModel
    {
        public string NombreCliente { get; set; } = "";
        public string TipoCliente { get; set; } = "";
        public int CantidadCompras { get; set; }
        public decimal TotalComprado { get; set; }
        public decimal GananciaGenerada { get; set; }
        public DateTime? UltimaCompra { get; set; }
    }
}

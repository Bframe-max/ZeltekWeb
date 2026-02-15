namespace ZeltekWeb.Models
{
    public class CompraListViewModel
    {
        public int CompraID { get; set; }
        public DateTime Fecha { get; set; }
        public string Vendedor { get; set; }
        public string Tracking { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; }
        public int CantidadItems { get; set; }

        // NUEVO: Para mostrar "iPad 11th Gen" en la lista
        public string ProductoPrincipal { get; set; }
        public int ItemsPendientes { get; set; }
    }
}
namespace ZeltekWeb.Models
{
    public class ProductoListViewModel
    {
        public int ProductoID { get; set; }
        public string? Nombre { get; set; }
        public string? Marca { get; set; }
        public string? Categoria { get; set; }
        public bool Activo { get; set; }
    }
}

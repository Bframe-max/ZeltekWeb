using System.ComponentModel.DataAnnotations;

namespace ZeltekWeb.Models 
{
    public class ProductoCreateEditViewModel
    {
        public int ProductoID { get; set; }
        [Required] public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        [Required] public int MarcaID { get; set; }
        public string? Modelo { get; set; }
        public int? CategoriaID { get; set; }
        public bool EsUnico { get; set; }
        public decimal? CostoReferencia { get; set; }


    }
}

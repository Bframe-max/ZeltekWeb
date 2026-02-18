using System.ComponentModel.DataAnnotations;

namespace ZeltekWeb.Models.DTOs
{
    public class ProductoDto
    {
        public int ProductoID { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Categoria { get; set; } = "Sin categoría";
        public bool EsUnico { get; set; }
        public string? Descripcion { get; set; }
        public string? Modelo { get; set; }
        public decimal? CostoReferencia { get; set; }
        public int MarcaID { get; set; }
        public int? CategoriaID { get; set; }
    }

    public class CreateProductoDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Nombre { get; set; } = string.Empty;
        
        public string? Descripcion { get; set; }
        
        [Required(ErrorMessage = "La marca es obligatoria")]
        public int MarcaID { get; set; }
        
        public string? Modelo { get; set; }
        public int? CategoriaID { get; set; }
        public bool EsUnico { get; set; }
        public decimal? CostoReferencia { get; set; }
    }

    public class UpdateProductoDto : CreateProductoDto
    {
        public int ProductoID { get; set; }
    }

    public class MarcaDto
    {
        public int MarcaID { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    public class CategoriaDto
    {
        public int CategoriaID { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }
}

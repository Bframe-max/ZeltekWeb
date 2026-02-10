using System.ComponentModel.DataAnnotations;

namespace ZeltekWeb.Models
{
    public class GastoEditViewModel
    {
        public int GastoID { get; set; }

        [Required]
        public DateTime FechaGasto { get; set; }

        [Required, StringLength(255)]
        public string Descripcion { get; set; } = "";

        public string? CategoriaGasto { get; set; }

        [Required, Range(0.01, 999999)]
        public decimal Monto { get; set; }
    }
}

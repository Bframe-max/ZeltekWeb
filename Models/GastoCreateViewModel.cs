using System;
using System.ComponentModel.DataAnnotations;

namespace ZeltekWeb.Models
{
    public class GastoCreateViewModel
    {
        [Required]
        [Display(Name = "Fecha")]
        public DateTime FechaGasto { get; set; } = DateTime.Today;

        [Required]
        [StringLength(255)]
        public string? Descripcion { get; set; }

        [Required]
        [Display(Name = "Categoría")]
        public string? CategoriaGasto { get; set; }

        [Required]
        [Range(0.01, 999999)]
        [Display(Name = "Monto")]
        public decimal Monto { get; set; }
    }
}

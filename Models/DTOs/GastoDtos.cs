using System;
using System.ComponentModel.DataAnnotations;

namespace ZeltekWeb.Models.DTOs
{
    public class GastoListDto
    {
        public int GastoID { get; set; }
        public DateTime FechaGasto { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string? CategoriaGasto { get; set; }
        public decimal Monto { get; set; }
    }

    public class GastoResumenDto
    {
        public int CantidadGastos { get; set; }
        public decimal TotalGastos { get; set; }
    }

    public class GastoPorCategoriaDto
    {
        public string Categoria { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }

    public class GastoCreateDto
    {
        [Required(ErrorMessage = "La fecha es requerida")]
        public DateTime FechaGasto { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "La descripción es requerida")]
        public string Descripcion { get; set; } = string.Empty;

        public string? CategoriaGasto { get; set; }

        [Required(ErrorMessage = "El monto es requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
        public decimal Monto { get; set; }
    }

    public class GastoEditDto : GastoCreateDto
    {
        public int GastoID { get; set; }
    }
}

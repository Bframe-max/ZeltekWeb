namespace ZeltekWeb.Models
{
    /// <summary>
    /// Total gastado por categoría (para gráfico en Gastos/Index).
    /// </summary>
    public class GastoPorCategoriaViewModel
    {
        public string Categoria { get; set; } = "";
        public decimal Total { get; set; }
    }
}
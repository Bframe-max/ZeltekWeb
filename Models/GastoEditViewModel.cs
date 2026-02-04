namespace ZeltekWeb.Models
{
    public class GastoEditViewModel
    {
        public int GastoID { get; set; }
        public DateTime FechaGasto { get; set; }
        public string Descripcion { get; set; } = "";
        public string? CategoriaGasto { get; set; }
        public decimal Monto { get; set; }
    }
}

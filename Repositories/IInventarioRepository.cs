namespace ZeltekWeb.Repositories
{
    public interface IInventarioRepository
    {
        Task RecibirProductoAsync(int itemId, int compraId, int productoIdLaptop, string numeroSerie, string condicion,
            string procesador, int? ram, int? almacenamiento, string? color, decimal costoEnvio, decimal costoOtros,
            string usuario, bool requiereCargadorDeStock, int? productoIdCargador);
    }
}

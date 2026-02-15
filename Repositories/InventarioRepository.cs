using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace ZeltekWeb.Repositories
{
    public class InventarioRepository : IInventarioRepository
    {
        private readonly IConfiguration _config;

        public InventarioRepository(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Ejecuta sp_RecibirLaptopConAccesorios. Parámetros mapeados al SP.
        /// </summary>
        public async Task RecibirProductoAsync(int itemId, int compraId, int productoIdLaptop, string numeroSerie, string condicion,
            string procesador, int? ram, int? almacenamiento, string? color, decimal costoEnvio, decimal costoOtros,
            string usuario, bool requiereCargadorDeStock, int? productoIdCargador)
        {
            var cadena = _config.GetConnectionString("ZeltekDB");
            using var con = new Microsoft.Data.SqlClient.SqlConnection(cadena);
            await con.OpenAsync();

            var p = new DynamicParameters();
            p.Add("@ItemID", itemId);
            p.Add("@CompraID", compraId);
            p.Add("@ProductoID_Laptop", productoIdLaptop);
            p.Add("@NumeroSerie", numeroSerie);
            p.Add("@Condicion", condicion);
            p.Add("@Procesador", procesador);
            p.Add("@Ram", ram);
            p.Add("@Almacenamiento", almacenamiento);
            p.Add("@Color", color);
            p.Add("@CostoEnvio", costoEnvio);
            p.Add("@CostoOtros", costoOtros);
            p.Add("@Usuario", usuario);
            p.Add("@RequiereCargadorDeStock", requiereCargadorDeStock);
            p.Add("@ProductoID_Cargador", productoIdCargador);

            await con.ExecuteAsync(
                "sp_RecibirLaptopConAccesorios",
                p,
                commandType: CommandType.StoredProcedure);
        }
    }
}

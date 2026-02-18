using Dapper;
using System.Data;
using System.Text.RegularExpressions;
using ZeltekWeb.Data;
using ZeltekWeb.Models.DTOs;

namespace ZeltekWeb.Repositories
{
    public class ClienteRepository : IClienteRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ClienteRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<dynamic>> BuscarClientesAsync(string termino, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT TOP 10 
                    ClienteID as id, 
                    NombreCompleto as nombre, 
                    Cedula as cedula, 
                    Telefono as telefono,
                    ISNULL(CONVERT(varchar, (SELECT TOP 1 FechaVenta FROM Venta WHERE ClienteID = c.ClienteID ORDER BY FechaVenta DESC), 23), 'Nuevo') as ultima
                FROM Cliente c
                WHERE (NombreCompleto LIKE '%' + @Termino + '%' 
                    OR Cedula LIKE '%' + @Termino + '%' 
                    OR Telefono LIKE '%' + @Termino + '%')";
            // TODO: Add AND TenantID = @TenantId
            
            return await db.QueryAsync(sql, new { Termino = termino, TenantId = tenantId });
        }

        public async Task<int> CrearClienteAsync(string nombre, string telefono, string cedula, string correo, string direccion, string tipo, int tenantId, IDbTransaction? transaction = null)
        {
            var sql = @"
                INSERT INTO Cliente (NombreCompleto, Telefono, Cedula, Email, Direccion, TipoCliente, FechaRegistro, TenantID)
                OUTPUT INSERTED.ClienteID
                VALUES (@Nombre, @Telefono, @Cedula, @Correo, @Direccion, @Tipo, GETDATE(), @TenantId)";

            var parameters = new
            {
                Nombre = nombre,
                Telefono = telefono ?? "",
                Cedula = cedula ?? "",
                Correo = correo ?? "",
                Direccion = direccion ?? "",
                Tipo = tipo ?? "Cliente Final",
                TenantId = tenantId
            };

            if (transaction != null)
            {
                return await transaction.Connection.ExecuteScalarAsync<int>(sql, parameters, transaction);
            }
            else
            {
                using var db = _connectionFactory.CreateConnection();
                return await db.ExecuteScalarAsync<int>(sql, parameters);
            }
        }
    }
}

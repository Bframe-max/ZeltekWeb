using System.Data;
using Microsoft.Data.SqlClient;

namespace ZeltekWeb.Services
{
    public class ConnectionFactory : IConnectionFactory
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public ConnectionFactory(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection")
                                ?? _configuration.GetConnectionString("ZeltekDB")
                                ?? throw new InvalidOperationException("No se encontró DefaultConnection ni ZeltekDB en ConnectionStrings."); 
        }

        public IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}

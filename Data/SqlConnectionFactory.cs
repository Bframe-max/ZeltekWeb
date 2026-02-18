using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Dapper;

namespace ZeltekWeb.Data
{
    public class SqlConnectionFactory : IDbConnectionFactory
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _connectionString;

        public SqlConnectionFactory(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _connectionString = _configuration.GetConnectionString("ZeltekDB") 
                                ?? _configuration.GetConnectionString("DefaultConnection")
                                ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'ZeltekDB' o 'DefaultConnection'.");
        }

        public IDbConnection CreateConnection()
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();

            // 🔐 RLS Injection Logic
            try 
            {
                var user = _httpContextAccessor.HttpContext?.User;
                var tenantIdClaim = user?.FindFirst("TenantId")?.Value;

                int tenantId = 0;
                if (!string.IsNullOrEmpty(tenantIdClaim) && int.TryParse(tenantIdClaim, out int parsedId))
                {
                    tenantId = parsedId;
                }

                // Execute SESSION_CONTEXT injection
                // We use Dapper or SqlCommand. Since Dapper is used elsewhere, let's use SqlCommand to avoid dependency if not imported, 
                // BUT this file imports System.Data. Let's use SqlCommand for raw speed/simplicity here.
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "EXEC sp_set_session_context @key=N'TenantID', @value=@TenantId";
                var param = cmd.CreateParameter();
                param.ParameterName = "@TenantId";
                param.Value = tenantId;
                cmd.Parameters.Add(param);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // Log error? For now, rethrow or handle. 
                // If we fail to set context, security is compromised (or queries fail). Better to fail.
                connection.Close();
                throw new Exception("Error initializing Database Session Context", ex);
            }

            return connection;
        }
    }
}

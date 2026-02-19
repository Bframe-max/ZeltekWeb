using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using ZeltekWeb.Constants;

namespace ZeltekWeb.Data
{
    public class DapperContext
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _connectionString;

        public DapperContext(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _connectionString = _configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<IDbConnection> CreateConnectionAsync()
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var tenantIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(AppConstants.TenantIdClaim);

            if (tenantIdClaim != null && int.TryParse(tenantIdClaim.Value, out int tenantId))
            {
                await connection.ExecuteAsync(
                    "EXEC sp_set_session_context @key=N'TenantId', @value=@TenantId", 
                    new { TenantId = tenantId }
                );
            }

            return connection;
        }
    }
}

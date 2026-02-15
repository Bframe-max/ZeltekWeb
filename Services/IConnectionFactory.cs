using System.Data;

namespace ZeltekWeb.Services
{
    public interface IConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}

using System.Data;

namespace ZeltekWeb.Data
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}

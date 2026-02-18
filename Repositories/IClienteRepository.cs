using ZeltekWeb.Models;
using ZeltekWeb.Models.DTOs;
using System.Data; // Added for IDbTransaction

namespace ZeltekWeb.Repositories
{
    public interface IClienteRepository
    {
        Task<IEnumerable<dynamic>> BuscarClientesAsync(string termino, int tenantId);
        Task<int> CrearClienteAsync(string nombre, string telefono, string cedula, string correo, string direccion, string tipo, int tenantId, IDbTransaction? transaction = null);
    }
}

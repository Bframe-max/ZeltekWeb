using ZeltekWeb.Models.DTOs;
using System.Data;

namespace ZeltekWeb.Repositories
{
    public interface IClienteRepository
    {
        Task<IEnumerable<dynamic>> BuscarClientesAsync(string termino);
        Task<int> CrearClienteAsync(string nombre, string telefono, string cedula, string correo, string direccion, string tipo, IDbTransaction? transaction = null);
    }
}

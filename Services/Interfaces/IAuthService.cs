using ZeltekWeb.Models.Entities;

namespace ZeltekWeb.Services.Interfaces
{
    public interface IAuthService
    {
        Task<Usuario?> ValidarUsuario(string usuario, string password);
    }
}

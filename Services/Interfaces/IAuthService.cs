using ZeltekWeb.Models.Entities;

namespace ZeltekWeb.Services.Interfaces
{
    public interface IAuthService
    {
        Usuario? ValidarUsuario(string usuario, string password);
    }
}

using Microsoft.AspNetCore.Identity;
using ZeltekWeb.Data;
using ZeltekWeb.Models.Entities;
using ZeltekWeb.Services.Interfaces;

namespace ZeltekWeb.Services
{
    public class AuthService : IAuthService
    {
        private readonly UsuarioRepository _repo;
        private readonly PasswordHasher<object> _hasher;

        public AuthService(UsuarioRepository repo)
        {
            _repo = repo;
            _hasher = new PasswordHasher<object>();
        }

        public async Task<Usuario?> ValidarUsuario(string usuario, string password)
        {
            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
                return null;

            usuario = usuario.Trim();

            var user = await _repo.ObtenerPorUsuarioAsync(usuario);

            if (user == null || !user.Activo)
                return null;

            var result = _hasher.VerifyHashedPassword(
                null!,
                user.PasswordHash,
                password
            );

            return result == PasswordVerificationResult.Success
                ? user
                : null;
        }
    }
}

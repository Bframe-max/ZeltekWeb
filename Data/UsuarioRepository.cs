using Microsoft.Data.SqlClient;
using ZeltekWeb.Models.Entities;
using ZeltekWeb.Models;

namespace ZeltekWeb.Data
{
    public class UsuarioRepository
    {
        private readonly string _cn;

        public UsuarioRepository(IConfiguration config)
        {
            _cn = config.GetConnectionString("ZeltekDB") ?? config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("No se encontró DefaultConnection ni ZeltekDB en ConnectionStrings.");
        }

        public Usuario? ObtenerPorUsuario(string nombreUsuario)
        {
            return ObtenerPorUsuarioAsync(nombreUsuario).GetAwaiter().GetResult();
        }

        public async Task<Usuario?> ObtenerPorUsuarioAsync(string nombreUsuario)
        {
            nombreUsuario = nombreUsuario.Trim();

            using var con = new SqlConnection(_cn);
            await con.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT UsuarioID, Usuario, PasswordHash, Rol, Activo
                FROM UsuarioSistema
                WHERE Usuario = @u
            ", con);

            cmd.Parameters.AddWithValue("@u", nombreUsuario);

            using var rd = await cmd.ExecuteReaderAsync();

            if (!await rd.ReadAsync())
                return null;

            return new Usuario
            {
                Id = Convert.ToInt32(rd["UsuarioID"]),
                NombreUsuario = rd["Usuario"].ToString()!,
                PasswordHash = rd["PasswordHash"].ToString()!,
                Rol = rd["Rol"]?.ToString() ?? UserRoles.Admin,
                Activo = Convert.ToBoolean(rd["Activo"])
            };
        }
    }
}

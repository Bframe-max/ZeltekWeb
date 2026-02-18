using Microsoft.Data.SqlClient;
using ZeltekWeb.Models.Entities;
using ZeltekWeb.Models; // For UserRoles

namespace ZeltekWeb.Data
{
    public class UsuarioRepository
    {
        private readonly string _cn;

        public UsuarioRepository(IConfiguration config)
        {
            _cn = config.GetConnectionString("DefaultConnection") ?? config.GetConnectionString("ZeltekDB")
                ?? throw new InvalidOperationException("No se encontró DefaultConnection ni ZeltekDB en ConnectionStrings.");
        }

        public Usuario? ObtenerPorUsuario(string nombreUsuario)
        {
            // Keep sync method for backward compatibility if needed, or redirect to .Result (bad practice)
            // Ideally refactor all to Async.
            // For now, I'll just add Async version.
            return ObtenerPorUsuarioAsync(nombreUsuario).GetAwaiter().GetResult();
        }

        public async Task<Usuario?> ObtenerPorUsuarioAsync(string nombreUsuario)
        {
            nombreUsuario = nombreUsuario.Trim();

            using var con = new SqlConnection(_cn);
            await con.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT UsuarioID, Usuario, PasswordHash, Rol, Activo, TenantID
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
                Activo = Convert.ToBoolean(rd["Activo"]),
                TenantId = rd["TenantID"] != DBNull.Value ? Convert.ToInt32(rd["TenantID"]) : 0
            };
        }
    }
}

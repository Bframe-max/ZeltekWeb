using Microsoft.Data.SqlClient;
using ZeltekWeb.Models.Entities;

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
            nombreUsuario = nombreUsuario.Trim();

            using var con = new SqlConnection(_cn);
            con.Open();

            using var cmd = new SqlCommand(@"
                SELECT UsuarioID, Usuario, PasswordHash, Rol, Activo
                FROM UsuarioSistema
                WHERE Usuario = @u
            ", con);

            cmd.Parameters.AddWithValue("@u", nombreUsuario);

            using var rd = cmd.ExecuteReader();

            if (!rd.Read())
                return null;

            return new Usuario
            {
                Id = Convert.ToInt32(rd["UsuarioID"]),
                NombreUsuario = rd["Usuario"].ToString()!,
                PasswordHash = rd["PasswordHash"].ToString()!, // 👈 CLAVE
                Rol = rd["Rol"]?.ToString() ?? "Admin",
                Activo = Convert.ToBoolean(rd["Activo"])
            };
        }
    }
}

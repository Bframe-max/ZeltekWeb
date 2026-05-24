namespace ZeltekWeb.Models.Entities
{
    public class Usuario
    {
        public int Id { get; set; }
        public string NombreUsuario { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string Rol { get; set; } = "Admin";
        public bool Activo { get; set; }
    }
}

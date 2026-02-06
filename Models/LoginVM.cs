namespace ZeltekWeb.Models.ViewModels
{
    public class LoginVM
    {
        public string Usuario { get; set; } = null!;
        public string Password { get; set; } = null!;
        public bool Recordarme { get; set; }
    }
}

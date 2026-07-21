namespace SERVIGO.Web.Models.ViewModels
{
    public class LoginViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string UserID { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Error { get; set; }
    }
}

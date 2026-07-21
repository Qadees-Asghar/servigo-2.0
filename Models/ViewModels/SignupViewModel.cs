namespace SERVIGO.Web.Models.ViewModels
{
    public class SignupViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string CNIC { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public bool IsProvider { get; set; }
        public int? CategoryID { get; set; }
        public List<CategoryOption> Categories { get; set; } = new();
        public string? Error { get; set; }
        public string? CreatedUserID { get; set; }
    }
}

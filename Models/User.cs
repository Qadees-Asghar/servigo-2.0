namespace SERVIGO.Web.Models
{
    public abstract class User
    {
        public string   UserID       { get; set; } = string.Empty;
        public string   FullName     { get; set; } = string.Empty;
        public string   Email        { get; set; } = string.Empty;
        public string   Phone        { get; set; } = string.Empty;
        public string   CNIC         { get; set; } = string.Empty;
        public string   PasswordHash { get; set; } = string.Empty;
        public int      RoleID       { get; set; }
        public bool     IsActive     { get; set; } = true;
        public DateTime CreatedAt    { get; set; } = DateTime.Now;

        protected User() { }

        protected User(string userID, string fullName, string email,
                        string phone, string cnic, int roleID)
        {
            UserID   = userID;
            FullName = fullName;
            Email    = email;
            Phone    = phone;
            CNIC     = cnic;
            RoleID   = roleID;
        }

        public abstract string GetRoleName();
    }
}

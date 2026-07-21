namespace SERVIGO.Web.Models
{
    public class ServiceProviderUser : User
    {
        public int     ProviderID     { get; set; }
        public int     CategoryID     { get; set; }
        public string  CategoryName   { get; set; } = string.Empty;
        public string  Description    { get; set; } = string.Empty;
        public bool    IsApproved     { get; set; }
        public decimal AverageRating  { get; set; }

        public ServiceProviderUser() : base() { RoleID = 3; }

        public ServiceProviderUser(string userID, string fullName, string email,
                                   string phone, string cnic,
                                   int categoryID, string description)
            : base(userID, fullName, email, phone, cnic, 3)
        {
            CategoryID  = categoryID;
            Description = description;
        }

        public override string GetRoleName() => "Service Provider";
    }
}

namespace SERVIGO.Web.Models
{
    public class ServiceModel
    {
        public int     ServiceID       { get; set; }
        public int     ProviderID      { get; set; }
        public string  ProviderName    { get; set; } = string.Empty;
        public string  CategoryName    { get; set; } = string.Empty;
        public string  ServiceName     { get; set; } = string.Empty;
        public string  Description     { get; set; } = string.Empty;
        public decimal Price           { get; set; }
        public int     DurationMinutes { get; set; }
        public bool    IsActive        { get; set; } = true;
        public decimal AverageRating   { get; set; }
    }
}

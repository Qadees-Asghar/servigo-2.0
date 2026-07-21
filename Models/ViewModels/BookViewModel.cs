namespace SERVIGO.Web.Models.ViewModels
{
    public class BookViewModel
    {
        public int ServiceID { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public int ProviderID { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public List<TimeSlotModel> Slots { get; set; } = new();
    }

    public class ReviewsViewModel
    {
        public List<UnreviewedBookingRow> Unreviewed { get; set; } = new();
        public List<ReviewedBookingRow> Reviewed { get; set; } = new();
    }
}

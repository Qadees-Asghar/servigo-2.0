namespace SERVIGO.Web.Models
{
    public class CategoryOption
    {
        public int    CategoryID   { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }

    public class CustomerRow
    {
        public string   UserID        { get; set; } = string.Empty;
        public string   FullName      { get; set; } = string.Empty;
        public string   Email         { get; set; } = string.Empty;
        public string   Phone         { get; set; } = string.Empty;
        public bool     IsActive      { get; set; }
        public DateTime CreatedAt     { get; set; }
        public int      TotalBookings { get; set; }
    }

    public class ProviderRow
    {
        public int      ProviderID        { get; set; }
        public string   UserID            { get; set; } = string.Empty;
        public string   FullName          { get; set; } = string.Empty;
        public string   Email             { get; set; } = string.Empty;
        public string   Phone             { get; set; } = string.Empty;
        public string   CategoryName      { get; set; } = string.Empty;
        public bool     IsApproved        { get; set; }
        public decimal  AverageRating     { get; set; }
        public bool     IsActive          { get; set; }
        public DateTime CreatedAt         { get; set; }
        public int      CompletedBookings { get; set; }
    }

    public class DashboardStats
    {
        public int TotalCustomers    { get; set; }
        public int TotalProviders    { get; set; }
        public int PendingApprovals  { get; set; }
        public int TotalBookings     { get; set; }
        public int PendingBookings   { get; set; }
        public int CompletedBookings { get; set; }
    }

    public class BookingSummaryRow
    {
        public string StatusName    { get; set; } = string.Empty;
        public int    TotalBookings { get; set; }
    }

    public class ProviderStatsRow
    {
        public int     ProviderID          { get; set; }
        public string  ProviderName        { get; set; } = string.Empty;
        public string  CategoryName        { get; set; } = string.Empty;
        public int     TotalBookings       { get; set; }
        public int     Completed           { get; set; }
        public int     CancelledOrRejected { get; set; }
        public decimal AverageRating       { get; set; }
    }

    public class RatingRow
    {
        public int      Stars        { get; set; }
        public string   Comment      { get; set; } = string.Empty;
        public DateTime CreatedAt    { get; set; }
        public string   CustomerName { get; set; } = string.Empty;
    }

    public class UnreviewedBookingRow
    {
        public int      BookingID    { get; set; }
        public string   ServiceName  { get; set; } = string.Empty;
        public string   ProviderName { get; set; } = string.Empty;
        public int      ProviderID   { get; set; }
        public DateTime SlotDate     { get; set; }
        public decimal  Price        { get; set; }
    }

    public class ReviewedBookingRow
    {
        public int      BookingID    { get; set; }
        public string   ServiceName  { get; set; } = string.Empty;
        public string   ProviderName { get; set; } = string.Empty;
        public int      ProviderID   { get; set; }
        public DateTime SlotDate     { get; set; }
        public decimal  Price        { get; set; }
        public int      Stars        { get; set; }
        public string   Comment      { get; set; } = string.Empty;
    }

    public class FeedbackReportRow
    {
        public int       ReportID        { get; set; }
        public string     ReportType     { get; set; } = string.Empty;
        public string     Subject        { get; set; } = string.Empty;
        public string     Description    { get; set; } = string.Empty;
        public bool       IsResolved     { get; set; }
        public DateTime   CreatedAt      { get; set; }
        public DateTime?  ResolvedAt     { get; set; }
        public string     SubmittedBy    { get; set; } = string.Empty;
        public string     SubmittedByName{ get; set; } = string.Empty;
        public string?     TargetUserID   { get; set; }
        public string      TargetName     { get; set; } = string.Empty;
    }
}

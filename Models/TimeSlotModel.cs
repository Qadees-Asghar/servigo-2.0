namespace SERVIGO.Web.Models
{
    public class TimeSlotModel
    {
        public int      SlotID       { get; set; }
        public int      ProviderID   { get; set; }
        public string   ProviderName { get; set; } = string.Empty;
        public DateTime SlotDate     { get; set; }
        public TimeSpan StartTime    { get; set; }
        public TimeSpan EndTime      { get; set; }
        public bool     IsAvailable  { get; set; } = true;

        public string DateDisplay   => SlotDate.ToString("ddd, MMM dd yyyy");
        public string TimeDisplay   => $"{DateTime.Today.Add(StartTime):hh\\:mm tt} - {DateTime.Today.Add(EndTime):hh\\:mm tt}";
        public string StatusDisplay => IsAvailable ? "Available" : "Booked";
    }
}

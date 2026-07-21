using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SERVIGO.Web.DAL;
using SERVIGO.Web.Helpers;
using SERVIGO.Web.Models;
using SERVIGO.Web.Models.ViewModels;

namespace SERVIGO.Web.Controllers
{
    [Authorize(Roles = "Provider")]
    public class ProviderController : Controller
    {
        private string UID => User.GetUserID();
        private int ProviderID => User.GetProviderID() ?? 0;

        public static readonly Dictionary<string, string[]> ServiceOptions
            = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AC Repair"]    = ["AC Installation", "AC Gas Refill", "AC Maintenance", "AC Deep Cleaning", "AC Repair"],
            ["Plumber"]      = ["Pipe Repair", "Drain Cleaning", "Leak Fixing", "Pipe Installation", "Water Heater Repair", "Tap Replacement"],
            ["Electrician"]  = ["Wiring", "Circuit Repair", "Fan Installation", "Light Fitting", "Switchboard Repair", "Generator Service"],
            ["Painter"]      = ["Interior Painting", "Exterior Painting", "Wall Texture", "Wood Painting", "Ceiling Painting"],
            ["Cleaner"]      = ["Deep Cleaning", "Sofa Cleaning", "Carpet Cleaning", "Kitchen Cleaning", "Bathroom Cleaning"],
            ["Carpenter"]    = ["Furniture Repair", "Door Fitting", "Cabinet Making", "Wood Work", "Shelf Installation"],
            ["Gardener"]     = ["Lawn Mowing", "Tree Trimming", "Plant Care", "Garden Design", "Pest Spray"],
            ["Mason"]        = ["Brickwork", "Plastering", "Tiling", "Concrete Work", "Foundation Repair"],
            ["Mechanic"]     = ["Oil Change", "Engine Repair", "Brake Service", "Tire Change", "General Checkup"],
            ["Laundry"]      = ["Wash & Fold", "Dry Cleaning", "Ironing", "Stain Removal"],
        };

        private static string[] OptionsFor(string category)
            => ServiceOptions.TryGetValue(category, out var o)
                ? o
                : new[] { "General Service", "Repair", "Installation", "Maintenance", "Inspection" };

        public IActionResult Index()
        {
            if (ProviderID == 0) { TempData["Error"] = "Provider account not fully configured."; return RedirectToAction("Logout", "Account"); }

            ViewData["PageTitle"] = $"Welcome, {User.GetFullName().Split(' ')[0]}! 👷";
            ViewData["Active"] = "home";

            var prov = ProviderDAL.GetByProviderID(ProviderID);
            var bookings = BookingDAL.GetProviderBookings(ProviderID);

            ViewBag.IsApproved = prov?.IsApproved ?? false;
            ViewBag.Completed = BookingDAL.GetProviderCompletedCount(ProviderID);
            ViewBag.Pending = bookings.Count(b => b.StatusID == 1);
            return View();
        }

        public IActionResult Services()
        {
            ViewData["PageTitle"] = "My Services";
            ViewData["PageSubtitle"] = "Add, edit or remove your offered services.";
            ViewData["Active"] = "services";

            var prov = ProviderDAL.GetByProviderID(ProviderID);
            ViewBag.ServiceOptions = OptionsFor(prov?.CategoryName ?? string.Empty);
            return View(ServiceDAL.GetByProvider(ProviderID));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddService(string serviceName, string? description, decimal price)
        {
            if (string.IsNullOrWhiteSpace(serviceName)) TempData["Error"] = "Please select a service type.";
            else if (price <= 0) TempData["Error"] = "Enter a valid price greater than 0.";
            else
            {
                ServiceDAL.CreateService(new ServiceModel
                {
                    ProviderID = ProviderID,
                    ServiceName = serviceName,
                    Description = description ?? string.Empty,
                    Price = price,
                    DurationMinutes = 0
                });
                TempData["Success"] = "Service added.";
            }
            return RedirectToAction(nameof(Services));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeactivateService(int serviceId)
        {
            ServiceDAL.DeleteService(serviceId);
            TempData["Success"] = "Service deactivated.";
            return RedirectToAction(nameof(Services));
        }

        public IActionResult Schedule()
        {
            ViewData["PageTitle"] = "My Schedule";
            ViewData["PageSubtitle"] = "Add available time slots. Customers can book within the next 7 days.";
            ViewData["Active"] = "schedule";
            return View(BookingDAL.GetSlotsByProvider(ProviderID));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddSlot(DateTime date, string startTime, string endTime)
        {
            var start = TimeSpan.Parse(startTime);
            var end = TimeSpan.Parse(endTime);

            if (start >= end) TempData["Error"] = "End time must be after start time.";
            else if (date.Date < DateTime.Today) TempData["Error"] = "Cannot create slots in the past.";
            else
            {
                try { BookingDAL.CreateSlot(ProviderID, date, start, end); TempData["Success"] = "Time slot added successfully."; }
                catch (Exception ex) { TempData["Error"] = ex.Message; }
            }
            return RedirectToAction(nameof(Schedule));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteSlot(int slotId)
        {
            BookingDAL.DeleteSlot(slotId);
            TempData["Success"] = "Time slot deleted.";
            return RedirectToAction(nameof(Schedule));
        }

        public IActionResult Bookings()
        {
            ViewData["PageTitle"] = "Incoming Bookings";
            ViewData["PageSubtitle"] = "Accept or reject customer booking requests.";
            ViewData["Active"] = "bookings";
            return View(BookingDAL.GetProviderBookings(ProviderID));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateBookingStatus(int bookingId, int newStatus)
        {
            try { BookingDAL.UpdateStatus(bookingId, newStatus, UID); TempData["Success"] = $"Booking #{bookingId} updated."; }
            catch (Exception ex) { TempData["Error"] = ex.Message; }
            return RedirectToAction(nameof(Bookings));
        }

        public IActionResult Feedback()
        {
            ViewData["PageTitle"] = "Feedback & Reports";
            ViewData["PageSubtitle"] = "Share feedback, report system issues, or report a customer.";
            ViewData["Active"] = "feedback";
            return View(FeedbackDAL.GetByUser(UID));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Feedback(string reportType, string subject, string description)
        {
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(description))
                TempData["Error"] = "Please fill in both subject and description.";
            else
            {
                FeedbackDAL.Submit(UID, reportType, null, subject, description);
                TempData["Success"] = "Your report has been submitted. Admin will review it.";
            }
            return RedirectToAction(nameof(Feedback));
        }

        public IActionResult Notifications()
        {
            ViewData["PageTitle"] = "Notifications";
            ViewData["PageSubtitle"] = "Booking alerts and updates.";
            ViewData["Active"] = "notifications";
            var list = NotificationDAL.GetByUser(UID);
            NotificationDAL.MarkAllRead(UID);
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkAllRead()
        {
            NotificationDAL.MarkAllRead(UID);
            return RedirectToAction(nameof(Notifications));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            try
            {
                UserDAL.DeleteUser(UID);
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Could not delete account: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}

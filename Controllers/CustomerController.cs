using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SERVIGO.Web.DAL;
using SERVIGO.Web.Helpers;
using SERVIGO.Web.Models;
using SERVIGO.Web.Models.ViewModels;

namespace SERVIGO.Web.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerController : Controller
    {
        private string UID => User.GetUserID();

        public IActionResult Index()
        {
            ViewData["PageTitle"] = null;
            ViewData["Active"] = "home";
            ViewBag.FirstName = User.GetFullName().Split(' ')[0];
            ViewBag.Categories = ProviderDAL.GetCategories();
            return View();
        }

        public IActionResult Browse(int? categoryId, string? q)
        {
            ViewData["PageTitle"] = "Services";
            ViewData["PageSubtitle"] = categoryId == null ? "Showing all services" : "Filtered by category";
            ViewData["Active"] = "home";
            ViewBag.Categories = ProviderDAL.GetCategories();
            ViewBag.CategoryId = categoryId;
            ViewBag.Query = q ?? string.Empty;
            var services = ServiceDAL.SearchServices(q ?? string.Empty, categoryId);
            return View(services);
        }

        [HttpGet]
        public IActionResult Book(int serviceId)
        {
            var svc = ServiceDAL.GetByID(serviceId);
            if (svc == null) { TempData["Error"] = "Service not found."; return RedirectToAction(nameof(Browse)); }

            ViewData["PageTitle"] = "Book Appointment";
            ViewData["PageSubtitle"] = "Select a time slot and confirm your booking.";
            ViewData["Active"] = "home";

            var vm = new BookViewModel
            {
                ServiceID = svc.ServiceID,
                ServiceName = svc.ServiceName,
                ProviderID = svc.ProviderID,
                ProviderName = svc.ProviderName,
                Price = svc.Price,
                Slots = BookingDAL.GetAvailableSlots(svc.ProviderID)
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Book(int serviceId, int slotId, string? notes)
        {
            try
            {
                int bookingID = BookingDAL.CreateBooking(UID, slotId, serviceId, string.IsNullOrWhiteSpace(notes) ? null : notes);
                TempData["Success"] = $"Booking confirmed! Booking ID #{bookingID} — Status: Pending (waiting for provider acceptance).";
                return RedirectToAction(nameof(MyBookings));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Book), new { serviceId });
            }
        }

        public IActionResult MyBookings()
        {
            ViewData["PageTitle"] = "My Bookings";
            ViewData["PageSubtitle"] = "All your bookings and their current status.";
            ViewData["Active"] = "mybookings";
            return View(BookingDAL.GetCustomerBookings(UID));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelBooking(int bookingId)
        {
            try { BookingDAL.UpdateStatus(bookingId, 4, UID); TempData["Success"] = $"Booking #{bookingId} cancelled."; }
            catch (Exception ex) { TempData["Error"] = ex.Message; }
            return RedirectToAction(nameof(MyBookings));
        }

        public IActionResult Reviews()
        {
            ViewData["PageTitle"] = "Reviews";
            ViewData["PageSubtitle"] = "Rate completed bookings and manage your reviews.";
            ViewData["Active"] = "reviews";
            var vm = new ReviewsViewModel
            {
                Unreviewed = RatingDAL.GetUnreviewedBookings(UID),
                Reviewed = RatingDAL.GetReviewedBookings(UID)
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitReview(int bookingId, int providerId, int stars, string? comment)
        {
            if (stars is < 1 or > 5) { TempData["Error"] = "Please select a star rating."; return RedirectToAction(nameof(Reviews)); }
            try
            {
                RatingDAL.SubmitRating(bookingId, providerId, UID, stars, comment ?? string.Empty);
                TempData["Success"] = $"Thank you! You rated {new string('*', stars)}";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }
            return RedirectToAction(nameof(Reviews));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditReview(int bookingId, int stars, string? comment)
        {
            if (stars is < 1 or > 5) { TempData["Error"] = "Please select a star rating."; return RedirectToAction(nameof(Reviews)); }
            try
            {
                RatingDAL.UpdateRating(bookingId, stars, comment ?? string.Empty);
                TempData["Success"] = "Review updated.";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }
            return RedirectToAction(nameof(Reviews));
        }

        public IActionResult Feedback()
        {
            ViewData["PageTitle"] = "Feedback & Reports";
            ViewData["PageSubtitle"] = "Share feedback, report system issues, or report a provider.";
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
            ViewData["PageSubtitle"] = "Your booking updates and alerts.";
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

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SERVIGO.Web.DAL;
using SERVIGO.Web.Helpers;

namespace SERVIGO.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private string UID => User.GetUserID();

        public IActionResult Index()
        {
            ViewData["PageTitle"] = "Dashboard Overview";
            ViewData["PageSubtitle"] = "System statistics and activity at a glance.";
            ViewData["Active"] = "home";
            return View(BookingDAL.GetDashboardStats());
        }

        public IActionResult Users(string? search)
        {
            ViewData["PageTitle"] = "Manage Users";
            ViewData["PageSubtitle"] = "View, activate/deactivate, or remove customers.";
            ViewData["Active"] = "users";
            ViewBag.Search = search ?? string.Empty;

            var list = UserDAL.GetAllCustomers();
            if (!string.IsNullOrWhiteSpace(search))
                list = list.Where(u => u.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)
                                     || u.Email.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleUserActive(string userId, bool active)
        {
            UserDAL.SetActiveStatus(userId, !active);
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUser(string userId, string fullName)
        {
            if (userId == UID)
            {
                TempData["Error"] = "You cannot delete your own account while logged in.";
                return RedirectToAction(nameof(Users));
            }
            try { UserDAL.DeleteUser(userId); TempData["Success"] = $"User \"{fullName}\" deleted successfully."; }
            catch (Exception ex) { TempData["Error"] = $"Could not delete user: {ex.Message}"; }
            return RedirectToAction(nameof(Users));
        }

        public IActionResult Providers()
        {
            ViewData["PageTitle"] = "Manage Providers";
            ViewData["PageSubtitle"] = "Grant or remove the Verified badge, or activate / deactivate provider accounts.";
            ViewData["Active"] = "providers";
            return View(UserDAL.GetAllProviders());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetProviderApproval(int providerId, bool approve)
        {
            ProviderDAL.SetApproval(providerId, approve);
            return RedirectToAction(nameof(Providers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleProviderActive(string userId, bool active)
        {
            UserDAL.SetActiveStatus(userId, !active);
            return RedirectToAction(nameof(Providers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteProvider(int providerId, string fullName)
        {
            try { ProviderDAL.DeleteProvider(providerId); TempData["Success"] = $"Provider \"{fullName}\" deleted successfully."; }
            catch (Exception ex) { TempData["Error"] = $"Could not delete provider: {ex.Message}"; }
            return RedirectToAction(nameof(Providers));
        }

        public IActionResult Bookings()
        {
            ViewData["PageTitle"] = "All Bookings";
            ViewData["PageSubtitle"] = "Complete booking history across all users.";
            ViewData["Active"] = "bookings";
            return View(BookingDAL.GetAllBookings());
        }

        public IActionResult Analytics()
        {
            ViewData["PageTitle"] = "Analytics & Reports";
            ViewData["PageSubtitle"] = "Booking summaries and provider performance.";
            ViewData["Active"] = "analytics";
            ViewBag.Summary = BookingDAL.GetBookingSummary();
            return View(BookingDAL.GetProviderStats());
        }

        public IActionResult Reports()
        {
            ViewData["PageTitle"] = "Feedback & Reports";
            ViewData["PageSubtitle"] = "Review user feedback, system issues, and provider/customer reports.";
            ViewData["Active"] = "reports";
            return View(FeedbackDAL.GetAll());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResolveReport(int reportId)
        {
            FeedbackDAL.MarkResolved(reportId);
            return RedirectToAction(nameof(Reports));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReopenReport(int reportId)
        {
            FeedbackDAL.MarkUnresolved(reportId);
            return RedirectToAction(nameof(Reports));
        }

        public IActionResult Logs()
        {
            ViewData["PageTitle"] = "Audit Logs";
            ViewData["PageSubtitle"] = "Activity log of key account and booking changes.";
            ViewData["Active"] = "logs";
            return View(AuditDAL.GetAll());
        }

        public IActionResult Notifications()
        {
            ViewData["PageTitle"] = "Notifications";
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
    }
}

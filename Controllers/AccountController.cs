using System.Security.Claims;
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
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToRoleHome();
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            model.UserID = model.UserID.Trim().ToUpper();

            if (string.IsNullOrWhiteSpace(model.Email) ||
                string.IsNullOrWhiteSpace(model.UserID) ||
                string.IsNullOrWhiteSpace(model.Password))
            {
                model.Error = "All fields are required.";
                return View(model);
            }

            User? user;
            try
            {
                user = UserDAL.Login(model.Email, model.UserID, model.Password);
            }
            catch (Exception ex)
            {
                model.Error = $"Login failed: {ex.Message}";
                return View(model);
            }

            if (user == null)
            {
                model.Error = "Invalid email, user ID, or password.";
                return View(model);
            }

            await SignInAsync(user);
            return RedirectToRoleHome();
        }

        [HttpGet]
        public IActionResult Signup()
        {
            var vm = new SignupViewModel { Categories = ProviderDAL.GetCategories() };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Signup(SignupViewModel model)
        {
            model.Categories = ProviderDAL.GetCategories();

            if (!ValidationHelper.ValidateSignup(model.FullName, model.Email, model.Phone,
                    model.CNIC, model.Password, model.ConfirmPassword, out string error))
            {
                model.Error = error;
                return View(model);
            }

            if (UserDAL.EmailExists(model.Email)) { model.Error = "This email is already registered."; return View(model); }
            if (UserDAL.PhoneExists(model.Phone)) { model.Error = "This phone number is already registered."; return View(model); }
            if (UserDAL.CNICExists(model.CNIC))   { model.Error = "This CNIC is already registered."; return View(model); }

            if (model.IsProvider && model.CategoryID == null)
            {
                model.Error = "Please select your service category.";
                return View(model);
            }

            try
            {
                int roleID = model.IsProvider ? 3 : 2;
                string userID = UserDAL.GenerateUserID();

                User newUser = roleID == 2
                    ? new CustomerUser(userID, model.FullName, model.Email, model.Phone, model.CNIC)
                    : new ServiceProviderUser(userID, model.FullName, model.Email, model.Phone, model.CNIC,
                          model.CategoryID!.Value, string.Empty);

                newUser.PasswordHash = PasswordHelper.Hash(model.Password);
                UserDAL.CreateUser(newUser);

                if (roleID == 3)
                    ProviderDAL.CreateProvider(model.CategoryID!.Value, userID, string.Empty);

                model.CreatedUserID = userID;
                return View("SignupSuccess", model);
            }
            catch (Exception ex)
            {
                model.Error = $"Signup failed: {ex.Message}";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        private async Task SignInAsync(User user)
        {
            string roleName = user.RoleID switch
            {
                1 => "Admin",
                2 => "Customer",
                3 => "Provider",
                _ => "Customer"
            };

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserID),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, roleName),
                new("RoleID", user.RoleID.ToString())
            };

            if (user.RoleID == 3)
            {
                var providerID = ProviderDAL.GetProviderIDByUserID(user.UserID);
                if (providerID != null)
                    claims.Add(new Claim("ProviderID", providerID.Value.ToString()));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        }

        private IActionResult RedirectToRoleHome()
        {
            if (User.IsInRole("Admin")) return RedirectToAction("Index", "Admin");
            if (User.IsInRole("Provider")) return RedirectToAction("Index", "Provider");
            return RedirectToAction("Index", "Customer");
        }
    }
}

using Microsoft.AspNetCore.Authentication.Cookies;
using SERVIGO.Web.DAL;
using SERVIGO.Web.Data;
using SERVIGO.Web.Helpers;
using SERVIGO.Web.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "SERVIGO.Auth";
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Auto-creates the SQLite database file and schema on first run — no SSMS,
// no separate database server, and no Visual Studio required.
var connectionString = app.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=App_Data/servigo.db";
Db.Initialize(connectionString);
SeedDefaultAdmin();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();

static void SeedDefaultAdmin()
{
    if (UserDAL.AdminExists()) return;

    var admin = new AdminUser
    {
        UserID       = "SRV-00001",
        FullName     = "System Administrator",
        Email        = "admin@servigo.com",
        Phone        = "03001234567",
        CNIC         = "1234567890123",
        PasswordHash = PasswordHelper.Hash("Admin@123"),
        RoleID       = 1,
        IsActive     = true,
        CreatedAt    = DateTime.Now
    };

    UserDAL.CreateUser(admin);
}

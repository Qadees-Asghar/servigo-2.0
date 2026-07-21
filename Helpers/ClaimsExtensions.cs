using System.Security.Claims;

namespace SERVIGO.Web.Helpers
{
    public static class ClaimsExtensions
    {
        public static string GetUserID(this ClaimsPrincipal user)
            => user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        public static string GetFullName(this ClaimsPrincipal user)
            => user.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        public static int GetRoleID(this ClaimsPrincipal user)
            => int.TryParse(user.FindFirstValue("RoleID"), out var id) ? id : 0;

        public static int? GetProviderID(this ClaimsPrincipal user)
            => int.TryParse(user.FindFirstValue("ProviderID"), out var id) ? id : null;
    }
}
